using UnityEngine;

namespace PaperShot
{
    /// <summary>
    /// PlayerPrefs-backed preferences and personal records. Every value has a
    /// safe default so a fresh install (or WebGL sandbox) just works.
    /// </summary>
    public static class SaveData
    {
        const string KeyMusic = "ps.music";
        const string KeySfx = "ps.sfx";
        const string KeyHaptics = "ps.haptics";
        const string KeyShake = "ps.shake";
        const string KeyReduced = "ps.reducedMotion";
        const string KeyTutorial = "ps.tutorialDone";
        const string KeyBestScore = "ps.bestScore";
        const string KeyBestRound = "ps.bestRound";

        public const float DefaultMusic = 0.35f;
        public const float DefaultSfx = 0.8f;

        public static float MusicVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, DefaultMusic));
            set { PlayerPrefs.SetFloat(KeyMusic, Mathf.Clamp01(value)); Save(); }
        }

        public static float SfxVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, DefaultSfx));
            set { PlayerPrefs.SetFloat(KeySfx, Mathf.Clamp01(value)); Save(); }
        }

        /// <summary>Opt-in vibration on misses/wrong shots (mobile only).</summary>
        public static bool Haptics
        {
            get => PlayerPrefs.GetInt(KeyHaptics, 0) == 1;
            set { PlayerPrefs.SetInt(KeyHaptics, value ? 1 : 0); Save(); }
        }

        /// <summary>Subtle camera shake on shots. Off overrides any value.</summary>
        public static bool ScreenShake
        {
            get => PlayerPrefs.GetInt(KeyShake, 1) == 1;
            set { PlayerPrefs.SetInt(KeyShake, value ? 1 : 0); Save(); }
        }

        /// <summary>Damps shake, paper kick and flashes for motion sensitivity.</summary>
        public static bool ReducedMotion
        {
            get => PlayerPrefs.GetInt(KeyReduced, 0) == 1;
            set { PlayerPrefs.SetInt(KeyReduced, value ? 1 : 0); Save(); }
        }

        /// <summary>Shake is allowed only when both shake is on and motion is not reduced.</summary>
        public static bool ShakeAllowed => ScreenShake && !ReducedMotion;

        public static bool TutorialDone
        {
            get => PlayerPrefs.GetInt(KeyTutorial, 0) == 1;
            set { PlayerPrefs.SetInt(KeyTutorial, value ? 1 : 0); Save(); }
        }

        public static int BestScore
        {
            get => PlayerPrefs.GetInt(KeyBestScore, 0);
            private set { PlayerPrefs.SetInt(KeyBestScore, value); }
        }

        public static int BestRound
        {
            get => PlayerPrefs.GetInt(KeyBestRound, 0);
            private set { PlayerPrefs.SetInt(KeyBestRound, value); }
        }

        /// <summary>Persists a finished run and reports whether a record was set.</summary>
        public static bool RecordGame(int score, int round)
        {
            bool newBest = score > BestScore || round > BestRound;
            if (score > BestScore) BestScore = score;
            if (round > BestRound) BestRound = round;
            Save();
            return newBest;
        }

        public static void Save() => PlayerPrefs.Save();

        public static void ResetRecords()
        {
            BestScore = 0;
            BestRound = 0;
            Save();
        }
    }
}
