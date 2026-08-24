using UnityEngine;

namespace CandyShop
{
    // Plugin-free Android haptics, gated by the haptics setting. Never used for ad starts.
    public static class Haptics
    {
        public static void Light()
        {
            // Handheld.Vibrate has fixed intensity; light taps skip real vibration on most devices.
        }

        public static void Medium()
        {
            var save = SaveDataService.Current;
            if (save == null || !save.hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
