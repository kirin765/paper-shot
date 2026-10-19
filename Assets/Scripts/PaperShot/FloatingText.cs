using UnityEngine;

namespace PaperShot
{
    /// <summary>
    /// A short-lived world-space label that rises and fades — used to show the
    /// time penalty right where a wrong shot landed.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public bool Dead { get; private set; }

        TextMesh mesh;
        Color color;
        float life, t, rise;
        Vector3 start;

        public void Init(string text, Vector2 pos, Color tint, float worldSize,
            float lifetime = 0.9f, float riseDistance = 0.9f)
        {
            start = new Vector3(pos.x, pos.y, 0f);
            transform.position = start;

            mesh = gameObject.AddComponent<TextMesh>();
            mesh.font = Fonts.Runtime;
            mesh.fontSize = 100;
            mesh.characterSize = worldSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.text = text;
            mesh.color = tint;
            mesh.richText = false;

            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 60;

            color = tint;
            life = Mathf.Max(0.05f, lifetime);
            rise = riseDistance;
        }

        public void Tick(float dt)
        {
            if (Dead) return;
            t += dt;
            float k = Mathf.Clamp01(t / life);

            Vector3 p = start;
            p.y += rise * k;
            transform.position = p;

            Color c = color;
            c.a = color.a * (1f - k * k);
            mesh.color = c;

            if (k >= 1f) Dead = true;
        }
    }
}
