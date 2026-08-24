using UnityEngine;

namespace CandyShop
{
    // Shop recipe that unlocks one non-starter candy type. cost(i) = 120 + i * 60 (set by the catalog builder).
    [CreateAssetMenu(fileName = "Recipe_", menuName = "CandyShop/Recipe Definition")]
    public class RecipeDefinition : ScriptableObject
    {
        public string recipeId;
        public CandyTypeDefinition candyType;
        public int cost;
    }
}
