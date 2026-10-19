using UnityEngine;

namespace PaperShot
{
    /// <summary>
    /// Looping background music. Loads the clip from Resources and starts it
    /// on the first user tap (WebGL/browsers block autoplay before a gesture).
    /// </summary>
    public class Bgm : MonoBehaviour
    {
        /// <summary>Resources path (no extension) of the background track.</summary>
        public const string ClipResourcePath = "Audio/Sneaky Snitch";

        public float Volume = 0.45f;

        AudioSource source;

        public AudioClip Clip => source != null ? source.clip : null;
        public bool IsLooping => source != null && source.loop;
        public bool IsPlaying => source != null && source.isPlaying;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = Resources.Load<AudioClip>(ClipResourcePath);
            if (source.clip == null)
                Debug.LogWarning("[PaperShot] BGM clip missing: Resources/" + ClipResourcePath);
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            ApplySettings();
            Resume();
        }

        /// <summary>Applies the saved music volume (safe to call when it changes).</summary>
        public void ApplySettings()
        {
            Volume = SaveData.MusicVolume;
            if (source != null) source.volume = Volume;
        }

        /// <summary>(Re)starts playback. Safe to call on every interaction.</summary>
        public void Resume()
        {
            if (source == null || source.clip == null) return;
            if (!source.isPlaying) source.Play();
        }
    }
}
