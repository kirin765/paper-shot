using UnityEngine;

namespace PaperShot
{
    /// <summary>
    /// Runtime procedural textures/sprites. Nothing is imported from disk, so the
    /// game has no art dependencies — every surface is generated on boot.
    /// </summary>
    public static class ProcArt
    {
        public const float PPU = 100f;

        /// <summary>Ring radius as a fraction of the full sprite width.</summary>
        public const float RingRadiusFraction = 0.44f;

        // ---- noise -----------------------------------------------------------

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        static float ValueNoise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);
            float a = Hash(xi, yi, seed);
            float b = Hash(xi + 1, yi, seed);
            float c = Hash(xi, yi + 1, seed);
            float d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        static float Fbm(float x, float y, int seed, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * ValueNoise(x * freq, y * freq, seed + i * 97);
                freq *= 2f;
                amp *= 0.5f;
            }
            return sum;
        }

        // ---- textures --------------------------------------------------------

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            return t;
        }

        static Sprite MakeSprite(Texture2D t, string name)
        {
            t.name = name;
            var s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f),
                PPU, 0, SpriteMeshType.FullRect);
            s.name = name;
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        /// <summary>Aged, stained, fibrous paper with a darkened vignette.</summary>
        public static Sprite Paper(int w, int h, int seed)
        {
            var tex = NewTex(w, h);
            var buf = new Color[w * h];
            var rng = new System.Random(seed);

            int stains = 8 + rng.Next(10);
            var sx = new float[stains];
            var sy = new float[stains];
            var sr = new float[stains];
            var ss = new float[stains];
            for (int i = 0; i < stains; i++)
            {
                sx[i] = (float)rng.NextDouble();
                sy[i] = (float)rng.NextDouble();
                sr[i] = 0.07f + 0.24f * (float)rng.NextDouble();
                ss[i] = 0.35f + 0.65f * (float)rng.NextDouble();
            }

            for (int y = 0; y < h; y++)
            {
                float fy = y / (h - 1f);
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (w - 1f);

                    float grain = Fbm(fx * 12f, fy * 9f, seed, 4);
                    float fiber = 0.5f + 0.5f * Mathf.Sin(fx * 190f + fy * 40f + grain * 9f);
                    float n = Mathf.Clamp01(0.84f + (grain - 0.5f) * 0.5f + (fiber - 0.5f) * 0.06f);
                    Color c = Color.Lerp(Palette.PaperDark, Palette.PaperLight, n);

                    float stain = 0f;
                    for (int s = 0; s < stains; s++)
                    {
                        float dx = fx - sx[s];
                        float dy = fy - sy[s];
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / sr[s];
                        if (d < 1f) stain += (1f - d) * (1f - d) * ss[s];
                    }
                    c = Color.Lerp(c, Palette.PaperStain, Mathf.Clamp01(stain) * 0.5f);

                    float vx = (fx - 0.5f) * 2f;
                    float vy = (fy - 0.5f) * 2f;
                    float vig = Mathf.Clamp01((Mathf.Sqrt(vx * vx + vy * vy) - 0.35f) / 0.9f);
                    c *= Mathf.Lerp(1f, 0.80f, vig);

                    c.a = 1f;
                    buf[y * w + x] = c;
                }
            }

            tex.SetPixels(buf);
            tex.Apply(false, false);
            return MakeSprite(tex, "Paper");
        }

        /// <summary>Hand-drawn looking line circle (ring).</summary>
        public static Sprite Ring(int px, Color ink, int seed)
        {
            var tex = NewTex(px, px);
            var buf = new Color[px * px];
            float c = px * 0.5f;
            float radius = px * RingRadiusFraction;
            float thick = Mathf.Max(2f, px * 0.034f);

            for (int y = 0; y < px; y++)
            {
                for (int x = 0; x < px; x++)
                {
                    float dx = x + 0.5f - c;
                    float dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float nx = Mathf.Cos(ang), ny = Mathf.Sin(ang);

                    float wob = (Fbm(nx * 2.3f + 10f, ny * 2.3f + 10f, seed, 3) - 0.5f) * px * 0.05f;
                    float rr = radius + wob;
                    float dist = Mathf.Abs(d - rr);
                    float a = 1f - Mathf.Clamp01((dist - thick * 0.5f) / 1.4f);
                    a *= 0.86f + 0.14f * Fbm(nx * 5f, ny * 5f, seed + 7, 2);

                    Color col = ink;
                    col.a = Mathf.Clamp01(a) * ink.a;
                    buf[y * px + x] = col;
                }
            }

            tex.SetPixels(buf);
            tex.Apply(false, false);
            return MakeSprite(tex, "Ring");
        }

        /// <summary>A clear shadow: solid core with a soft rim (not a faint smudge).</summary>
        public static Sprite Blob(int px, Color color, float edge = 0.35f)
        {
            var tex = NewTex(px, px);
            var buf = new Color[px * px];
            float c = px * 0.5f;
            for (int y = 0; y < px; y++)
            {
                for (int x = 0; x < px; x++)
                {
                    float dx = (x + 0.5f - c) / c;
                    float dy = (y + 0.5f - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((1f - d) / edge);
                    a = a * a * (3f - 2f * a);
                    Color col = color;
                    col.a = a * color.a;
                    buf[y * px + x] = col;
                }
            }
            tex.SetPixels(buf);
            tex.Apply(false, false);
            return MakeSprite(tex, "Blob");
        }

        /// <summary>
        /// A decoy shadow: nearly the same silhouette as a real blob, but with a
        /// slightly harder edge, a faint rim and a little irregularity, so an
        /// attentive player can learn to tell it apart.
        /// </summary>
        public static Sprite FakeBlob(int px, Color color, int seed)
        {
            var tex = NewTex(px, px);
            var buf = new Color[px * px];
            float c = px * 0.5f;
            for (int y = 0; y < px; y++)
            {
                for (int x = 0; x < px; x++)
                {
                    float dx = (x + 0.5f - c) / c;
                    float dy = (y + 0.5f - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float nx = Mathf.Cos(ang), ny = Mathf.Sin(ang);

                    float jag = 0.88f + 0.12f * (Fbm(nx * 2.5f + 5f, ny * 2.5f + 5f, seed, 3) - 0.5f) * 2f;
                    float dd = d / jag;

                    // solid core with a crisper edge than a real shadow
                    float a = Mathf.Clamp01((1f - dd) / 0.16f);
                    a = a * a * (3f - 2f * a);

                    // a distinct rim so the decoy is clearly readable
                    float rim = 1f - Mathf.Clamp01(Mathf.Abs(dd - 0.9f) / 0.07f);
                    a = Mathf.Clamp01(Mathf.Max(a, rim * 0.6f));

                    Color col = color;
                    col.a = a * color.a;
                    buf[y * px + x] = col;
                }
            }
            tex.SetPixels(buf);
            tex.Apply(false, false);
            return MakeSprite(tex, "FakeBlob");
        }

        /// <summary>Jagged ink burst for a confirmed hit.</summary>
        public static Sprite Splat(int px, Color ink, int seed)
        {
            var tex = NewTex(px, px);
            var buf = new Color[px * px];
            float c = px * 0.5f;
            for (int y = 0; y < px; y++)
            {
                for (int x = 0; x < px; x++)
                {
                    float dx = (x + 0.5f - c) / c;
                    float dy = (y + 0.5f - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float nx = Mathf.Cos(ang), ny = Mathf.Sin(ang);

                    float jag = 0.60f + 0.36f * Fbm(nx * 3.1f + 3f, ny * 3.1f + 3f, seed, 3);
                    float band = Mathf.Abs(d - jag);
                    float a = 1f - Mathf.Clamp01(band / 0.30f);
                    a *= a;

                    float core = 1f - Mathf.Clamp01(d / 0.30f);
                    core *= core;
                    a = Mathf.Max(a, core * 0.9f);

                    Color col = ink;
                    col.a = Mathf.Clamp01(a) * ink.a;
                    buf[y * px + x] = col;
                }
            }
            tex.SetPixels(buf);
            tex.Apply(false, false);
            return MakeSprite(tex, "Splat");
        }

        /// <summary>Small pellet hole stamped on the paper where the shot landed.</summary>
        public static Sprite Hole(int px, Color ink)
        {
            var tex = NewTex(px, px);
            var buf = new Color[px * px];
            float c = px * 0.5f;
            for (int y = 0; y < px; y++)
            {
                for (int x = 0; x < px; x++)
                {
                    float dx = (x + 0.5f - c) / c;
                    float dy = (y + 0.5f - c) / c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float ring = 1f - Mathf.Clamp01(Mathf.Abs(d - 0.66f) / 0.22f);
                    float core = 1f - Mathf.Clamp01(d / 0.34f);
                    core *= core;
                    float a = Mathf.Max(ring * 0.85f, core);

                    Color col = ink;
                    col.a = Mathf.Clamp01(a) * ink.a;
                    buf[y * px + x] = col;
                }
            }
            tex.SetPixels(buf);
            tex.Apply(false, false);
            return MakeSprite(tex, "Hole");
        }
    }
}
