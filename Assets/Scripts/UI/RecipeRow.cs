using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CandyShop
{
    // One recipe row in the shop list.
    public class RecipeRow : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private RecipeRowFx _fx;
        [SerializeField] private Image _candyIcon;
        [SerializeField] private Image _lockIcon;
        [SerializeField] private Image _checkIcon;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image[] _stars;
        [SerializeField] private TMP_Text _subText;
        [SerializeField] private TMP_Text _ownedLabel;
        [SerializeField] private TMP_Text _milestoneTag;
        [SerializeField] private Button _buyButton;
        [SerializeField] private TMP_Text _buyButtonLabel;

        public RecipeDefinition Recipe { get; private set; }
        public RecipeRowFx Fx => _fx;
        public Button BuyButton => _buyButton;

        public void Bind(RecipeDefinition recipe, bool owned, bool featured, int price, bool afford)
        {
            Recipe = recipe;
            int rank = Mathf.Clamp(recipe.starRank, 1, 5);
            _fx.Setup(rank, owned);
            _frame.color = RankFrameColor(rank, owned);

            _candyIcon.sprite = UIKit.CandyIcon(recipe.candyType);
            if (recipe.isSpecial)
                _candyIcon.color = SpecialTint(recipe.candyType.typeId); // pastel variant tint
            else
                _candyIcon.color = Color.white;

            _lockIcon.gameObject.SetActive(!owned);
            _checkIcon.gameObject.SetActive(owned);

            string displayName = recipe.candyType.LocalizedName;
            if (recipe.isSpecial) displayName += "  " + I18nService.Get("special_badge");
            else if (featured) displayName += "  " + I18nService.Get("recipe_featured_tag");
            _nameText.text = displayName;
            _nameText.color = owned ? new Color(0.55f, 0.5f, 0.45f) : UIKit.Cocoa;

            for (int s = 0; s < _stars.Length && s < 5; s++)
                _stars[s].sprite = UIKit.LoadSprite(s < rank ? "icon_star" : "frame_star_empty");

            Color subColor;
            if (recipe.isSpecial && !owned)
            {
                _subText.text = I18nService.Get("special_locked_hint");
                subColor = UIKit.Berry;
            }
            else if (featured)
            {
                _subText.text = I18nService.Get("daily_recipe_unlock_hint");
                subColor = UIKit.Berry;
            }
            else
            {
                _subText.text = I18nService.Get("recipe_sub_normal");
                subColor = UIKit.Grape;
            }
            _subText.color = subColor;

            bool buyable = !owned && !recipe.isSpecial;
            _ownedLabel.gameObject.SetActive(owned);
            _milestoneTag.gameObject.SetActive(!owned && recipe.isSpecial);
            _buyButton.gameObject.SetActive(buyable);
            if (!buyable) return;

            _buyButtonLabel.text = string.Format(I18nService.Get("recipe_buy"), price);
            _buyButton.image.color = afford ? Color.white : new Color(0.82f, 0.62f, 0.66f);
            _buyButtonLabel.color = afford ? Color.white : new Color(1f, 0.85f, 0.88f);
        }

        // Rank frame tiers (supplements 2.0): higher rank = visibly more premium.
        private static Color RankFrameColor(int rank, bool owned)
        {
            switch (rank)
            {
                case 2: return new Color(1f, 0.97f, 0.9f);   // light icing rim
                case 3: return new Color(0.92f, 0.9f, 1f);   // grape accent
                case 4: return new Color(0.95f, 0.92f, 1f);  // dual-tone frosting
                case 5: return new Color(1f, 0.93f, 0.9f);   // lemon + sugar-pink hero
                default: return owned ? new Color(0.93f, 0.9f, 0.86f) : UIKit.Cream;
            }
        }

        // Deterministic pastel tint per special id so the variant reads consistently.
        private static Color SpecialTint(string typeId)
        {
            int h = typeId.GetHashCode();
            float hue = (h & 0xFF) / 255f;
            Color c = Color.HSVToRGB(Mathf.Clamp01(0.85f + hue * 0.3f) % 1f, 0.45f, 1f);
            c.a = 1f;
            return c;
        }
    }
}
