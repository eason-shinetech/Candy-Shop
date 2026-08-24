using UnityEngine;

namespace CandyShop
{
    // Data for one power-up: magnet / tornado / freeze. VFX prefab is required per spec section 9.1.
    [CreateAssetMenu(fileName = "PowerUp_", menuName = "CandyShop/PowerUp Definition")]
    public class PowerUpDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string powerUpId; // magnet | tornado | freeze
        public string displayNameZh;
        public string displayNameEn;

        // Player-facing name: i18n key first (powerup_magnet/tornado/freeze), zh as fallback.
        public string LocalizedName
        {
            get
            {
                string key = "powerup_" + powerUpId;
                string localized = I18nService.Get(key);
                if (!string.IsNullOrEmpty(localized) && localized != key) return localized;
                return I18nService.Language == "en" && !string.IsNullOrEmpty(displayNameEn)
                    ? displayNameEn : displayNameZh;
            }
        }

        [Header("Economy")]
        public int buyCost; // coins AND a rewarded ad on buy

        [Header("Gameplay")]
        public float effectDuration; // tornado lift seconds / freeze pause seconds

        [Header("VFX (required)")]
        public GameObject vfxPrefab;

        [Header("UI")]
        public Color accentColor = new Color(1f, 0.42f, 0.42f, 1f);
    }
}
