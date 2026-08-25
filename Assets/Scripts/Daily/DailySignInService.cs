using System;
using System.Collections.Generic;
using UnityEngine;

namespace CandyShop
{
    // What happened during this boot's sign-in evaluation (shown as a panel on Main Menu).
    public class SignInResult
    {
        public bool anyReward;
        public int coinsGranted;
        public string grantedRecipeName;
        public int allUnlockedBonus;
        public int staminaGranted;
        public bool dailyExtraAdAvailable;
    }

    // Daily sign-in, streak, streak-7 recipe and daily featured-recipe challenge (spec sections 8 / 8.1).
    public static class DailySignInService
    {
        public static SignInResult LastBootResult { get; private set; }

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");
        private static string Yesterday => DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        public static SignInResult Evaluate(
            SaveDataModel save,
            EconomyConfig econ,
            DailyChallengeConfig challenge,
            RecipeDefinition[] recipesSortedByCost,
            CandyTypeDefinition[] catalog)
        {
            var result = new SignInResult();

            if (save.lastSignInDate != Today)
            {
                // Streak: consecutive days; a missed day resets to 1 (today counts).
                save.dailyStreak = save.lastSignInDate == Yesterday ? save.dailyStreak + 1 : 1;
                save.lastSignInDate = Today;

                result.coinsGranted = econ.dailyCoins;
                result.anyReward = true;

                // Every daily claim also grants stamina (supplements 2.0, always-add-then-clamp rule).
                // The date refresh in StaminaService usually filled the pool to max first, so this is
                // often a no-op; the panel only shows the line when stamina actually increased.
                var staminaCfg = Resources.Load<StaminaConfig>("Data/StaminaConfig");
                if (staminaCfg != null && staminaCfg.signInStaminaGrant > 0)
                    result.staminaGranted += StaminaService.GrantHardClamped(staminaCfg.signInStaminaGrant);

                // Streak reaches 7 on this claim: unlock the cheapest remaining NORMAL recipe
                // (once per cycle). Once all normals are owned, the slot grants a sign-in-track
                // special edition instead (supplements 2.0 long line).
                if (save.dailyStreak == econ.streakRecipeDay)
                {
                    save.sevenCyclesCompleted++;

                    bool grantedNormal = false;
                    if (recipesSortedByCost != null)
                    {
                        foreach (var recipe in recipesSortedByCost)
                        {
                            if (recipe == null || recipe.isSpecial || recipe.candyType == null) continue;
                            if (Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) < 0)
                            {
                                UnlockRecipe(save, recipe);
                                result.grantedRecipeName = recipe.candyType.LocalizedName;
                                grantedNormal = true;
                                break;
                            }
                        }
                    }

                    if (!grantedNormal)
                    {
                        // Streak-7 slot falls through to the sign-in long line.
                        if (CollectionService.GrantSignInTrackSpecial(save, recipesSortedByCost))
                            result.anyReward = true;
                    }

                    // Long-line steps: 2nd / 3rd / 4th time the streak hits 7 each grant 1 special.
                    int cycles = save.sevenCyclesCompleted;
                    if (cycles >= 2 && cycles <= 4)
                    {
                        if (CollectionService.GrantSignInTrackSpecial(save, recipesSortedByCost))
                            result.anyReward = true;
                    }

                    int staminaGrant = econ.streakSevenStaminaGrant;
                    if (staminaGrant > 0)
                    {
                        result.staminaGranted += StaminaService.GrantBonus(staminaGrant);
                    }
                }

                if (result.staminaGranted > 0) result.anyReward = true;

                // Owned-count milestones may fire after sign-in recipe grants (catch-up).
                CollectionService.CheckOwnedMilestones(save, recipesSortedByCost);
            }

            // All recipes unlocked bonus (+500), once ever.
            if (!save.allRecipesBonusClaimed && recipesSortedByCost != null && recipesSortedByCost.Length > 0)
            {
                bool allOwned = true;
                foreach (var r in recipesSortedByCost)
                {
                    if (r == null) continue;
                    if (Array.IndexOf(save.unlockedRecipeIds, r.recipeId) < 0) { allOwned = false; break; }
                }
                if (allOwned)
                {
                    save.allRecipesBonusClaimed = true;
                    result.allUnlockedBonus += econ.allUnlockedBonus;
                    result.anyReward = true;
                }
            }

            if (result.coinsGranted > 0) save.coins += result.coinsGranted;
            if (result.allUnlockedBonus > 0) save.coins += result.allUnlockedBonus;

            // Roll today's featured candy and reset progress on a new date.
            RollDailyChallenge(save, catalog);

            result.dailyExtraAdAvailable = save.dailyCoinAdClaimedDate != Today;
            LastBootResult = result;
            SaveDataService.Save();
            return result;
        }

        private static void UnlockRecipe(SaveDataModel save, RecipeDefinition recipe)
        {
            var list = new List<string>(save.unlockedRecipeIds);
            if (!list.Contains(recipe.recipeId))
            {
                list.Add(recipe.recipeId);
                save.unlockedRecipeIds = list.ToArray();
            }
        }

        public static void ClaimDailyExtraAd(SaveDataModel save, EconomyConfig econ)
        {
            var today = Today;
            if (save.dailyCoinAdClaimedDate == today) return;
            save.dailyCoinAdClaimedDate = today;
            save.coins += econ.dailyExtraAdCoins;
            SaveDataService.Save();
        }

        // Pick rule: hash(yyyy-MM-dd) % catalogCount; avoid yesterday's id by taking the next index.
        public static void RollDailyChallenge(SaveDataModel save, CandyTypeDefinition[] catalog)
        {
            if (catalog == null || catalog.Length == 0) return;

            if (save.dailyChallengeDate == Today) return; // do not re-roll mid-day

            // Special editions are milestone rewards, not daily-challenge candidates.
            var pool = new List<CandyTypeDefinition>();
            foreach (var c in catalog)
                if (c != null && !c.isSpecial) pool.Add(c);
            if (pool.Count == 0) return;

            int hash = StableHash(Today);
            int index = hash % pool.Count;
            if (pool.Count > 1 && pool[index].typeId == save.dailyChallengeYesterdayId)
                index = (index + 1) % pool.Count;

            save.dailyChallengeDate = Today;
            save.dailyChallengeTypeId = pool[index].typeId;
            save.dailyChallengeYesterdayId = pool[index].typeId;
            save.dailyChallengeProgress = 0;
            save.dailyChallengeClaimed = false;
        }

        // Called after every correct pick of the featured type (Magnet auto-picks count).
        public static void ReportCorrectPick(SaveDataModel save, string typeId, DailyChallengeConfig cfg)
        {
            if (cfg == null || save.dailyChallengeTypeId != typeId || save.dailyChallengeClaimed) return;
            if (save.dailyChallengeDate != Today) return;

            save.dailyChallengeProgress++;
            if (save.dailyChallengeProgress >= cfg.quota)
            {
                save.dailyChallengeProgress = cfg.quota;
                save.dailyChallengeClaimed = true;
                save.coins += cfg.rewardCoins;
                save.freezeCount += cfg.rewardFreezeCount;
            }
            SaveDataService.Save();
        }

        // Shop price for a locked recipe: featured rows get 20% off for that date only,
        // rounded to the nearest 10 with a floor of 80.
        public static int GetShopPrice(RecipeDefinition recipe, SaveDataModel save)
        {
            int cost = recipe.cost;
            if (IsFeatured(save, recipe.candyType) &&
                Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) < 0)
            {
                int discounted = Mathf.RoundToInt(cost * 0.8f / 10f) * 10;
                cost = Mathf.Max(80, discounted);
            }
            return cost;
        }

        public static bool IsFeatured(SaveDataModel save, CandyTypeDefinition type)
        {
            return type != null && save.dailyChallengeTypeId == type.typeId;
        }

        public static CandyTypeDefinition GetFeatured(CandyTypeDefinition[] catalog, SaveDataModel save)
        {
            if (catalog == null) return null;
            foreach (var c in catalog)
                if (c != null && c.typeId == save.dailyChallengeTypeId) return c;
            return null;
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in s)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)(hash & 0x7FFFFFFF);
            }
        }
    }
}
