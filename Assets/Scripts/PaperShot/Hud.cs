using UnityEngine;

namespace PaperShot
{
    /// <summary>IMGUI overlay: round, score, timer bar and state banners.</summary>
    public class Hud : MonoBehaviour
    {
        public PaperShotGame Game;

        GUIStyle big, mid, left, right;
        bool ready;

        void Build()
        {
            big = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            mid = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            left = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
            right = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };

            // Let text overflow its rect instead of clipping, and drop the skin
            // padding/margins that otherwise squeeze large glyphs vertically.
            foreach (GUIStyle s in new[] { big, mid, left, right })
            {
                s.clipping = TextClipping.Overflow;
                s.wordWrap = false;
                s.richText = false;
                s.padding = new RectOffset(0, 0, 0, 0);
                s.margin = new RectOffset(0, 0, 0, 0);
                s.contentOffset = Vector2.zero;
                s.fixedHeight = 0f;
            }
            ready = true;
        }

        void OnGUI()
        {
            if (Game == null) return;
            if (!ready) Build();

            float unit = Mathf.Max(0.6f, Mathf.Min(Screen.width, Screen.height) / 1080f);
            Color ink = Palette.Ink;

            // ---- timer bar ----
            float barW = Screen.width * 0.82f;
            float barH = 16f * unit;
            float barX = (Screen.width - barW) * 0.5f;
            float barY = 30f * unit;

            float frac = Game.RoundTime > 0f ? Mathf.Clamp01(Game.TimeLeft / Game.RoundTime) : 0f;
            GUI.color = new Color(ink.r, ink.g, ink.b, 0.18f);
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), Util.White);
            Color fill = Color.Lerp(Palette.Accent, Palette.Good, frac);
            GUI.color = new Color(fill.r, fill.g, fill.b, 0.9f);
            GUI.DrawTexture(new Rect(barX, barY, barW * frac, barH), Util.White);
            GUI.color = Color.white;

            // ---- top row: round + hits/score ----
            float rowY = barY + barH + 6f * unit;
            int rowFont = Mathf.RoundToInt(30 * unit);
            left.fontSize = rowFont;
            left.normal.textColor = ink;
            right.fontSize = rowFont;
            right.normal.textColor = ink;
            GUI.Label(new Rect(barX, rowY, barW * 0.5f, 56f * unit), "ROUND " + Game.Round, left);
            GUI.Label(new Rect(barX + barW * 0.5f, rowY, barW * 0.5f, 56f * unit),
                "HITS " + Game.RoundHits + "/" + Game.RoundHitsRequired + "   SCORE " + Game.Score, right);

            // ---- shoot-order hint (sequential, so it leaks nothing) ----
            if (Game.State == GameState.Playing)
            {
                mid.fontSize = Mathf.RoundToInt(30 * unit);
                mid.normal.textColor = new Color(ink.r, ink.g, ink.b, 0.85f);
                GUI.Label(new Rect(0f, rowY + 44f * unit, Screen.width, 56f * unit),
                    "SHOOT  " + Game.CurrentOrderStep + " / " + Game.RoundHitsRequired, mid);
            }

            // ---- centre banner ----
            string title = null, sub = null;
            switch (Game.State)
            {
                case GameState.Ready:
                    title = "PAPER SHOT";
                    sub = "TAP TO START\nmemorize the numbered circles, then shoot them in order";
                    break;
                case GameState.Intro:
                    title = "ROUND " + Game.Round;
                    sub = "GET READY";
                    break;
                case GameState.RoundClear:
                    title = "ROUND CLEAR";
                    sub = "SCORE " + Game.Score;
                    break;
                case GameState.GameOver:
                    title = "TIME UP";
                    sub = "SCORE " + Game.Score + "   -   TAP TO RETRY";
                    break;
            }

            if (title != null)
            {
                big.fontSize = Mathf.RoundToInt(84 * unit);
                big.normal.textColor = ink;
                DrawShadowed(new Rect(0f, Screen.height * 0.34f, Screen.width, 150f * unit), title, big, ink, unit);

                mid.fontSize = Mathf.RoundToInt(30 * unit);
                mid.normal.textColor = new Color(ink.r, ink.g, ink.b, 0.78f);
                DrawShadowed(new Rect(Screen.width * 0.08f, Screen.height * 0.34f + 140f * unit,
                    Screen.width * 0.84f, 180f * unit), sub, mid, new Color(ink.r, ink.g, ink.b, 0.78f), unit);
            }

            DrawToast(unit);
        }

        void DrawToast(float unit)
        {
            if (Game.ToastTimer <= 0f || string.IsNullOrEmpty(Game.Toast)) return;

            float fade = Mathf.Clamp01(Game.ToastTimer / 0.9f);
            Color accent = new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, fade);
            big.fontSize = Mathf.RoundToInt(60 * unit);
            big.normal.textColor = accent;
            GUI.Label(new Rect(0f, Screen.height * 0.22f, Screen.width, 130f * unit), Game.Toast, big);
        }

        void DrawShadowed(Rect r, string text, GUIStyle style, Color color, float unit)
        {
            Color prev = style.normal.textColor;
            style.normal.textColor = new Color(0.98f, 0.96f, 0.90f, color.a);
            GUI.Label(new Rect(r.x + unit, r.y + unit, r.width, r.height), text, style);
            style.normal.textColor = prev;
            GUI.Label(r, text, style);
        }
    }
}
