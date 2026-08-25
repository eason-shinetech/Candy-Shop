using System;
using UnityEngine;

namespace CandyShop
{
    // Daily stamina state machine (spec 8.2). All mutations clamp to [0, dailyMax]
    // and persist immediately. Refresh happens only at Boot / Main Menu / Start tap,
    // never mid-customer.
    public static class StaminaService
    {
        private static StaminaConfig _config;
        public static event Action<int> StaminaChanged;
        public static event Action<string> FloatingTextRequested; // localized floating text like "Stamina -1"

        private static StaminaConfig Config
        {
            get
            {
                if (_config == null)
                    _config = Resources.Load<StaminaConfig>("Data/StaminaConfig");
                return _config;
            }
        }

        // Called from BootLoader (with sign-in) and when entering Main Menu.
        public static void RefreshOnDateRoll()
        {
            var save = SaveDataService.Current;
            var cfg = Config;
            if (save == null || cfg == null) return;

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (save.staminaDate != today)
            {
                // New local date: discard leftovers and refill to the daily max (spec 8.2).
                save.stamina = cfg.dailyMax;
                save.staminaDate = today;
                save.perfectStaminaRefundsToday = 0;
                SaveDataService.Save();
                NotifyChanged();
            }
        }

        public static int Current
        {
            get
            {
                var save = SaveDataService.Current;
                return save != null ? save.stamina : 0;
            }
        }

        public static bool CanStartGuest => Current >= (Config != null ? Config.costPerCustomer : 1);

        // Spend when a guest becomes current. Returns false (and spends nothing)
        // when stamina is below the cost — caller must not start the guest.
        public static bool SpendForCurrentGuest()
        {
            var cfg = Config;
            int cost = cfg != null ? cfg.costPerCustomer : 1;
            if (Current < cost) return false;

            var save = SaveDataService.Current;
            save.stamina -= cost;
            ClampAndSave(save, cfg);
            ShowFloat(I18nService.IsReady ? I18nService.Get("stamina_minus_one") : "体力-1");
            return true;
        }

        // Perfect serve: refund stamina when under the daily cap (then clamp).
        // Star restore is handled by GameManager separately and is not capped here.
        public static void SettlePerfect()
        {
            var cfg = Config;
            var save = SaveDataService.Current;
            if (cfg == null || save == null) return;

            int refund = cfg.perfectRefund;
            if (refund <= 0) return;

            int cap = Mathf.Max(0, cfg.maxPerfectRefundsPerDay);
            if (save.perfectStaminaRefundsToday >= cap) return;

            save.stamina += refund;
            save.perfectStaminaRefundsToday++;
            ClampAndSave(save, cfg);
            ShowFloat(I18nService.Get("hud_stamina_plus"));
        }

        // Pass serve: no change (kept for spec clarity / future tuning).
        public static void SettlePass()
        {
            // passDelta is +0 by design; nothing to apply or show.
        }

        // Confirmed fail (left Game Over without revive, or confirmed quit). Not for Shift Over.
        public static void ApplyFailPenalty()
        {
            var cfg = Config;
            SaveDataService.Current.stamina -= cfg != null ? cfg.failPenalty : 3;
            ClampAndSave(SaveDataService.Current, cfg);
            ShowFloat(I18nService.IsReady ? I18nService.Get("hud_stamina_fail") : "体力-3");
        }

        // Bonus grant (e.g. streak-7). May soft-overflow above dailyMax up to bonusOverflowMax.
        // Returns the actual amount applied after clamp.
        public static int GrantBonus(int amount)
        {
            if (amount <= 0) return 0;
            var cfg = Config;
            var save = SaveDataService.Current;
            if (save == null) return 0;

            int before = save.stamina;
            save.stamina += amount;
            ClampAndSave(save, cfg);
            int applied = save.stamina - before;
            if (applied > 0)
            {
                string floatText = I18nService.IsReady
                    ? I18nService.Get("hud_stamina_bonus", applied)
                    : $"体力+{applied}";
                ShowFloat(floatText);
            }
            return applied;
        }

        // Watch-ad / sign-in stamina (supplements 2.0): hard clamp to dailyMax, no overflow.
        // Returns the actual amount applied after clamp.
        public static int GrantHardClamped(int amount)
        {
            if (amount <= 0) return 0;
            var cfg = Config;
            var save = SaveDataService.Current;
            if (save == null) return 0;

            int before = save.stamina;
            save.stamina += amount;
            int max = cfg != null ? cfg.dailyMax : 20;
            save.stamina = Mathf.Clamp(save.stamina, 0, max);
            SaveDataService.Save();
            NotifyChanged();
            return save.stamina - before;
        }

        private static void ClampAndSave(SaveDataModel save, StaminaConfig cfg)
        {
            int max = cfg != null ? cfg.dailyMax : 20;
            int overflow = cfg != null ? Mathf.Max(0, cfg.bonusOverflowMax) : 0;
            save.stamina = Mathf.Clamp(save.stamina, 0, max + overflow);
            SaveDataService.Save();
            NotifyChanged();
        }

        private static void NotifyChanged()
        {
            StaminaChanged?.Invoke(Current);
        }

        private static void ShowFloat(string text)
        {
            if (!string.IsNullOrEmpty(text)) FloatingTextRequested?.Invoke(text);
        }

    }
}
