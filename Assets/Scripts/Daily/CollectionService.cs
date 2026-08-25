using System;
using System.Collections.Generic;
using UnityEngine;

namespace CandyShop
{
    // Special-edition collection grants (supplements 2.0).
    // Two tracks, ~50/50:
    //   Owned-count track: 5 normals / 10 normals / all 3-star / all 4-star (full shelf sweeps the rest).
    //   Sign-in long line: streak-7 slot after all normals are owned, plus cycles 2/3/4.
    // Prerequisite for any special: its base (same mesh, normal) recipe is already owned.
    public static class CollectionService
    {
        public const int TrackOwned = 0;
        public const int TrackSignIn = 1;

        private static readonly int[] ShelfMilestones = { 5, 10 };

        // Latest grant waiting for a toast (consumed by Main Menu / Game HUD).
        public static string PendingGrantName;

        public static event Action<string> SpecialGranted;

        // Normal recipes owned (starters + bought shop recipes; specials do not count).
        public static int NormalsOwned(SaveDataModel save, RecipeDefinition[] recipes)
        {
            int n = 0;
            foreach (var r in recipes)
            {
                if (r == null || r.isSpecial || r.candyType == null) continue;
                if (r.candyType.isStarter || Array.IndexOf(save.unlockedRecipeIds, r.recipeId) >= 0)
                    n++;
            }
            return n;
        }

        private static bool AllNormalsOfRankOwned(SaveDataModel save, RecipeDefinition[] recipes, int rank)
        {
            foreach (var r in recipes)
            {
                if (r == null || r.isSpecial || r.candyType == null || r.starRank != rank) continue;
                if (r.candyType.isStarter) continue;
                if (Array.IndexOf(save.unlockedRecipeIds, r.recipeId) < 0) return false;
            }
            return true;
        }

        private static bool BaseOwned(SaveDataModel save, RecipeDefinition special, RecipeDefinition[] recipes)
        {
            if (string.IsNullOrEmpty(special.baseRecipeId)) return true;
            foreach (var r in recipes)
                if (r != null && r.recipeId == special.baseRecipeId)
                    return Array.IndexOf(save.unlockedRecipeIds, r.recipeId) >= 0;
            return false;
        }

        private static bool Grant(SaveDataModel save, RecipeDefinition special)
        {
            var list = new List<string>(save.unlockedRecipeIds);
            if (list.Contains(special.recipeId)) return false;
            list.Add(special.recipeId);
            save.unlockedRecipeIds = list.ToArray();
            SaveDataService.Save();

            string name = special.candyType != null ? special.candyType.LocalizedName : special.recipeId;
            PendingGrantName = name;
            SpecialGranted?.Invoke(name);
            Debug.Log("Special edition granted: " + special.recipeId);
            return true;
        }

        private static RecipeDefinition FindEligible(SaveDataModel save, RecipeDefinition[] recipes, int track)
        {
            foreach (var r in recipes)
            {
                if (r == null || !r.isSpecial || r.specialTrack != track) continue;
                if (Array.IndexOf(save.unlockedRecipeIds, r.recipeId) >= 0) continue;
                if (!BaseOwned(save, r, recipes)) continue;
                return r;
            }
            return null;
        }

        // Owned-count track. Call after any normal recipe unlock (buy / sign-in / boot catch-up).
        public static void CheckOwnedMilestones(SaveDataModel save, RecipeDefinition[] recipes)
        {
            if (save == null || recipes == null) return;

            int owned = NormalsOwned(save, recipes);
            int totalNormals = 0;
            foreach (var r in recipes)
                if (r != null && !r.isSpecial && r.candyType != null && !r.candyType.isStarter)
                    totalNormals++;

            // Shelf count milestones (5 / 10 normals).
            foreach (int threshold in ShelfMilestones)
            {
                string flag = "shelf_" + threshold;
                if (owned >= threshold && !MilestoneDone(save, flag))
                {
                    var special = FindEligible(save, recipes, TrackOwned);
                    if (special != null)
                    {
                        Grant(save, special);
                        MarkMilestone(save, flag);
                    }
                    // No eligible special: skip and retry when prerequisites unlock.
                }
            }

            // Star-band milestones: all 3-star, then all 4-star normals.
            foreach (int rank in new[] { 3, 4 })
            {
                string flag = "band_" + rank;
                if (AllNormalsOfRankOwned(save, recipes, rank) && !MilestoneDone(save, flag))
                {
                    var special = FindEligible(save, recipes, TrackOwned);
                    if (special != null)
                    {
                        Grant(save, special);
                        MarkMilestone(save, flag);
                    }
                }
            }

            // Full shelf: sweep any remaining owned-track specials.
            if (totalNormals > 0 && owned >= totalNormals)
            {
                RecipeDefinition special;
                while ((special = FindEligible(save, recipes, TrackOwned)) != null)
                    Grant(save, special);
            }
        }

        // Sign-in long line (supplements 2.0): called from DailySignInService on the
        // streak-7 transition. Grants at most one special per call.
        public static bool GrantSignInTrackSpecial(SaveDataModel save, RecipeDefinition[] recipes)
        {
            var special = FindEligible(save, recipes, TrackSignIn);
            if (special == null) return false;
            return Grant(save, special);
        }

        private static bool MilestoneDone(SaveDataModel save, string flag)
        {
            return Array.IndexOf(save.collectionMilestonesDone, flag) >= 0;
        }

        private static void MarkMilestone(SaveDataModel save, string flag)
        {
            var list = new List<string>(save.collectionMilestonesDone);
            if (!list.Contains(flag)) list.Add(flag);
            save.collectionMilestonesDone = list.ToArray();
        }

        // Human-readable hint for the next owned-track milestone (shop header).
        public static string OwnedTrackHint(SaveDataModel save, RecipeDefinition[] recipes)
        {
            int owned = NormalsOwned(save, recipes);
            foreach (int threshold in ShelfMilestones)
                if (owned < threshold && !MilestoneDone(save, "shelf_" + threshold))
                    return I18nService.Get("special_track_shelf", threshold);

            foreach (int rank in new[] { 3, 4 })
                if (!MilestoneDone(save, "band_" + rank))
                    return I18nService.Get("special_track_band", rank);

            return I18nService.Get("special_track_full");
        }
    }
}
