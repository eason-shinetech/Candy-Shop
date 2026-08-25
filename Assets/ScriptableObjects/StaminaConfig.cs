using UnityEngine;

namespace CandyShop
{
    // Daily stamina tuning. Rules in design spec section 8.2.
    [CreateAssetMenu(fileName = "StaminaConfig", menuName = "CandyShop/Stamina Config")]
    public class StaminaConfig : ScriptableObject
    {
        [Header("Pool")]
        public int dailyMax = 20;          // pool size and daily refresh target
        public int costPerCustomer = 1;    // spent when a guest becomes current

        [Header("Settle deltas")]
        public int perfectRefund = 1;      // +1 after a perfect serve (when under daily cap)
        public int passDelta = 0;          // +0 after a non-perfect serve
        public int failPenalty = 3;        // -3 after a confirmed fail (not on revive)

        [Header("Anti-farm")]
        // Perfect stamina refunds stop after this many per local date (star restore still applies).
        public int maxPerfectRefundsPerDay = 5;
        // Stamina may temporarily exceed dailyMax by this much (e.g. streak-7 bonus).
        public int bonusOverflowMax = 5;

        [Header("Grants (supplements 2.0)")]
        [Tooltip("reward_stamina grant on the empty sheet / Shift Over, opt-in only.")]
        public int staminaAdGrant = 5;
        [Tooltip("Added on every new-date sign-in claim, then clamped (always-add rule).")]
        public int signInStaminaGrant = 3;
    }
}
