using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PaperShot.Tests
{
    /// <summary>
    /// Renders the game world into PNGs for visual inspection.
    /// Run with: unity test --mode PlayMode --filter PaperShot.Tests.PreviewTests
    /// </summary>
    public class PreviewTests
    {
        static void Step(PaperShotGame game, float seconds)
        {
            int steps = Mathf.CeilToInt(seconds * 60f);
            for (int i = 0; i < steps; i++) game.Tick(1f / 60f);
        }

        static Camera MakeCamera(string name, int w, int h, float ortho)
        {
            var go = new GameObject(name);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = ortho;
            cam.aspect = (float)w / h;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.PaperDark;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            return cam;
        }

        static void Save(RenderTexture rt, int w, int h, string path)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void WaitFor(PaperShotGame game, GameState state)
        {
            int guard = 0;
            while (game.State != state && guard++ < 3000) game.Tick(1f / 60f);
        }

        static void ClearRound(PaperShotGame game)
        {
            WaitFor(game, GameState.Playing);
            int guard = 0;
            while (game.Targets.Count > 0 && guard++ < 200)
            {
                TargetCircle a = game.ActiveTarget();
                game.Shoot(a.Pos);
            }
            WaitFor(game, GameState.Intro); // into the next round's intro
        }

        [UnityTest, Category("Preview")]
        public IEnumerator RenderReveal()
        {
            int w = 900, h = 1600;
            Camera cam = MakeCamera("PreviewCamera1", w, h, 6f);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var game = PaperShotGame.Create(cam, 99, false);
            game.StartNewGame();
            ClearRound(game);
            ClearRound(game);
            ClearRound(game); // now at the intro of round 4 (4 numbered circles)

            yield return null;
            yield return null;
            Save(rt, w, h, "/tmp/papershot-reveal.png");

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }

        [UnityTest, Category("Preview")]
        public IEnumerator RenderShots()
        {
            int w = 900, h = 1600;
            Camera cam = MakeCamera("PreviewCamera2", w, h, 6f);
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var game = PaperShotGame.Create(cam, 7, false);
            game.StartNewGame();
            WaitFor(game, GameState.Playing); // numbers hidden, ready to shoot

            // shoot the first two circles in order, each leaves a bullet hole
            for (int i = 0; i < 2 && game.Targets.Count > 0; i++)
            {
                TargetCircle a = game.ActiveTarget();
                game.Shoot(a.Pos);
            }

            yield return null;
            yield return null;
            Save(rt, w, h, "/tmp/papershot-shots.png");

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(game.gameObject);
        }
    }
}
