#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public static class AirSketchARProjectSetup
{
    private const string ScenePath = "Assets/AirSketchAR/AirSketchAR.unity";
    private const string XRSettingsPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
    private const string ARKitLoaderType = "UnityEngine.XR.ARKit.ARKitLoader";

    [MenuItem("AirSketch AR/Setup iPhone AR")]
    public static void Setup()
    {
        ConfigureARKit();
        CreateScene();
        AddSceneToBuildSettings();
        SetCameraPermission();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "AirSketch AR eingerichtet",
                "Die AR-Zeichenszene ist erstellt und steht an erster Stelle der Build-Szenen.\n\n" +
                "Für den iPhone-Build: iOS Build Support installieren, iOS als Build-Plattform wählen " +
                "und das Projekt auf einem Mac mit Xcode bauen.",
                "OK");
        }
    }

    private static void ConfigureARKit()
    {
        string directory = Path.GetDirectoryName(XRSettingsPath);
        if (string.IsNullOrEmpty(directory))
            throw new InvalidOperationException("Der Ordner für XR-Einstellungen ist ungültig.");

        Directory.CreateDirectory(directory);
        AssetDatabase.Refresh();

        XRGeneralSettingsPerBuildTarget perTarget =
            AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XRSettingsPath);
        if (perTarget == null)
        {
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, XRSettingsPath);
        }

        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);

        XRGeneralSettings generalSettings = perTarget.SettingsForBuildTarget(BuildTargetGroup.iOS);
        if (generalSettings == null)
        {
            generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
            generalSettings.name = "iOS XR Settings";
            perTarget.SetSettingsForBuildTarget(BuildTargetGroup.iOS, generalSettings);
            AssetDatabase.AddObjectToAsset(generalSettings, perTarget);
        }

        XRManagerSettings manager = generalSettings.Manager;
        if (manager == null)
        {
            manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = "iOS XR Providers";
            generalSettings.Manager = manager;
            AssetDatabase.AddObjectToAsset(manager, perTarget);
        }

        manager.automaticLoading = true;
        manager.automaticRunning = true;

        if (!XRPackageMetadataStore.AssignLoader(manager, ARKitLoaderType, BuildTargetGroup.iOS))
            throw new InvalidOperationException(
                "Der ARKit-Provider konnte nicht aktiviert werden. Prüfe, ob das ARKit-Paket installiert ist.");

        EditorUtility.SetDirty(perTarget);
        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(manager);
    }

    private static void CreateScene()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/AirSketchAR");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject app = new GameObject("AirSketch AR");
        app.AddComponent<AirSketchARController>();
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        EditorBuildSettingsScene arScene = existing.FirstOrDefault(scene => scene.path == ScenePath);

        if (arScene == null)
            arScene = new EditorBuildSettingsScene(ScenePath, true);
        else
            arScene.enabled = true;

        EditorBuildSettings.scenes = new[] { arScene }
            .Concat(existing.Where(scene => scene.path != ScenePath))
            .ToArray();
    }

    private static void SetCameraPermission()
    {
        PlayerSettings.iOS.cameraUsageDescription =
            "AirSketch AR benötigt die Kamera, um den Raum zu erfassen und deine 3D-Zeichnung anzuzeigen.";
        PlayerSettings.iOS.targetOSVersionString = "13.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.example.airsketchar");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
    }
}
#endif
