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

                // Streak reaches 7 on this claim: unlock the cheapest remaining recipe (once per cycle).
                if (save.dailyStreak == econ.streakRecipeDay && recipesSortedByCost != null)
                {
                    foreach (var recipe in recipesSortedByCost)
                    {
                        if (recipe == null || recipe.candyType == null) continue;
                        if (Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) < 0)
                        {
                            UnlockRecipe(save, recipe);
                            result.grantedRecipeName = recipe.candyType.displayNameZh;
                            break;
                        }
                    }
                }
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

            int hash = StableHash(Today);
            int index = hash % catalog.Length;
            if (catalog.Length > 1 && catalog[index].typeId == save.dailyChallengeYesterdayId)
                index = (index + 1) % catalog.Length;

            save.dailyChallengeDate = Today;
            save.dailyChallengeTypeId = catalog[index].typeId;
            save.dailyChallengeYesterdayId = catalog[index].typeId;
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
