using UnityEngine;

namespace CandyShop
{
    // Ad frequency caps and stub timing. Defaults from design spec section 14.
    [CreateAssetMenu(fileName = "AdConfig", menuName = "CandyShop/Ad Config")]
    public class AdConfig : ScriptableObject
    {
        [Header("Global")]
        public bool interstitialEnabled = false; // disabled by default per spec
        public float stubAdDelaySeconds = 0.8f;

        [Header("Frequency caps")]
        public float minSecondsBetweenRewarded = 45f;      // optional rewarded only
        public int maxOptionalRewardedPerDay = 8;
        public int maxPowerupBuyAdsPerDay = 6;
        public int maxRewardCoinsPerDay = 4;
        public int maxDoubleServePerRun = 3;
        public int maxRevivePerRun = 1;

        [Header("Stamina ads (supplements 2.0)")]
        [Tooltip("Separate per-date cap for reward_stamina (also counts toward the optional daily max).")]
        public int maxStaminaAdsPerDate = 3;
    }
}
