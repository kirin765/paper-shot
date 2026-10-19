using NUnit.Framework;
using UnityEngine;
using PaperShot;

namespace PaperShot.Tests
{
    public class BgmTests
    {
        [Test]
        public void Clip_IsAvailableInResources()
        {
            var clip = Resources.Load<AudioClip>(Bgm.ClipResourcePath);
            Assert.NotNull(clip, "BGM missing at Resources/" + Bgm.ClipResourcePath);
        }

        [Test]
        public void Bgm_LoadsClipAndLoops()
        {
            var go = new GameObject("BgmTest");
            var bgm = go.AddComponent<Bgm>();

            Assert.NotNull(bgm.Clip, "Bgm should load its clip on Awake");
            Assert.IsTrue(bgm.IsLooping, "background music should loop");

            Object.DestroyImmediate(go);
        }
    }
}
