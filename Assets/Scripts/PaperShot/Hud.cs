using UnityEngine;

namespace PaperShot
{
    /// <summary>Resolved rectangles and font sizes for one HUD frame.</summary>
    public struct HudLayout
    {
        public float Unit;
        public Rect Column;
        public Rect TimerBar;
        public Rect RoundLabel;
        public Rect ScoreLabel;
        public Rect ShootLabel;
        public Rect TitleRect;
        public Rect SubRect;
        public Rect ToastRect;
        public int TitleFontSize;
        public int TextFontSize;
        public int ToastFontSize;
        public bool ShowShoot;
        public bool ShowTitle;
        public bool ShowToast;
    }

    /// <summary>
    /// Pure HUD geometry: given a viewport and the current game state it returns
    /// every label rect so nothing clips off-screen and no two blocks overlap.
    /// Kept free of IMGUI so it can be unit-tested.
    /// </summary>
    public static class HudLayoutBuilder
    {
        /// <summary>Screen width the HUD sizes are authored against (9:16 column).</summary>
        public const float DesignWidth = 820f;
        public const float DesignTitleFont = 96f;
        public const float DesignTextFont = 34f;
        public const float DesignToastFont = 68f;

        public static HudLayout Compute(float screenW, float screenH, bool playing,
            bool hasTitle, string sub, bool hasToast)
        {
            var L = new HudLayout();

            float colW = Mathf.Min(screenW, screenH * PaperShotGame.DesignAspect);
            float colX = (screenW - colW) * 0.5f;
            L.Column = new Rect(colX, 0f, colW, screenH);

            // Scale off the design column, not the raw screen, so the HUD stays
            // proportionate on small/wide viewports.
            float unit = Mathf.Max(colW, 1f) / DesignWidth;
            L.Unit = unit;

            float pad = 26f * unit;
            float innerX = colX + pad;
            float innerW = colW - pad * 2f;
            float gap = 10f * unit;
            float rowH = 46f * unit;

            L.TextFontSize = Mathf.Max(1, Mathf.RoundToInt(DesignTextFont * unit));
            L.TitleFontSize = Mathf.Max(1, Mathf.RoundToInt(DesignTitleFont * unit));
            L.ToastFontSize = Mathf.Max(1, Mathf.RoundToInt(DesignToastFont * unit));

            // ---- timer bar ----
            float barY = 28f * unit;
            float barH = 16f * unit;
            L.TimerBar = new Rect(innerX, barY, innerW, barH);

            // ---- top row: round + hits/score ----
            float rowY = barY + barH + gap;
            L.RoundLabel = new Rect(innerX, rowY, innerW * 0.5f, rowH);
            L.ScoreLabel = new Rect(innerX + innerW * 0.5f, rowY, innerW * 0.5f, rowH);
            float topBottom = rowY + rowH;

            // ---- shoot-order hint (stacks below the row, never over it) ----
            if (playing)
            {
                L.ShowShoot = true;
                float shootY = topBottom + gap;
                L.ShootLabel = new Rect(colX, shootY, colW, rowH);
                topBottom = shootY + rowH;
            }

            float bottomLimit = screenH - 40f * unit;
            float regionTop = topBottom + gap * 2f;

            // ---- centre banner ----
            if (hasTitle)
            {
                L.ShowTitle = true;
                float titleH = L.TitleFontSize * 1.35f;
                float subW = innerW * 0.92f;
                int subLines = EstimatedLines(sub, L.TextFontSize, subW);
                float subH = subLines * L.TextFontSize * 1.35f;
                float blockH = titleH + gap + subH;

                float blockTop = Mathf.Clamp(screenH * 0.34f, regionTop,
                    Mathf.Max(regionTop, bottomLimit - blockH));
                L.TitleRect = new Rect(innerX, blockTop, innerW, titleH);
                L.SubRect = new Rect(innerX + innerW * 0.04f, blockTop + titleH + gap, subW, subH);
            }

            // ---- toast (kept between the top block and the banner) ----
            if (hasToast)
            {
                L.ShowToast = true;
                float toastH = L.ToastFontSize * 1.35f;
                float maxY = L.ShowTitle ? L.TitleRect.y - gap - toastH : bottomLimit - toastH;
                float top = Mathf.Clamp(screenH * 0.22f, topBottom + gap, Mathf.Max(topBottom + gap, maxY));
                L.ToastRect = new Rect(colX, top, colW, toastH);
            }

            return L;
        }

        /// <summary>
        /// Estimated wrapped line count. The real font is unknown here, so we use
        /// a conservative average glyph width and round up — over-estimating only
        /// adds breathing room, never clipping.
        /// </summary>
        static int EstimatedLines(string text, int fontSize, float width)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            float charW = Mathf.Max(1f, fontSize * 0.52f);
            int maxChars = Mathf.Max(1, Mathf.FloorToInt(width / charW));
            int lines = 0;
            foreach (string part in text.Split('\n'))
                lines += Mathf.Max(1, Mathf.CeilToInt(part.Length / (float)maxChars));
            return Mathf.Max(1, lines);
        }
    }

    /// <summary>IMGUI overlay: round, score, timer bar, banners and settings.</summary>
    public class Hud : MonoBehaviour
    {
        public PaperShotGame Game;
        public Bgm Bgm;

        GUIStyle big, mid, left, right, small, btn, panel, rowLabel;
        bool ready;
        bool settingsOpen;
        Rect settingsButton;
        bool settingsButtonShown;

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
            small = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            rowLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };

            foreach (GUIStyle s in new[] { big, mid, left, right, small, rowLabel })
            {
                s.clipping = TextClipping.Overflow;
                s.wordWrap = false;
                s.richText = false;
                s.padding = new RectOffset(0, 0, 0, 0);
                s.margin = new RectOffset(0, 0, 0, 0);
                s.contentOffset = Vector2.zero;
                s.fixedHeight = 0f;
            }
            mid.wordWrap = true;
            small.wordWrap = true;

            // Paper-toned buttons so the UI matches the game instead of the OS skin.
            btn = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            btn.normal.background = Solid(new Color(0.93f, 0.91f, 0.85f, 0.97f));
            btn.hover.background = Solid(new Color(0.98f, 0.96f, 0.91f, 1f));
            btn.active.background = Solid(new Color(0.84f, 0.80f, 0.72f, 1f));
            btn.normal.textColor = Palette.Ink;
            btn.hover.textColor = Palette.Ink;
            btn.active.textColor = Palette.Ink;
            btn.border = new RectOffset(2, 2, 2, 2);
            btn.padding = new RectOffset(8, 8, 4, 4);
            btn.clipping = TextClipping.Clip;

            panel = new GUIStyle(GUI.skin.box);
            panel.normal.background = Solid(new Color(0.96f, 0.94f, 0.89f, 0.98f));
            panel.border = new RectOffset(4, 4, 4, 4);

            ready = true;
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        void OnGUI()
        {
            if (Game == null) return;
            if (!ready) Build();

            bool playing = Game.State == GameState.Playing;
            string sub = SubFor(Game.State);
            bool hasToast = Game.ToastTimer > 0f && !string.IsNullOrEmpty(Game.Toast);

            HudLayout L = HudLayoutBuilder.Compute(
                Screen.width, Screen.height, playing, !playing, sub, hasToast);

            Color ink = Palette.Ink;

            // ---- timer bar ----
            float frac = Game.RoundTime > 0f ? Mathf.Clamp01(Game.TimeLeft / Game.RoundTime) : 0f;
            GUI.color = new Color(ink.r, ink.g, ink.b, 0.18f);
            GUI.DrawTexture(L.TimerBar, Util.White);
            Color fill = Color.Lerp(Palette.Accent, Palette.Good, frac);
            GUI.color = new Color(fill.r, fill.g, fill.b, 0.9f);
            GUI.DrawTexture(new Rect(L.TimerBar.x, L.TimerBar.y, L.TimerBar.width * frac, L.TimerBar.height),
                Util.White);
            GUI.color = Color.white;

            // ---- top row: round + hits/score ----
            string roundText = Game.IsPractice ? "PRACTICE" : "ROUND " + Game.Round;
            left.fontSize = L.TextFontSize;
            left.normal.textColor = ink;
            right.fontSize = L.TextFontSize;
            right.normal.textColor = ink;
            GUI.Label(L.RoundLabel, roundText, left);
            GUI.Label(L.ScoreLabel,
                "HITS " + Game.RoundHits + "/" + Game.RoundHitsRequired + "   SCORE " + Game.Score, right);

            // ---- shoot-order hint (sequential, so it leaks nothing) ----
            if (L.ShowShoot)
            {
                mid.fontSize = L.TextFontSize;
                mid.normal.textColor = new Color(ink.r, ink.g, ink.b, 0.85f);
                string hint = Game.IsPractice
                    ? "PRACTICE - SHOOT IN ORDER   " + Game.CurrentOrderStep + " / " + Game.RoundHitsRequired
                    : "SHOOT  " + Game.CurrentOrderStep + " / " + Game.RoundHitsRequired;
                GUI.Label(L.ShootLabel, hint, mid);
            }

            // ---- centre banner ----
            if (L.ShowTitle)
            {
                big.fontSize = L.TitleFontSize;
                big.normal.textColor = ink;
                DrawShadowed(L.TitleRect, TitleFor(Game.State), big, ink, L.Unit);

                mid.fontSize = L.TextFontSize;
                mid.normal.textColor = new Color(ink.r, ink.g, ink.b, 0.78f);
                DrawShadowed(L.SubRect, sub, mid, new Color(ink.r, ink.g, ink.b, 0.78f), L.Unit);
            }

            // ---- toast ----
            if (L.ShowToast)
            {
                float fade = Mathf.Clamp01(Game.ToastTimer / 0.9f);
                big.fontSize = L.ToastFontSize;
                big.normal.textColor = new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, fade);
                GUI.Label(L.ToastRect, Game.Toast, big);
            }

            DrawAttribution(L);
            DrawSettingsButton(L);
            if (settingsOpen) DrawSettingsPanel(L);

            Game.InputBlocked = settingsOpen;
        }

        // ---- settings --------------------------------------------------------

        void DrawSettingsButton(HudLayout L)
        {
            settingsButtonShown = Game.State == GameState.Ready || Game.State == GameState.GameOver;
            if (!settingsButtonShown)
            {
                settingsOpen = false;
                return;
            }

            float w = 150f * L.Unit;
            float h = 44f * L.Unit;
            settingsButton = new Rect(L.Column.xMax - 26f * L.Unit - w,
                Screen.height - 40f * L.Unit - h, w, h);
            btn.fontSize = Mathf.RoundToInt(22 * L.Unit);
            if (GUI.Button(settingsButton, settingsOpen ? "CLOSE" : "SETTINGS", btn))
                settingsOpen = !settingsOpen;
        }

        void DrawSettingsPanel(HudLayout L)
        {
            float pad = 26f * L.Unit;
            Rect r = new Rect(L.Column.x + pad, L.Column.y + pad * 2f,
                L.Column.width - pad * 2f, L.Column.height - pad * 4f);
            GUI.Box(r, GUIContent.none, panel);

            float u = L.Unit;
            float innerPad = 20f * u;
            float x = r.x + innerPad;
            float w = r.width - innerPad * 2f;
            float y = r.y + 18f * u;
            float rowH = 52f * u;
            float gap = 8f * u;

            big.fontSize = Mathf.RoundToInt(44 * u);
            big.normal.textColor = Palette.Ink;
            GUI.Label(new Rect(x, y, w, rowH), "SETTINGS", big);
            y += rowH + gap;

            Slider("MUSIC", SaveData.MusicVolume, ref y, x, w, rowH, gap, u, v =>
            {
                SaveData.MusicVolume = v;
                if (Bgm != null) Bgm.ApplySettings();
            });
            Slider("SOUND", SaveData.SfxVolume, ref y, x, w, rowH, gap, u, v => SaveData.SfxVolume = v);
            Toggle("HAPTICS", SaveData.Haptics, ref y, x, w, rowH, gap, u, v => SaveData.Haptics = v);
            Toggle("SCREEN SHAKE", SaveData.ScreenShake, ref y, x, w, rowH, gap, u, v => SaveData.ScreenShake = v);
            Toggle("REDUCED MOTION", SaveData.ReducedMotion, ref y, x, w, rowH, gap, u, v => SaveData.ReducedMotion = v);

            y += gap;
            btn.fontSize = Mathf.RoundToInt(22 * u);
            if (GUI.Button(new Rect(x, r.yMax - 150f * u, w, 44f * u), "PRIVACY POLICY", btn))
                Application.OpenURL("https://gist.github.com/kirin765/325433caa3a451c6a32d49e5e2d43b63");
            small.fontSize = Mathf.RoundToInt(18 * u);
            small.normal.textColor = new Color(Palette.Ink.r, Palette.Ink.g, Palette.Ink.b, 0.7f);
            GUI.Label(new Rect(x, r.yMax - 96f * u, w, 90f * u),
                "Music: \u201CSneaky Snitch\u201D \u2014 Kevin MacLeod (incompetech.com), CC BY 4.0",
                small);

            btn.fontSize = Mathf.RoundToInt(26 * u);
            if (GUI.Button(new Rect(x, r.yMax - 60f * u, w, 46f * u), "CLOSE", btn))
                settingsOpen = false;
        }

        void Slider(string label, float value, ref float y, float x, float w, float rowH,
            float gap, float u, System.Action<float> set)
        {
            rowLabel.fontSize = Mathf.RoundToInt(24 * u);
            rowLabel.normal.textColor = Palette.Ink;
            GUI.Label(new Rect(x, y, w * 0.4f, rowH), label, rowLabel);

            float bw = 44f * u;
            btn.fontSize = Mathf.RoundToInt(26 * u);
            if (GUI.Button(new Rect(x + w - (bw * 2f + 90f * u + gap * 2f), y, bw, rowH), "-", btn))
                set(Mathf.Clamp01(value - 0.1f));

            mid.fontSize = Mathf.RoundToInt(24 * u);
            mid.normal.textColor = Palette.Ink;
            GUI.Label(new Rect(x + w - (bw + 90f * u + gap), y, 90f * u, rowH),
                Mathf.RoundToInt(value * 100f) + "%", mid);

            if (GUI.Button(new Rect(x + w - bw, y, bw, rowH), "+", btn))
                set(Mathf.Clamp01(value + 0.1f));

            y += rowH + gap;
        }

        void Toggle(string label, bool value, ref float y, float x, float w, float rowH,
            float gap, float u, System.Action<bool> set)
        {
            rowLabel.fontSize = Mathf.RoundToInt(24 * u);
            rowLabel.normal.textColor = Palette.Ink;
            GUI.Label(new Rect(x, y, w * 0.55f, rowH), label, rowLabel);

            float bw = 110f * u;
            btn.fontSize = Mathf.RoundToInt(20 * u);
            if (GUI.Button(new Rect(x + w - bw, y, bw, rowH), value ? "ON" : "OFF", btn))
                set(!value);

            y += rowH + gap;
        }

        void DrawAttribution(HudLayout L)
        {
            if (Game.State != GameState.Ready) return;
            small.fontSize = Mathf.RoundToInt(16 * L.Unit);
            small.normal.textColor = new Color(Palette.Ink.r, Palette.Ink.g, Palette.Ink.b, 0.55f);
            GUI.Label(new Rect(L.Column.x + 20f * L.Unit, Screen.height - 150f * L.Unit,
                L.Column.width - 40f * L.Unit, 40f * L.Unit),
                "Music: \u201CSneaky Snitch\u201D \u2014 Kevin MacLeod (incompetech.com), CC BY 4.0", small);
        }

        /// <summary>True when a screen-space pointer should be routed to the UI, not the game.</summary>
        public bool CapturesPointer(Vector2 screenPos)
        {
            if (settingsOpen) return true;
            if (!settingsButtonShown) return false;
            // Input System uses bottom-left origin; IMGUI uses top-left.
            var gui = new Vector2(screenPos.x, Screen.height - screenPos.y);
            return settingsButton.Contains(gui);
        }

        // ---- text ------------------------------------------------------------

        string TitleFor(GameState state)
        {
            switch (state)
            {
                case GameState.Ready: return "PAPER SHOT";
                case GameState.Intro: return Game.IsPractice ? "PRACTICE" : "ROUND " + Game.Round;
                case GameState.RoundClear: return Game.IsPractice ? "NICE!" : "ROUND CLEAR";
                case GameState.GameOver: return "TIME UP";
            }
            return null;
        }

        string SubFor(GameState state)
        {
            switch (state)
            {
                case GameState.Ready:
                    return Game.Tutorial
                        ? "TAP TO START\npractice round: remember the order, then shoot it"
                        : "TAP TO START\nmemorize the numbered circles, then shoot them in order";
                case GameState.Intro:
                    return Game.IsPractice ? "REMEMBER THE ORDER" : "GET READY";
                case GameState.RoundClear:
                    return Game.IsPractice ? "now beat the clock" : "SCORE " + Game.Score;
                case GameState.GameOver:
                    string best = Game.NewBest ? "   NEW BEST!" : "   BEST " + SaveData.BestScore;
                    return "SCORE " + Game.Score + best
                        + "\nROUND " + Game.Round + "   ACCURACY " + Mathf.RoundToInt(Game.Accuracy * 100f) + "%"
                        + "\nTAP TO RETRY";
            }
            return null;
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
