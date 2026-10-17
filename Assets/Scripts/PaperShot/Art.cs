using UnityEngine;

namespace PaperShot
{
    /// <summary>Cached sprites shared by every object in the game.</summary>
    public static class Art
    {
        const int Seed = 20261017;

        static Sprite _ring, _splat, _hole;

        public static Sprite Ring => _ring ??= ProcArt.Ring(240, Palette.Target, 7331);
        public static Sprite Splat => _splat ??= ProcArt.Splat(200, Palette.Ink, 5150);
        public static Sprite Hole => _hole ??= ProcArt.Hole(96, Palette.Ink);

        /// <summary>Sprite world size needed so the drawn ring has the given radius.</summary>
        public static float RingSpriteSize(float radius)
            => radius / ProcArt.RingRadiusFraction;

        public static Sprite PaperFor(float aspect)
        {
            int h = 1024;
            int w = Mathf.Clamp(Mathf.RoundToInt(h * aspect), 256, 2048);
            return ProcArt.Paper(w, h, Seed);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _ring = _splat = _hole = null;
        }
    }
}
