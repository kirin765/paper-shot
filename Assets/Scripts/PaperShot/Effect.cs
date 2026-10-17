using UnityEngine;

namespace PaperShot
{
    /// <summary>A short-lived sprite burst (hit splat, dust puff, pellet hole).</summary>
    public class Effect : MonoBehaviour
    {
        public bool Dead { get; private set; }

        SpriteRenderer sr;
        Color color;
        float life, t, worldW, worldH, grow;

        public void Init(Sprite sprite, Vector2 pos, float w, float h, Color tint,
            float lifetime, float growTo, int order)
        {
            transform.position = new Vector3(pos.x, pos.y, 0f);
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;

            color = tint;
            sr.color = color;
            life = Mathf.Max(0.01f, lifetime);
            grow = growTo;
            Util.FitWH(sr, w, h);
            worldW = w;
            worldH = h;
        }

        public void Tick(float dt)
        {
            if (Dead) return;
            t += dt;
            float k = Mathf.Clamp01(t / life);

            float scale = Mathf.Lerp(1f, grow, k);
            Util.FitWH(sr, worldW * scale, worldH * scale);

            Color c = color;
            c.a = color.a * (1f - k);
            sr.color = c;

            if (k >= 1f) Dead = true;
        }
    }
}
