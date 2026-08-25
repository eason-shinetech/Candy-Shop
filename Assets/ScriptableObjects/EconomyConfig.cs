using UnityEngine;

namespace CandyShop
{
    // Coin and reward tuning. Defaults from design spec sections 7 and 8.
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "CandyShop/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Serve reward: baseReward + totalCandies * perCandy + speedRatio * speedBonusMax, min minReward")]
        public int baseReward = 32;             // ×4 from original 8
        public int perCandy = 4;                // ×4 from original 1
        public int speedBonusMax = 96;          // ×4 from original 24
        public int minReward = 40;              // ×4 from original 10

        [Header("Perfect serve")]
        public int perfectBonus = 25;

        [Header("Daily sign-in")]
        public int dailyCoins = 500;
        public int streakRecipeDay = 7;
        public int allUnlockedBonus = 500;
        public int dailyExtraAdCoins = 50;
        // Extra stamina when dailyStreak hits streakRecipeDay (applied after date refresh; may soft-overflow).
        public int streakSevenStaminaGrant = 5;

        [Header("Recipes")]
        public int recipeBaseCost = 120;
        public int recipeCostStep = 60;

        [Header("Ads coin grants")]
        public int adCoinGrant = 80;

        [Header("Fail coin penalty (supplements 2.0)")]
        [Tooltip("Confirmed fail deducts this ratio of this run's serve earnings.")]
        [Range(0f, 1f)] public float failCoinPenaltyRatio = 0.25f;
        [Tooltip("Minimum penalty when the wallet can cover it (also applied when runEarned is 0).")]
        public int failCoinPenaltyMin = 20;
    }
}
