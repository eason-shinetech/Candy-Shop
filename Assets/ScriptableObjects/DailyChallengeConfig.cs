using UnityEngine;

namespace CandyShop
{
    // Daily featured-recipe challenge numbers. Rules in design spec section 8.1.
    [CreateAssetMenu(fileName = "DailyChallengeConfig", menuName = "CandyShop/Daily Challenge Config")]
    public class DailyChallengeConfig : ScriptableObject
    {
        public int quota = 12;                    // correct picks of the featured type
        [Range(0f, 1f)] public float biasChance = 0.7f;   // chance the featured type appears in an order (if unlocked)
        [Range(0f, 1f)] public float lockedDiscount = 0.2f; // 20% off in shop when locked
        public int rewardCoins = 120;
        public int rewardFreezeCount = 1;
    }
}
