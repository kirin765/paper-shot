using NUnit.Framework;
using UnityEngine;
using PaperShot;

namespace PaperShot.Tests
{
    public class GameTests
    {
        Camera MakeCamera()
        {
            var go = new GameObject("TestCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.aspect = 1080f / 1920f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            return cam;
        }

        PaperShotGame MakeGame(Camera cam, int seed = 42)
        {
            var game = PaperShotGame.Create(cam, seed, false);
            game.StartNewGame();
            return game;
        }

        static void Advance(PaperShotGame game, float seconds)
        {
            int steps = Mathf.CeilToInt(seconds * 60f);
            for (int i = 0; i < steps; i++) game.Tick(1f / 60f);
        }

        static void EnterPlaying(PaperShotGame game)
        {
            int guard = 0;
            while ((game.State == GameState.Intro || game.State == GameState.RoundClear) && guard++ < 2000)
                game.Tick(1f / 60f);
        }

        static void ClearRound(PaperShotGame game)
        {
            EnterPlaying(game);
            Assert.AreEqual(GameState.Playing, game.State);
            int guard = 0;
            while (game.Targets.Count > 0 && guard++ < 100)
            {
                TargetCircle a = game.ActiveTarget();
                Assert.NotNull(a, "active target should exist while circles remain");
                game.Shoot(a.Pos);
            }
            game.Tick(1f / 60f);
        }

        static TargetCircle OtherThan(PaperShotGame game, TargetCircle t)
        {
            foreach (TargetCircle c in game.Targets)
                if (c != t) return c;
            return null;
        }

        [Test]
        public void Rules_MoreCirclesAsRoundsRise()
        {
            Assert.AreEqual(2, Rules.TargetsForRound(1));
            Assert.AreEqual(2, Rules.TargetsForRound(2));
            Assert.AreEqual(3, Rules.TargetsForRound(3));
            Assert.AreEqual(4, Rules.TargetsForRound(5));
            Assert.AreEqual(5, Rules.TargetsForRound(7));
            Assert.AreEqual(6, Rules.TargetsForRound(9));
            Assert.AreEqual(6, Rules.TargetsForRound(20));
            Assert.AreEqual(Rules.TargetsForRound(5), Rules.HitsRequired(5));
        }

        [Test]
        public void ProcArt_GeneratesSprites()
        {
            Assert.NotNull(Art.Ring);
            Assert.NotNull(Art.Splat);
            Assert.NotNull(Art.Hole);
            Assert.NotNull(Art.PaperFor(1080f / 1920f));
        }

        [Test]
        public void Game_BootsIntoReadyAndStartsOnTap()
        {
            var cam = MakeCamera();
            var game = PaperShotGame.Create(cam, 1, false);

            Assert.AreEqual(GameState.Ready, game.State);
            Assert.AreEqual(Rules.TargetsForRound(1), game.Targets.Count);

            game.OnTap(Vector2.zero);
            Assert.AreEqual(GameState.Intro, game.State);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Game_RevealsTheOrderThenHidesIt()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);

            Assert.AreEqual(GameState.Intro, game.State);
            Assert.IsTrue(game.ShowOrderNumbers);

            Advance(game, Rules.OrderRevealSeconds(1) + 0.3f);
            Assert.AreEqual(GameState.Playing, game.State);
            Assert.IsFalse(game.ShowOrderNumbers);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Targets_AreRandomWithinBounds()
        {
            var cam = MakeCamera();
            var a = PaperShotGame.Create(cam, 11, false);
            a.StartNewGame();
            var b = PaperShotGame.Create(cam, 22, false);
            b.StartNewGame();

            Assert.AreNotEqual(a.Targets[0].Pos, b.Targets[0].Pos);
            foreach (TargetCircle t in a.Targets)
            {
                Assert.LessOrEqual(Mathf.Abs(t.Pos.x), a.PlayHalfW);
                Assert.LessOrEqual(Mathf.Abs(t.Pos.y), a.PlayHalfH);
            }

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(a.gameObject);
            Object.DestroyImmediate(b.gameObject);
        }

        [Test]
        public void Layout_IsClampedToNineBySixteen()
        {
            var cam = MakeCamera();
            cam.aspect = 1.5f; // a wide viewport must still use a 9:16 play column
            var game = PaperShotGame.Create(cam, 7, false);
            game.StartNewGame();

            Assert.AreEqual(PaperShotGame.DesignHalfHeight, cam.orthographicSize, 1e-4f);

            float designHalfW = PaperShotGame.DesignHalfHeight * PaperShotGame.DesignAspect;
            float expected = Mathf.Min(designHalfW - 0.4f, game.PlayHalfH * 0.72f);
            Assert.AreEqual(expected, game.PlayHalfW, 1e-3f);
            Assert.Less(game.PlayHalfW, cam.orthographicSize * 1.5f - 0.4f);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Targets_DoNotOverlap()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);
            for (int i = 0; i < game.Targets.Count; i++)
                for (int j = i + 1; j < game.Targets.Count; j++)
                {
                    float min = game.Targets[i].Radius + game.Targets[j].Radius;
                    Assert.GreaterOrEqual(Vector2.Distance(game.Targets[i].Pos, game.Targets[j].Pos), min - 1e-3f);
                }

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Shooting_CorrectCircleScoresAndErasesIt()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);
            EnterPlaying(game);

            int before = game.Targets.Count;
            TargetCircle active = game.ActiveTarget();
            Assert.AreEqual(1, active.Order);

            ShootResult r = game.Shoot(active.Pos);
            Assert.AreEqual(ShootResult.Hit, r);
            Assert.AreEqual(before - 1, game.Targets.Count);
            Assert.AreEqual(1, game.Score);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Shooting_WrongCircleIsPenalized()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);
            EnterPlaying(game);

            TargetCircle active = game.ActiveTarget();
            TargetCircle other = OtherThan(game, active);
            Assert.NotNull(other);

            float before = game.TimeLeft;
            ShootResult r = game.Shoot(other.Pos);
            Assert.AreEqual(ShootResult.Wrong, r);
            Assert.Less(game.TimeLeft, before);
            Assert.AreEqual(0, game.Score);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Shooting_EmptyPaperLeavesAHoleButNoProgress()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);
            EnterPlaying(game);

            int effectsBefore = game.EffectCount;
            ShootResult r = game.Shoot(new Vector2(999f, 999f));

            Assert.AreEqual(ShootResult.Miss, r);
            Assert.AreEqual(0, game.Score);
            Assert.Greater(game.EffectCount, effectsBefore); // hole + flash were spawned

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Round_ClearsWhenAllShotInOrder()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);
            ClearRound(game);
            Assert.AreEqual(0, game.Targets.Count);
            Assert.Greater(game.Score, 0);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void Round_AdvancesWithMoreCircles()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);
            ClearRound(game); // 1
            ClearRound(game); // 2
            ClearRound(game); // 3
            EnterPlaying(game); // into round 4

            Assert.AreEqual(4, game.Round);
            Assert.AreEqual(Rules.TargetsForRound(4), game.Targets.Count);
            Assert.Greater(Rules.TargetsForRound(4), Rules.TargetsForRound(1));

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [Test]
        public void GameOver_WhenTimeExpires()
        {
            var cam = MakeCamera();
            var game = MakeGame(cam, 7);
            EnterPlaying(game);

            Advance(game, Rules.RoundSeconds(1) + 0.5f);
            Assert.AreEqual(GameState.GameOver, game.State);

            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }
    }
}
