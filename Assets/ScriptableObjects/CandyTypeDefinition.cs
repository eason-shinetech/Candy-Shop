using UnityEngine;

namespace CandyShop
{
    // One prefab under Assets/Prefabs/Candy = one CandyTypeId.
    [CreateAssetMenu(fileName = "CandyType_", menuName = "CandyShop/Candy Type Definition")]
    public class CandyTypeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string typeId;
        public string displayNameZh;
        public string displayNameEn;

        // Player-facing name follows the active locale (i18n spec).
        public string LocalizedName =>
            I18nService.Language == "en" && !string.IsNullOrEmpty(displayNameEn) ? displayNameEn : displayNameZh;

        [Tooltip("Prefab from Assets/Prefabs/Candy used to fill the pile.")]
        public GameObject prefab;

        [Tooltip("Starters are always unlocked and have no shop recipe.")]
        public bool isStarter;

        [Tooltip("Special edition (same mesh, different color); unlocked via collection milestones only.")]
        public bool isSpecial;

        [Tooltip("UI thumb from Assets/Art/Candy Icon/<PrefabName>.png.")]
        public Sprite icon;

        [Tooltip("Fallback tint used by HUD chips when no icon exists.")]
        public Color chipColor = new Color(1f, 0.56f, 0.72f, 1f);
    }
}
