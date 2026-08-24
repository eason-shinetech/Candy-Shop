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
        public int perfectRefund = 1;      // +1 after a perfect serve
        public int passDelta = 0;          // +0 after a non-perfect serve
        public int failPenalty = 3;        // -3 after a confirmed fail (not on revive)
    }
}
