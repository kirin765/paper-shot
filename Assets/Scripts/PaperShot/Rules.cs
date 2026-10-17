using UnityEngine;

namespace PaperShot
{
    /// <summary>
    /// Memory-game rules: each round shows a random set of numbered circles,
    /// then hides the numbers and the player must shoot them back in order.
    /// Higher rounds bring more circles (a longer order).
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

        /// <summary>How long the order numbers stay visible before hiding (memory window).</summary>
        public static float OrderRevealSeconds(int round) => 0.6f + 0.35f * TargetsForRound(round);

        public static float RoundSeconds(int round) => Mathf.Max(10f, 20f - (round - 1) * 0.5f);

        public static float WrongOrderPenalty => 1.0f;
    }
}
