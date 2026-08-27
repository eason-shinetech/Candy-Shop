using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CandyShop
{
    // Recipe shop (spec section 10.3): one recipe per non-starter candy, cost = 120 + i*60,
    // featured row gets the daily 20% discount while locked.
    // Layout lives in Assets/Prefabs/UI/RecipeShop.prefab; rows instantiate RecipeRow.prefab.
    public class RecipeShopController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private Button _backButton;
        [SerializeField] private RectTransform _listContent;
        [SerializeField] private RecipeRow _recipeRowPrefab;
        [SerializeField] private SpecialHeader _specialHeaderPrefab;
        [SerializeField] private GameObject _insufficientSheet;
        [SerializeField] private Button _insufficientAdBtn;
        [SerializeField] private Button _insufficientCloseBtn;

        private GameManager _game;
        private RecipeDefinition _insufficientRecipe;

        private void Awake()
        {
            if (FindObjectOfType<GameManager>() == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
            _game = FindObjectOfType<GameManager>();
            _game.EnsureConfigs();
        }

        private void Start()
        {
            _titleText.text = I18nService.Get("recipe_shop_title");
            _backButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.MainMenu));
            _insufficientAdBtn.onClick.AddListener(OnInsufficientAdPressed);
            _insufficientCloseBtn.onClick.AddListener(() => _insufficientSheet.SetActive(false));
            _insufficientSheet.SetActive(false);

            RebuildList();
        }

        private void OnInsufficientAdPressed()
        {
            var ads = AdServiceLocator.Service;
            if (ads == null || !ads.IsReady(AdPlacement.reward_coins))
                return;
            ads.ShowRewarded(AdPlacement.reward_coins, ok =>
            {
                if (ok && EconomyManager.Config != null)
                    EconomyManager.AddCoins(EconomyManager.Config.adCoinGrant);
                RefreshCoins();
                _insufficientSheet.SetActive(false);
            });
        }

        private void RefreshCoins()
        {
            _coinsText.text = string.Format(I18nService.Get("label_coins"), EconomyManager.FormatCoins(EconomyManager.Coins));
        }

        private void RebuildList()
        {
            foreach (Transform child in _listContent)
                Destroy(child.gameObject);

            var save = SaveDataService.Current;
            var recipes = _game.recipesSortedByCost;
            int rowIndex = 0;

            // Collection progress header (supplements 2.0): 特别版 n/N + owned-track hint.
            int specialsTotal = 0, specialsOwned = 0;
            foreach (var r in recipes)
            {
                if (r == null || !r.isSpecial) continue;
                specialsTotal++;
                if (System.Array.IndexOf(save.unlockedRecipeIds, r.recipeId) >= 0) specialsOwned++;
            }
            if (specialsTotal > 0)
            {
                var header = Instantiate(_specialHeaderPrefab, _listContent);
                rowIndex++;
                header.Bind(I18nService.Get("special_progress", specialsOwned, specialsTotal),
                    CollectionService.OwnedTrackHint(save, recipes));
            }

            for (int i = 0; i < recipes.Length; i++)
            {
                var recipe = recipes[i];
                if (recipe == null || recipe.candyType == null) continue;

                bool owned = System.Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) >= 0;
                bool featured = !recipe.isSpecial &&
                                DailySignInService.IsFeatured(save, recipe.candyType) && !owned;
                int price = recipe.isSpecial ? 0 : DailySignInService.GetShopPrice(recipe, save);

                // List content is a VerticalLayoutGroup + ContentSizeFitter;
                // prefab carries the row height, layout group handles position and width.
                var row = Instantiate(_recipeRowPrefab, _listContent);
                rowIndex++;
                row.BuyButton.onClick.AddListener(() => TryBuy(row));

                row.Bind(recipe, owned, featured, price, EconomyManager.Coins >= price);
            }

            _listContent.anchoredPosition = new Vector2(0, 0);

            RefreshCoins();
        }

        private void TryBuy(RecipeRow row)
        {
            var recipe = row.Recipe;
            var save = SaveDataService.Current;
            if (recipe == null ||
                System.Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) >= 0)
                return; // cannot buy an already unlocked recipe
            if (recipe.isSpecial)
                return; // specials unlock via collection milestones only (supplements 2.0)

            int price = DailySignInService.GetShopPrice(recipe, save);
            if (!EconomyManager.TrySpend(price))
            {
                _insufficientRecipe = recipe;
                // Disable ONLY the +80-ad button when no ad is ready; Cancel stays tappable
                // so the player can always close the sheet (review P1).
                var ads = AdServiceLocator.Service;
                bool adReady = ads != null && ads.IsReady(AdPlacement.reward_coins);
                _insufficientAdBtn.interactable = adReady;
                _insufficientSheet.SetActive(true);
                return;
            }

            // Unlock is instant and persisted; toast on next Game scene load.
            var list = new List<string>(save.unlockedRecipeIds) { recipe.recipeId };
            save.unlockedRecipeIds = list.ToArray();
            SaveDataService.Save();

            GameHUDController.PendingRecipeUnlockToast = recipe.candyType.LocalizedName;

            // Purchase sparkle on the row + collection milestones may fire (supplements 2.0).
            if (row.Fx != null) row.Fx.PlayUnlockBurst();
            CollectionService.CheckOwnedMilestones(save, _game.recipesSortedByCost);

            RebuildList();
        }
    }
}
