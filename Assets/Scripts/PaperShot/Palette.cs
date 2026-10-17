using UnityEngine;

namespace PaperShot
{
    /// <summary>Aged-paper colour scheme for the whole game.</summary>
    public static class Palette
    {
        public static readonly Color PaperLight = new Color(0.961f, 0.937f, 0.871f);
        public static readonly Color PaperDark = new Color(0.847f, 0.796f, 0.678f);
        public static readonly Color PaperStain = new Color(0.612f, 0.545f, 0.396f);
        public static readonly Color Ink = new Color(0.149f, 0.129f, 0.110f);
        public static readonly Color InkSoft = new Color(0.325f, 0.290f, 0.247f);
        /// <summary>Target line circles are drawn in a clear ink-blue.</summary>
        public static readonly Color Target = new Color(0.145f, 0.227f, 0.451f);
        public static readonly Color Shadow = new Color(0.086f, 0.082f, 0.078f);
        /// <summary>Decoy shadows are clearly warmer/browner than real ones.</summary>
        public static readonly Color FakeShadow = new Color(0.302f, 0.180f, 0.094f);
        public static readonly Color Accent = new Color(0.706f, 0.192f, 0.153f);
        public static readonly Color Good = new Color(0.149f, 0.443f, 0.290f);
    }
}
