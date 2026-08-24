using System;

namespace CandyShop
{
    // Local JSON save blob. Field names follow design spec section 12 exactly.
    [Serializable]
    public class SaveDataModel
    {
        public int schemaVersion = 1;
        public int coins = 0;
        public string[] unlockedRecipeIds = new string[0];
        public int dailyStreak = 0;
        public string lastSignInDate = "";
        public bool allRecipesBonusClaimed = false;
        public string adsWatchedDate = "";
        public int adsWatchedCountToday = 0;
        public string dailyCoinAdClaimedDate = "";
        public int magnetCount = 1;
        public int tornadoCount = 1;
        public int freezeCount = 1;
        public bool starterPowerUpsGranted = false;
        public bool tutorialDone = false;
        public int bestCustomersServed = 0;
        public bool musicEnabled = true;
        public bool sfxEnabled = true;
        public bool hapticsEnabled = true;
        public string dailyChallengeDate = "";
        public string dailyChallengeTypeId = "";
        public string dailyChallengeYesterdayId = "";
        public int dailyChallengeProgress = 0;
        public bool dailyChallengeClaimed = false;

        // Daily stamina (spec 8.2). Missing staminaDate on an old save is treated
        // as a new date by StaminaService and refreshed to dailyMax.
        public int stamina = 20;
        public string staminaDate = "";

        // Active locale: "zh" or "en" (i18n spec section 1).
        public string language = "";
    }
}
