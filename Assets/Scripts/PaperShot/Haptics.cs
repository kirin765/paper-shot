using UnityEngine;

namespace PaperShot
{
    /// <summary>Opt-in, platform-guarded vibration. No-op when disabled.</summary>
    public static class Haptics
    {
        public static void Buzz()
        {
            if (!SaveData.Haptics) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
