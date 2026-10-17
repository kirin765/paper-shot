using UnityEngine;

namespace PaperShot
{
    /// <summary>A hand-drawn line circle the player aims at.</summary>
    public class TargetCircle : MonoBehaviour
    {
        public float Radius { get; private set; }
        public Vector2 Pos => transform.position;

        /// <summary>The number shown on this circle for ordered rounds (1-based).</summary>
        int order;
        public int Order
        {
            get => order;
            set { order = value; if (label != null) label.text = value > 0 ? value.ToString() : ""; }
        }

        public int Hits { get; private set; }
        public int HitsRequired { get; set; } = 1;
        public bool IsComplete => Hits >= HitsRequired;

        SpriteRenderer sr;
        TextMesh label;
        Vector3 baseScale;
        float flash;

        public void RegisterHit() => Hits++;

        public void SetLabelVisible(bool visible)
        {
            if (label != null && label.gameObject.activeSelf != visible)
                label.gameObject.SetActive(visible);
        }

        static Font _font;
        static Font DigitFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        public void Setup(Vector2 pos, float radius)
        {
            transform.position = new Vector3(pos.x, pos.y, 0f);
            Radius = radius;

            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Ring;
            sr.sortingOrder = 3;
            sr.color = Color.white; // colour is baked into the ring sprite

            float d = Art.RingSpriteSize(radius);
            Util.FitWH(sr, d, d);
            baseScale = transform.localScale;

            var labelGo = new GameObject("Order");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localScale = Vector3.one;
            label = labelGo.AddComponent<TextMesh>();
            label.font = DigitFont;
            label.fontSize = 96;
            label.characterSize = radius * 0.16f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Palette.Ink;
            label.text = "";
            var mr = label.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 20;
            labelGo.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (flash <= 0f) return;

            flash = Mathf.Max(0f, flash - dt * 3.5f);
            sr.color = Color.Lerp(Color.white, Palette.Accent, flash);
            float s = 1f + 0.12f * Mathf.Sin(flash * Mathf.PI);
            transform.localScale = baseScale * s;
        }

        public void Flash() => flash = 1f;

        public bool Contains(Vector2 world, float pad = 0f)
            => Vector2.Distance(world, Pos) <= Radius + pad;
    }
}
