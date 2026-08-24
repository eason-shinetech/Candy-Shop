using UnityEngine;

namespace CandyShop
{
    // Coin and reward tuning. Defaults from design spec sections 7 and 8.
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "CandyShop/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Serve reward: baseReward + totalCandies * perCandy + speedRatio * speedBonusMax, min minReward")]
        public int baseReward = 8;
        public int perCandy = 1;
        public int speedBonusMax = 24;
        public int minReward = 10;

        [Header("Perfect serve")]
        public int perfectBonus = 5;

        [Header("Daily sign-in")]
        public int dailyCoins = 200;             // spec constant
        public int streakRecipeDay = 7;
        public int allUnlockedBonus = 500;
        public int dailyExtraAdCoins = 50;

        [Header("Recipes")]
        public int recipeBaseCost = 120;
        public int recipeCostStep = 60;

        [Header("Ads coin grants")]
        public int adCoinGrant = 80;
    }
}
