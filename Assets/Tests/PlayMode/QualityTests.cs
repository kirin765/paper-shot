using NUnit.Framework;
using UnityEngine;
using PaperShot;

namespace PaperShot.Tests
{
    /// <summary>Coverage for the quality pass: audio, feedback, practice, records.</summary>
    public class QualityTests
    {
        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteAll();

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteAll();

        Camera MakeCamera(float aspect = 1080f / 1920f)
        {
            var go = new GameObject("TestCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.aspect = aspect;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            return cam;
        }

        static void Advance(PaperShotGame game, float seconds)
        {
            int steps = Mathf.CeilToInt(seconds * 60f);
            for (int i = 0; i < steps; i++) game.Tick(1f / 60f);
        }

        static void EnterPlaying(PaperShotGame game)
        {
            int guard = 0;
            while ((game.State == GameState.Intro || game.State == GameState.RoundClear) && guard++ < 4000)
                game.Tick(1f / 60f);
        }

        static TargetCircle OtherThan(PaperShotGame game, TargetCircle t)
        {
            foreach (TargetCircle c in game.Targets)
                if (c != t) return c;
            return null;
        }

        // ---- audio -----------------------------------------------------------

        [Test]
        public void SfxSynth_EveryKindProducesAudio()
        {
            foreach (SfxKind kind in System.Enum.GetValues(typeof(SfxKind)))
            {
                float[] samples = SfxSynth.Sample(kind);
                Assert.Greater(samples.Length, 0, kind + " should have samples");
                bool anySound = false;
                foreach (float s in samples)
                {
                    Assert.IsFalse(float.IsNaN(s) || float.IsInfinity(s), kind + " must be finite");
                    if (Mathf.Abs(s) > 0.001f) anySound = true;
                }
                Assert.IsTrue(anySound, kind + " should not be silent");
            }
        }

        [Test]
        public void SfxSynth_CreateReturnsPlayableClip()
        {
            var clip = SfxSynth.Create(SfxKind.Hit);
            Assert.NotNull(clip);
            Assert.Greater(clip.samples, 0);
        }

        // ---- records ---------------------------------------------------------

        [Test]
        public void SaveData_TracksPersonalBests()
        {
            Assert.AreEqual(0, SaveData.BestScore);
            Assert.AreEqual(0, SaveData.BestRound);

            Assert.IsTrue(SaveData.RecordGame(10, 3));
            Assert.AreEqual(10, SaveData.BestScore);
            Assert.AreEqual(3, SaveData.BestRound);

            Assert.IsFalse(SaveData.RecordGame(5, 2), "a weaker run is not a record");
            Assert.AreEqual(10, SaveData.BestScore);

            Assert.IsTrue(SaveData.RecordGame(12, 1), "a higher score is a record");
            Assert.AreEqual(12, SaveData.BestScore);
            Assert.AreEqual(3, SaveData.BestRound);
        }

        [Test]
        public void SaveData_ReducedMotionForcesShakeOff()
        {
            SaveData.ReducedMotion = false;
            SaveData.ScreenShake = true;
            Assert.IsTrue(SaveData.ShakeAllowed);

            SaveData.ReducedMotion = true;
            Assert.IsFalse(SaveData.ShakeAllowed);

            SaveData.ScreenShake = false;
            SaveData.ReducedMotion = false;
            Assert.IsFalse(SaveData.ShakeAllowed);
        }

        [Test]
        public void SaveData_ClampsVolumes()
        {
            SaveData.MusicVolume = 5f;
            SaveData.SfxVolume = -3f;
            Assert.AreEqual(1f, SaveData.MusicVolume, 1e-4f);
            Assert.AreEqual(0f, SaveData.SfxVolume, 1e-4f);
        }

        // ---- practice round --------------------------------------------------

        [Test]
        public void Practice_IsUntimed_ThenHandsOffToRealPlay()
        {
            var cam = MakeCamera();
            var game = PaperShotGame.Create(cam, 7, false, tutorial: true);

            Assert.AreEqual(GameState.Ready, game.State);
            Assert.IsFalse(game.IsPractice);

            game.OnTap(Vector2.zero);
            Assert.AreEqual(GameState.Intro, game.State);
            Assert.IsTrue(game.IsPractice);

            EnterPlaying(game);
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.IsTrue(game.IsPractice);

            // No clock pressure during practice.
            float before = game.TimeLeft;
            Advance(game, 60f);
            Assert.AreEqual(GameState.Playing, game.State, "practice must not time out");
            Assert.AreEqual(before, game.TimeLeft, 1e-3f);

            // Clear the practice order, then it must hand off to a timed round 1.
            int guard = 0;
            while (game.Targets.Count > 0 && guard++ < 100)
                game.Shoot(game.ActiveTarget().Pos);
            game.Tick(1f / 60f);
            Assert.AreEqual(GameState.RoundClear, game.State);

            Advance(game, 2f);
            Assert.AreEqual(GameState.Intro, game.State);
            Assert.IsFalse(game.IsPractice, "practice is not repeated");
            Assert.IsFalse(game.Tutorial);
            Assert.IsTrue(SaveData.TutorialDone);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        // ---- feedback --------------------------------------------------------

        [Test]
        public void WrongShot_AppliesExactPenaltyAndShowsFeedback()
        {
            var cam = MakeCamera();
            var game = PaperShotGame.Create(cam, 7, false);
            game.StartNewGame();
            EnterPlaying(game);

            TargetCircle active = game.ActiveTarget();
            TargetCircle other = OtherThan(game, active);
            float before = game.TimeLeft;

            ShootResult r = game.Shoot(other.Pos);
            Assert.AreEqual(ShootResult.Wrong, r);
            Assert.AreEqual(before - Rules.WrongOrderPenalty, game.TimeLeft, 1e-3f);
            Assert.Greater(game.FeedbackCount, 0, "penalty should be shown on site");

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Accuracy_TracksHitsOverShots()
        {
            var cam = MakeCamera();
            var game = PaperShotGame.Create(cam, 7, false);
            game.StartNewGame();
            EnterPlaying(game);

            game.Shoot(game.ActiveTarget().Pos); // hit
            Assert.AreEqual(1, game.Shots);
            Assert.AreEqual(1, game.Hits);
            Assert.AreEqual(1f, game.Accuracy, 1e-3f);

            game.Shoot(new Vector2(999f, 999f)); // miss
            Assert.AreEqual(2, game.Shots);
            Assert.AreEqual(1, game.Hits);
            Assert.AreEqual(0.5f, game.Accuracy, 1e-3f);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        // ---- difficulty ------------------------------------------------------

        [Test]
        public void Difficulty_EasesOneFactorAtATime()
        {
            for (int round = 1; round <= 12; round++)
            {
                Assert.LessOrEqual(Rules.OrderRevealSeconds(round), Rules.OrderRevealSeconds(round + 1) + 1e-4f);
                Assert.GreaterOrEqual(Rules.RoundSeconds(round), Rules.RoundSeconds(round + 1) - 1e-4f);
            }
            Assert.GreaterOrEqual(Rules.RoundSeconds(50), 9f);
            Assert.Greater(Rules.WrongOrderPenalty, 0f);
            Assert.Greater(Rules.TargetSpacing(0.5f), 1.0f, "spacing exceeds diameter");
        }
    }
}
