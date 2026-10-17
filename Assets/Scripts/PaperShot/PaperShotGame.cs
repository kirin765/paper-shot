using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaperShot
{
    public enum GameState { Ready, Intro, Playing, RoundClear, GameOver }

    public enum ShootResult { Ignored, Miss, Wrong, Hit }

    /// <summary>
    /// Paper Shot — a memory shooting game. Each round places numbered line
    /// circles on the paper, reveals the order briefly, then hides it. The
    /// player taps to fire (a bullet hole is left where they shot) and must
    /// shoot the circles back in that order.
    /// </summary>
    public class PaperShotGame : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Ready;
        public int Round { get; private set; } = 1;
        public int Score { get; private set; }
        public int RoundHits { get; private set; }
        public int RoundHitsRequired { get; private set; } = 1;
        public float TimeLeft { get; private set; }
        public float RoundTime { get; private set; } = 1f;
        public bool AutoTick { get; set; } = true;
        public string Toast { get; private set; }
        public float ToastTimer { get; private set; }

        public int CurrentOrderStep => orderStep;
        public bool ShowOrderNumbers => orderRevealTimer > 0f;
        public float OrderRevealAlpha => orderRevealTimer <= 0f ? 0f : Mathf.Clamp01(orderRevealTimer / 0.5f);
        public Camera Camera => cam;
        public IReadOnlyList<TargetCircle> Targets => targets;
        public int EffectCount => effects.Count;

        public float PlayHalfW { get; private set; }
        public float PlayHalfH { get; private set; }

        readonly List<TargetCircle> targets = new List<TargetCircle>();
        readonly List<Effect> effects = new List<Effect>();

        Camera cam;
        System.Random rng;
        Transform root;
        Hud hud;
        SpriteRenderer paperSr;
        float halfW, halfH;
        float phaseTimer;
        int orderStep;
        float orderRevealTimer;

        // ---- lifecycle -------------------------------------------------------

        public static PaperShotGame Create(Camera camera, int seed = 12345, bool autoTick = true)
        {
            var go = new GameObject("PaperShotGame");
            var game = go.AddComponent<PaperShotGame>();
            game.AutoTick = autoTick;
            game.Init(camera, seed);
            return game;
        }

        void Init(Camera camera, int seed)
        {
            cam = camera != null ? camera : Camera.main;
            if (cam == null)
            {
                Debug.LogError("[PaperShot] No camera available.");
                enabled = false;
                return;
            }
            rng = new System.Random(seed);

            halfH = cam.orthographicSize;
            halfW = halfH * Mathf.Max(cam.aspect, 0.3f);

            PlayHalfH = halfH - 0.4f;
            PlayHalfW = Mathf.Min(halfW - 0.4f, PlayHalfH * 0.72f);

            root = new GameObject("PaperShotRoot").transform;

            var paper = Util.NewSprite("Paper", Art.PaperFor(halfW * 2f / (halfH * 2f)), -100, root, Color.white);
            Util.FitWH(paper, halfW * 2f, halfH * 2f);
            paper.transform.position = Vector3.zero;
            paperSr = paper;

            var hudGo = new GameObject("Hud");
            hud = hudGo.AddComponent<Hud>();
            hud.Game = this;

            ResetToReady();
        }

        void Update()
        {
            if (!AutoTick) return;
            ReadPointer();
            Tick(Time.deltaTime);
        }

        // ---- public API ------------------------------------------------------

        public void StartNewGame()
        {
            Round = 1;
            Score = 0;
            StartRound();
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) dt = 1e-4f;
            if (ToastTimer > 0f) ToastTimer = Mathf.Max(0f, ToastTimer - dt);
            if (orderRevealTimer > 0f) orderRevealTimer = Mathf.Max(0f, orderRevealTimer - dt);
            UpdateOrderLabels();
            RefitPaper();

            switch (State)
            {
                case GameState.Intro:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) State = GameState.Playing;
                    break;

                case GameState.Playing:
                    TimeLeft = Mathf.Max(0f, TimeLeft - dt);
                    if (RoundHits >= RoundHitsRequired) { State = GameState.RoundClear; phaseTimer = 1.5f; }
                    else if (TimeLeft <= 0f) State = GameState.GameOver;
                    break;

                case GameState.RoundClear:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) { Round++; StartRound(); }
                    break;
            }

            TickTargets(dt);
            TickEffects(dt);
        }

        /// <summary>
        /// Fire at a world position. The gun always leaves a bullet hole where
        /// you shot; the shot scores only if it lands on the next circle.
        /// </summary>
        public ShootResult Shoot(Vector2 world)
        {
            if (State != GameState.Playing) return ShootResult.Ignored;

            // a real bullet hole, left on the paper where the shot landed
            SpawnEffect(Art.Hole, world, 0.55f, 0.55f,
                new Color(Palette.Ink.r, Palette.Ink.g, Palette.Ink.b, 0.92f), 999f, 1f, -50);
            // quick muzzle flash
            SpawnEffect(Art.Splat, world, 0.46f, 0.46f,
                new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.85f), 0.16f, 0.7f, 12);

            TargetCircle tapped = CircleAt(world);
            if (tapped == null) return ShootResult.Miss; // shot empty paper

            if (tapped == ActiveTarget())
            {
                Score++;
                RoundHits++;
                orderStep++;
                EraseTarget(tapped); // cleared circles are wiped off the paper
                return ShootResult.Hit;
            }

            TimeLeft = Mathf.Max(0f, TimeLeft - Rules.WrongOrderPenalty);
            SetToast("WRONG ORDER", 0.8f);
            return ShootResult.Wrong;
        }

        // ---- state transitions ----------------------------------------------

        public void ResetToReady()
        {
            State = GameState.Ready;
            Score = 0;
            Round = 1;
            RoundHits = 0;
            RoundHitsRequired = Rules.HitsRequired(1);
            RoundTime = Rules.RoundSeconds(1);
            TimeLeft = RoundTime;
            orderStep = 1;
            orderRevealTimer = 0f;
            BuildTargetsFor(1);
            ClearEffects();
            UpdateOrderLabels();
        }

        void StartRound()
        {
            RoundHits = 0;
            RoundHitsRequired = Rules.HitsRequired(Round);
            RoundTime = Rules.RoundSeconds(Round);
            TimeLeft = RoundTime;
            orderStep = 1;
            BuildTargetsFor(Round);
            ClearEffects();

            float reveal = Rules.OrderRevealSeconds(Round);
            State = GameState.Intro;
            phaseTimer = reveal;
            orderRevealTimer = reveal;
            UpdateOrderLabels();
        }

        // ---- world building --------------------------------------------------

        void BuildTargetsFor(int round)
        {
            foreach (TargetCircle t in targets)
                if (t != null) Destroy(t.gameObject);
            targets.Clear();

            int n = Rules.TargetsForRound(round);
            float r = Rules.TargetRadius(round);
            Vector2[] layout = RandomLayout(n, r);
            int[] numbers = ShuffledNumbers(n);

            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Target" + i);
                go.transform.SetParent(root, false);
                var tc = go.AddComponent<TargetCircle>();
                tc.Setup(layout[i], r);
                tc.Order = numbers[i];
                tc.HitsRequired = 1; // one shot per circle
                targets.Add(tc);
            }
        }

        /// <summary>Places n circles at random, non-overlapping spots on the paper.</summary>
        Vector2[] RandomLayout(int n, float radius)
        {
            var result = new Vector2[n];
            float mx = Mathf.Max(0.2f, PlayHalfW - radius - 0.2f);
            float my = Mathf.Max(0.2f, PlayHalfH - radius - 0.2f);
            float minGap = radius * 2f + 0.45f;

            for (int i = 0; i < n; i++)
            {
                Vector2 p = Vector2.zero;
                for (int attempt = 0; attempt < 80; attempt++)
                {
                    p = new Vector2(Rand(-mx, mx), Rand(-my, my));
                    bool ok = true;
                    for (int j = 0; j < i; j++)
                    {
                        if (Vector2.Distance(p, result[j]) < minGap) { ok = false; break; }
                    }
                    if (ok) break;
                }
                result[i] = p;
            }
            return result;
        }

        /// <summary>A random permutation of 1..n used to number the circles.</summary>
        int[] ShuffledNumbers(int n)
        {
            var nums = new int[n];
            for (int i = 0; i < n; i++) nums[i] = i + 1;
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (nums[i], nums[j]) = (nums[j], nums[i]);
            }
            return nums;
        }

        /// <summary>The circle the player must shoot next.</summary>
        public TargetCircle ActiveTarget()
        {
            foreach (TargetCircle t in targets)
                if (t.Order == orderStep && !t.IsComplete) return t;
            return null;
        }

        // ---- helpers ---------------------------------------------------------

        TargetCircle CircleAt(Vector2 world)
        {
            TargetCircle best = null;
            float bd = float.MaxValue;
            foreach (TargetCircle t in targets)
            {
                float d = Vector2.Distance(world, t.Pos);
                if (d <= t.Radius && d < bd) { bd = d; best = t; }
            }
            return best;
        }

        void TickTargets(float dt)
        {
            foreach (TargetCircle t in targets)
                if (t != null) t.Tick(dt);
        }

        void TickEffects(float dt)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                Effect e = effects[i];
                e.Tick(dt);
                if (e.Dead)
                {
                    Destroy(e.gameObject);
                    effects.RemoveAt(i);
                }
            }
        }

        void ClearEffects()
        {
            foreach (Effect e in effects)
                if (e != null) Destroy(e.gameObject);
            effects.Clear();
        }

        Effect SpawnEffect(Sprite sprite, Vector2 pos, float w, float h, Color color,
            float life, float grow, int order)
        {
            var go = new GameObject("Effect");
            go.transform.SetParent(root, false);
            var e = go.AddComponent<Effect>();
            e.Init(sprite, pos, w, h, color, life, grow, order);
            effects.Add(e);
            return e;
        }

        /// <summary>Wipes a cleared circle off the paper with a small puff.</summary>
        void EraseTarget(TargetCircle t)
        {
            if (t == null) return;
            Vector2 p = t.Pos;
            float r = t.Radius;
            SpawnEffect(Art.Splat, p, r * 2.6f, r * 2.6f,
                new Color(Palette.InkSoft.r, Palette.InkSoft.g, Palette.InkSoft.b, 0.6f), 0.4f, 1.8f, 8);
            targets.Remove(t);
            Destroy(t.gameObject);
        }

        void SetToast(string text, float seconds)
        {
            Toast = text;
            ToastTimer = seconds;
        }

        bool labelsVisibleApplied;
        void UpdateOrderLabels()
        {
            bool show = ShowOrderNumbers;
            if (show == labelsVisibleApplied) return;
            labelsVisibleApplied = show;
            foreach (TargetCircle t in targets)
                if (t != null) t.SetLabelVisible(show);
        }

        // Keeps the paper covering the camera even when the viewport aspect
        // changes (e.g. different webviews on Toss/WebGL), with a small overscan.
        void RefitPaper()
        {
            if (paperSr == null || cam == null) return;
            float h = cam.orthographicSize;
            float w = h * Mathf.Max(cam.aspect, 0.3f);
            Util.FitWH(paperSr, w * 2f * 1.06f, h * 2f * 1.06f);
        }

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // ---- input -----------------------------------------------------------

        void ReadPointer()
        {
            Pointer p = Pointer.current;
            if (p == null || !p.press.wasPressedThisFrame) return;

            Vector2 screen = p.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            OnTap(world);
        }

        public void OnTap(Vector2 world)
        {
            switch (State)
            {
                case GameState.Ready:
                    StartNewGame();
                    break;
                case GameState.Playing:
                    Shoot(world);
                    break;
                case GameState.GameOver:
                    StartNewGame();
                    break;
            }
        }
    }
}
