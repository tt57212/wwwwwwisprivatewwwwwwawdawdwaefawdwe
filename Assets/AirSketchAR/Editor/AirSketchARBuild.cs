using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AirSketchARBuild
{
    [MenuItem("AirSketch AR/Export iPhone Project")]
    public static void ExportXcodeProject()
    {
        string outputPath = Environment.GetEnvironmentVariable("AIRSKETCH_IOS_OUTPUT");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "..",
                "New folder (2)",
                "AirSketchAR-iOS"));
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("Es sind keine Build-Szenen aktiviert.");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(
                $"Der iPhone-Export ist fehlgeschlagen: {report.summary.result}.");

        Debug.Log($"iPhone-Xcode-Projekt exportiert nach: {outputPath}");
    }
}
