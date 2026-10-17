// One-click repair for the Korean fallback font used by the UI.
//
// Background: Assets/Fonts/NanumGothic SDF.asset can be saved without its
// material and atlas-texture sub-assets, so TMP throws
// UnassignedReferenceException ("m_AtlasTextures ... has not been assigned")
// whenever a Hangul glyph is requested and all Korean text renders blank.
//
// This regenerates the font asset (including its sub-assets) and re-links it as
// a fallback on every TMP font asset + TMP Settings.
// Run: Tools > PaperShot > Fix Korean Font (NanumGothic SDF)
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class KoreanFontAssetFixer
{
    public const string KoreanTtfPath = "Assets/Fonts/NanumGothic.ttf";
    public const string KoreanFontAssetPath = "Assets/Fonts/NanumGothic SDF.asset";

    [MenuItem("Tools/PaperShot/Fix Korean Font (NanumGothic SDF)")]
    public static void Fix()
    {
        TMP_FontAsset korean = EnsureValidFontAsset();
        if (korean == null) return;

        int linked = RelinkFallbacks(korean);
        AssetDatabase.SaveAssets();
        Debug.Log("[KoreanFont] Done. Fallback linked on " + linked + " TMP asset(s).");
    }

    /// <summary>
    /// Loads the Korean font asset, regenerating it first if its material or
    /// atlas texture is missing. Returns null when the source TTF is absent.
    /// </summary>
    public static TMP_FontAsset EnsureValidFontAsset()
    {
        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(KoreanTtfPath);
        if (ttf == null)
        {
            Debug.LogError("[KoreanFont] Korean TTF not found: " + KoreanTtfPath);
            return null;
        }

        TMP_FontAsset korean = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (!IsBroken(korean))
        {
            Debug.Log("[KoreanFont] Font asset is valid; no regeneration needed.");
            return korean;
        }

        if (korean != null)
            AssetDatabase.DeleteAsset(KoreanFontAssetPath);

        EnsureTmpSettings();

        korean = TMP_FontAsset.CreateFontAsset(ttf);
        korean.name = "NanumGothic SDF";
        AssetDatabase.CreateAsset(korean, KoreanFontAssetPath);
        SaveSubAssets(korean);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(KoreanFontAssetPath, ImportAssetOptions.ForceUpdate);

        Debug.Log("[KoreanFont] Regenerated " + KoreanFontAssetPath + " (with atlas + material).");
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
    }

    /// <summary>
    /// Persists the material and atlas as sub-assets of the font asset. Without
    /// this the asset is written blank and TMP fails at runtime.
    /// </summary>
    public static void SaveSubAssets(TMP_FontAsset fa)
    {
        if (fa.material != null)
        {
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }
        if (fa.atlasTextures == null) return;
        for (int i = 0; i < fa.atlasTextures.Length; i++)
        {
            Texture2D tex = fa.atlasTextures[i];
            if (tex == null) continue;
            tex.name = fa.name + (i == 0 ? " Atlas" : " Atlas " + i);
            AssetDatabase.AddObjectToAsset(tex, fa);
        }
    }

    /// <summary>Adds the Korean font as a fallback on every TMP font asset + TMP Settings.</summary>
    public static int RelinkFallbacks(TMP_FontAsset korean)
    {
        if (korean == null) return 0;
        int updated = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == KoreanFontAssetPath) continue;
            TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa == null) continue;

            List<TMP_FontAsset> table = fa.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
            bool changed = table.RemoveAll(x => x == null) > 0;
            if (!table.Contains(korean))
            {
                table.Add(korean);
                changed = true;
            }
            if (!changed) continue;

            fa.fallbackFontAssetTable = table;
            EditorUtility.SetDirty(fa);
            updated++;
        }

        if (TMP_Settings.instance != null)
        {
            List<TMP_FontAsset> global = TMP_Settings.fallbackFontAssets
                ?? new List<TMP_FontAsset>();
            global.RemoveAll(x => x == null);
            if (!global.Contains(korean))
            {
                global.Add(korean);
                TMP_Settings.fallbackFontAssets = global;
                EditorUtility.SetDirty(TMP_Settings.instance);
            }
        }

        return updated;
    }

    private static bool IsBroken(TMP_FontAsset fa)
    {
        if (fa == null) return true;
        if (fa.material == null) return true;
        if (fa.atlasTextures == null || fa.atlasTextures.Length == 0) return true;
        for (int i = 0; i < fa.atlasTextures.Length; i++)
            if (fa.atlasTextures[i] != null) return false;
        return true;
    }

    private static void EnsureTmpSettings()
    {
        if (TMP_Settings.instance != null) return;
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            AssetDatabase.CreateFolder("Assets", "TextMesh Pro");
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro/Resources"))
            AssetDatabase.CreateFolder("Assets/TextMesh Pro", "Resources");
        var settings = ScriptableObject.CreateInstance<TMP_Settings>();
        AssetDatabase.CreateAsset(settings, "Assets/TextMesh Pro/Resources/TMP Settings.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("[KoreanFont] Created TMP Settings asset.");
    }
}
