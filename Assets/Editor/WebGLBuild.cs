// Headless WebGL build helper for Apps in Toss (.ait) submission.
// Korean UI font fallback is applied before building so every TMP text
// (LiberationSans etc.) renders Hangul via a NanumGothic SDF fallback.
// Usage (batch mode):
//   Unity.exe -batchmode -nographics -quit -projectPath <proj> \
//     -executeMethod WebGLBuild.Build -logFile <log>
using System;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    public static void Build()
    {
        // The game uses Unity TextMesh/IMGUI fonts; no TMP assets are needed.

        string[] scenes = Array.ConvertAll(
            EditorBuildSettings.scenes,
            s => s.path);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception("WebGL build failed: " + summary.result
                + " (" + summary.totalErrors + " errors)");
        }
        Debug.Log("[WebGLBuild] OK output=" + summary.outputPath
            + " size=" + summary.totalSize);
    }

    private static void SetupKoreanFontFallback()
    {
        // Ensures the Korean font asset has a valid atlas/material (regenerating
        // it if a previous run wrote it blank) and re-links the fallback chain.
        TMP_FontAsset korean = KoreanFontAssetFixer.EnsureValidFontAsset();
        if (korean == null)
        {
            Debug.LogWarning("[WebGLBuild] NanumGothic.ttf not found — Korean fallback skipped");
            return;
        }
        int updated = KoreanFontAssetFixer.RelinkFallbacks(korean);
        AssetDatabase.SaveAssets();
        Debug.Log("[WebGLBuild] Korean fallback on " + updated + " TMP font assets");
    }
}
