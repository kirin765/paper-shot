using UnityEngine;

namespace PaperShot
{
    /// <summary>Shared helpers for creating sprite objects at runtime.</summary>
    public static class Util
    {
        static Texture2D _white;

        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                }
                return _white;
            }
        }

        public static SpriteRenderer NewSprite(string name, Sprite sprite, int order, Transform parent, Color color)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }

        /// <summary>Scales a sprite renderer so the sprite occupies w x h world units.</summary>
        public static void FitWH(SpriteRenderer sr, float w, float h)
        {
            Vector3 b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(
                w / Mathf.Max(b.x, 1e-4f),
                h / Mathf.Max(b.y, 1e-4f),
                1f);
        }
    }
}
