using System;
using UnityEngine;

namespace CandyShop
{
    // Run-level state: stars, customers served, fail conditions, pause and revive.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configs")]
        public CustomerOrderConfig orderConfig;
        public EconomyConfig economyConfig;
        public AdConfig adConfig;
        public DailyChallengeConfig dailyChallengeConfig;
        public PowerUpDefinition magnetDef;
        public PowerUpDefinition tornadoDef;
        public PowerUpDefinition freezeDef;

        [Header("Data")]
        public CandyTypeDefinition[] catalog;
        public RecipeDefinition[] recipesSortedByCost;

        // Run state
        public int Stars { get; private set; }
        public int CustomersServed { get; private set; }
        public int CoinsEarnedThisRun { get; private set; }
        public bool RunActive { get; private set; }
        public bool Paused { get; private set; }
        public bool ReviveUsed { get; private set; }
        public int DoublesUsedThisRun { get; private set; }

        // Optional ad placements are hidden until the player finished one game over.
        public bool AllowOptionalAds => SaveDataService.Current.bestCustomersServed > 0;

        public event Action<int> StarsChanged;
        public event Action<string> RunEnded; // reason: "stars" | "timeout" | "quit"
        public event Action<bool> PauseChanged;

        private CustomerOrderManager _orders;
        private PowerUpManager _powerUps;
        private CandyPileRestock _pile;

        private void Awake()
        {
            Instance = this;
            Stars = 3;
            EnsureConfigs();
        }

        // Auto-load generated ScriptableObject data so scenes stay thin.
        public void EnsureConfigs()
        {
            if (orderConfig == null) orderConfig = Resources.Load<CustomerOrderConfig>("Data/CustomerOrderConfig");
            if (economyConfig == null) economyConfig = Resources.Load<EconomyConfig>("Data/EconomyConfig");
            if (adConfig == null) adConfig = Resources.Load<AdConfig>("Data/AdConfig");
            if (dailyChallengeConfig == null) dailyChallengeConfig = Resources.Load<DailyChallengeConfig>("Data/DailyChallengeConfig");
            if (magnetDef == null) magnetDef = Resources.Load<PowerUpDefinition>("Data/PowerUps/PowerUp_magnet");
            if (tornadoDef == null) tornadoDef = Resources.Load<PowerUpDefinition>("Data/PowerUps/PowerUp_tornado");
            if (freezeDef == null) freezeDef = Resources.Load<PowerUpDefinition>("Data/PowerUps/PowerUp_freeze");

            if (catalog == null || catalog.Length == 0)
                catalog = Resources.LoadAll<CandyTypeDefinition>("Data/Catalog");
            if (recipesSortedByCost == null || recipesSortedByCost.Length == 0)
            {
                var recipes = new System.Collections.Generic.List<RecipeDefinition>(
                    Resources.LoadAll<RecipeDefinition>("Data/Recipes"));
                recipes.RemoveAll(r => r == null || r.candyType == null);
                recipes.Sort((a, b) => a.cost.CompareTo(b.cost));
                recipesSortedByCost = recipes.ToArray();
            }
        }

        public void StartRun()
        {
            Stars = 3;
            CustomersServed = 0;
            CoinsEarnedThisRun = 0;
            ReviveUsed = false;
            DoublesUsedThisRun = 0;
            Paused = false;
            RunActive = true;
            StarsChanged?.Invoke(Stars);

            _orders = FindObjectOfType<CustomerOrderManager>();
            _powerUps = FindObjectOfType<PowerUpManager>();
            _pile = FindObjectOfType<CandyPileRestock>();

            if (_orders != null) _orders.BeginQueue();
        }

        public void NotifyCorrectPick(string typeId)
        {
            DailySignInService.ReportCorrectPick(SaveDataService.Current, typeId, dailyChallengeConfig);
        }

        public void OnWrongPick()
        {
            if (!RunActive) return;
            Stars = Mathf.Max(0, Stars - 1);
            StarsChanged?.Invoke(Stars);
            if (Stars <= 0)
                EndRun("stars");
        }

        public void OnCustomerServed(int reward, bool perfect)
        {
            if (!RunActive) return;
            CoinsEarnedThisRun += reward + (perfect ? economyConfig.perfectBonus : 0);
            CustomersServed++;

            // Perfect serve restores 1 star (cap 3) with juice handled by the HUD.
            if (perfect && Stars < 3)
            {
                Stars = Mathf.Min(3, Stars + 1);
                StarsChanged?.Invoke(Stars);
            }
        }

        public void OnTimerExpired() => EndRun("timeout");

        public void QuitRun() => EndRun("quit");

        public void SetPaused(bool paused)
        {
            // Pause freezes the customer timer without Freeze VFX.
            if (!RunActive) return;
            Paused = paused;
            PauseChanged?.Invoke(paused);
        }

        public void RegisterDoubleReward()
        {
            DoublesUsedThisRun++;
        }

        public void ApplyRevive(string failReason)
        {
            ReviveUsed = true;
            if (failReason == "stars")
            {
                Stars = 1;
                StarsChanged?.Invoke(Stars);
            }
            else
            {
                Stars = Mathf.Max(1, Stars); // timeout keeps stars as-is but never below 1
            }
            RunActive = true;
            if (_orders != null) _orders.ResumeAfterRevive(failReason == "timeout");
        }

        public void EndRun(string reason)
        {
            if (!RunActive && reason != "quit") return;
            RunActive = false;

            var save = SaveDataService.Current;
            if (reason != "aborted")
            {
                save.bestCustomersServed = Mathf.Max(save.bestCustomersServed, CustomersServed);
                SaveDataService.Save();
            }

            if (_powerUps != null) _powerUps.CancelEffects();
            RunEnded?.Invoke(reason);
        }

        // Called by the HUD after the Game Over popup was dismissed with 回到主菜单.
        public void ReturnToMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
