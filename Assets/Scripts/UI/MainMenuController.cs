using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CandyShop
{
    // Portrait main menu: title, run/shop/settings, coins, best score, streak dots, 今日配方 banner,
    // daily sign-in popup (spec sections 8 / 10.2 / supplements 1.4).
    public class MainMenuController : MonoBehaviour
    {
        private GameManager _game;

        private Text _coinsText;
        private Text _bestText;
        private RectTransform _streakRow;
        private Text _challengeBanner;
        private Button _challengeBannerButton;
        private GameObject _signInPopup;
        private Text _signInBody;
        private Button _extraAdButton;
        private GameObject _settingsPopup;

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
            RefreshAll();

            // Show the sign-in popup when this boot granted something.
            var result = DailySignInService.LastBootResult;
            if (result != null && result.anyReward)
            {
                string body = "";
                if (result.coinsGranted > 0) body += "每日签到 +" + result.coinsGranted + " 金币\n";
                if (!string.IsNullOrEmpty(result.grantedRecipeName)) body += "连续签到奖励：解锁新配方 " + result.grantedRecipeName + "\n";
                if (result.allUnlockedBonus > 0) body += "全部配方已解锁，奖励 +" + result.allUnlockedBonus + " 金币";
                _signInBody.text = body.TrimEnd();
                bool canExtra = result.dailyExtraAdAvailable && _game.AllowOptionalAds &&
                                AdServiceLocator.Service != null &&
                                AdServiceLocator.Service.IsReady(AdPlacement.reward_daily_extra);
                _extraAdButton.gameObject.SetActive(canExtra);
                _signInPopup.SetActive(true);
            }
        }

        private void BuildUI()
        {
            var canvas = UIKit.CreateCanvas(null, "MainMenu");
            var safeRoot = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvas.transform, false);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            // Coins top-right
            var coinsPanel = UIKit.CreatePanel(safeRoot, "Coins", UIKit.Lemon);
            coinsPanel.sizeDelta = new Vector2(260, 90);
            coinsPanel.anchorMin = new Vector2(1, 1);
            coinsPanel.anchorMax = new Vector2(1, 1);
            coinsPanel.anchoredPosition = new Vector2(-160, -80);
            _coinsText = UIKit.CreateText(coinsPanel, "金币 0", 40, UIKit.Cocoa);
            UIKit.Stretch((RectTransform)_coinsText.transform, coinsPanel);

            // Title
            var title = UIKit.CreateText(safeRoot, "糖果店", 130, UIKit.SugarPink);
            var trt = (RectTransform)title.transform;
            trt.anchorMin = new Vector2(0.5f, 1);
            trt.anchorMax = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -420);

            var subtitle = UIKit.CreateText(safeRoot, "欢迎光临～今天也想快点打烊吗？", 36, UIKit.Cocoa);
            var srt = (RectTransform)subtitle.transform;
            srt.anchorMin = new Vector2(0.5f, 1);
            srt.anchorMax = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -560);

            // Best score
            _bestText = UIKit.CreateText(safeRoot, "", 40, UIKit.Cocoa);
            var brt = (RectTransform)_bestText.transform;
            brt.anchorMin = new Vector2(0.5f, 1);
            brt.anchorMax = new Vector2(0.5f, 1);
            brt.anchoredPosition = new Vector2(0, -650);

            // Streak dots (7)
            _streakRow = UIKit.CreatePanel(safeRoot, "Streak", new Color(0, 0, 0, 0));
            UIKit.Place(_streakRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-300, -760), new Vector2(300, -700));

            // 今日配方 banner
            var banner = UIKit.CreatePanel(safeRoot, "ChallengeBanner", UIKit.Grape);
            banner.anchorMin = new Vector2(0.5f, 1);
            banner.anchorMax = new Vector2(0.5f, 1);
            banner.sizeDelta = new Vector2(900, 120);
            banner.anchoredPosition = new Vector2(0, -880);
            _challengeBanner = UIKit.CreateText(banner, "", 38, Color.white);
            UIKit.Stretch((RectTransform)_challengeBanner.transform, banner);
            var bannerBtn = banner.gameObject.AddComponent<Button>();
            banner.GetComponent<Image>().raycastTarget = true;
            bannerBtn.onClick.AddListener(OnBannerTapped);
            _challengeBannerButton = bannerBtn;

            // Buttons
            var startBtn = UIKit.CreateButton(safeRoot, "开始营业", new Vector2(760, 190), UIKit.SugarPink, 56);
            var startRt = (RectTransform)startBtn.transform;
            startRt.anchorMin = new Vector2(0.5f, 0.5f);
            startRt.anchorMax = new Vector2(0.5f, 0.5f);
            startRt.anchoredPosition = new Vector2(0, -100);
            startBtn.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Game));

            var shopBtn = UIKit.CreateButton(safeRoot, "配方商店", new Vector2(760, 160), UIKit.SkyMint, 48, UIKit.Cocoa);
            var shopRt = (RectTransform)shopBtn.transform;
            shopRt.anchorMin = new Vector2(0.5f, 0.5f);
            shopRt.anchorMax = new Vector2(0.5f, 0.5f);
            shopRt.anchoredPosition = new Vector2(0, -340);
            shopBtn.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.RecipeShop));

            var settingsBtn = UIKit.CreateButton(safeRoot, "设置", new Vector2(400, 120), new Color(0.78f, 0.75f, 0.7f), 40, UIKit.Cocoa);
            var setRt = (RectTransform)settingsBtn.transform;
            setRt.anchorMin = new Vector2(0.5f, 0);
            setRt.anchorMax = new Vector2(0.5f, 0);
            setRt.anchoredPosition = new Vector2(0, 220);
            settingsBtn.onClick.AddListener(() => _settingsPopup.SetActive(true));

            // Sign-in popup
            var signInDim = UIKit.CreatePanel(canvas.transform, "SignInDim", new Color(0, 0, 0, 0.5f));
            UIKit.Stretch(signInDim, canvas.transform);
            signInDim.GetComponent<Image>().raycastTarget = true;
            _signInPopup = signInDim.gameObject;

            var panel = UIKit.CreatePanel(signInDim, "SignInPanel", UIKit.Cream);
            panel.sizeDelta = new Vector2(880, 800);
            panel.anchoredPosition = Vector2.zero;

            var st = UIKit.CreateText(panel, "每日签到", 64, UIKit.SugarPink);
            st.rectTransform.anchoredPosition = new Vector2(0, 280);

            // 7-dot streak inside popup
            var dotsRow = UIKit.CreatePanel(panel, "Dots", new Color(0, 0, 0, 0));
            dotsRow.sizeDelta = new Vector2(640, 70);
            dotsRow.anchoredPosition = new Vector2(0, 170);
            for (int i = 0; i < 7; i++)
            {
                var dot = UIKit.CreatePanel(dotsRow, "Dot" + i, UIKit.Lemon);
                dot.sizeDelta = Vector2.one * 64;
                float x = -288 + i * 96;
                dot.anchorMin = new Vector2(0.5f, 0.5f);
                dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.anchoredPosition = new Vector2(x, 0);
                dot.name = "Dot_" + i;
            }

            _signInBody = UIKit.CreateText(panel, "", 42, UIKit.Cocoa, TextAnchor.UpperCenter);
            var bodyRt = (RectTransform)_signInBody.transform;
            bodyRt.sizeDelta = new Vector2(780, 220);
            bodyRt.anchoredPosition = new Vector2(0, -30);

            _extraAdButton = UIKit.CreateButton(panel, "看广告再领 50 金币", new Vector2(660, 130), UIKit.Lemon, 38, UIKit.Cocoa);
            _extraAdButton.transform.localPosition = new Vector3(0, -220, 0);
            _extraAdButton.onClick.AddListener(ClaimExtraAd);

            var closeSign = UIKit.CreateButton(panel, "领取", new Vector2(400, 130), UIKit.SugarPink);
            closeSign.transform.localPosition = new Vector3(0, -330, 0);
            closeSign.onClick.AddListener(() => { _signInPopup.SetActive(false); RefreshAll(); });

            _signInPopup.SetActive(false);

            // Settings popup
            var setDim = UIKit.CreatePanel(canvas.transform, "SettingsDim", new Color(0, 0, 0, 0.5f));
            UIKit.Stretch(setDim, canvas.transform);
            setDim.GetComponent<Image>().raycastTarget = true;
            _settingsPopup = setDim.gameObject;

            var sp = UIKit.CreatePanel(setDim, "SettingsPanel", UIKit.Cream);
            sp.sizeDelta = new Vector2(760, 620);
            sp.anchoredPosition = Vector2.zero;

            var setTitle = UIKit.CreateText(sp, "设置", 56, UIKit.Cocoa);
            setTitle.rectTransform.anchoredPosition = new Vector2(0, 210);

            AddSettingsToggle(sp, "音乐", SaveDataService.Current.musicEnabled, v =>
            {
                SaveDataService.Current.musicEnabled = v;
                SaveDataService.Save();
            }, 90);
            AddSettingsToggle(sp, "音效", SaveDataService.Current.sfxEnabled, v =>
            {
                SaveDataService.Current.sfxEnabled = v;
                SaveDataService.Save();
            }, -20);
            AddSettingsToggle(sp, "振动", SaveDataService.Current.hapticsEnabled, v =>
            {
                SaveDataService.Current.hapticsEnabled = v;
                SaveDataService.Save();
            }, -130);

            var closeSet = UIKit.CreateButton(sp, "关闭", new Vector2(360, 110),
                new Color(0.78f, 0.75f, 0.7f), 38, UIKit.Cocoa);
            closeSet.transform.localPosition = new Vector3(0, -230, 0);
            closeSet.onClick.AddListener(() => _settingsPopup.SetActive(false));

            _settingsPopup.SetActive(false);
        }

        private void AddSettingsToggle(Transform parent, string label, bool value, System.Action<bool> onChanged, float y)
        {
            var row = new GameObject("Toggle_" + label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rr = (RectTransform)row.transform;
            rr.sizeDelta = new Vector2(600, 84);
            rr.anchoredPosition = new Vector2(0, y);

            var t = UIKit.CreateText(row.transform, label, 40, UIKit.Cocoa, TextAnchor.MiddleLeft);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = new Vector2(0, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = new Vector2(350, 0);

            var btn = UIKit.CreateButton(row.transform, value ? "开" : "关", new Vector2(150, 78),
                value ? UIKit.SkyMint : new Color(0.72f, 0.72f, 0.72f), 36, UIKit.Cocoa);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-85, 0);

            btn.onClick.AddListener(() =>
            {
                var txt = btn.GetComponentInChildren<Text>();
                bool isOn = txt.text == "开";
                isOn = !isOn;
                txt.text = isOn ? "开" : "关";
                btn.image.color = isOn ? UIKit.SkyMint : new Color(0.72f, 0.72f, 0.72f);
                onChanged(isOn);
            });
        }

        private void ClaimExtraAd()
        {
            var ads = AdServiceLocator.Service;
            if (ads == null || !ads.IsReady(AdPlacement.reward_daily_extra))
            {
                _signInBody.text = "广告还没准备好";
                return;
            }
            ads.ShowRewarded(AdPlacement.reward_daily_extra, ok =>
            {
                if (ok)
                    DailySignInService.ClaimDailyExtraAd(SaveDataService.Current, EconomyManager.Config);
                _extraAdButton.gameObject.SetActive(false);
                RefreshAll();
            });
        }

        private void OnBannerTapped()
        {
            var save = SaveDataService.Current;
            var featured = DailySignInService.GetFeatured(_game.catalog, save);
            if (featured != null && !featured.isStarter &&
                System.Array.IndexOf(save.unlockedRecipeIds, featured.typeId) < 0)
                SceneManager.LoadScene(SceneNames.RecipeShop); // locked: go buy it
            else
                SceneManager.LoadScene(SceneNames.Game);
        }

        private void RefreshAll()
        {
            var save = SaveDataService.Current;
            _coinsText.text = "金币 " + save.coins;
            _bestText.text = save.bestCustomersServed > 0
                ? "历史最佳：服务 " + save.bestCustomersServed + " 位客人"
                : "开始你的第一天营业吧！";

            // Streak dots: filled = days in the current cycle up to 7.
            int filled = Mathf.Clamp(save.dailyStreak, 0, 7);
            for (int i = 0; i < 7; i++)
            {
                var dot = _streakRow.Find("Dot_" + i);
                if (dot != null)
                    dot.GetComponent<Image>().color = i < filled ? UIKit.Berry : new Color(1, 1, 1, 0.35f);
            }
            // Popup dots mirror the row.
            var popupDots = _signInPopup != null && _signInPopup.activeSelf ? _signInPopup.transform.Find("SignInPanel/Dots") : null;
            if (popupDots != null)
            {
                for (int i = 0; i < 7; i++)
                {
                    var dot = popupDots.Find("Dot_" + i);
                    if (dot != null)
                        dot.GetComponent<Image>().color = i < filled ? UIKit.Berry : new Color(1, 1, 1, 0.35f);
                }
            }

            // 今日配方 banner
            var cfg = _game.dailyChallengeConfig;
            var featuredType = DailySignInService.GetFeatured(_game.catalog, save);
            if (cfg == null || featuredType == null)
            {
                _challengeBanner.text = "今日配方：敬请期待";
            }
            else if (save.dailyChallengeClaimed)
            {
                _challengeBanner.text = "今日配方：" + featuredType.displayNameZh + " 已完成";
            }
            else if (save.dailyChallengeTypeId == "" || save.dailyChallengeDate == "")
            {
                _challengeBanner.text = "今日配方：" + featuredType.displayNameZh;
            }
            else
            {
                bool unlocked = featuredType.isStarter ||
                                System.Array.IndexOf(save.unlockedRecipeIds, featuredType.typeId) >= 0;
                _challengeBanner.text = unlocked
                    ? "今日配方：" + featuredType.displayNameZh + " 进度 " + save.dailyChallengeProgress + "/" + cfg.quota
                    : "今日配方：" + featuredType.displayNameZh + "（未解锁）";
            }
        }
    }
}
