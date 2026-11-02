using System.Globalization;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class movement_player : NetworkBehaviour
{
    public enum ControlMode { Auto, PC, Mobile }

    // Der Spieler, der diesem Rechner gehört (für Shop/UI-Skripte)
    public static movement_player Local { get; private set; }

    [Header("Schalter")]
    public bool controlsEnabled = true;
    public bool lookEnabled = true;
    public bool moveEnabled = true;
    public bool jumpEnabled = true;
    public ControlMode controlMode = ControlMode.Auto;

    [Header("PC")]
    public bool requireRightMouse = false;
    public bool lockCursor = true;
    public float mouseSensitivity = 0.15f;

    [Header("Handy")]
    public float touchSensitivity = 0.15f;
    public float joystickRadius = 150f;

    [Header("Joystick UI (optional)")]
    public RectTransform joystickBase;
    public RectTransform joystickKnob;

    [Header("Allgemein")]
    public float moveSpeed = 5f;
    public Transform cameraTransform;

    [Header("Springen")]
    public float jumpHeight = 1.5f;
    public float groundCheckDistance = 0.15f;
    public LayerMask groundMask = ~0;

    [Header("Einstellungen (ESC)")]
    public GameObject settingsPanel;

    private Rigidbody rb;
    private Collider col;

    private bool separateCamera;
    private bool jumpRequested;

    private float rotationX;
    private float rotationY;

    private Vector2 moveInput;

    private int moveTouchId = -1;
    private int lookTouchId = -1;

    private Vector2 moveStartPos;

    private bool uiOpen = false;
    private bool settingsOpen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.isKinematic = false;

        if (cameraTransform == null)
        {
            foreach (Camera cam in GetComponentsInChildren<Camera>(true))
            {
                if (cam.transform != transform)
                {
                    cameraTransform = cam.transform;
                    break;
                }
            }
        }

        separateCamera =
            cameraTransform != null &&
            cameraTransform != transform;

        rb.interpolation =
            separateCamera
            ? RigidbodyInterpolation.Interpolate
            : RigidbodyInterpolation.None;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // =========================================================
    // NETZWERK
    // =========================================================

    // Läuft ein Netzwerk-Spiel (Host/Server/Client)?
    private bool NetworkActive =>
        NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsListening;

    // Singleplayer oder Objekt ohne NetworkObject: immer steuerbar.
    // Multiplayer mit NetworkObject: nur der Owner eines gespawnten Spielers.
    // HasNetworkObject ist bereits in NetworkBehaviour vorhanden.
    private bool HasControl =>
        !NetworkActive || !HasNetworkObject || (IsSpawned && IsOwner);

    void Start()
    {
        // Kein Netzwerk aktiv ODER kein NetworkObject (z.B. Lobby-Szene)
        // -> normale lokale Steuerung, kein Warten auf OnNetworkSpawn.
        if (!NetworkActive || !HasNetworkObject)
            InitLocalPlayer();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Fremde Spieler: keine Eingaben, keine Kamera, kein Ton.
            // Die Position kommt vom NetworkTransform (Authority Mode: Owner).
            enabled = false;
            rb.isKinematic = true; // Position kommt vom Netzwerk

            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null) cam.enabled = false;

            AudioListener al = GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;

            return;
        }

        InitLocalPlayer();
    }

    private bool initialized;

    private void InitLocalPlayer()
    {
        if (initialized) return;
        initialized = true;

        Local = this;

        // Kamera explizit aktivieren (falls im Prefab ausgeschaltet).
        // Dabei auch alle Eltern-Objekte der Kamera aktivieren, falls
        // diese ebenfalls deaktiviert sind (sonst bleibt die Kamera
        // trotz eigenem SetActive(true) unsichtbar).
        Camera myCam = GetComponentInChildren<Camera>(true);
        if (myCam != null)
        {
            Transform t = myCam.transform;

            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);

                t = t.parent;
            }

            myCam.enabled = true;
            myCam.tag = "MainCamera";
        }
        else
        {
            Debug.LogWarning("Keine Kamera im Player-Prefab gefunden!", this);
        }

        AudioListener myAl = GetComponentInChildren<AudioListener>(true);
        if (myAl != null) myAl.enabled = true;

        rotationY = transform.eulerAngles.y;

        float x = separateCamera
            ? cameraTransform.localEulerAngles.x
            : transform.eulerAngles.x;

        rotationX = x > 180f ? x - 360f : x;

        if (joystickBase != null)
            joystickBase.gameObject.SetActive(false);

        if (!UseMobile() && lockCursor)
            SetCursorLock(true);
    }

    public override void OnNetworkDespawn()
    {
        if (Local == this)
            Local = null;
    }

    // =========================================================
    // SPEED UPGRADE
    // =========================================================

    public void AddSpeed(float amount)
    {
        moveSpeed += amount;
    }

    public float GetSpeed()
    {
        return moveSpeed;
    }

    // =========================================================
    // SHOP / UI
    // =========================================================

    public void SetUIOpen(bool open)
    {
        uiOpen = open;

        if (open)
        {
            moveInput = Vector2.zero;
            jumpRequested = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (joystickBase != null)
                joystickBase.gameObject.SetActive(false);
        }
        else
        {
            if (!UseMobile() && lockCursor)
                SetCursorLock(true);

            Cursor.visible = false;
        }
    }

    // =========================================================
    // EINSTELLUNGEN (ESC)
    // =========================================================

    private void ToggleSettings()
    {
        if (settingsPanel == null)
            return;

        if (!settingsOpen)
            OpenSettings();
        else
            CloseSettings();
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
            return;

        settingsOpen = true;
        settingsPanel.SetActive(true);

        SetUIOpen(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel == null)
            return;

        settingsOpen = false;
        settingsPanel.SetActive(false);

        SetUIOpen(false);
    }

    // =========================================================
    // CURSOR
    // =========================================================

    void SetCursorLock(bool locked)
    {
        Cursor.lockState =
            locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }

    // =========================================================
    // MOBILE
    // =========================================================

    bool UseMobile()
    {
        if (controlMode == ControlMode.Mobile)
            return true;

        if (controlMode == ControlMode.PC)
            return false;

        // Automatik: Application.isMobilePlatform ist in WebGL-Builds
        // unzuverlässig (erkennt Desktop- UND Mobile-Browser oft falsch).
        // Stattdessen direkt prüfen, ob ein Touchscreen vorhanden UND
        // aktuell auch tatsächlich der primäre Eingabeweg ist.
        bool hasTouch = Touchscreen.current != null;
        bool hasMouse = Mouse.current != null;

        // Hat das Gerät einen Touchscreen, aber keine echte Maus
        // (typisch für Handys/Tablets) -> Mobile-Steuerung verwenden.
        if (hasTouch && !hasMouse)
            return true;

        // Beides vorhanden (z.B. Touch-Laptop) oder nur Maus ->
        // Standardmäßig PC-Steuerung, außer die Plattform ist
        // eindeutig mobil.
        return Application.isMobilePlatform && hasTouch;
    }

    // =========================================================
    // JUMP
    // =========================================================

    public void Jump()
    {
        if (!HasControl)
            return;

        if (uiOpen)
            return;

        if (jumpEnabled && controlsEnabled)
            jumpRequested = true;
    }

    // =========================================================
    // GROUND CHECK
    // =========================================================

    bool IsGrounded()
    {
        if (col == null)
            return true;

        Bounds b = col.bounds;

        float dist =
            b.extents.y +
            groundCheckDistance;

        return Physics.Raycast(
            b.center,
            Vector3.down,
            dist,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (!HasControl)
            return;

        // ESC öffnet/schließt die Einstellungen, auch wenn
        // sonst gerade ein anderes UI (Shop etc.) geöffnet ist,
        // solange dieses nicht gerade selbst offen ist.
        if (settingsPanel != null &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame &&
            (!uiOpen || settingsOpen))
        {
            ToggleSettings();
        }

        if (uiOpen)
        {
            moveInput = Vector2.zero;
            jumpRequested = false;
            return;
        }

        if (!controlsEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 look = Vector2.zero;
        Vector2 move = Vector2.zero;

        if (UseMobile())
            ReadMobile(ref look, ref move);
        else
            ReadPC(ref look, ref move);

        moveInput =
            moveEnabled
            ? move
            : Vector2.zero;

        if (lookEnabled)
        {
            rotationY += look.x;

            rotationX =
                Mathf.Clamp(
                    rotationX - look.y,
                    -90f,
                    90f
                );

            if (separateCamera)
            {
                cameraTransform.localRotation =
                    Quaternion.Euler(
                        rotationX,
                        0f,
                        0f
                    );
            }
            else
            {
                transform.rotation =
                    Quaternion.Euler(
                        rotationX,
                        rotationY,
                        0f
                    );
            }
        }
    }

    // =========================================================
    // FIXED UPDATE
    // =========================================================

    void FixedUpdate()
    {
        if (!HasControl)
            return;

        if (uiOpen)
        {
            Vector3 stoppedVelocity = rb.linearVelocity;
            stoppedVelocity.x = 0f;
            stoppedVelocity.z = 0f;

            rb.linearVelocity = stoppedVelocity;

            jumpRequested = false;
            return;
        }

        if (!controlsEnabled)
        {
            jumpRequested = false;
            return;
        }

        if (lookEnabled && separateCamera)
        {
            rb.MoveRotation(
                Quaternion.Euler(
                    0f,
                    rotationY,
                    0f
                )
            );
        }

        Quaternion yaw =
            Quaternion.Euler(
                0f,
                rotationY,
                0f
            );

        Vector3 vel =
            yaw *
            new Vector3(
                moveInput.x,
                0f,
                moveInput.y
            ) *
            moveSpeed;

        vel.y = rb.linearVelocity.y;

        if (jumpRequested)
        {
            if (IsGrounded())
            {
                vel.y =
                    Mathf.Sqrt(
                        2f *
                        Mathf.Abs(Physics.gravity.y) *
                        jumpHeight
                    );
            }

            jumpRequested = false;
        }

        rb.linearVelocity = vel;
    }

    // =========================================================
    // PC INPUT
    // =========================================================

    void ReadPC(ref Vector2 look, ref Vector2 move)
    {
#if ENABLE_LEGACY_INPUT_MANAGER && UNITY_WEBGL && !UNITY_EDITOR
        // Altes Input-System für WebGL-Builds (nur wenn es in den
        // Project Settings aktiv ist: Old oder Both)
        if (lockCursor)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                SetCursorLock(false);
            else if (
                Cursor.lockState != CursorLockMode.Locked &&
                Input.GetMouseButtonDown(0)
            )
                // WebGL: Pointer-Lock muss aus einem echten Klick-Event
                // heraus angefordert werden (Browser-Sicherheitsregel).
                SetCursorLock(true);
        }

        bool cursorOk =
            !lockCursor ||
            Cursor.lockState == CursorLockMode.Locked;

        bool canLook =
            cursorOk &&
            (!requireRightMouse || Input.GetMouseButton(1));

        if (canLook)
        {
            look.x = Input.GetAxis("Mouse X") * mouseSensitivity * 10f;
            look.y = Input.GetAxis("Mouse Y") * mouseSensitivity * 10f;
        }

        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");
        move = Vector2.ClampMagnitude(move, 1f);

        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
#else
        if (Mouse.current != null)
        {
            if (lockCursor)
            {
                if (
                    Cursor.lockState != CursorLockMode.Locked &&
                    Mouse.current.leftButton.wasPressedThisFrame
                )
                {
                    // WebGL: jeder Klick versucht erneut zu locken,
                    // da der Browser den ersten Request oft ablehnt
                    // (z.B. direkt nach Escape oder Szenenwechsel).
                    SetCursorLock(true);
                }
            }

            bool cursorOk =
                !lockCursor ||
                Cursor.lockState == CursorLockMode.Locked;

            bool canLook =
                cursorOk &&
                (
                    !requireRightMouse ||
                    Mouse.current.rightButton.isPressed
                );

            if (canLook)
            {
                look =
                    Mouse.current.delta.ReadValue() *
                    mouseSensitivity;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                move.y += 1;

            if (Keyboard.current.sKey.isPressed)
                move.y -= 1;

            if (Keyboard.current.dKey.isPressed)
                move.x += 1;

            if (Keyboard.current.aKey.isPressed)
                move.x -= 1;

            move =
                Vector2.ClampMagnitude(
                    move,
                    1f
                );

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                Jump();
        }
#endif
    }

    // =========================================================
    // MOBILE INPUT
    // =========================================================

    void ReadMobile(ref Vector2 look, ref Vector2 move)
    {
        if (Touchscreen.current == null)
            return;

        foreach (var t in Touchscreen.current.touches)
        {
            int id = t.touchId.ReadValue();

            if (!t.press.isPressed)
            {
                if (id == moveTouchId)
                {
                    moveTouchId = -1;

                    if (joystickBase != null)
                        joystickBase.gameObject.SetActive(false);
                }

                if (id == lookTouchId)
                    lookTouchId = -1;

                continue;
            }

            Vector2 pos = t.position.ReadValue();

            if (t.press.wasPressedThisFrame)
            {
                if (
                    pos.x < Screen.width * 0.5f &&
                    moveTouchId == -1
                )
                {
                    moveTouchId = id;
                    moveStartPos = pos;

                    if (joystickBase != null)
                    {
                        joystickBase.gameObject.SetActive(true);
                        joystickBase.position = pos;

                        if (joystickKnob != null)
                            joystickKnob.anchoredPosition =
                                Vector2.zero;
                    }
                }
                else if (
                    pos.x >= Screen.width * 0.5f &&
                    lookTouchId == -1
                )
                {
                    lookTouchId = id;
                }
            }

            if (id == moveTouchId)
            {
                move =
                    Vector2.ClampMagnitude(
                        (pos - moveStartPos) /
                        joystickRadius,
                        1f
                    );

                if (joystickKnob != null)
                {
                    joystickKnob.anchoredPosition =
                        move * joystickRadius;
                }
            }

            if (id == lookTouchId)
            {
                look =
                    t.delta.ReadValue() *
                    touchSensitivity;
            }
        }
    }
}