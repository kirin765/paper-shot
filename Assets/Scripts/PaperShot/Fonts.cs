using UnityEngine;

namespace PaperShot
{
    /// <summary>Shared runtime font lookup for TextMesh labels.</summary>
    public static class Fonts
    {
        static Font _runtime;

        public static Font Runtime
        {
            get
            {
                if (_runtime == null)
                {
                    _runtime = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _runtime;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => _runtime = null;
    }
}
