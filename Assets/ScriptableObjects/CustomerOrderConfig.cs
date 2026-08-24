using UnityEngine;

namespace CandyShop
{
    // Order generation and per-customer timer tuning. Defaults come from design spec section 5.
    [CreateAssetMenu(fileName = "CustomerOrderConfig", menuName = "CandyShop/Customer Order Config")]
    public class CustomerOrderConfig : ScriptableObject
    {
        [Header("Timer: baseSeconds + totalCandies * secondsPerCandy, clamped")]
        public float baseSeconds = 6f;
        public float secondsPerCandy = 1.15f;
        public float minSeconds = 10f;
        public float maxSeconds = 45f;

        [Header("Order shape")]
        public int minTypes = 1;
        public int maxTypes = 3;
        public int minTotal = 6;
        public int maxTotal = 30;
        [Tooltip("After every N served customers, bias total toward the upper half.")]
        public int scaleEveryCustomers = 5;

        [Header("Queue")]
        [Tooltip("Waiting customers visible behind the current one.")]
        public int waitingCount = 2;

        [Header("Pile")]
        [Tooltip("Target pile instances per unlocked candy type for restock.")]
        public int targetInstancesPerType = 12;

        [Header("UX")]
        public float buriedHintSeconds = 8f;
        public float doubleRewardAutoContinueSeconds = 2.5f;
        public float freezeDurationSeconds = 5f;
        public float tornadoDurationSeconds = 4f;
        public float magnetMaxPicks = 3;
    }
}
