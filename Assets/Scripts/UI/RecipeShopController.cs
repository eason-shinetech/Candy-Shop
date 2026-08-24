using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CandyShop
{
    // Recipe shop (spec section 10.3): one recipe per non-starter candy, cost = 120 + i*60,
    // featured row gets the daily 20% discount while locked.
    public class RecipeShopController : MonoBehaviour
    {
        private GameManager _game;
        private RectTransform _listContent;
        private Text _coinsText;
        private GameObject _insufficientSheet;
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
            BuildUI();
            RebuildList();
        }

        private void BuildUI()
        {
            var canvas = UIKit.CreateCanvas(null, "RecipeShop");
            var safeRoot = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvas.transform, false);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            var title = UIKit.CreateText(safeRoot, "配方商店", 80, UIKit.SugarPink);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0.5f, 1);
            trt.anchorMax = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -140);

            var coinsPanel = UIKit.CreatePanel(safeRoot, "Coins", UIKit.Lemon);
            coinsPanel.sizeDelta = new Vector2(260, 90);
            coinsPanel.anchorMin = new Vector2(1, 1);
            coinsPanel.anchorMax = new Vector2(1, 1);
            coinsPanel.anchoredPosition = new Vector2(-160, -90);
            _coinsText = UIKit.CreateText(coinsPanel, "金币 0", 40, UIKit.Cocoa);
            UIKit.Stretch((RectTransform)_coinsText.transform, coinsPanel);

            var backBtn = UIKit.CreateButton(safeRoot, "← 返回", new Vector2(220, 100), UIKit.Grape, 36);
            var brt = (RectTransform)backBtn.transform;
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(0, 1);
            brt.anchoredPosition = new Vector2(150, -110);
            backBtn.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.MainMenu));

            // Scrollable list
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(safeRoot, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(60, 60);
            srt.offsetMax = new Vector2(-60, -260);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vrt = (RectTransform)viewportGo.transform;
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _listContent = (RectTransform)contentGo.transform;
            _listContent.anchorMin = new Vector2(0, 1);
            _listContent.anchorMax = new Vector2(1, 1);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = vrt;
            scroll.content = _listContent;
            scroll.vertical = true;
            scroll.horizontal = false;

            // Insufficient-coins sheet: 看广告+80 then buy again
            var dim = UIKit.CreatePanel(canvas.transform, "InsufficientDim", new Color(0, 0, 0, 0.5f));
            UIKit.Stretch(dim, canvas.transform);
            dim.GetComponent<Image>().raycastTarget = true;
            var ip = UIKit.CreatePanel(dim, "InsufficientPanel", UIKit.Lemon);
            ip.sizeDelta = new Vector2(820, 520);
            ip.anchoredPosition = Vector2.zero;

            var msg = UIKit.CreateText(ip, "金币不足", 48, UIKit.Cocoa);
            msg.rectTransform.anchoredPosition = new Vector2(0, 160);

            var adBtn = UIKit.CreateButton(ip, "看广告获得 80 金币", new Vector2(640, 130), UIKit.SugarPink, 38);
            adBtn.transform.localPosition = new Vector3(0, 10, 0);
            adBtn.onClick.AddListener(() =>
            {
                var ads = AdServiceLocator.Service;
                if (ads == null || !ads.IsReady(AdPlacement.reward_coins))
                    return;
                ads.ShowRewarded(AdPlacement.reward_coins, ok =>
                {
                    if (ok && EconomyManager.Config != null)
                        EconomyManager.AddCoins(EconomyManager.Config.adCoinGrant);
                    RefreshCoins();
                    dim.gameObject.SetActive(false);
                });
            });

            var close = UIKit.CreateButton(ip, "取消", new Vector2(640, 110),
                new Color(0.75f, 0.72f, 0.68f), 36, UIKit.Cocoa);
            close.transform.localPosition = new Vector3(0, -160, 0);
            close.onClick.AddListener(() => dim.gameObject.SetActive(false));

            _insufficientSheet = dim.gameObject;
            _insufficientSheet.SetActive(false);
        }

        private void RefreshCoins()
        {
            _coinsText.text = "金币 " + EconomyManager.Coins;
        }

        private void RebuildList()
        {
            foreach (Transform child in _listContent)
                Destroy(child.gameObject);

            var save = SaveDataService.Current;
            var recipes = _game.recipesSortedByCost;
            float rowH = 170f, gap = 24f;

            for (int i = 0; i < recipes.Length; i++)
            {
                var recipe = recipes[i];
                if (recipe == null || recipe.candyType == null) continue;

                bool owned = System.Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) >= 0;
                bool featured = DailySignInService.IsFeatured(save, recipe.candyType) && !owned;
                int price = DailySignInService.GetShopPrice(recipe, save);

                var row = UIKit.CreatePanel(_listContent, "Row_" + recipe.recipeId,
                    owned ? new Color(0.93f, 0.9f, 0.86f) : UIKit.Cream);
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1f);
                float y = -i * (rowH + gap);
                row.anchoredPosition = new Vector2(0, y);
                row.sizeDelta = new Vector2(0, rowH);

                if (!owned)
                    row.GetComponent<Image>().color *= new Color(1f, 1f, 1f, 1f); // locked rows slightly grey via text below

                // Icon dot
                var dot = UIKit.CreatePanel(row, "Dot", recipe.candyType.chipColor);
                dot.sizeDelta = Vector2.one * 110;
                dot.anchorMin = new Vector2(0, 0.5f);
                dot.anchorMax = new Vector2(0, 0.5f);
                dot.anchoredPosition = new Vector2(95, 0);

                // Name + featured badge
                var name = UIKit.CreateText(row,
                    featured ? recipe.candyType.displayNameZh + "  [今日配方]" : recipe.candyType.displayNameZh,
                    42, owned ? new Color(0.55f, 0.5f, 0.45f) : UIKit.Cocoa, TextAnchor.MiddleLeft, FontStyle.Bold);
                var nrt = (RectTransform)name.transform;
                nrt.anchorMin = new Vector2(0, 0.5f);
                nrt.anchorMax = new Vector2(1, 0.5f);
                nrt.offsetMin = new Vector2(180, -20);
                nrt.offsetMax = new Vector2(-320, 50);

                var sub = UIKit.CreateText(row, featured ? "解锁后才能完成今日挑战" : "新糖果配方",
                    28, featured ? UIKit.Berry : UIKit.Grape, TextAnchor.MiddleLeft, FontStyle.Normal);
                var subrt = (RectTransform)sub.transform;
                subrt.anchorMin = new Vector2(0, 0.5f);
                subrt.anchorMax = new Vector2(1, 0.5f);
                subrt.offsetMin = new Vector2(180, -55);
                subrt.offsetMax = new Vector2(-320, -5);

                if (owned)
                {
                    var ownedLabel = UIKit.CreateText(row, "已解锁", 38, new Color(0.6f, 0.6f, 0.58f),
                        TextAnchor.MiddleRight, FontStyle.Normal);
                    var ort = (RectTransform)ownedLabel.transform;
                    ort.anchorMin = new Vector2(1, 0.5f);
                    ort.anchorMax = new Vector2(1, 0.5f);
                    ort.offsetMin = new Vector2(-300, -40);
                    ort.offsetMax = new Vector2(-40, 40);
                }
                else
                {
                    bool afford = EconomyManager.Coins >= price;
                    var buyBtn = UIKit.CreateButton(row, "购买 " + price,
                        new Vector2(280, 110), afford ? UIKit.SugarPink : new Color(0.85f, 0.55f, 0.62f), 34);
                    var brrt = (RectTransform)buyBtn.transform;
                    brrt.anchorMin = new Vector2(1, 0.5f);
                    brrt.anchorMax = new Vector2(1, 0.5f);
                    brrt.anchoredPosition = new Vector2(-180, 0);
                    var capturedPrice = price;
                    buyBtn.onClick.AddListener(() => TryBuy(recipe, capturedPrice));

                    // Red cost when unaffordable
                    var btnText = buyBtn.GetComponentInChildren<Text>();
                    if (!afford) btnText.color = new Color(1f, 0.85f, 0.88f);
                }
            }

            // Size content for scrolling.
            int rows = Mathf.Max(1, recipes.Length);
            _listContent.sizeDelta = new Vector2(0, rows * (rowH + gap));
            _listContent.anchoredPosition = new Vector2(0, 0);

            RefreshCoins();
        }

        private void TryBuy(RecipeDefinition recipe, int price)
        {
            var save = SaveDataService.Current;
            if (System.Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) >= 0)
                return; // cannot buy an already unlocked recipe

            if (!EconomyManager.TrySpend(price))
            {
                _insufficientRecipe = recipe;
                _insufficientSheet.SetActive(true);
                return;
            }

            // Unlock is instant and persisted; toast on next Game scene load.
            var list = new List<string>(save.unlockedRecipeIds) { recipe.recipeId };
            save.unlockedRecipeIds = list.ToArray();
            SaveDataService.Save();

            GameHUDController.PendingRecipeUnlockToast = recipe.candyType.displayNameZh;
            RebuildList();
        }
    }
}
