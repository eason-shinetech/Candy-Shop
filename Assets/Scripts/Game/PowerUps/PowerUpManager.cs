using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyShop
{
    // Power-up inventory and use/buy flow (spec section 9):
    // count > 0 -> tap uses immediately, free, no ad.
    // count == 0 -> buy sheet: coins AND rewarded ad; skip refunds coins; success auto-uses.
    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        public GameManager game;
        public CustomerOrderManager orders;
        public CandyPileRestock pile;
        public PowerUpVfxPlayer vfx;

        // HUD wires these so the buy sheet can be shown/hidden.
        public event Action<PowerUpDefinition> BuySheetRequested;

        private void Awake()
        {
            Instance = this;
            if (game == null) game = FindObjectOfType<GameManager>();
            if (orders == null) orders = FindObjectOfType<CustomerOrderManager>();
            if (pile == null) pile = FindObjectOfType<CandyPileRestock>();
            if (vfx == null) vfx = PowerUpVfxPlayer.Instance;
        }

        public int CountOf(string powerUpId)
        {
            var save = SaveDataService.Current;
            switch (powerUpId)
            {
                case "magnet": return save.magnetCount;
                case "tornado": return save.tornadoCount;
                case "freeze": return save.freezeCount;
                default: return 0;
            }
        }

        private void SetCount(string powerUpId, int value)
        {
            var save = SaveDataService.Current;
            value = Mathf.Max(0, value);
            switch (powerUpId)
            {
                case "magnet": save.magnetCount = value; break;
                case "tornado": save.tornadoCount = value; break;
                case "freeze": save.freezeCount = value; break;
            }
            SaveDataService.Save();
        }

        // Tap on the HUD button.
        public void TapUse(PowerUpDefinition def)
        {
            if (def == null || game == null) return;
            if (!game.RunActive || game.Paused) return;

            if (CountOf(def.powerUpId) > 0)
            {
                Use(def);
            }
            else
            {
                BuySheetRequested?.Invoke(def); // open buy sheet, no use
            }
        }

        // Optional "+" badge: stockpile without auto-use (same buy rule).
        public void TapStockpile(PowerUpDefinition def)
        {
            if (def == null) return;
            BuySheetRequested?.Invoke(def);
        }

        private void Use(PowerUpDefinition def)
        {
            switch (def.powerUpId)
            {
                case "magnet":
                    if (!TryUseMagnet(def)) return; // do not consume a charge with 0 required candies
                    break;
                case "tornado":
                    if (pile != null && !pile.IsBusyLifting)
                    {
                        pile.LiftFor(game.orderConfig.tornadoDurationSeconds);
                        PlayVfxLoop(def, game.orderConfig.tornadoDurationSeconds);
                        Consume(def);
                    }
                    break;
                case "freeze":
                    if (orders != null && !orders.Frozen)
                    {
                        orders.FreezeTimer(game.orderConfig.freezeDurationSeconds);
                        PlayVfxLoop(def, game.orderConfig.freezeDurationSeconds);
                        Consume(def);
                    }
                    break;
                default:
                    Debug.LogError("Unknown power-up id: " + def.powerUpId);
                    break;
            }
        }

        private bool TryUseMagnet(PowerUpDefinition def)
        {
            if (orders == null) return false;
            if (orders.AwaitingServeUi) return false; // serve chip up: keep the charge
            var picks = orders.GetRequiredCandies(Mathf.RoundToInt(game.orderConfig.magnetMaxPicks));
            if (picks.Count == 0) return false; // nothing required: keep the charge

            PlayVfxOneShot(def);
            orders.ApplyMagnetPicks(picks);
            Consume(def);
            return true;
        }

        private void Consume(PowerUpDefinition def) => SetCount(def.powerUpId, CountOf(def.powerUpId) - 1);

        private void GrantOne(PowerUpDefinition def) => SetCount(def.powerUpId, CountOf(def.powerUpId) + 1);

        private void PlayVfxOneShot(PowerUpDefinition def)
        {
            if (vfx != null && def.vfxPrefab != null)
                vfx.PlayOneShot(def.vfxPrefab, PileCenter());
        }

        private void PlayVfxLoop(PowerUpDefinition def, float duration)
        {
            if (vfx == null || def.vfxPrefab == null)
            {
                Debug.LogError("Missing VFX prefab for " + def.powerUpId + "; applying gameplay anyway");
                return;
            }
            _activeVfxHandle = vfx.PlayLoop(def.vfxPrefab, PileCenter());
            StartCoroutine(StopAfter(_activeVfxHandle, duration));
        }

        private GameObject _activeVfxHandle;

        private IEnumerator StopAfter(GameObject handle, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (vfx != null) vfx.StopLoop(handle);
            if (handle == _activeVfxHandle) _activeVfxHandle = null;
        }

        private Vector3 PileCenter()
        {
            return pile != null ? pile.transform.position + Vector3.up * 1f : Vector3.zero;
        }

        // ---- Buy sheet actions (called by HUD) ----

        public string GetBuyPriceText(PowerUpDefinition def)
        {
            return string.Format(I18nService.Get("powerup_price"), def.buyCost);
        }

        public bool CanAfford(PowerUpDefinition def) => EconomyManager.Coins >= def.buyCost;

        // Step 1 of the buy sheet when coins are insufficient: watch an ad for +80.
        public void WatchAdForCoins(PowerUpDefinition def, Action onSuccess)
        {
            var ads = AdServiceLocator.Service;
            var econ = EconomyManager.Config;
            if (ads == null || !ads.IsReady(AdPlacement.reward_coins))
            {
                onSuccess?.Invoke();
                return; // UI shows 广告还没准备好
            }
            ads.ShowRewarded(AdPlacement.reward_coins, ok =>
            {
                if (ok)
                {
                    EconomyManager.AddCoins(econ.adCoinGrant);
                }
                onSuccess?.Invoke();
            });
        }

        // Purchase requires coins AND ad. Deduct first; refund if the ad is skipped.
        public void TryPurchaseAndAutoUse(PowerUpDefinition def, Action<string> messageOut)
        {
            var ads = AdServiceLocator.Service;
            if (ads == null || !ads.IsReady(BuyPlacement(def)))
            {
                messageOut?.Invoke(I18nService.Get("ad_not_ready"));
                return;
            }
            if (!EconomyManager.TrySpend(def.buyCost))
            {
                messageOut?.Invoke(I18nService.Get("coins_short"));
                return;
            }

            ads.ShowRewarded(BuyPlacement(def), ok =>
            {
                if (ok)
                {
                    GrantOne(def);
                    messageOut?.Invoke(null);
                    Use(def); // auto-use the purchased unit
                }
                else
                {
                    EconomyManager.AddCoins(def.buyCost); // refund coins
                    messageOut?.Invoke(I18nService.Get("ad_refund"));
                }
            });
        }

        // Stockpile purchase ("+" badge): no auto-use.
        public void TryPurchaseOnly(PowerUpDefinition def, Action<string> messageOut)
        {
            var ads = AdServiceLocator.Service;
            if (ads == null || !ads.IsReady(BuyPlacement(def)))
            {
                messageOut?.Invoke(I18nService.Get("ad_not_ready"));
                return;
            }
            if (!EconomyManager.TrySpend(def.buyCost))
            {
                messageOut?.Invoke(I18nService.Get("coins_short"));
                return;
            }
            ads.ShowRewarded(BuyPlacement(def), ok =>
            {
                if (ok)
                {
                    GrantOne(def);
                    messageOut?.Invoke(null);
                }
                else
                {
                    EconomyManager.AddCoins(def.buyCost);
                    messageOut?.Invoke(I18nService.Get("ad_refund"));
                }
            });
        }

        private static AdPlacement BuyPlacement(PowerUpDefinition def)
        {
            switch (def.powerUpId)
            {
                case "magnet": return AdPlacement.reward_powerup_buy_magnet;
                case "tornado": return AdPlacement.reward_powerup_buy_tornado;
                default: return AdPlacement.reward_powerup_buy_freeze;
            }
        }

        public void CancelEffects()
        {
            StopAllCoroutines();
            // StopAfter coroutines are dead now — stop any looping VFX directly.
            if (vfx != null) vfx.StopAllLoops();
            // Spec 9.1: lifted candies settle back the same frame gameplay ends.
            if (pile != null) pile.EndLiftImmediately();
        }
    }

    // Simple access to the current IAdService instance.
    public static class AdServiceLocator
    {
        public static IAdService Service { get; set; }
    }
}
