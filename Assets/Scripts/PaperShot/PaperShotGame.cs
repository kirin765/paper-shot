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
        /// <summary>Portrait design aspect (9:16).</summary>
        public const float DesignAspect = 9f / 16f;
        /// <summary>Vertical framing is locked to this half-height (world units).</summary>
        public const float DesignHalfHeight = 6f;

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

        /// <summary>First-time practice round (Remember → Wait → Shoot, untimed).</summary>
        public bool Tutorial { get; private set; }
        public bool IsPractice { get; private set; }

        /// <summary>Run stats for the results screen.</summary>
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public float Accuracy => Shots > 0 ? (float)Hits / Shots : 1f;
        public bool NewBest { get; private set; }

        /// <summary>Set by the HUD while a settings panel is open; blocks play input.</summary>
        public bool InputBlocked { get; set; }

        public int CurrentOrderStep => orderStep;
        public bool ShowOrderNumbers => orderRevealTimer > 0f;
        public float OrderRevealAlpha => orderRevealTimer <= 0f ? 0f : Mathf.Clamp01(orderRevealTimer / 0.5f);
        public Camera Camera => cam;
        public IReadOnlyList<TargetCircle> Targets => targets;
        public int EffectCount => effects.Count;
        public int FeedbackCount => floating.Count;

        public float PlayHalfW { get; private set; }
        public float PlayHalfH { get; private set; }

        readonly List<TargetCircle> targets = new List<TargetCircle>();
        readonly List<Effect> effects = new List<Effect>();
        readonly List<FloatingText> floating = new List<FloatingText>();

        Camera cam;
        Vector3 camBasePos;
        System.Random rng;
        Transform root;
        Hud hud;
        Bgm bgm;
        SpriteRenderer paperSr;
        float halfW, halfH;
        float phaseTimer;
        int orderStep;
        float orderRevealTimer;
        int lastTickSecond = -1;
        bool paused;

        // shot feel
        float shake;
        float paperKick;
        Vector2 kickDir;

        void OnDestroy()
        {
            RestoreCamera();
        }

        // ---- lifecycle -------------------------------------------------------

        public static PaperShotGame Create(Camera camera, int seed = 12345,
            bool autoTick = true, bool tutorial = false)
        {
            var go = new GameObject("PaperShotGame");
            var game = go.AddComponent<PaperShotGame>();
            game.AutoTick = autoTick;
            game.Tutorial = tutorial;
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

            // Lock vertical framing and keep the play field inside a 9:16 column.
            cam.orthographicSize = DesignHalfHeight;
            camBasePos = cam.transform.position;
            halfH = DesignHalfHeight;
            float actualHalfW = halfH * Mathf.Max(cam.aspect, 0.3f);
            float designHalfW = halfH * DesignAspect;
            halfW = Mathf.Min(actualHalfW, designHalfW);

            PlayHalfH = halfH - 0.4f;
            PlayHalfW = Mathf.Min(halfW - 0.4f, PlayHalfH * 0.72f);

            root = new GameObject("PaperShotRoot").transform;
            root.SetParent(transform, false);

            var paper = Util.NewSprite("Paper", Art.PaperFor(halfW * 2f / (halfH * 2f)), -100, root, Color.white);
            Util.FitWH(paper, halfW * 2f, halfH * 2f);
            paper.transform.position = Vector3.zero;
            paperSr = paper;

            var hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(transform, false);
            hud = hudGo.AddComponent<Hud>();
            hud.Game = this;

            var sfxGo = new GameObject("Sfx");
            sfxGo.transform.SetParent(transform, false);
            sfxGo.AddComponent<Sfx>();

            var bgmGo = new GameObject("Bgm");
            bgmGo.transform.SetParent(transform, false);
            bgm = bgmGo.AddComponent<Bgm>();
            hud.Bgm = bgm;

            ResetToReady();
        }

        void Update()
        {
            if (!AutoTick || paused) return;
            ReadPointer();
            Tick(Mathf.Min(Time.deltaTime, 1f / 15f));
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                paused = false;
            }
            else if (State != GameState.Ready)
            {
                paused = true;
                RestoreCamera();
            }
        }

        void OnApplicationPause(bool pause)
        {
            if (!pause) return;
            paused = State != GameState.Ready;
            RestoreCamera();
        }

        // ---- public API ------------------------------------------------------

        public void StartNewGame()
        {
            Round = 1;
            Score = 0;
            Shots = 0;
            Hits = 0;
            NewBest = false;
            StartRound();
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) dt = 1e-4f;
            if (ToastTimer > 0f) ToastTimer = Mathf.Max(0f, ToastTimer - dt);
            if (orderRevealTimer > 0f) orderRevealTimer = Mathf.Max(0f, orderRevealTimer - dt);
            UpdateOrderLabels();
            RefitPaper();
            TickShake(dt);
            TickPaperKick(dt);

            switch (State)
            {
                case GameState.Intro:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) State = GameState.Playing;
                    break;

                case GameState.Playing:
                    if (!IsPractice) TickTimer(dt);
                    if (RoundHits >= RoundHitsRequired)
                    {
                        Sfx.Play(SfxKind.RoundClear);
                        State = GameState.RoundClear;
                        phaseTimer = 1.5f;
                    }
                    else if (!IsPractice && TimeLeft <= 0f)
                    {
                        EndGame();
                    }
                    break;

                case GameState.RoundClear:
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f)
                    {
                        if (IsPractice)
                        {
                            Tutorial = false;
                            SaveData.TutorialDone = true;
                        }
                        else
                        {
                            Round++;
                        }
                        StartRound();
                    }
                    break;
            }

            TickTargets(dt);
            TickEffects(dt);
            TickFloating(dt);
        }

        void TickTimer(float dt)
        {
            TimeLeft = Mathf.Max(0f, TimeLeft - dt);
            int sec = Mathf.CeilToInt(TimeLeft);
            if (sec >= 1 && sec <= 5 && sec != lastTickSecond)
            {
                lastTickSecond = sec;
                Sfx.Play(SfxKind.Tick, 0.7f);
            }
        }

        /// <summary>
        /// Fire at a world position. The gun always leaves a bullet hole where
        /// you shot; the shot scores only if it lands on the next circle.
        /// </summary>
        public ShootResult Shoot(Vector2 world)
        {
            if (State != GameState.Playing) return ShootResult.Ignored;
            Shots++;

            // A real bullet hole, sized and rotated a little differently each time.
            float size = Rules.DecalSize * Random.Range(0.85f, 1.15f);
            var hole = SpawnEffect(Art.Hole, world, size, size, Color.white, 999f, 1f, -50);
            hole.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            // Crisp impact: a muzzle flash plus a quick expanding ring.
            SpawnEffect(Art.SplatWhite, world, 0.5f, 0.5f, WithAlpha(Palette.Accent, 0.9f),
                0.18f, 0.85f, 12);
            SpawnEffect(Art.RingWhite, world, 0.5f, 0.5f, WithAlpha(Palette.Accent, 0.5f),
                0.22f, 2.1f, 11);

            KickPaper();
            Shake(0.8f);
            Sfx.Play(SfxKind.Shot);

            TargetCircle tapped = CircleAt(world);
            if (tapped == null)
            {
                // Empty paper: a dull thud, distinct from a scoring hit.
                Sfx.Play(SfxKind.Miss);
                SpawnFloating("MISS", world, Palette.InkSoft, 0.3f, 0.7f, 0.5f);
                return ShootResult.Miss;
            }

            if (tapped == ActiveTarget())
            {
                Vector2 p = tapped.Pos;
                float r = tapped.Radius;
                Score++;
                RoundHits++;
                Hits++;
                orderStep++;
                tapped.Flash(Palette.Good);
                SpawnEffect(Art.RingWhite, p, r * 2.6f, r * 2.6f, WithAlpha(Palette.Good, 0.85f),
                    0.34f, 1.7f, 10);
                Sfx.Play(SfxKind.Hit);
                EraseTarget(tapped); // cleared circles are wiped off the paper
                return ShootResult.Hit;
            }

            // Wrong order: red flash on the circle, a buzz, and the penalty shown on site.
            float penalty = Rules.WrongOrderPenalty;
            TimeLeft = Mathf.Max(0f, TimeLeft - penalty);
            tapped.Flash(Palette.Accent);
            SpawnEffect(Art.RingWhite, tapped.Pos, tapped.Radius * 2.4f, tapped.Radius * 2.4f,
                WithAlpha(Palette.Accent, 0.8f), 0.32f, 1.6f, 10);
            SetToast("WRONG ORDER", 0.8f);
            Sfx.Play(SfxKind.Wrong);
            SpawnFloating("-" + penalty.ToString("0.0") + "s", world, Palette.Accent, 0.34f, 1.0f, 0.7f);
            Haptics.Buzz();
            return ShootResult.Wrong;
        }

        // ---- state transitions ----------------------------------------------

        public void ResetToReady()
        {
            State = GameState.Ready;
            Score = 0;
            Round = 1;
            Shots = 0;
            Hits = 0;
            NewBest = false;
            IsPractice = false;
            RoundHits = 0;
            RoundHitsRequired = Rules.HitsRequired(1);
            RoundTime = Rules.RoundSeconds(1);
            TimeLeft = RoundTime;
            orderStep = 1;
            orderRevealTimer = 0f;
            lastTickSecond = -1;
            BuildTargetsFor(1);
            ClearEffects();
            ClearFloating();
            UpdateOrderLabels();
        }

        void StartRound()
        {
            IsPractice = Tutorial && Round == 1;
            RoundHits = 0;
            RoundHitsRequired = Rules.HitsRequired(Round);
            RoundTime = Rules.RoundSeconds(Round);
            TimeLeft = RoundTime;
            orderStep = 1;
            lastTickSecond = -1;
            BuildTargetsFor(Round);
            ClearEffects();
            ClearFloating();

            float reveal = Rules.OrderRevealSeconds(Round);
            State = GameState.Intro;
            phaseTimer = reveal;
            orderRevealTimer = reveal;
            UpdateOrderLabels();
        }

        void EndGame()
        {
            State = GameState.GameOver;
            RestoreCamera();
            NewBest = SaveData.RecordGame(Score, Round);
            Sfx.Play(SfxKind.GameOver);
            Haptics.Buzz();
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
            float margin = radius + 0.35f; // keep circles off the paper edge
            float mx = Mathf.Max(0.2f, PlayHalfW - margin);
            float my = Mathf.Max(0.2f, PlayHalfH - margin);
            float minGap = Rules.TargetSpacing(radius);

            for (int i = 0; i < n; i++)
            {
                Vector2 p = Vector2.zero;
                for (int attempt = 0; attempt < 120; attempt++)
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
                if (d <= t.Radius + Rules.TouchPadding && d < bd) { bd = d; best = t; }
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

        void TickFloating(float dt)
        {
            for (int i = floating.Count - 1; i >= 0; i--)
            {
                FloatingText f = floating[i];
                f.Tick(dt);
                if (f.Dead)
                {
                    Destroy(f.gameObject);
                    floating.RemoveAt(i);
                }
            }
        }

        void ClearEffects()
        {
            foreach (Effect e in effects)
                if (e != null) Destroy(e.gameObject);
            effects.Clear();
        }

        void ClearFloating()
        {
            foreach (FloatingText f in floating)
                if (f != null) Destroy(f.gameObject);
            floating.Clear();
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

        void SpawnFloating(string text, Vector2 pos, Color color, float size,
            float life, float rise)
        {
            var go = new GameObject("Feedback");
            go.transform.SetParent(root, false);
            var f = go.AddComponent<FloatingText>();
            f.Init(text, pos, color, size, life, rise);
            floating.Add(f);
        }

        /// <summary>Wipes a cleared circle off the paper (the bullet hole remains).</summary>
        void EraseTarget(TargetCircle t)
        {
            if (t == null) return;
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

        // ---- shot feel -------------------------------------------------------

        void Shake(float amount)
        {
            if (!SaveData.ShakeAllowed) return;
            shake = Mathf.Max(shake, amount);
        }

        void TickShake(float dt)
        {
            if (shake <= 0f)
            {
                RestoreCamera();
                return;
            }
            if (!SaveData.ShakeAllowed)
            {
                shake = 0f;
                RestoreCamera();
                return;
            }
            shake = Mathf.Max(0f, shake - dt * 4f);
            Vector2 o = Random.insideUnitCircle * (0.045f * shake);
            cam.transform.position = camBasePos + new Vector3(o.x, o.y, 0f);
        }

        void RestoreCamera()
        {
            shake = 0f;
            if (cam != null) cam.transform.position = camBasePos;
        }

        void KickPaper()
        {
            if (SaveData.ReducedMotion) return;
            paperKick = 1f;
            kickDir = Random.insideUnitCircle.normalized;
        }

        void TickPaperKick(float dt)
        {
            if (paperSr == null) return;
            if (paperKick > 0f)
            {
                paperKick = Mathf.Max(0f, paperKick - dt * 7f);
                Vector3 o = kickDir * (0.035f * paperKick);
                paperSr.transform.localPosition = o;
            }
            else if (paperSr.transform.localPosition != Vector3.zero)
            {
                paperSr.transform.localPosition = Vector3.zero;
            }
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        // ---- input -----------------------------------------------------------

        void ReadPointer()
        {
            Pointer p = Pointer.current;
            if (p == null || !p.press.wasPressedThisFrame) return;

            Vector2 screen = p.position.ReadValue();
            if (hud != null && hud.CapturesPointer(screen)) return;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            OnTap(world);
        }

        public void OnTap(Vector2 world)
        {
            if (InputBlocked) return;

            // First tap doubles as the user gesture that unblocks WebGL audio.
            if (bgm != null) bgm.Resume();

            switch (State)
            {
                case GameState.Ready:
                    Sfx.Play(SfxKind.Start);
                    StartNewGame();
                    break;
                case GameState.Playing:
                    Shoot(world);
                    break;
                case GameState.GameOver:
                    Sfx.Play(SfxKind.Start);
                    StartNewGame();
                    break;
            }
        }
    }
}
