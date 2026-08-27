using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CandyShop
{
    // One thumb-zone power-up button; three instances are created by GameHUDController.
    public class PowerButton : MonoBehaviour
    {
        [SerializeField] private Image _powerIcon;
        [SerializeField] private TMP_Text _badgeText;

        public PowerUpDefinition Def { get; private set; }

        public void Setup(PowerUpManager powerUps, PowerUpDefinition def)
        {
            Def = def;
            string iconPath = def.powerUpId == "magnet" ? "icon_magnet"
                : def.powerUpId == "tornado" ? "icon_tornado" : "icon_freeze";
            _powerIcon.sprite = UIKit.LoadSprite(iconPath);
            GetComponent<Button>().onClick.AddListener(() =>
            {
                Haptics.Light();
                powerUps.TapUse(def);
            });
        }

        public void SetBadge(int count)
        {
            _badgeText.text = count > 0 ? count.ToString() : "+";
        }
    }
}
