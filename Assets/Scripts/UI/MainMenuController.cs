using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CandyShop
{
    // Portrait main menu (spec sections 8 / 8.2 / 10.2 / supplements 1.4). All copy via I18nService.
    // Layout lives in Assets/Prefabs/UI/MainMenu.prefab; this controller only binds data.
    public class MainMenuController : MonoBehaviour
    {
        private static readonly string[] ToggleKeys = { "label_music", "label_sfx", "label_haptics" };

        [Header("HUD")]
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private TMP_Text _staminaText;
        [SerializeField] private TMP_Text _bestText;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private Image[] _streakDots; // 7
        [SerializeField] private TMP_Text _challengeBannerText;

        [Header("Buttons")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _challengeBannerButton;

        [Header("Sign-in popup")]
        [SerializeField] private GameObject _signInPopup;
        [SerializeField] private TMP_Text _signInTitle;
        [SerializeField] private TMP_Text _signInBody;
        [SerializeField] private Image[] _signInDots; // 7
        [SerializeField] private Button _extraAdButton;
        [SerializeField] private Button _claimButton;

        [Header("Settings popup")]
        [SerializeField] private GameObject _settingsPopup;
        [SerializeField] private TMP_Text _settingsTitle;
        [SerializeField] private TMP_Text[] _toggleLabels;   // aligned with ToggleKeys
        [SerializeField] private Button[] _toggleButtons; // aligned with ToggleKeys
        [SerializeField] private TMP_Text _langLabel;
        [SerializeField] private Button _langButton;
        [SerializeField] private Button _settingsCloseButton;

        [Header("Empty stamina sheet")]
        [SerializeField] private GameObject _emptyStaminaSheet;
        [SerializeField] private TMP_Text _emptyStaminaTitle;
        [SerializeField] private TMP_Text _emptyStaminaBody;
        [SerializeField] private Button _staminaAdButton;
        [SerializeField] private Button _emptyCloseButton;

        [Header("Toast")]
        [SerializeField] private RectTransform _toastTemplate;
        [SerializeField] private Canvas _canvas;

        private GameManager _game;
        private readonly List<(string key, TMP_Text label, Button btn)> _toggles =
            new List<(string, TMP_Text, Button)>();

        private void Awake()
        {
            if (FindObjectOfType<GameManager>() == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
            _game = FindObjectOfType<GameManager>();
            _game.EnsureConfigs();
            WireButtons();
        }

        private void Start()
        {
            RefreshAll();
            I18nService.OnLanguageChanged += RefreshAll;

            // Catch-up: collection milestones may be pending from a previous unlock (supplements 2.0).
            CollectionService.CheckOwnedMilestones(SaveDataService.Current, _game.recipesSortedByCost);

            // Toasts waiting from the collection long line / fail coin penalty.
            if (CollectionService.PendingGrantName != null)
            {
                StartCoroutine(ShowMenuToast(I18nService.Get("special_toast", CollectionService.PendingGrantName)));
                CollectionService.PendingGrantName = null;
            }
            else if (GameHUDController.PendingCoinPenaltyToast > 0)
            {
                StartCoroutine(ShowMenuToast(I18nService.Get("coin_penalty_toast", GameHUDController.PendingCoinPenaltyToast)));
                GameHUDController.PendingCoinPenaltyToast = 0;
            }

            // Show the sign-in popup when this boot granted something.
            var result = DailySignInService.LastBootResult;
            if (result != null && result.anyReward)
            {
                string body = "";
                if (result.coinsGranted > 0)
                    body += I18nService.Get("signin_coins", result.coinsGranted) + "\n";
                if (!string.IsNullOrEmpty(result.grantedRecipeName))
                    body += I18nService.Get("signin_streak_reward", LocalizeCandyName(result.grantedRecipeName)) + "\n";
                if (result.staminaGranted > 0)
                    body += I18nService.Get("signin_streak_stamina", result.staminaGranted) + "\n";
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

        private void WireButtons()
        {
            _startButton.onClick.AddListener(OnStartTapped);
            _shopButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.RecipeShop));
            _settingsButton.onClick.AddListener(() => _settingsPopup.SetActive(true));
            _challengeBannerButton.onClick.AddListener(OnBannerTapped);

            _extraAdButton.onClick.AddListener(ClaimExtraAd);
            _claimButton.onClick.AddListener(() => { _signInPopup.SetActive(false); RefreshAll(); });

            for (int i = 0; i < ToggleKeys.Length && i < _toggleButtons.Length; i++)
            {
                var key = ToggleKeys[i];
                var btn = _toggleButtons[i];
                AddToggleBinding(btn, v =>
                {
                    switch (key)
                    {
                        case "label_music": SaveDataService.Current.musicEnabled = v; break;
                        case "label_sfx": SaveDataService.Current.sfxEnabled = v; break;
                        case "label_haptics": SaveDataService.Current.hapticsEnabled = v; break;
                    }
                    SaveDataService.Save();
                });
                _toggles.Add((key, i < _toggleLabels.Length ? _toggleLabels[i] : null, btn));
            }

            _langButton.onClick.AddListener(I18nService.ToggleLanguage);
            _settingsCloseButton.onClick.AddListener(() => _settingsPopup.SetActive(false));

            _emptyCloseButton.onClick.AddListener(() => _emptyStaminaSheet.SetActive(false));
            _staminaAdButton.onClick.AddListener(() =>
            {
                var ads = AdServiceLocator.Service;
                if (ads == null || !ads.IsReady(AdPlacement.reward_stamina)) return;
                ads.ShowRewarded(AdPlacement.reward_stamina, ok =>
                {
                    if (!ok) return;
                    var cfg = _game.staminaConfig;
                    StaminaService.GrantHardClamped(cfg != null ? cfg.staminaAdGrant : 5);
                    RefreshAll();
                    _emptyStaminaSheet.SetActive(false);
                });
            });

            _signInPopup.SetActive(false);
            _settingsPopup.SetActive(false);
            _emptyStaminaSheet.SetActive(false);
        }

        private void AddToggleBinding(Button btn, System.Action<bool> onChanged)
        {
            btn.onClick.AddListener(() =>
            {
                var txt = btn.GetComponentInChildren<TMP_Text>();
                bool isOn = txt.text == I18nService.Get("toggle_on");
                isOn = !isOn;
                txt.text = isOn ? I18nService.Get("toggle_on") : I18nService.Get("toggle_off");
                btn.image.color = isOn ? Color.white : new Color(0.72f, 0.72f, 0.72f);
                onChanged(isOn);
            });
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
            // Disabled look per spec: greyed but still tappable to open the sheet.
            _startButton.image.color = new Color(0.78f, 0.74f, 0.72f);
            StartCoroutine(RestoreStartColorNextFrame());

            _emptyStaminaSheet.SetActive(true);
            _emptyStaminaTitle.text = I18nService.Get("stamina_empty_title");
            _emptyStaminaBody.text = I18nService.Get("stamina_empty_body");
            RefreshEmptyStaminaSheet();
        }

        // Show/hide the stamina-ad button on the empty sheet per readiness (supplements 2.0).
        private void RefreshEmptyStaminaSheet()
        {
            if (_emptyStaminaSheet == null || !_emptyStaminaSheet.activeSelf) return;
            var ads = AdServiceLocator.Service;
            bool ready = ads != null && ads.IsReady(AdPlacement.reward_stamina) && !StaminaService.CanStartGuest;
            _staminaAdButton.gameObject.SetActive(ready);
            if (ready)
            {
                var cfg = _game.staminaConfig;
                _staminaAdButton.GetComponentInChildren<TMP_Text>().text =
                    I18nService.Get("ad_stamina", cfg != null ? cfg.staminaAdGrant : 5);
            }
        }

        private IEnumerator RestoreStartColorNextFrame()
        {
            yield return null;
            if (_startButton != null && StaminaService.CanStartGuest)
                _startButton.image.color = Color.white;
        }

        private IEnumerator ShowMenuToast(string message)
        {
            yield return new WaitForSeconds(0.4f);
            var toast = Instantiate(_toastTemplate, _canvas.transform);
            toast.gameObject.SetActive(true);
            var txt = toast.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = message;
            yield return new WaitForSeconds(2.4f);
            Destroy(toast.gameObject);
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

            // Stamina-ad button readiness on the empty sheet (supplements 2.0).
            RefreshEmptyStaminaSheet();

            // Start button look tracks the gate (spec 8.2: grey when empty).
            _startButton.image.color = StaminaService.CanStartGuest
                ? Color.white
                : new Color(0.78f, 0.74f, 0.72f);

            _titleText.text = I18nService.Get("app_title");
            if (_subtitleText != null) _subtitleText.text = I18nService.Get("menu_subtitle");
            SetButtonText(_shopButton, I18nService.Get("btn_recipes"));
            SetButtonText(_settingsButton, I18nService.Get("btn_settings"));
            SetButtonText(_startButton, I18nService.Get("btn_start"));

            _bestText.text = save.bestCustomersServed > 0
                ? I18nService.Get("best_served", save.bestCustomersServed)
                : I18nService.Get("menu_first_day");

            // Streak dots: filled = days in the current cycle up to 7.
            int filled = Mathf.Clamp(save.dailyStreak, 0, 7);
            SetDotsFilled(_streakDots, filled);
            SetDotsFilled(_signInDots, filled); // popup dots mirror the row

            // Daily recipe banner
            var cfg = _game.dailyChallengeConfig;
            var featuredType = DailySignInService.GetFeatured(_game.catalog, save);
            if (cfg == null || featuredType == null)
            {
                _challengeBannerText.text = I18nService.Get("daily_recipe_tap_pending");
            }
            else if (save.dailyChallengeClaimed)
            {
                _challengeBannerText.text = I18nService.Get("daily_recipe") + "：" +
                                            featuredType.LocalizedName + " " + I18nService.Get("daily_recipe_done");
            }
            else if (save.dailyChallengeTypeId == "" || save.dailyChallengeDate == "")
            {
                _challengeBannerText.text = I18nService.Get("daily_recipe") + "：" + featuredType.LocalizedName;
            }
            else
            {
                bool unlocked = featuredType.isStarter ||
                                System.Array.IndexOf(save.unlockedRecipeIds, featuredType.typeId) >= 0;
                _challengeBannerText.text = unlocked
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
                    var txt = entry.btn.GetComponentInChildren<TMP_Text>();
                    bool isOn = txt != null && txt.text == I18nService.Get("toggle_on");
                    if (txt != null) txt.text = isOn ? I18nService.Get("toggle_on") : I18nService.Get("toggle_off");
                }
            }
            if (_langLabel != null)
                _langLabel.text = I18nService.Get("label_language");
            if (_langButton != null)
            {
                // Button shows the OTHER locale so the player knows what they switch to.
                SetButtonText(_langButton,
                    I18nService.Language == "en" ? I18nService.Get("lang_zh") : I18nService.Get("lang_en"));
            }
            SetButtonText(_settingsCloseButton, I18nService.Get("btn_back"));
            SetButtonText(_emptyCloseButton, I18nService.Get("btn_confirm"));
            SetButtonText(_extraAdButton, I18nService.Get("ad_extra_50"));
            SetButtonText(_claimButton, I18nService.Get("signin_claim"));
            if (_signInTitle != null)
                _signInTitle.text = I18nService.Get("signin_title");

            // Empty-stamina sheet texts (in case the locale changed while it exists).
            if (_emptyStaminaSheet != null && _emptyStaminaSheet.activeSelf)
            {
                _emptyStaminaTitle.text = I18nService.Get("stamina_empty_title");
                _emptyStaminaBody.text = I18nService.Get("stamina_empty_body");
            }
        }

        private static void SetDotsFilled(Image[] dots, int filled)
        {
            if (dots == null) return;
            for (int i = 0; i < dots.Length; i++)
                if (dots[i] != null)
                    dots[i].color = i < filled ? UIKit.Berry : new Color(1, 1, 1, 0.35f);
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

        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            var txt = button.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = value;
        }
    }
}
