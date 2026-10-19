using NUnit.Framework;
using UnityEngine;
using PaperShot;

namespace PaperShot.Tests
{
    /// <summary>
    /// Guards the HUD geometry: text must scale with the 9:16 column (not the
    /// raw screen), stay on-screen, and never overlap.
    /// </summary>
    public class HudLayoutTests
    {
        const float Eps = 1e-3f;

        static HudLayout Compute(float w, float h, bool playing, bool hasTitle,
            string sub, bool hasToast)
        {
            return HudLayoutBuilder.Compute(w, h, playing, hasTitle, sub, hasToast);
        }

        static void AssertOrdered(float top, float bottom, string what)
        {
            Assert.LessOrEqual(top, bottom + Eps, what + " should sit above the next block");
        }

        [Test]
        public void Unit_ScalesWithColumnWidth_NotFloored()
        {
            var portrait = Compute(1080f, 1920f, false, true, "TAP TO START", false);
            Assert.AreEqual(1080f / HudLayoutBuilder.DesignWidth, portrait.Unit, Eps,
                "unit scales with the 9:16 column width");

            // A small viewport must not be blown up by a 0.6 minimum.
            var small = Compute(400f, 700f, false, true, "TAP TO START", false);
            Assert.Less(small.Unit, 0.6f, "small viewports scale the HUD down");
            Assert.AreEqual(393.75f / HudLayoutBuilder.DesignWidth, small.Unit, Eps);
        }

        [Test]
        public void TopRows_DoNotOverlap_WhenPlaying()
        {
            foreach (Vector2 screen in new[]
            {
                new Vector2(1080f, 1920f),
                new Vector2(400f, 700f),
                new Vector2(1920f, 1080f),
                new Vector2(1000f, 1000f),
            })
            {
                HudLayout l = Compute(screen.x, screen.y, true, false, null, false);
                string tag = screen.x + "x" + screen.y;
                AssertOrdered(l.TimerBar.yMax, l.RoundLabel.y, tag + " timer/round");
                AssertOrdered(l.RoundLabel.yMax, l.ShootLabel.y, tag + " round/shoot");
                Assert.AreEqual(l.ShootLabel.yMax, l.ShootLabel.y + l.ShootLabel.height, Eps);
            }
        }

        [Test]
        public void Banner_StaysOnScreen_AndBelowTopBlock()
        {
            foreach (Vector2 screen in new[]
            {
                new Vector2(1080f, 1920f),
                new Vector2(400f, 700f),
                new Vector2(1920f, 1080f),
            })
            {
                HudLayout l = Compute(screen.x, screen.y, false, true,
                    "TAP TO START\nmemorize the numbered circles, then shoot them in order", false);
                string tag = screen.x + "x" + screen.y;

                Assert.GreaterOrEqual(l.TitleRect.y, -Eps, tag + " title top");
                Assert.LessOrEqual(l.SubRect.yMax, screen.y + Eps, tag + " sub bottom");
                AssertOrdered(l.RoundLabel.yMax, l.TitleRect.y, tag + " round/banner");
                AssertOrdered(l.TitleRect.yMax, l.SubRect.y, tag + " title/sub");

                // The banner stays inside the centred 9:16 column.
                Assert.GreaterOrEqual(l.SubRect.x, l.Column.x - Eps, tag + " sub left");
                Assert.LessOrEqual(l.SubRect.xMax, l.Column.xMax + Eps, tag + " sub right");
            }
        }

        [Test]
        public void Toast_DoesNotOverlapBanner_OnShortScreens()
        {
            foreach (Vector2 screen in new[]
            {
                new Vector2(1080f, 1920f),
                new Vector2(1000f, 600f),
                new Vector2(1920f, 1080f),
            })
            {
                HudLayout l = Compute(screen.x, screen.y, false, true,
                    "SCORE 0   -   TAP TO RETRY", true);
                string tag = screen.x + "x" + screen.y;
                AssertOrdered(l.RoundLabel.yMax, l.ToastRect.y, tag + " round/toast");
                AssertOrdered(l.ToastRect.yMax, l.TitleRect.y, tag + " toast/banner");
            }
        }

        [Test]
        public void Toast_StaysOnScreen_WhenPlaying()
        {
            HudLayout l = Compute(1080f, 1920f, true, false, null, true);
            AssertOrdered(l.RoundLabel.yMax, l.ToastRect.y, "round/toast");
            AssertOrdered(l.ShootLabel.yMax, l.ToastRect.y, "shoot/toast");
            Assert.LessOrEqual(l.ToastRect.yMax, 1920f + Eps, "toast bottom on-screen");
        }
    }
}
