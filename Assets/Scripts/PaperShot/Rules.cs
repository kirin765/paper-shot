using UnityEngine;

namespace PaperShot
{
    /// <summary>
    /// Memory-game rules: each round shows a random set of numbered circles,
    /// then hides the numbers and the player must shoot them back in order.
    /// Difficulty rises one factor at a time — more circles, then a shorter
    /// memory window and timer relative to that bigger order — so no round
    /// ever spikes two pressures at once.
    /// </summary>
    public static class Rules
    {
        /// <summary>Number of circles in a round — the length of the shot order.</summary>
        public static int TargetsForRound(int round)
        {
            return Mathf.Clamp(1 + (round + 1) / 2, 2, 6);
        }

        public static int HitsRequired(int round) => TargetsForRound(round);

        /// <summary>Each circle is shot once, so the radius shrinks as more appear.</summary>
        public static float TargetRadius(int round)
        {
            switch (TargetsForRound(round))
            {
                case 2: return 0.62f;
                case 3: return 0.56f;
                case 4: return 0.50f;
                case 5: return 0.46f;
                default: return 0.42f;
            }
        }

        /// <summary>Minimum centre distance between circles, so layouts never crowd.</summary>
        public static float TargetSpacing(float radius) => radius * 2f + 0.5f;

        /// <summary>How long the order numbers stay visible before hiding (memory window).</summary>
        public static float OrderRevealSeconds(int round)
        {
            // budget grows with the order so every circle gets a fair look.
            return 0.9f + 0.55f * TargetsForRound(round);
        }

        /// <summary>Time allowed to shoot a round; eases down slowly, floors at 9s.</summary>
        public static float RoundSeconds(int round) => Mathf.Max(9f, 18f - (round - 1) * 0.5f);

        /// <summary>Seconds removed for shooting the wrong circle out of order.</summary>
        public static float WrongOrderPenalty => 1.5f;

        /// <summary>Base world size of a bullet-hole decal (varied slightly per shot).</summary>
        public const float DecalSize = 0.72f;

        /// <summary>Extra world radius added to taps, so fingers don't have to be pixel-perfect.</summary>
        public const float TouchPadding = 0.06f;
    }
}
