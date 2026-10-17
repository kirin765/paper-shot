using UnityEngine;

namespace PaperShot
{
    /// <summary>
    /// Boots the game automatically when any scene loads, so no scene setup is
    /// required. Disable by deleting this file / removing the attribute.
    /// </summary>
    public static class PaperShotBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindAnyObjectByType<PaperShotGame>() != null) return;

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("[PaperShot] No camera found; skipping boot.");
                return;
            }

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.PaperDark;

            PaperShotGame.Create(cam, Random.Range(1, 999999), true);
        }
    }
}
