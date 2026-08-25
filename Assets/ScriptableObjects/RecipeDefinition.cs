using UnityEngine;

namespace CandyShop
{
    // Shop recipe that unlocks one non-starter candy type.
    // MVP+ (supplements 2.0): price comes from the star-rank table, not the old linear formula.
    [CreateAssetMenu(fileName = "Recipe_", menuName = "CandyShop/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        public string recipeId;
        public CandyTypeDefinition candyType;
        public int cost;

        [Header("Star rank (supplements 2.0)")]
        [Range(1, 5)] public int starRank = 1;

        [Header("Special edition (supplements 2.0)")]
        public bool isSpecial;
        [Tooltip("0 = owned-count / shelf milestone track, 1 = sign-in long-line track.")]
        public int specialTrack;
        [Tooltip("Normal recipe that must be owned before this special can be granted.")]
        public string baseRecipeId;

        // Fixed price table (supplements 2.0). Index 0 = rank 1.
        public static readonly int[] RankCosts = { 1000, 3000, 5000, 8000, 10000 };

        public static int CostForRank(int rank)
        {
            rank = Mathf.Clamp(rank, 1, RankCosts.Length);
            return RankCosts[rank - 1];
        }
    }
}
