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
