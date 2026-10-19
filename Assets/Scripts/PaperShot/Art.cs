using UnityEngine;

namespace PaperShot
{
    /// <summary>Cached sprites shared by every object in the game.</summary>
    public static class Art
    {
        const int Seed = 20261017;

        static Sprite _ring, _splat, _hole, _ringWhite, _splatWhite;

        public static Sprite Ring => _ring ??= ProcArt.Ring(240, Palette.Target, 7331);
        public static Sprite Splat => _splat ??= ProcArt.Splat(200, Palette.Ink, 5150);
        public static Sprite Hole => _hole ??= LoadHole();

        /// <summary>Untinted white versions for effects that need colour multiplication.</summary>
        public static Sprite RingWhite => _ringWhite ??= ProcArt.Ring(240, Color.white, 7331);
        public static Sprite SplatWhite => _splatWhite ??= ProcArt.Splat(200, Color.white, 5150);

        static Sprite LoadHole()
        {
            var texture = Resources.Load<Texture2D>("Decals/bullet-hole-paper");
            if (texture == null) return ProcArt.Hole(96, Palette.Ink);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

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
            _ringWhite = _splatWhite = null;
        }
    }
}
