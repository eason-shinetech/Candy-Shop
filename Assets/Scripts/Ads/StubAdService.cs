using System;
using System.Collections;
using UnityEngine;

namespace CandyShop
{
    // MVP stub: simulates a rewarded ad with a short delay, enforcing frequency caps.
    public class StubAdService : IAdService
    {
        private readonly AdConfig _config;
        private readonly SaveDataModel _save;

        // In-memory per-date counters for caps that are not part of the save schema.
        private string _powerupBuyAdDate = "";
        private int _powerupBuyAdsToday = 0;
        private string _coinAdDate = "";
        private int _coinAdsToday = 0;
        private string _staminaAdDate = "";
        private int _staminaAdsToday = 0;
        private float _lastOptionalRewardedTime = -999f;

        public StubAdService(AdConfig config, SaveDataModel save)
        {
            _config = config;
            _save = save;
            RefreshDateCounters();
        }

        private void RefreshDateCounters()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_save.adsWatchedDate != today)
            {
                _save.adsWatchedDate = today;
                _save.adsWatchedCountToday = 0;
            }
            if (_powerupBuyAdDate != today)
            {
                _powerupBuyAdDate = today;
                _powerupBuyAdsToday = 0;
            }
            if (_coinAdDate != today)
            {
                _coinAdDate = today;
                _coinAdsToday = 0;
            }
            if (_staminaAdDate != today)
            {
                _staminaAdDate = today;
                _staminaAdsToday = 0;
            }
        }

        public bool IsReady(AdPlacement placement)
        {
            RefreshDateCounters();
            switch (placement)
            {
                case AdPlacement.reward_powerup_buy_magnet:
                case AdPlacement.reward_powerup_buy_tornado:
                case AdPlacement.reward_powerup_buy_freeze:
                    return _powerupBuyAdsToday < _config.maxPowerupBuyAdsPerDay;
                case AdPlacement.reward_coins:
                    return OptionalCapOk() && _coinAdsToday < _config.maxRewardCoinsPerDay;
                case AdPlacement.reward_stamina:
                    return OptionalCapOk() && _staminaAdsToday < _config.maxStaminaAdsPerDate;
                case AdPlacement.reward_daily_extra:
                    return OptionalCapOk() && _save.dailyCoinAdClaimedDate != DateTime.Now.ToString("yyyy-MM-dd");
                case AdPlacement.reward_double_serve:
                case AdPlacement.reward_revive:
                    return OptionalCapOk();
                default:
                    return false;
            }
        }

        private bool OptionalCapOk()
        {
            RefreshDateCounters();
            if (Time.realtimeSinceStartup - _lastOptionalRewardedTime < _config.minSecondsBetweenRewarded)
                return false;
            return _save.adsWatchedCountToday < _config.maxOptionalRewardedPerDay;
        }

        public void ShowRewarded(AdPlacement placement, Action<bool> onRewarded)
        {
            if (!IsReady(placement))
            {
                onRewarded?.Invoke(false);
                return;
            }

            bool isPowerupBuy =
                placement == AdPlacement.reward_powerup_buy_magnet ||
                placement == AdPlacement.reward_powerup_buy_tornado ||
                placement == AdPlacement.reward_powerup_buy_freeze;

            // Simulate the ad with a fixed delay so flows can be tested without a network.
            // Spec 14: the customer timer must not run while a rewarded ad shows.
            var host = new GameObject("StubAdHost");
            UnityEngine.Object.DontDestroyOnLoad(host);
            var runner = host.AddComponent<StubAdRunner>();
            var game = GameManager.Instance;
            bool pausedRun = false;
            if (game != null && game.RunActive && !game.Paused && isRewardedInRun(placement))
            {
                game.SetPaused(true);
                pausedRun = true;
            }
            runner.StartCoroutine(RunAd(host, () =>
            {
                RefreshDateCounters();
                if (isPowerupBuy) _powerupBuyAdsToday++;
                else
                {
                    _lastOptionalRewardedTime = Time.realtimeSinceStartup;
                    _save.adsWatchedCountToday++;
                    if (placement == AdPlacement.reward_coins) _coinAdsToday++;
                    if (placement == AdPlacement.reward_stamina) _staminaAdsToday++;
                    SaveDataService.Save();
                }
                if (pausedRun && game != null) game.SetPaused(false);
            }, onRewarded));
        }

        // In-run rewarded placements that should freeze the customer timer.
        private static bool isRewardedInRun(AdPlacement placement)
        {
            return placement == AdPlacement.reward_coins ||
                   placement == AdPlacement.reward_double_serve ||
                   placement == AdPlacement.reward_powerup_buy_magnet ||
                   placement == AdPlacement.reward_powerup_buy_tornado ||
                   placement == AdPlacement.reward_powerup_buy_freeze;
        }

        private IEnumerator RunAd(GameObject host, Action onComplete, Action<bool> onRewarded)
        {
            yield return new WaitForSecondsRealtime(_config.stubAdDelaySeconds);
            onComplete?.Invoke();
            onRewarded?.Invoke(true);
            UnityEngine.Object.Destroy(host);
        }

        public void ShowInterstitial(AdPlacement placement, Action onClosed)
        {
            if (!_config.interstitialEnabled)
            {
                onClosed?.Invoke();
                return;
            }
            // Interstitials are disabled by default; nothing to show in the stub.
            onClosed?.Invoke();
        }

        private class StubAdRunner : MonoBehaviour { }
    }
}
