using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class AirSketchARController : MonoBehaviour
{
    private static readonly Color[] Palette =
    {
        Color.white,
        new Color(0.20f, 0.86f, 1.00f),
        new Color(1.00f, 0.28f, 0.58f),
        new Color(1.00f, 0.64f, 0.16f),
        new Color(0.29f, 0.91f, 0.55f),
        new Color(0.68f, 0.47f, 1.00f)
    };

    private readonly List<Stroke> strokes = new List<Stroke>();
    private Camera arCamera;
    private Text statusText;
    private Text distanceText;
    private Text widthText;
    private Text errorText;
    private Outline[] colorOutlines;
    private Sprite circleSprite;
    private Material lineMaterial;
    private Color selectedColor = Palette[1];
    private int selectedColorIndex = 1;
    private float drawDistance = 0.8f;
    private float brushWidth = 0.012f;
    private float statusRefreshAt;

    private sealed class Stroke
    {
        public GameObject root;
        public LineRenderer line;
        public Material material;
        public Vector3 lastPoint;
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        CreateARCamera();
        CreateInterface();
    }

    private void Update()
    {
        if (Time.unscaledTime < statusRefreshAt)
            return;

        statusRefreshAt = Time.unscaledTime + 0.2f;
        RefreshTrackingStatus();
    }

    private void OnDestroy()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        if (circleSprite != null)
        {
            Destroy(circleSprite.texture);
            Destroy(circleSprite);
        }

        if (lineMaterial != null)
            Destroy(lineMaterial);
    }

    private void CreateARCamera()
    {
        GameObject sessionObject = new GameObject("AR Session");
        sessionObject.AddComponent<ARSession>();

        GameObject originObject = new GameObject("XR Origin");
        XROrigin origin = originObject.AddComponent<XROrigin>();

        GameObject cameraObject = new GameObject(
            "AR Camera",
            typeof(Camera),
            typeof(ARCameraManager),
            typeof(ARCameraBackground));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(origin.transform, false);
        arCamera = cameraObject.GetComponent<Camera>();
        arCamera.clearFlags = CameraClearFlags.SolidColor;
        arCamera.backgroundColor = Color.black;
        origin.Camera = arCamera;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader != null)
            lineMaterial = new Material(shader) { color = Color.white };
    }

    private void CreateInterface()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

        GameObject canvasObject = new GameObject(
            "AirSketch AR Interface",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390f, 844f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = CreateRect("Safe Area", canvasObject.transform);
        ApplySafeArea(root);

        RectTransform drawingSurface = CreateRect("Drawing Surface", canvasObject.transform);
        Stretch(drawingSurface);
        Image surfaceImage = drawingSurface.gameObject.AddComponent<Image>();
        surfaceImage.color = new Color(1f, 1f, 1f, 0f);
        surfaceImage.raycastTarget = true;
        AirSketchDrawingSurface surface = drawingSurface.gameObject.AddComponent<AirSketchDrawingSurface>();
        surface.Controller = this;
        drawingSurface.SetAsFirstSibling();

        BuildHeader(root);
        BuildToolbar(root);
    }

    private void BuildHeader(RectTransform parent)
    {
        RectTransform header = CreatePanel(parent, "Tracking Status");
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -12f);
        header.sizeDelta = new Vector2(-24f, 58f);

        HorizontalLayoutGroup layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 8, 8);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        GameObject dot = new GameObject("Tracking Indicator", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        dot.transform.SetParent(header, false);
        dot.GetComponent<Image>().color = Palette[4];
        dot.GetComponent<LayoutElement>().preferredWidth = 12f;

        statusText = CreateText(header, "Raum wird erfasst …", 15, FontStyle.Bold, TextAnchor.MiddleLeft);
        statusText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void BuildToolbar(RectTransform parent)
    {
        RectTransform toolbar = CreatePanel(parent, "Zeichenwerkzeuge");
        toolbar.anchorMin = Vector2.zero;
        toolbar.anchorMax = new Vector2(1f, 0f);
        toolbar.pivot = Vector2.zero;
        toolbar.anchoredPosition = new Vector2(0f, 12f);
        toolbar.sizeDelta = new Vector2(-24f, 310f);

        VerticalLayoutGroup layout = toolbar.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 7f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(toolbar, "FARBE", 11, FontStyle.Bold, TextAnchor.MiddleLeft)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        BuildPalette(toolbar);
        BuildValueRow(toolbar, "ABSTAND", 54f, () => AdjustDistance(-0.1f), () => AdjustDistance(0.1f), out distanceText);
        BuildValueRow(toolbar, "STRICH", 54f, () => AdjustWidth(-0.004f), () => AdjustWidth(0.004f), out widthText);
        BuildActions(toolbar);
        errorText = CreateText(toolbar, string.Empty, 12, FontStyle.Normal, TextAnchor.MiddleCenter);
        errorText.color = new Color(1f, 0.65f, 0.35f);
        errorText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        RefreshValues();
    }

    private void BuildPalette(RectTransform parent)
    {
        RectTransform row = CreateRect("Farben", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        circleSprite = CreateCircleSprite();
        colorOutlines = new Outline[Palette.Length];
        for (int i = 0; i < Palette.Length; i++)
        {
            int colorIndex = i;
            GameObject buttonObject = new GameObject(
                "Farbe " + (i + 1),
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(Outline));
            buttonObject.transform.SetParent(row, false);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = circleSprite;
            image.color = Palette[i];
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectColor(colorIndex));
            buttonObject.GetComponent<LayoutElement>().preferredWidth = 40f;
            Outline outline = buttonObject.GetComponent<Outline>();
            colorOutlines[i] = outline;
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.enabled = i == selectedColorIndex;
        }
    }

    private void BuildValueRow(
        RectTransform parent,
        string title,
        float height,
        Action decrease,
        Action increase,
        out Text valueText)
    {
        RectTransform row = CreateRect(title, parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateText(row, title, 11, FontStyle.Bold, TextAnchor.MiddleLeft)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 66f;
        CreateActionButton(row, "−", decrease, 44f);
        valueText = CreateText(row, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 68f;
        CreateActionButton(row, "+", increase, 44f);
    }

    private void BuildActions(RectTransform parent)
    {
        RectTransform row = CreateRect("Aktionen", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateActionButton(row, "Letzten Strich zurück", Undo, 0f);
        CreateActionButton(row, "Alles löschen", Clear, 0f);
    }

    private Button CreateActionButton(Transform parent, string label, Action action, float width)
    {
        GameObject buttonObject = new GameObject(
            label,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.10f, 0.23f, 0.34f, 0.95f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());

        LayoutElement element = buttonObject.GetComponent<LayoutElement>();
        if (width > 0f)
            element.preferredWidth = width;

        Text text = CreateText(buttonObject.transform, label, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return button;
    }

    private RectTransform CreatePanel(Transform parent, string name)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.025f, 0.055f, 0.095f, 0.91f);
        return rect;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private Text CreateText(Transform parent, string value, int fontSize, FontStyle style, TextAnchor alignment)
    {
        GameObject gameObject = new GameObject(value, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        Text text = gameObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void SelectColor(int index)
    {
        selectedColorIndex = index;
        selectedColor = Palette[index];
        for (int i = 0; i < colorOutlines.Length; i++)
            colorOutlines[i].enabled = i == index;
    }

    private void AdjustDistance(float amount)
    {
        drawDistance = Mathf.Clamp(drawDistance + amount, 0.3f, 2.0f);
        RefreshValues();
    }

    private void AdjustWidth(float amount)
    {
        brushWidth = Mathf.Clamp(brushWidth + amount, 0.004f, 0.04f);
        RefreshValues();
    }

    private void RefreshValues()
    {
        if (distanceText != null)
            distanceText.text = drawDistance.ToString("0.0") + " m";
        if (widthText != null)
            widthText.text = Mathf.RoundToInt(brushWidth * 1000f) + " mm";
    }

    private void RefreshTrackingStatus()
    {
        if (statusText == null)
            return;

        switch (ARSession.state)
        {
            case ARSessionState.SessionTracking:
                statusText.text = "Raum erfasst · du kannst herumgehen";
                break;
            case ARSessionState.SessionInitializing:
                statusText.text = "Raum wird erfasst …";
                break;
            case ARSessionState.Unsupported:
                statusText.text = "ARKit auf diesem Gerät nicht verfügbar";
                break;
            case ARSessionState.NeedsInstall:
                statusText.text = "AR-Unterstützung muss installiert werden";
                break;
            case ARSessionState.None:
                statusText.text = "AR wird gestartet …";
                break;
            default:
                statusText.text = ARSession.state.ToString();
                break;
        }

        if (ARSession.state == ARSessionState.SessionTracking)
        {
            switch (ARSession.notTrackingReason)
            {
                case NotTrackingReason.ExcessiveMotion:
                    statusText.text = "Langsamer bewegen";
                    break;
                case NotTrackingReason.InsufficientFeatures:
                    statusText.text = "Mehr sichtbare Details nötig";
                    break;
                case NotTrackingReason.Relocalizing:
                    statusText.text = "Raum wird wiedergefunden …";
                    break;
            }
        }
    }

    private void SetError(string message)
    {
        if (errorText != null)
            errorText.text = message;
    }

    private void Undo()
    {
        if (strokes.Count == 0)
            return;

        Stroke stroke = strokes[strokes.Count - 1];
        strokes.RemoveAt(strokes.Count - 1);
        Destroy(stroke.root);
        Destroy(stroke.material);
    }

    private void Clear()
    {
        foreach (Stroke stroke in strokes)
        {
            Destroy(stroke.root);
            Destroy(stroke.material);
        }

        strokes.Clear();
        SetError(string.Empty);
    }

    internal void BeginStroke(Vector2 screenPosition)
    {
        if (arCamera == null || lineMaterial == null)
        {
            SetError("AR-Kamera oder Zeichenmaterial nicht verfügbar.");
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        Vector3 point = ray.origin + ray.direction * drawDistance;
        GameObject root = new GameObject("AR-Strich " + (strokes.Count + 1));
        LineRenderer line = root.AddComponent<LineRenderer>();
        Material material = new Material(lineMaterial) { color = selectedColor };
        line.useWorldSpace = true;
        line.sharedMaterial = material;
        line.startColor = selectedColor;
        line.endColor = selectedColor;
        line.startWidth = brushWidth;
        line.endWidth = brushWidth;
        line.numCapVertices = 6;
        line.numCornerVertices = 6;
        line.positionCount = 1;
        line.SetPosition(0, point);

        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "Strichanfang";
        dot.transform.SetParent(root.transform, true);
        dot.transform.position = point;
        dot.transform.localScale = Vector3.one * brushWidth;
        Collider collider = dot.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        dot.GetComponent<Renderer>().sharedMaterial = material;

        strokes.Add(new Stroke { root = root, line = line, material = material, lastPoint = point });
        SetError(string.Empty);
    }

    internal void ContinueStroke(Vector2 screenPosition)
    {
        if (strokes.Count == 0)
            return;

        Stroke stroke = strokes[strokes.Count - 1];
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        Vector3 point = ray.origin + ray.direction * drawDistance;
        if (Vector3.Distance(stroke.lastPoint, point) < 0.006f)
            return;

        stroke.line.positionCount++;
        stroke.line.SetPosition(stroke.line.positionCount - 1, point);
        stroke.lastPoint = point;
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "AirSketch Circle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(center.x + 0.5f - distance));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ApplySafeArea(RectTransform rect)
    {
        Rect safeArea = Screen.safeArea;
        rect.anchorMin = safeArea.position / new Vector2(Screen.width, Screen.height);
        rect.anchorMax = (safeArea.position + safeArea.size) / new Vector2(Screen.width, Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
