using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CandyShop
{
    // Portrait main menu: title, run/shop/settings, coins, stamina, best score, streak dots,
    // daily sign-in popup (spec sections 8 / 8.2 / 10.2 / supplements 1.4). All copy via I18nService.
    public class MainMenuController : MonoBehaviour
    {
        private GameManager _game;

        private Text _coinsText;
        private Text _staminaText;
        private Text _bestText;
        private RectTransform _streakRow;
        private Text _challengeBanner;
        private Button _challengeBannerButton;
        private GameObject _signInPopup;
        private Text _signInTitle;
        private Text _signInBody;
        private Button _extraAdButton;
        private GameObject _settingsPopup;
        private Text _settingsTitle;
        private Text _langToggleLabel;
        private Button _langToggleButton;
        private Button _startButton;

        // Toggle rows remember their label + button so the language switch can redraw them.
        private readonly List<(string key, Text label, Button btn)> _toggles =
            new List<(string, Text, Button)>();

        private SignInResult _lastResult;
        private Text _titleText;
        private Text _subtitleText;
        private Button _shopButton;
        private Button _settingsButton;
        private Canvas _canvas;
        private Button _claimButton;
        private GameObject _emptyStaminaSheet;

        private void Awake()
        {
            if (FindObjectOfType<GameManager>() == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
            _game = FindObjectOfType<GameManager>();
            _game.EnsureConfigs();

            StaminaService.RefreshOnDateRoll(); // menu entry is a refresh point (spec 8.2)
        }

        private void Start()
        {
            BuildUI();
            RefreshAll();
            I18nService.OnLanguageChanged += RefreshAll;

            // Show the sign-in popup when this boot granted something.
            var result = DailySignInService.LastBootResult;
            if (result != null && result.anyReward)
            {
                string body = "";
                if (result.coinsGranted > 0)
                    body += I18nService.Get("signin_coins", result.coinsGranted) + "\n";
                if (!string.IsNullOrEmpty(result.grantedRecipeName))
                    body += I18nService.Get("signin_streak_reward", LocalizeCandyName(result.grantedRecipeName)) + "\n";
                if (result.allUnlockedBonus > 0)
                    body += I18nService.Get("signin_all_recipes", result.allUnlockedBonus);
                _signInBody.text = body.TrimEnd();
                bool canExtra = result.dailyExtraAdAvailable && _game.AllowOptionalAds &&
                                AdServiceLocator.Service != null &&
                                AdServiceLocator.Service.IsReady(AdPlacement.reward_daily_extra);
                _extraAdButton.gameObject.SetActive(canExtra);
                _signInPopup.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            I18nService.OnLanguageChanged -= RefreshAll;
        }

        private void BuildUI()
        {
            var canvas = UIKit.CreateCanvas(null, "MainMenu");
            _canvas = canvas;
            UIKit.CreateBackground(canvas.transform, "bg_main_menu");
            var safeRoot = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvas.transform, false);
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            // Coins top-right
            var coinsPanel = UIKit.CreatePanel(safeRoot, "Coins", UIKit.Cream);
            coinsPanel.sizeDelta = new Vector2(280, 90);
            coinsPanel.anchorMin = new Vector2(1, 1);
            coinsPanel.anchorMax = new Vector2(1, 1);
            coinsPanel.anchoredPosition = new Vector2(-170, -80);
            var menuCoinIcon = UIKit.CreateIcon(coinsPanel, "icon_coin", Vector2.one * 64f);
            var mci = (RectTransform)menuCoinIcon.transform;
            mci.anchorMin = new Vector2(0, 0.5f);
            mci.anchorMax = new Vector2(0, 0.5f);
            mci.anchoredPosition = new Vector2(40, 0);
            _coinsText = UIKit.CreateText(coinsPanel, "", 36, UIKit.Cocoa, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)_coinsText.transform,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(80, 0), new Vector2(-8, 0));

            // Stamina n/20 next to coins (spec 8.2 Main Menu gate)
            var staminaPanel = UIKit.CreatePanel(safeRoot, "Stamina", UIKit.Cream);
            staminaPanel.sizeDelta = new Vector2(280, 90);
            staminaPanel.anchorMin = new Vector2(1, 1);
            staminaPanel.anchorMax = new Vector2(1, 1);
            staminaPanel.anchoredPosition = new Vector2(-170, -190);
            var menuStamIcon = UIKit.CreateIcon(staminaPanel, "icon_stamina", Vector2.one * 64f);
            var msi = (RectTransform)menuStamIcon.transform;
            msi.anchorMin = new Vector2(0, 0.5f);
            msi.anchorMax = new Vector2(0, 0.5f);
            msi.anchoredPosition = new Vector2(40, 0);
            _staminaText = UIKit.CreateText(staminaPanel, "", 36, UIKit.Cocoa, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)_staminaText.transform,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(80, 0), new Vector2(-8, 0));

            // Title
            _titleText = UIKit.CreateText(safeRoot, "", 130, UIKit.SugarPink);
            var trt = (RectTransform)_titleText.transform;
            trt.anchorMin = new Vector2(0.5f, 1);
            trt.anchorMax = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -420);

            _subtitleText = UIKit.CreateText(safeRoot, "", 36, UIKit.Cocoa);
            var srt = (RectTransform)_subtitleText.transform;
            srt.anchorMin = new Vector2(0.5f, 1);
            srt.anchorMax = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -560);

            // Best score
            _bestText = UIKit.CreateText(safeRoot, "", 40, UIKit.Cocoa);
            var brt = (RectTransform)_bestText.transform;
            brt.anchorMin = new Vector2(0.5f, 1);
            brt.anchorMax = new Vector2(0.5f, 1);
            brt.anchoredPosition = new Vector2(0, -650);

            // Streak dots (7) on the menu itself (spec 10.2 / supplements 1.4).
            _streakRow = UIKit.CreatePanel(safeRoot, "Streak", new Color(0, 0, 0, 0));
            UIKit.Place(_streakRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(-300, -760), new Vector2(300, -700));
            for (int i = 0; i < 7; i++)
            {
                var dot = UIKit.CreatePanel(_streakRow, "Dot" + i, UIKit.Lemon);
                dot.sizeDelta = Vector2.one * 56;
                dot.anchorMin = new Vector2(0.5f, 0.5f);
                dot.anchorMax = new Vector2(0.5f, 0.5f);
                dot.anchoredPosition = new Vector2(-192 + i * 64, 0);
                dot.name = "Dot_" + i;
            }

            // Daily recipe banner
            var banner = UIKit.CreatePanel(safeRoot, "ChallengeBanner", UIKit.Grape);
            banner.anchorMin = new Vector2(0.5f, 1);
            banner.anchorMax = new Vector2(0.5f, 1);
            banner.sizeDelta = new Vector2(900, 120);
            banner.anchoredPosition = new Vector2(0, -880);
            _challengeBanner = UIKit.CreateText(banner, "", 38, Color.white);
            _challengeBanner.name = "ChallengeBannerText";
            UIKit.Stretch((RectTransform)_challengeBanner.transform, banner);
            var bannerBtn = banner.gameObject.AddComponent<Button>();
            banner.GetComponent<Image>().raycastTarget = true;
            bannerBtn.onClick.AddListener(OnBannerTapped);
            _challengeBannerButton = bannerBtn;

            // Buttons
            _startButton = UIKit.CreateButton(safeRoot, "", new Vector2(760, 190), UIKit.SugarPink, 56);
            _startButton.name = "StartButton";
            var startRt = (RectTransform)_startButton.transform;
            startRt.anchorMin = new Vector2(0.5f, 0.5f);
            startRt.anchorMax = new Vector2(0.5f, 0.5f);
            startRt.anchoredPosition = new Vector2(0, -100);
            _startButton.onClick.AddListener(OnStartTapped);

            var shopBtn = UIKit.CreateButton(safeRoot, "", new Vector2(760, 160), UIKit.SkyMint, 48, UIKit.Cocoa);
            _shopButton = shopBtn;
            var shopRt = (RectTransform)shopBtn.transform;
            shopRt.anchorMin = new Vector2(0.5f, 0.5f);
            shopRt.anchorMax = new Vector2(0.5f, 0.5f);
            shopRt.anchoredPosition = new Vector2(0, -340);
            shopBtn.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.RecipeShop));

            var settingsBtn = UIKit.CreateButton(safeRoot, "", new Vector2(400, 120), new Color(0.78f, 0.75f, 0.7f), 40, UIKit.Cocoa);
            _settingsButton = settingsBtn;
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

            _signInTitle = UIKit.CreateText(panel, "", 64, UIKit.SugarPink);
            _signInTitle.rectTransform.anchoredPosition = new Vector2(0, 280);
            UIKit.CreateIcon(panel, "popup_signin", Vector2.one * 110f).rectTransform.anchoredPosition = new Vector2(0, 200);

            // 7-dot streak inside popup
            var dotsRow = UIKit.CreatePanel(panel, "Dots", new Color(0, 0, 0, 0));
            dotsRow.sizeDelta = new Vector2(640, 70);
            dotsRow.anchoredPosition = new Vector2(0, 90);
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

            _extraAdButton = UIKit.CreateButton(panel, "", new Vector2(660, 130), UIKit.Lemon, 38, UIKit.Cocoa);
            _extraAdButton.transform.localPosition = new Vector3(0, -220, 0);
            _extraAdButton.onClick.AddListener(ClaimExtraAd);

            _claimButton = UIKit.CreateButton(panel, "", new Vector2(400, 130), UIKit.SugarPink);
            _claimButton.transform.localPosition = new Vector3(0, -330, 0);
            _claimButton.onClick.AddListener(() => { _signInPopup.SetActive(false); RefreshAll(); });

            _signInPopup.SetActive(false);

            // Settings popup
            var setDim = UIKit.CreatePanel(canvas.transform, "SettingsDim", new Color(0, 0, 0, 0.5f));
            UIKit.Stretch(setDim, canvas.transform);
            setDim.GetComponent<Image>().raycastTarget = true;
            _settingsPopup = setDim.gameObject;

            var sp = UIKit.CreatePanel(setDim, "SettingsPanel", UIKit.Cream);
            sp.sizeDelta = new Vector2(760, 760);
            sp.anchoredPosition = Vector2.zero;

            _settingsTitle = UIKit.CreateText(sp, "", 56, UIKit.Cocoa);
            _settingsTitle.rectTransform.anchoredPosition = new Vector2(0, 290);

            AddSettingsToggle(sp, "label_music", SaveDataService.Current.musicEnabled, v =>
            {
                SaveDataService.Current.musicEnabled = v;
                SaveDataService.Save();
            }, 180);
            AddSettingsToggle(sp, "label_sfx", SaveDataService.Current.sfxEnabled, v =>
            {
                SaveDataService.Current.sfxEnabled = v;
                SaveDataService.Save();
            }, 70);
            AddSettingsToggle(sp, "label_haptics", SaveDataService.Current.hapticsEnabled, v =>
            {
                SaveDataService.Current.hapticsEnabled = v;
                SaveDataService.Save();
            }, -40);

            AddLanguageToggle(sp, -150);

            var closeSet = UIKit.CreateButton(sp, "", new Vector2(360, 110),
                new Color(0.78f, 0.75f, 0.7f), 38, UIKit.Cocoa);
            closeSet.transform.localPosition = new Vector3(0, -300, 0);
            closeSet.onClick.AddListener(() => _settingsPopup.SetActive(false));

            _settingsPopup.SetActive(false);
        }

        private void AddSettingsToggle(Transform parent, string i18nKey, bool value,
            System.Action<bool> onChanged, float y)
        {
            var row = new GameObject("Toggle_" + i18nKey, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rr = (RectTransform)row.transform;
            rr.sizeDelta = new Vector2(600, 84);
            rr.anchoredPosition = new Vector2(0, y);

            var t = UIKit.CreateText(row.transform, "", 40, UIKit.Cocoa, TextAnchor.MiddleLeft);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = new Vector2(0, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = new Vector2(350, 0);

            var btn = UIKit.CreateButton(row.transform, value ? I18nService.Get("toggle_on") : I18nService.Get("toggle_off"),
                new Vector2(150, 78), value ? UIKit.SkyMint : new Color(0.72f, 0.72f, 0.72f), 36, UIKit.Cocoa);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-85, 0);

            btn.onClick.AddListener(() =>
            {
                var txt = btn.GetComponentInChildren<Text>();
                bool isOn = txt.text == I18nService.Get("toggle_on");
                isOn = !isOn;
                txt.text = isOn ? I18nService.Get("toggle_on") : I18nService.Get("toggle_off");
                btn.image.color = isOn ? UIKit.SkyMint : new Color(0.72f, 0.72f, 0.72f);
                onChanged(isOn);
            });

            _toggles.Add((i18nKey, t, btn));
        }

        // Bilingual labels on this row so a player stuck in the wrong language can still switch (i18n spec section 1).
        private void AddLanguageToggle(Transform parent, float y)
        {
            var row = new GameObject("Toggle_Language", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rr = (RectTransform)row.transform;
            rr.sizeDelta = new Vector2(600, 84);
            rr.anchoredPosition = new Vector2(0, y);

            _langToggleLabel = UIKit.CreateText(row.transform, "", 40, UIKit.Cocoa, TextAnchor.MiddleLeft);
            var trt = (RectTransform)_langToggleLabel.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = new Vector2(0, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = new Vector2(350, 0);

            var currentIsEn = I18nService.Language == "en";
            _langToggleButton = UIKit.CreateButton(row.transform,
                currentIsEn ? I18nService.Get("lang_zh") : I18nService.Get("lang_en"),
                new Vector2(150, 78), UIKit.Grape, 32);
            var btn = _langToggleButton;
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-85, 0);

            btn.onClick.AddListener(() => I18nService.ToggleLanguage());
        }

        // Start tap: refresh stamina, gate at < 1 with the empty sheet; never enter Game scene (spec 8.2).
        private void OnStartTapped()
        {
            StaminaService.RefreshOnDateRoll();
            if (!StaminaService.CanStartGuest)
            {
                ShowEmptyStaminaSheet();
                return;
            }
            SceneManager.LoadScene(SceneNames.Game);
        }

        private void ShowEmptyStaminaSheet()
        {
            if (_startButton != null)
            {
                // Disabled look per spec: greyed but still tappable to open the sheet.
                _startButton.image.color = new Color(0.78f, 0.74f, 0.72f);
                StartCoroutine(RestoreStartColorNextFrame());
            }

            if (_emptyStaminaSheet != null)
            {
                _emptyStaminaSheet.SetActive(true);
                return;
            }

            var dim = UIKit.CreatePanel(_canvas.transform, "EmptyStaminaDim", new Color(0, 0, 0, 0.5f));
            UIKit.Stretch(dim, _canvas.transform);
            dim.GetComponent<Image>().raycastTarget = true;
            _emptyStaminaSheet = dim.gameObject;

            var panel = UIKit.CreatePanel(dim, "EmptyStaminaPanel", UIKit.Cream);
            panel.sizeDelta = new Vector2(800, 520);
            panel.anchoredPosition = Vector2.zero;

            var title = UIKit.CreateText(panel, "", 56, UIKit.Berry);
            title.name = "EmptyTitle";
            title.rectTransform.anchoredPosition = new Vector2(0, 140);

            var body = UIKit.CreateText(panel, "", 40, UIKit.Cocoa);
            body.name = "EmptyBody";
            body.rectTransform.anchoredPosition = new Vector2(0, 20);

            var close = UIKit.CreateButton(panel, "", new Vector2(400, 120), UIKit.SugarPink);
            close.transform.localPosition = new Vector3(0, -140, 0);
            close.onClick.AddListener(() => _emptyStaminaSheet.SetActive(false));

            // Fill texts now (and again on language change via RefreshAll).
            SetTextSafe(panel, "EmptyTitle", I18nService.Get("stamina_empty_title"));
            SetTextSafe(panel, "EmptyBody", I18nService.Get("stamina_empty_body"));
            SetButtonText(close, I18nService.Get("btn_confirm"));
        }

        private IEnumerator RestoreStartColorNextFrame()
        {
            yield return null;
            if (_startButton != null && StaminaService.CanStartGuest)
                _startButton.image.color = UIKit.SugarPink;
        }

        private void ClaimExtraAd()
        {
            var ads = AdServiceLocator.Service;
            if (ads == null || !ads.IsReady(AdPlacement.reward_daily_extra))
            {
                _signInBody.text = I18nService.Get("ad_not_ready");
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
                OnStartTapped(); // same stamina gate as the start button (spec 8.1)
        }

        private void RefreshAll()
        {
            var save = SaveDataService.Current;
            _coinsText.text = I18nService.Get("label_coins", save.coins);
            _staminaText.text = I18nService.Get("stamina_label_frac", StaminaService.Current,
                _game.staminaConfig != null ? _game.staminaConfig.dailyMax : 20);

            // Start button look tracks the gate (spec 8.2: grey when empty).
            if (_startButton != null)
                _startButton.image.color = StaminaService.CanStartGuest
                    ? UIKit.SugarPink
                    : new Color(0.78f, 0.74f, 0.72f);

            _titleText.text = I18nService.Get("app_title");
            if (_subtitleText != null) _subtitleText.text = I18nService.Get("menu_subtitle");
            if (_shopButton != null) SetButtonText(_shopButton, I18nService.Get("btn_recipes"));
            if (_settingsButton != null) SetButtonText(_settingsButton, I18nService.Get("btn_settings"));
            if (_startButton != null)
                SetButtonText(_startButton, I18nService.Get("btn_start"));

            _bestText.text = save.bestCustomersServed > 0
                ? I18nService.Get("best_served", save.bestCustomersServed)
                : I18nService.Get("menu_first_day");

            // Streak dots: filled = days in the current cycle up to 7.
            int filled = Mathf.Clamp(save.dailyStreak, 0, 7);
            for (int i = 0; i < 7; i++)
            {
                var dot = _streakRow.Find("Dot_" + i);
                if (dot != null)
                    dot.GetComponent<Image>().color = i < filled ? UIKit.Berry : new Color(1, 1, 1, 0.35f);
            }
            // Popup dots mirror the row.
            var popupDots = _signInPopup != null && _signInPopup.activeSelf
                ? _signInPopup.transform.Find("SignInPanel/Dots") : null;
            if (popupDots != null)
            {
                for (int i = 0; i < 7; i++)
                {
                    var dot = popupDots.Find("Dot_" + i);
                    if (dot != null)
                        dot.GetComponent<Image>().color = i < filled ? UIKit.Berry : new Color(1, 1, 1, 0.35f);
                }
            }

            // Daily recipe banner
            var cfg = _game.dailyChallengeConfig;
            var featuredType = DailySignInService.GetFeatured(_game.catalog, save);
            if (cfg == null || featuredType == null)
            {
                _challengeBanner.text = I18nService.Get("daily_recipe_tap_pending");
            }
            else if (save.dailyChallengeClaimed)
            {
                _challengeBanner.text = I18nService.Get("daily_recipe") + "：" +
                                        featuredType.LocalizedName + " " + I18nService.Get("daily_recipe_done");
            }
            else if (save.dailyChallengeTypeId == "" || save.dailyChallengeDate == "")
            {
                _challengeBanner.text = I18nService.Get("daily_recipe") + "：" + featuredType.LocalizedName;
            }
            else
            {
                bool unlocked = featuredType.isStarter ||
                                System.Array.IndexOf(save.unlockedRecipeIds, featuredType.typeId) >= 0;
                _challengeBanner.text = unlocked
                    ? I18nService.Get("daily_recipe_progress", featuredType.LocalizedName,
                        save.dailyChallengeProgress, cfg.quota)
                    : I18nService.Get("daily_recipe") + "：" + featuredType.LocalizedName +
                      I18nService.Get("daily_recipe_locked");
            }

            // Settings popup texts
            if (_settingsTitle != null) _settingsTitle.text = I18nService.Get("btn_settings");
            foreach (var entry in _toggles)
            {
                if (entry.label != null) entry.label.text = I18nService.Get(entry.key);
                if (entry.btn != null)
                {
                    var txt = entry.btn.GetComponentInChildren<Text>();
                    bool isOn = txt != null && txt.text == I18nService.Get("toggle_on");
                    if (txt != null) txt.text = isOn ? I18nService.Get("toggle_on") : I18nService.Get("toggle_off");
                }
            }
            if (_langToggleLabel != null)
                _langToggleLabel.text = I18nService.Get("label_language") + " / Language";
            if (_langToggleButton != null)
            {
                // Button shows the OTHER locale so the player knows what they switch to.
                SetButtonText(_langToggleButton,
                    I18nService.Language == "en" ? I18nService.Get("lang_zh") : I18nService.Get("lang_en"));
            }
            if (_extraAdButton != null)
                SetButtonText(_extraAdButton, I18nService.Get("ad_extra_50"));
            if (_claimButton != null)
                SetButtonText(_claimButton, I18nService.Get("signin_claim"));
            if (_signInTitle != null)
                _signInTitle.text = I18nService.Get("signin_title");

            // Empty-stamina sheet texts (in case the locale changed while it exists).
            if (_emptyStaminaSheet != null)
            {
                var sheetPanel = _emptyStaminaSheet.transform.Find("EmptyStaminaPanel");
                if (sheetPanel != null)
                {
                    SetTextSafe(sheetPanel, "EmptyTitle", I18nService.Get("stamina_empty_title"));
                    SetTextSafe(sheetPanel, "EmptyBody", I18nService.Get("stamina_empty_body"));
                }
            }
        }

        // Saved rewards store the zh name; re-localize it for the active locale.
        private string LocalizeCandyName(string zhName)
        {
            if (_game?.catalog == null || I18nService.Language != "en") return zhName;
            foreach (var c in _game.catalog)
                if (c != null && c.displayNameZh == zhName && !string.IsNullOrEmpty(c.displayNameEn))
                    return c.displayNameEn;
            return zhName;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private void SetTextSafe(Transform parent, string childName, string value)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                var t = child.GetComponent<Text>();
                if (t != null) t.text = value;
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            var txt = button.GetComponentInChildren<Text>();
            if (txt != null) txt.text = value;
        }
    }
}
