using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CandyShop
{
    // Displays one day slot in the daily sign-in reward grid.
    public class DailyRewardCard : MonoBehaviour
    {
        public enum RewardType
        {
            Coins,
            Stamina,
            Recipe,
            ExtraAd,
        }

        [Header("Visual refs")]
        [SerializeField] private Text _dayLabel;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Image _checkmarkOverlay;
        [SerializeField] private Image _backgroundImage;

        private RewardType _rewardType;
        private int _rewardValue;

        public void Init(int dayNumber, RewardType rewardType, int rewardValue, bool claimed)
        {
            _rewardType = rewardType;
            _rewardValue = rewardValue;

            if (_dayLabel != null)
            {
                if (dayNumber == 0)
                {
                    _dayLabel.gameObject.SetActive(false);
                }
                else
                {
                    _dayLabel.gameObject.SetActive(true);
                    _dayLabel.text = I18nService.Get("signin_day", dayNumber);
                }
            }

            SetIcon(rewardType);

            if (_rewardText != null)
            {
                switch (rewardType)
                {
                    case RewardType.Coins:
                        _rewardText.text = EconomyManager.FormatCoins(rewardValue);
                        break;
                    case RewardType.Stamina:
                        _rewardText.text = rewardValue.ToString();
                        break;
                    case RewardType.Recipe:
                        _rewardText.text = I18nService.Get("signin_streak_reward");
                        break;
                    case RewardType.ExtraAd:
                        _rewardText.text = I18nService.Get("ad_extra_50");
                        break;
                }
            }

            _checkmarkOverlay.gameObject.SetActive(claimed);
            gameObject.SetActive(true);
        }

        private void SetIcon(RewardType type)
        {
            if (_iconImage == null) return;
            string iconPath = type switch
            {
                RewardType.Coins    => "UI/icon_coin",
                RewardType.Stamina  => "UI/icon_stamina",
                RewardType.Recipe   => "UI/icon_recipe_book",
                RewardType.ExtraAd  => "UI/icon_ad",
                _                   => null,
            };
            if (!string.IsNullOrEmpty(iconPath))
                _iconImage.sprite = LoadFallbackSprite(iconPath);
        }

        // Tries Resources/UI first (where the real UI sprites live), then falls back to Art/UI.
        private Sprite LoadFallbackSprite(string relativePath)
        {
            var s = Resources.Load<Sprite>(relativePath);
            if (s != null) return s;
            var t = Resources.Load<Texture2D>(relativePath);
            if (t != null)
            {
                t.filterMode = FilterMode.Bilinear;
                t.wrapMode = TextureWrapMode.Clamp;
                return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
            }
            return UIKit.RoundedSprite();
        }
    }
}
