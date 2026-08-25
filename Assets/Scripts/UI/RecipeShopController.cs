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
            UIKit.CreateBackground(canvas.transform, "bg_main_menu");
            var safeRoot = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvas.transform, false);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            var title = UIKit.CreateText(safeRoot, I18nService.Get("recipe_shop_title"), 80, UIKit.SugarPink);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0.5f, 1);
            trt.anchorMax = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -140);

            var coinsPanel = UIKit.CreatePanel(safeRoot, "Coins", Color.white, spriteName: UIKit.PanelSprite);
            coinsPanel.sizeDelta = new Vector2(260, 90);
            coinsPanel.anchorMin = new Vector2(1, 1);
            coinsPanel.anchorMax = new Vector2(1, 1);
            coinsPanel.anchoredPosition = new Vector2(-160, -90);
            _coinsText = UIKit.CreateText(coinsPanel, "", 40, UIKit.Cocoa);
            UIKit.Stretch((RectTransform)_coinsText.transform, coinsPanel);

            var backBtn = UIKit.CreateButton(safeRoot, I18nService.Get("btn_back"), new Vector2(220, 100), Color.white, 36,
                spriteName: UIKit.ButtonSecondary);
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
            var ip = UIKit.CreatePanel(dim, "InsufficientPanel", Color.white, spriteName: UIKit.PanelSprite);
            ip.sizeDelta = new Vector2(820, 520);
            ip.anchoredPosition = Vector2.zero;

            var msg = UIKit.CreateText(ip, I18nService.Get("coins_short"), 48, UIKit.Cocoa);
            msg.rectTransform.anchoredPosition = new Vector2(0, 160);

            var adBtn = UIKit.CreateButton(ip, I18nService.Get("ad_coins_80"), new Vector2(640, 130), UIKit.SugarPink, 38);
            adBtn.name = "AdButton";
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

            var close = UIKit.CreateButton(ip, I18nService.Get("btn_cancel"), new Vector2(640, 110),
                Color.white, 36, spriteName: UIKit.ButtonSecondary);
            close.transform.localPosition = new Vector3(0, -160, 0);
            close.onClick.AddListener(() => dim.gameObject.SetActive(false));

            _insufficientSheet = dim.gameObject;
            _insufficientSheet.SetActive(false);
        }

        private void RefreshCoins()
        {
            _coinsText.text = string.Format(I18nService.Get("label_coins"), EconomyManager.Coins);
        }

        private void RebuildList()
        {
            foreach (Transform child in _listContent)
                Destroy(child.gameObject);

            var save = SaveDataService.Current;
            var recipes = _game.recipesSortedByCost;
            float rowH = 190f, gap = 24f;
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
                var header = UIKit.CreatePanel(_listContent, "SpecialHeader", Color.white, spriteName: UIKit.PanelSprite);
                header.anchorMin = new Vector2(0, 1);
                header.anchorMax = new Vector2(1, 1);
                header.pivot = new Vector2(0.5f, 1f);
                header.anchoredPosition = new Vector2(0, -rowIndex * (rowH + gap));
                header.sizeDelta = new Vector2(0, 110);
                rowIndex++;

                var prog = UIKit.CreateText(header,
                    I18nService.Get("special_progress", specialsOwned, specialsTotal), 40, UIKit.Cocoa,
                    TextAnchor.MiddleLeft);
                ((RectTransform)prog.transform).anchoredPosition = new Vector2(-260, 12);

                var hint = UIKit.CreateText(header,
                    CollectionService.OwnedTrackHint(save, recipes), 28, new Color(0.45f, 0.3f, 0.2f, 0.85f),
                    TextAnchor.MiddleLeft, FontStyle.Normal);
                ((RectTransform)hint.transform).anchoredPosition = new Vector2(-260, -28);
            }

            for (int i = 0; i < recipes.Length; i++)
            {
                var recipe = recipes[i];
                if (recipe == null || recipe.candyType == null) continue;

                bool owned = System.Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) >= 0;
                bool featured = !recipe.isSpecial &&
                                DailySignInService.IsFeatured(save, recipe.candyType) && !owned;
                int rank = Mathf.Clamp(recipe.starRank, 1, 5);
                int price = recipe.isSpecial ? 0 : DailySignInService.GetShopPrice(recipe, save);

                var row = UIKit.CreatePanel(_listContent, "Row_" + recipe.recipeId, RankFrameColor(rank, owned));
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0.5f, 1f);
                float y = -rowIndex * (rowH + gap);
                row.anchoredPosition = new Vector2(0, y);
                row.sizeDelta = new Vector2(0, rowH);
                rowIndex++;

                // Idle / burst FX preset scaled by rank (supplements 2.0).
                var fx = row.gameObject.AddComponent<RecipeRowFx>();
                fx.Setup(rank, owned);

                var candyIcon = UIKit.CreateIcon(row, UIKit.CandyIconPath(recipe.candyType.typeId), Vector2.one * 110f);
                var iconRt = (RectTransform)candyIcon.transform;
                iconRt.anchorMin = new Vector2(0, 0.5f);
                iconRt.anchorMax = new Vector2(0, 0.5f);
                iconRt.anchoredPosition = new Vector2(95, 0);
                if (recipe.isSpecial)
                {
                    // Pastel tint so the special reads as a color variant of the same mesh.
                    candyIcon.color = SpecialTint(recipe.candyType.typeId);
                }
                if (!owned)
                {
                    var lockImg = UIKit.CreateIcon(row, "icon_lock", Vector2.one * 44f);
                    var lockRt = (RectTransform)lockImg.transform;
                    lockRt.anchorMin = new Vector2(0, 0.5f);
                    lockRt.anchorMax = new Vector2(0, 0.5f);
                    lockRt.anchoredPosition = new Vector2(140, -36);
                }
                else
                {
                    var checkImg = UIKit.CreateIcon(row, "icon_check", Vector2.one * 44f);
                    var checkRt = (RectTransform)checkImg.transform;
                    checkRt.anchorMin = new Vector2(0, 0.5f);
                    checkRt.anchorMax = new Vector2(0, 0.5f);
                    checkRt.anchoredPosition = new Vector2(140, -36);
                }

                string displayName = recipe.candyType.LocalizedName;
                if (recipe.isSpecial) displayName += "  " + I18nService.Get("special_badge");
                else if (featured) displayName += "  " + I18nService.Get("recipe_featured_tag");

                var name = UIKit.CreateText(row, displayName,
                    42, owned ? new Color(0.55f, 0.5f, 0.45f) : UIKit.Cocoa, TextAnchor.MiddleLeft, FontStyle.Bold);
                var nrt = (RectTransform)name.transform;
                nrt.anchorMin = new Vector2(0, 0.5f);
                nrt.anchorMax = new Vector2(1, 0.5f);
                nrt.offsetMin = new Vector2(180, -8);
                nrt.offsetMax = new Vector2(-320, 52);

                // Star rank row (icons, never color-only).
                var starsRow = new GameObject("Stars", typeof(RectTransform));
                starsRow.transform.SetParent(row, false);
                var srrt = (RectTransform)starsRow.transform;
                srrt.anchorMin = new Vector2(0, 0.5f);
                srrt.anchorMax = new Vector2(0, 0.5f);
                srrt.anchoredPosition = new Vector2(200, -48);
                for (int s = 0; s < 5; s++)
                {
                    var star = UIKit.CreateIcon(starsRow.transform, s < rank ? "icon_star" : "frame_star_empty",
                        Vector2.one * 34f);
                    var stRt = (RectTransform)star.transform;
                    stRt.anchorMin = new Vector2(0, 0.5f);
                    stRt.anchorMax = new Vector2(0, 0.5f);
                    stRt.anchoredPosition = new Vector2(s * 40f, 0);
                }

                string subText;
                Color subColor = UIKit.Grape;
                if (recipe.isSpecial && !owned)
                {
                    subText = I18nService.Get("special_locked_hint");
                    subColor = UIKit.Berry;
                }
                else if (featured)
                {
                    subText = I18nService.Get("daily_recipe_unlock_hint");
                    subColor = UIKit.Berry;
                }
                else
                {
                    subText = I18nService.Get("recipe_sub_normal");
                }

                var sub = UIKit.CreateText(row, subText, 28, subColor, TextAnchor.MiddleLeft, FontStyle.Normal);
                var subrt = (RectTransform)sub.transform;
                subrt.anchorMin = new Vector2(0, 0.5f);
                subrt.anchorMax = new Vector2(1, 0.5f);
                subrt.offsetMin = new Vector2(180, -70);
                subrt.offsetMax = new Vector2(-320, -40);

                if (owned)
                {
                    var ownedLabel = UIKit.CreateText(row, I18nService.Get("recipe_owned"), 38,
                        new Color(0.6f, 0.6f, 0.58f), TextAnchor.MiddleRight, FontStyle.Normal);
                    var ort = (RectTransform)ownedLabel.transform;
                    ort.anchorMin = new Vector2(1, 0.5f);
                    ort.anchorMax = new Vector2(1, 0.5f);
                    ort.offsetMin = new Vector2(-300, -40);
                    ort.offsetMax = new Vector2(-40, 40);
                }
                else if (recipe.isSpecial)
                {
                    // Specials are milestone rewards: no coin buy, no ads (supplements 2.0).
                    var hintLabel = UIKit.CreateText(row, I18nService.Get("special_milestone_tag"), 34,
                        UIKit.Grape, TextAnchor.MiddleRight, FontStyle.Normal);
                    var hrt = (RectTransform)hintLabel.transform;
                    hrt.anchorMin = new Vector2(1, 0.5f);
                    hrt.anchorMax = new Vector2(1, 0.5f);
                    hrt.offsetMin = new Vector2(-340, -40);
                    hrt.offsetMax = new Vector2(-40, 40);
                }
                else
                {
                    bool afford = EconomyManager.Coins >= price;
                    var buyBtn = UIKit.CreateButton(row, string.Format(I18nService.Get("recipe_buy"), price),
                        new Vector2(280, 110), Color.white, 34);
                    var brrt = (RectTransform)buyBtn.transform;
                    brrt.anchorMin = new Vector2(1, 0.5f);
                    brrt.anchorMax = new Vector2(1, 0.5f);
                    brrt.anchoredPosition = new Vector2(-180, 0);
                    var capturedPrice = price;
                    buyBtn.onClick.AddListener(() => TryBuy(recipe, capturedPrice, row));

                    // Red cost when unaffordable
                    var btnText = buyBtn.GetComponentInChildren<Text>();
                    buyBtn.image.color = afford ? Color.white : new Color(0.82f, 0.62f, 0.66f);
                    if (!afford) btnText.color = new Color(1f, 0.85f, 0.88f);
                }
            }

            // Size content for scrolling.
            _listContent.sizeDelta = new Vector2(0, Mathf.Max(1, rowIndex) * (rowH + gap));
            _listContent.anchoredPosition = new Vector2(0, 0);

            RefreshCoins();
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

        private void TryBuy(RecipeDefinition recipe, int price, RectTransform row)
        {
            var save = SaveDataService.Current;
            if (System.Array.IndexOf(save.unlockedRecipeIds, recipe.recipeId) >= 0)
                return; // cannot buy an already unlocked recipe
            if (recipe.isSpecial)
                return; // specials unlock via collection milestones only (supplements 2.0)

            if (!EconomyManager.TrySpend(price))
            {
                _insufficientRecipe = recipe;
                if (_insufficientSheet != null)
                {
                    // Disable ONLY the +80-ad button when no ad is ready; Cancel stays tappable
                    // so the player can always close the sheet (review P1).
                    var ads = AdServiceLocator.Service;
                    bool adReady = ads != null && ads.IsReady(AdPlacement.reward_coins);
                    var adBtnTr = _insufficientSheet.transform.Find("InsufficientPanel/AdButton");
                    if (adBtnTr != null)
                        adBtnTr.GetComponent<Button>().interactable = adReady;
                    _insufficientSheet.SetActive(true);
                }
                return;
            }

            // Unlock is instant and persisted; toast on next Game scene load.
            var list = new List<string>(save.unlockedRecipeIds) { recipe.recipeId };
            save.unlockedRecipeIds = list.ToArray();
            SaveDataService.Save();

            GameHUDController.PendingRecipeUnlockToast = recipe.candyType.LocalizedName;

            // Purchase sparkle on the row + collection milestones may fire (supplements 2.0).
            var fx = row != null ? row.GetComponent<RecipeRowFx>() : null;
            if (fx != null) fx.PlayUnlockBurst();
            CollectionService.CheckOwnedMilestones(save, _game.recipesSortedByCost);

            RebuildList();
        }
    }
}
