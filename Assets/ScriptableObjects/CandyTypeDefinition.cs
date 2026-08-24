using UnityEngine;

namespace CandyShop
{
    // One candy mesh from the 3D kit = one CandyTypeId.
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

        [Tooltip("Extracted prefab of the candy mesh used to fill the pile.")]
        public GameObject prefab;

        [Tooltip("Starters are always unlocked and have no shop recipe.")]
        public bool isStarter;

        [Tooltip("Optional generated UI icon; may be null in MVP (colored dot fallback).")]
        public Sprite icon;

        [Tooltip("Fallback tint used by HUD chips when no icon exists.")]
        public Color chipColor = new Color(1f, 0.56f, 0.72f, 1f);
    }
}
