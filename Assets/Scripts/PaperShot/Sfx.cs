using System;
using UnityEngine;

namespace PaperShot
{
    public enum SfxKind { Shot, Hit, Wrong, Miss, RoundClear, GameOver, Tick, Start }

    /// <summary>
    /// Generates short sound effects at runtime with plain math, so the game
    /// ships without any imported audio. Deterministic and side-effect free.
    /// </summary>
    public static class SfxSynth
    {
        public const int Rate = 44100;
        const float Tau = Mathf.PI * 2f;

        public static AudioClip Create(SfxKind kind)
        {
            float[] data = Sample(kind);
            var clip = AudioClip.Create("sfx_" + kind.ToString().ToLowerInvariant(),
                data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] Sample(SfxKind kind)
        {
            float[] d;
            switch (kind)
            {
                case SfxKind.Shot: d = Shot(); break;
                case SfxKind.Hit: d = Hit(); break;
                case SfxKind.Wrong: d = Wrong(); break;
                case SfxKind.Miss: d = Miss(); break;
                case SfxKind.RoundClear: d = Sequence(new[] { 523.25f, 659.25f, 783.99f }, 0.12f, 0.10f); break;
                case SfxKind.GameOver: d = Sequence(new[] { 392f, 329.63f, 261.63f }, 0.18f, 0.20f); break;
                case SfxKind.Tick: d = Tick(); break;
                default: d = Start(); break;
            }
            Declick(d);
            return d;
        }

        static float[] Buffer(float seconds) => new float[Mathf.Max(1, (int)(Rate * seconds))];

        static float SoftClip(float x) => x / (1f + Mathf.Abs(x));

        // ---- effects ---------------------------------------------------------

        static float[] Shot()
        {
            var d = Buffer(0.17f);
            var rnd = new System.Random(101);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.35f;
                float crack = Mathf.Exp(-t / 0.004f);
                float burst = Mathf.Exp(-t / 0.030f);
                float thump = Mathf.Sin(Tau * 90f * t) * Mathf.Exp(-t / 0.050f);
                float s = (noise * 0.5f + lp * 0.5f) * burst + thump * 0.55f + noise * crack * 0.7f;
                d[i] = SoftClip(s * 0.9f);
            }
            return d;
        }

        static float[] Hit()
        {
            var d = Buffer(0.25f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float atk = Mathf.Clamp01(t / 0.004f);
                float env = atk * Mathf.Exp(-t / 0.085f);
                float s = Mathf.Sin(Tau * 880f * t) * 0.60f
                        + Mathf.Sin(Tau * 1320f * t) * 0.28f
                        + Mathf.Sin(Tau * 1760f * t) * 0.12f;
                d[i] = SoftClip(s * env * 0.8f);
            }
            return d;
        }

        static float[] Wrong()
        {
            var d = Buffer(0.27f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t / 0.11f);
                float a = Mathf.Sign(Mathf.Sin(Tau * 150f * t));
                float b = Mathf.Sign(Mathf.Sin(Tau * 158.5f * t));
                float s = a * 0.45f + b * 0.30f + Mathf.Sin(Tau * 300f * t) * 0.15f;
                d[i] = SoftClip(s * env * 0.7f);
            }
            return d;
        }

        static float[] Miss()
        {
            var d = Buffer(0.15f);
            var rnd = new System.Random(202);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.08f; // dull, bodyless thud
                float env = Mathf.Exp(-t / 0.045f);
                d[i] = SoftClip(lp * env * 1.4f);
            }
            return d;
        }

        static float[] Sequence(float[] freqs, float noteDur, float decay)
        {
            int per = (int)(Rate * noteDur);
            var d = new float[per * freqs.Length];
            for (int n = 0; n < freqs.Length; n++)
                for (int i = 0; i < per; i++)
                {
                    float t = i / (float)Rate;
                    float atk = Mathf.Clamp01(t / 0.006f);
                    float env = atk * Mathf.Exp(-t / decay);
                    float s = Mathf.Sin(Tau * freqs[n] * t) * 0.7f
                            + Mathf.Sin(Tau * freqs[n] * 2f * t) * 0.15f;
                    d[n * per + i] = SoftClip(s * env * 0.7f);
                }
            return d;
        }

        static float[] Tick()
        {
            var d = Buffer(0.06f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t / 0.014f);
                d[i] = Mathf.Sin(Tau * 1500f * t) * env * 0.5f;
            }
            return d;
        }

        static float[] Start()
        {
            var d = Buffer(0.1f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t / 0.035f);
                float s = Mathf.Sin(Tau * 660f * t) * 0.7f + Mathf.Sin(Tau * 990f * t) * 0.3f;
                d[i] = SoftClip(s * env * 0.6f);
            }
            return d;
        }

        /// <summary>Short fades so clips never click at their edges.</summary>
        static void Declick(float[] d)
        {
            int f = Rate / 500; // 2 ms
            if (d.Length < f * 2) f = Mathf.Max(1, d.Length / 4);
            for (int i = 0; i < f; i++)
            {
                float g = i / (float)f;
                d[i] *= g;
                d[d.Length - 1 - i] *= g;
            }
        }
    }

    /// <summary>Plays synthesised one-shot effects through the SFX volume setting.</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx Instance { get; private set; }

        static AudioClip[] cache;
        AudioSource source;

        public static int CachedClipCount
        {
            get
            {
                if (cache == null) return 0;
                int n = 0;
                foreach (var c in cache) if (c != null) n++;
                return n;
            }
        }

        void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            EnsureCache();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static void EnsureCache()
        {
            if (cache == null)
                cache = new AudioClip[Enum.GetValues(typeof(SfxKind)).Length];
        }

        public static AudioClip Clip(SfxKind kind)
        {
            EnsureCache();
            int i = (int)kind;
            if (cache[i] == null) cache[i] = SfxSynth.Create(kind);
            return cache[i];
        }

        public static void Play(SfxKind kind, float volumeScale = 1f)
        {
            Sfx inst = Instance;
            if (inst == null || inst.source == null) return;
            float v = SaveData.SfxVolume * volumeScale;
            if (v <= 0.0001f) return;
            inst.source.PlayOneShot(Clip(kind), Mathf.Clamp01(v));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            cache = null;
        }
    }
}
