using UnityEngine;

namespace CandyShop
{
    // Static wallet bound to the current save file. Coins never go below 0.
    public static class EconomyManager
    {
        private static SaveDataModel _save;
        private static EconomyConfig _config;

        public static EconomyConfig Config
        {
            get
            {
                if (_config == null)
                    _config = Resources.Load<EconomyConfig>("Data/EconomyConfig");
                return _config;
            }
        }

        public static void Init(SaveDataModel save, EconomyConfig config)
        {
            _save = save;
            _config = config;
        }

        // Lazy fallback so scenes can be play-tested directly from the editor.
        private static SaveDataModel Save
        {
            get
            {
                if (_save == null && SaveDataService.Current != null)
                    _save = SaveDataService.Current;
                return _save;
            }
        }

        public static int Coins => Save != null ? Save.coins : 0;

        public static void AddCoins(int amount)
        {
            if (Save == null || amount <= 0) return;
            Save.coins += amount;
            SaveDataService.Save();
        }

        // Failed spend is a no-op returning false (UI shows a shake).
        public static bool TrySpend(int amount)
        {
            if (Save == null) return false;
            if (amount < 0) return false;
            if (Save.coins < amount) return false;
            Save.coins -= amount;
            SaveDataService.Save();
            return true;
        }
    }
}
