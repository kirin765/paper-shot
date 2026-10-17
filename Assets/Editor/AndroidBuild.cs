// Headless Android build helper for Google Play (signed release .aab).
//
// Usage (batch mode):
//   <Unity> -batchmode -nographics -quit -projectPath <proj> \
//     -executeMethod AndroidBuild.Build -logFile <log>
//
// Signing credentials are read from <projectRoot>/android-keystore.properties
// (git-ignored) or from env vars (PAPERSHOT_KEYSTORE_* ). Never commit secrets.
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuild
{
    const string OutputPath = "Builds/Android/PaperShot.aab";
    const string KeystoreProps = "android-keystore.properties";

    public static void Build()
    {
        ApplyKoreanFontFallback();
        ApplyPlayerSettings();
        ApplySigning();

        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        string[] scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path);
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception("Android build failed: " + summary.result
                + " (" + summary.totalErrors + " errors)");
        }
        Debug.Log("[AndroidBuild] OK output=" + summary.outputPath + " size=" + summary.totalSize);
    }

    private static void ApplyPlayerSettings()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.DefaultCompany.PaperShot");
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        Debug.Log("[AndroidBuild] bundleId=com.DefaultCompany.PaperShot v1.0 (code 1), ARM64, IL2CPP, portrait");
    }

    private static void ApplySigning()
    {
        Dictionary<string, string> props = new Dictionary<string, string>();

        string file = Path.Combine(Directory.GetCurrentDirectory(), KeystoreProps);
        if (File.Exists(file))
        {
            foreach (var raw in File.ReadAllLines(file))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                props[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
        }

        string Get(string key, string env)
        {
            if (props.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)) return v;
            return Environment.GetEnvironmentVariable(env);
        }

        string store = Get("storeFile", "PAPERSHOT_KEYSTORE_FILE");
        string storePass = Get("storePassword", "PAPERSHOT_KEYSTORE_PASS");
        string alias = Get("keyAlias", "PAPERSHOT_KEY_ALIAS");
        string aliasPass = Get("keyPassword", "PAPERSHOT_KEY_PASS");

        if (string.IsNullOrEmpty(store) || string.IsNullOrEmpty(storePass))
        {
            Debug.LogWarning("[AndroidBuild] keystore not configured (" + KeystoreProps
                + " missing) — building with the debug keystore.");
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = store;
        PlayerSettings.Android.keystorePass = storePass;
        PlayerSettings.Android.keyaliasName = string.IsNullOrEmpty(alias) ? "upload" : alias;
        PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(aliasPass) ? storePass : aliasPass;
        Debug.Log("[AndroidBuild] signing with " + store + " alias=" + PlayerSettings.Android.keyaliasName);
    }

    private static void ApplyKoreanFontFallback()
    {
        TMP_FontAsset korean = KoreanFontAssetFixer.EnsureValidFontAsset();
        if (korean == null)
        {
            Debug.LogWarning("[AndroidBuild] NanumGothic.ttf not found — Korean fallback skipped");
            return;
        }
        int updated = KoreanFontAssetFixer.RelinkFallbacks(korean);
        AssetDatabase.SaveAssets();
        Debug.Log("[AndroidBuild] Korean fallback on " + updated + " TMP font assets");
    }
}
