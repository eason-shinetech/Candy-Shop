using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace CandyShop
{
    // Drives the in-run portrait HUD (spec section 10.4 + supplements section 1).
    // Layout lives in Assets/Prefabs/UI/GameHUD.prefab; this controller only binds data.
    public class GameHUDController : MonoBehaviour
    {
        private static readonly string[] PauseToggleKeys = { "label_music", "label_sfx", "label_haptics" };

        [Header("Top bar")]
        [SerializeField] private TMP_Text _starsAnchor; // invisible spotlight target
        [SerializeField] private Image[] _starIcons;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private TMP_Text _staminaText;
        [SerializeField] private Image _vignette;
        [SerializeField] private RectTransform _timerFill;
        [SerializeField] private Image _timerFillImage;
        [SerializeField] private RectTransform _queueStrip;
        [SerializeField] private RectTransform _orderChipsRow;
        [SerializeField] private TMP_Text _challengeChipText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private TMP_Text _perfectStamp;

        [Header("Bottom bar")]
        [SerializeField] private RectTransform _bottomBar;
        [SerializeField] private PowerButton[] _powerButtons;

        [Header("Pause")]
        [SerializeField] private GameObject _pausePopup;
        [SerializeField] private TMP_Text _pauseTitle;
        [SerializeField] private TMP_Text _pauseQuitHint;
        [SerializeField] private Button _pauseContinueBtn;
        [SerializeField] private Button _pauseQuitBtn;
        [SerializeField] private Button _pauseLangButton;
        [SerializeField] private TMP_Text _pauseLangLabel;
        [SerializeField] private TMP_Text[] _pauseToggleLabels;   // aligned with PauseToggleKeys
        [SerializeField] private Button[] _pauseToggleButtons;

        [Header("Quit confirm")]
        [SerializeField] private GameObject _quitConfirm;
        [SerializeField] private TMP_Text _quitConfirmBody;
        [SerializeField] private Button _quitYesBtn;
        [SerializeField] private Button _quitNoBtn;

        [Header("Game over")]
        [SerializeField] private GameObject _gameOverPopup;
        [SerializeField] private TMP_Text _gameOverTitle;
        [SerializeField] private TMP_Text _gameOverStats;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _menuButton;

        [Header("Serve chip")]
        [SerializeField] private GameObject _servePopup;
        [SerializeField] private TMP_Text _serveRewardText;
        [SerializeField] private Button _doubleButton;

        [Header("Buy sheet")]
        [SerializeField] private GameObject _buySheet;
        [SerializeField] private TMP_Text _buyTitle;
        [SerializeField] private TMP_Text _buyPrice;
        [SerializeField] private TMP_Text _buyNote;
        [SerializeField] private TMP_Text _buyMessage;
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _buyCancelBtn;
        [SerializeField] private GameObject _insufficientSheet;
        [SerializeField] private TMP_Text _insufficientMsg;
        [SerializeField] private Button _insufficientAdBtn;
        [SerializeField] private Button _insufficientCloseBtn;

        [Header("Shift over")]
        [SerializeField] private GameObject _shiftOverPopup;
        [SerializeField] private TMP_Text _shiftOverTitle;
        [SerializeField] private TMP_Text _shiftOverBody;
        [SerializeField] private TMP_Text _shiftOverStats;
        [SerializeField] private Button _shiftOverMenuBtn;
        [SerializeField] private Button _shiftOverAdBtn;

        [Header("Tutorial")]
        [SerializeField] private GameObject _tutorialPopup;
        [SerializeField] private TMP_Text _tutorialBody;
        [SerializeField] private Button _tutorialNext;
        [SerializeField] private Button _tutorialSkip;

        [Header("Dynamic items")]
        [SerializeField] private OrderChip _orderChipPrefab;
        [SerializeField] private CustomerCard _currentCardPrefab;
        [SerializeField] private CustomerCard _waitingCardPrefab;

        [Header("Feedback")]
        [SerializeField] private TMP_Text _staminaFloatText;
        [SerializeField] private RectTransform _toastTemplate;

        private GameManager _game;
        private CustomerOrderManager _orders;
        private PowerUpManager _powerUps;

        private readonly Dictionary<string, OrderChip> _chipsByType =
            new Dictionary<string, OrderChip>();

        private Coroutine _staminaFloatCo;
        private Coroutine _comboCo;
        private Coroutine _stampCo;

        private int _comboCount;
        private bool _challengeWasClaimed;
        private int _tutorialStep;
        private bool _runStarted;
        private int _bestAtRunStart;
        private string _lastFailReason;
        private bool _failPenaltyApplied;
        private float _badgeTimer;
        private int _lastCoins = int.MinValue;
        private Coroutine _autoContinueCo;
        private PowerUpDefinition _buySheetDef;
        private bool _pausedForBuySheet;

        public static string PendingRecipeUnlockToast; // set by RecipeShopController
        public static int PendingCoinPenaltyToast; // fail coin loss shown on the next menu load

        private void Awake()
        {
            _game = GameManager.Instance;
            _orders = FindObjectOfType<CustomerOrderManager>();
            _powerUps = FindObjectOfType<PowerUpManager>();
            if (_powerUps == null) _powerUps = gameObject.AddComponent<PowerUpManager>();
            _bestAtRunStart = SaveDataService.Current.bestCustomersServed;
        }

        private void Start()
        {
            CreatePowerButtons();
            WireButtons();
            WireEvents();
            StaminaService.StaminaChanged += OnStaminaChanged;
            StaminaService.FloatingTextRequested += ShowStaminaFloat;
            I18nService.OnLanguageChanged += RefreshLocalizedTexts;

            if (PendingRecipeUnlockToast != null)
            {
                StartCoroutine(ShowToastRoutine(string.Format(I18nService.Get("recipe_new_toast"), PendingRecipeUnlockToast), 0.6f));
                PendingRecipeUnlockToast = null;
            }

            // First run: 3-card tutorial, timer starts after dismissal.
            if (!SaveDataService.Current.tutorialDone)
                ShowTutorial(1);
            else
                BeginRun();
        }

        private void OnDestroy()
        {
            if (_orders != null)
            {
                _orders.OrderStarted -= OnOrderStarted;
                _orders.QueueChanged -= RebuildQueueStrip;
                _orders.TimerTick -= OnTimerTick;
                _orders.CorrectPick -= OnCorrectPick;
                _orders.WrongPick -= OnWrongPick;
                _orders.ServeCompletedRewardText -= OnServeCompleted;
                _orders.ServePerfectStamp -= OnPerfectStamp;
                _orders.BuriedHintRequested -= OnBuriedHint;
            }
            if (_game != null)
            {
                _game.StarsChanged -= RefreshStars;
                _game.RunEnded -= OnRunEnded;
            }
            if (_powerUps != null)
                _powerUps.BuySheetRequested -= OpenBuySheet;
            StaminaService.StaminaChanged -= OnStaminaChanged;
            StaminaService.FloatingTextRequested -= ShowStaminaFloat;
            I18nService.OnLanguageChanged -= RefreshLocalizedTexts;
        }

        private void BeginRun()
        {
            if (_runStarted) return;
            _runStarted = true;
            _failPenaltyApplied = false;
            _game.StartRun();
        }

        // ================= Setup =================

        private void CreatePowerButtons()
        {
            var defs = new[] { _game.magnetDef, _game.tornadoDef, _game.freezeDef };
            for (int i = 0; i < defs.Length && i < _powerButtons.Length; i++)
                _powerButtons[i].Setup(_powerUps, defs[i]);
        }

        private void WireButtons()
        {
            _pauseButton.onClick.AddListener(() =>
            {
                _game.SetPaused(true);
                _pausePopup.SetActive(true);
            });

            _pauseContinueBtn.onClick.AddListener(() => { HideAllPopups(); _game.SetPaused(false); });
            _pauseQuitBtn.onClick.AddListener(ShowQuitConfirm);
            _pauseLangButton.onClick.AddListener(I18nService.ToggleLanguage);
            for (int i = 0; i < PauseToggleKeys.Length && i < _pauseToggleButtons.Length; i++)
            {
                var key = PauseToggleKeys[i];
                AddToggleBinding(_pauseToggleButtons[i], v =>
                {
                    switch (key)
                    {
                        case "label_music": SaveDataService.Current.musicEnabled = v; break;
                        case "label_sfx": SaveDataService.Current.sfxEnabled = v; break;
                        case "label_haptics": SaveDataService.Current.hapticsEnabled = v; break;
                    }
                    SaveDataService.Save();
                });
            }

            _quitYesBtn.onClick.AddListener(() => { HideAllPopups(); ConfirmQuitRun(); });
            _quitNoBtn.onClick.AddListener(() => _quitConfirm.SetActive(false));

            _reviveButton.onClick.AddListener(DoRevive);
            _menuButton.onClick.AddListener(LeaveGameOver);

            _doubleButton.onClick.AddListener(DoDoubleReward);

            _buyButton.onClick.AddListener(OnBuyPressed);
            _buyCancelBtn.onClick.AddListener(CloseBuySheet);
            _insufficientAdBtn.onClick.AddListener(OnInsufficientAdPressed);
            _insufficientCloseBtn.onClick.AddListener(() => _insufficientSheet.SetActive(false));

            _shiftOverMenuBtn.onClick.AddListener(() => _game.ReturnToMenu());
            _shiftOverAdBtn.onClick.AddListener(() =>
            {
                var ads = AdServiceLocator.Service;
                if (ads == null || !ads.IsReady(AdPlacement.reward_stamina)) return;
                ads.ShowRewarded(AdPlacement.reward_stamina, ok =>
                {
                    if (!ok) return;
                    var cfg = _game.staminaConfig;
                    StaminaService.GrantHardClamped(cfg != null ? cfg.staminaAdGrant : 5);
                    RefreshStaminaLabel();
                    RefreshShiftOverAdButton();
                });
            });

            _tutorialNext.onClick.AddListener(() =>
            {
                if (_tutorialStep >= 3) FinishTutorial();
                else ShowTutorial(_tutorialStep + 1);
            });
            _tutorialSkip.onClick.AddListener(FinishTutorial);

            // Spotlight package handles its own blocking when available.
            _tutorialPopup.GetComponent<Image>().raycastTarget = !TutorialSpotlightAdapter.Available;

            _pausePopup.SetActive(false);
            _quitConfirm.SetActive(false);
            _gameOverPopup.SetActive(false);
            _servePopup.SetActive(false);
            _buySheet.SetActive(false);
            _insufficientSheet.SetActive(false);
            _shiftOverPopup.SetActive(false);
            _tutorialPopup.SetActive(false);

            RefreshLocalizedTexts();
            RefreshCoins();
            RefreshStars(3);
            RefreshStaminaLabel();
            RefreshChallengeChip();
            RefreshBadges();
        }

        private static void AddToggleBinding(Button btn, System.Action<bool> onChanged)
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

        private void OnBuyPressed()
        {
            if (_buySheetDef == null) return;
            if (EconomyManager.Coins < _buySheetDef.buyCost)
            {
                ShowInsufficientInBuySheet();
                return;
            }
            _powerUps.TryPurchaseAndAutoUse(_buySheetDef, msg =>
            {
                _buyMessage.text = msg ?? "";
                if (msg == null) CloseBuySheet();
                RefreshCoins();
                RefreshBadges();
            });
        }

        private void OnInsufficientAdPressed()
        {
            if (_buySheetDef == null) return;
            _powerUps.WatchAdForCoins(_buySheetDef, () =>
            {
                RefreshCoins();
                RefreshBadges();
                _insufficientSheet.SetActive(false);
            });
        }

        private void WireEvents()
        {
            _orders.OrderStarted += OnOrderStarted;
            _orders.QueueChanged += RebuildQueueStrip;
            _orders.TimerTick += OnTimerTick;
            _orders.CorrectPick += OnCorrectPick;
            _orders.WrongPick += OnWrongPick;
            _orders.ServeCompletedRewardText += OnServeCompleted;
            _orders.ServePerfectStamp += OnPerfectStamp;
            _orders.BuriedHintRequested += OnBuriedHint;
            _game.StarsChanged += RefreshStars;
            _game.RunEnded += OnRunEnded;
            _powerUps.BuySheetRequested += OpenBuySheet;
        }

        private void OnOrderStarted(CustomerOrderState order)
        {
            RebuildOrderChips(order);
            RebuildQueueStrip();
            RefreshChallengeChip();
        }

        private void RebuildOrderChips(CustomerOrderState order)
        {
            foreach (Transform child in _orderChipsRow)
                Destroy(child.gameObject);
            _chipsByType.Clear();

            // The chips row is a HorizontalLayoutGroup; prefab carries the size.
            for (int i = 0; i < order.types.Count; i++)
            {
                var type = order.types[i];
                var chip = Instantiate(_orderChipPrefab, _orderChipsRow);
                chip.Bind(type, order.remaining[i]);
                _chipsByType[type.typeId] = chip;
            }
        }

        private void RebuildQueueStrip()
        {
            if (_orders.Current == null) return;
            foreach (Transform child in _queueStrip)
                Destroy(child.gameObject);

            // The queue strip is a HorizontalLayoutGroup; the two card prefabs carry their sizes.
            var current = Instantiate(_currentCardPrefab, _queueStrip);
            current.Bind(PortraitPath(0), I18nService.Get("queue_current"));

            for (int i = 0; i < _game.orderConfig.waitingCount; i++)
            {
                var card = Instantiate(_waitingCardPrefab, _queueStrip);
                card.Bind(PortraitPath(i + 1), I18nService.Get("queue_waiting"));
            }
        }

        private void OnTimerTick(float left, float total)
        {
            float k = total > 0 ? Mathf.Clamp01(left / total) : 0f;
            // Scale from the left edge: keep pivot at x=0.
            _timerFill.pivot = new Vector2(0, 0.5f);
            _timerFill.localScale = new Vector3(Mathf.Max(0.001f, k), 1f, 1f);
            _timerFillImage.color = left < 5f ? UIKit.MagnetRed : UIKit.SkyMint;

            float targetAlpha = left < 5f
                ? Mathf.Lerp(0.05f, 0.22f, 0.5f + 0.5f * Mathf.Sin(Time.time * 3f * Mathf.PI))
                : 0f;
            var c = _vignette.color;
            c.a = targetAlpha;
            _vignette.color = c;
        }

        private void Update()
        {
            if (EconomyManager.Coins != _lastCoins)
            {
                _lastCoins = EconomyManager.Coins;
                RefreshCoins();
            }

            _badgeTimer += Time.unscaledDeltaTime;
            if (_badgeTimer >= 0.25f)
            {
                _badgeTimer = 0f;
                RefreshBadges();
            }
        }

        private void RefreshCoins()
        {
            if (_coinsText != null) _coinsText.text = string.Format(I18nService.Get("label_coins"), EconomyManager.Coins);
        }

        private void RefreshStaminaLabel()
        {
            if (_staminaText == null) return;
            int max = _game.staminaConfig != null ? _game.staminaConfig.dailyMax : 20;
            _staminaText.text = string.Format(I18nService.Get("stamina_label_frac"), StaminaService.Current, max);
        }

        private void OnStaminaChanged(int value)
        {
            RefreshStaminaLabel();
        }

        // Floating text on the stamina chip: 体力-1 / 体力+1 / 体力-3 (spec 8.2).
        private void ShowStaminaFloat(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (_staminaFloatCo != null) StopCoroutine(_staminaFloatCo);
            _staminaFloatCo = StartCoroutine(StaminaFloatRoutine(text));
        }

        private IEnumerator StaminaFloatRoutine(string text)
        {
            _staminaFloatText.text = text;
            var rt = (RectTransform)_staminaFloatText.transform.parent;
            float t = 0f;
            Color c = _staminaFloatText.color;
            Vector2 start = rt.anchoredPosition;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(1.5f - t * 1.5f);
                _staminaFloatText.color = c;
                rt.anchoredPosition = start + Vector2.up * (t * 70f);
                yield return null;
            }
            _staminaFloatText.text = "";
            rt.anchoredPosition = start;
            _staminaFloatCo = null;
        }

        private void RefreshBadges()
        {
            foreach (var entry in _powerButtons)
                entry.SetBadge(_powerUps.CountOf(entry.Def.powerUpId));
        }

        private void RefreshStars(int stars)
        {
            for (int i = 0; i < _starIcons.Length; i++)
            {
                if (_starIcons[i] == null) continue;
                _starIcons[i].sprite = UIKit.LoadSprite(i < stars ? "icon_star" : "frame_star_empty");
                _starIcons[i].color = Color.white;
            }
        }

        private string PortraitPath(int slot)
        {
            int n = ((_game != null ? _game.CustomersServed : 0) + slot) % 6 + 1;
            return "Customers/portrait_customer_0" + n;
        }

        private void RefreshChallengeChip()
        {
            var save = SaveDataService.Current;
            var cfg = _game.dailyChallengeConfig;
            if (cfg == null || string.IsNullOrEmpty(save.dailyChallengeTypeId))
                return;
            _challengeChipText.text = save.dailyChallengeClaimed
                ? I18nService.Get("challenge_chip_done")
                : string.Format(I18nService.Get("challenge_chip_progress"),
                    save.dailyChallengeProgress, cfg.quota);

            // Spec 8.1: reaching the quota shows a one-line completion toast.
            if (save.dailyChallengeClaimed && !_challengeWasClaimed)
            {
                _challengeWasClaimed = true;
                StartCoroutine(ShowToastRoutine(I18nService.Get("challenge_complete_toast"), 0f));
            }
        }

        // Re-applies every static label after a language switch (i18n spec section 4).
        private void RefreshLocalizedTexts()
        {
            if (_pauseTitle != null) _pauseTitle.text = I18nService.Get("pause_title");
            SetButtonText(_pauseContinueBtn, I18nService.Get("btn_continue"));
            SetButtonText(_pauseQuitBtn, I18nService.Get("btn_quit_run"));
            if (_pauseQuitHint != null) _pauseQuitHint.text = I18nService.Get("pause_quit_hint");
            if (_quitConfirmBody != null) _quitConfirmBody.text = I18nService.Get("quit_confirm_body");
            SetButtonText(_quitYesBtn, I18nService.Get("quit_yes"));
            SetButtonText(_quitNoBtn, I18nService.Get("quit_no"));
            if (_gameOverTitle != null) _gameOverTitle.text = I18nService.Get("game_over_title");
            SetButtonText(_reviveButton, I18nService.Get("ad_revive"));
            SetButtonText(_menuButton, I18nService.Get("btn_main_menu"));
            SetButtonText(_doubleButton, I18nService.Get("ad_double"));
            SetButtonText(_buyButton, I18nService.Get("ad_buy_cta"));
            SetButtonText(_buyCancelBtn, I18nService.Get("btn_cancel"));
            SetButtonText(_shiftOverMenuBtn, I18nService.Get("btn_main_menu"));
            if (_buyNote != null) _buyNote.text = I18nService.Get("ad_buy_need");
            if (_insufficientMsg != null) _insufficientMsg.text = I18nService.Get("coins_short");
            SetButtonText(_tutorialNext, I18nService.Get("tutorial_next"));
            SetButtonText(_pauseLangButton,
                I18nService.Language == "en" ? I18nService.Get("lang_zh") : I18nService.Get("lang_en"));
            if (_shiftOverTitle != null)
                _shiftOverTitle.text = I18nService.Get("stamina_shift_title");
            if (_shiftOverBody != null)
                _shiftOverBody.text = I18nService.Get("stamina_shift_body");

            // Pause toggle labels (music/sfx/haptics).
            if (_pauseToggleLabels != null)
                for (int i = 0; i < _pauseToggleLabels.Length && i < PauseToggleKeys.Length; i++)
                    if (_pauseToggleLabels[i] != null)
                        _pauseToggleLabels[i].text = I18nService.Get(PauseToggleKeys[i]);

            // Power-up button labels: stored references, no hierarchy walking.
            foreach (var entry in _powerButtons)
                entry.LabelText.text = entry.Def.LocalizedName;

            if (_pauseLangLabel != null)
                _pauseLangLabel.text = I18nService.Get("label_language");

            // Order chips show candy names — rebuild them so they switch locale mid-run.
            if (_orders != null && _orders.Current != null)
                RebuildOrderChips(_orders.Current);
            RefreshChallengeChip();
        }

        // ================= Pick feedback =================

        private void OnCorrectPick(CandyTypeDefinition type)
        {
            Haptics.Light();
            Sfx.Pop();
            PunchChip(type);
            UpdateChipCounts();
            RefreshChallengeChip();

            _comboCount++;
            if (_comboCount >= 2)
                ShowCombo(string.Format(I18nService.Get("hud_combo"), _comboCount));
        }

        private void OnWrongPick()
        {
            _comboCount = 0;
            Haptics.Medium();
            Sfx.Thud();
            StartCoroutine(ShakeCamera());
        }

        private IEnumerator ShakeCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;
            Vector3 origin = cam.transform.position;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                cam.transform.position = origin + cam.transform.right * Mathf.Sin(t * 70f) * 0.08f * (1f - t / 0.25f);
                yield return null;
            }
            cam.transform.position = origin;
        }

        private void PunchChip(CandyTypeDefinition type)
        {
            OrderChip chip;
            if (_chipsByType.TryGetValue(type.typeId, out chip))
                StartCoroutine(PunchScale(chip.transform));
        }

        private IEnumerator PunchScale(Transform target)
        {
            float t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(t / 0.18f * Mathf.PI);
                target.localScale = Vector3.one * (1f + 0.18f * k);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private void UpdateChipCounts()
        {
            var cur = _orders.Current;
            if (cur == null) return;
            for (int i = 0; i < cur.types.Count; i++)
            {
                OrderChip chip;
                if (_chipsByType.TryGetValue(cur.types[i].typeId, out chip))
                    chip.SetCount(cur.types[i].LocalizedName, cur.remaining[i]);
            }
        }

        private void ShowCombo(string text)
        {
            if (_comboCo != null) StopCoroutine(_comboCo);
            _comboCo = StartCoroutine(ComboRoutine(text));
        }

        private IEnumerator ComboRoutine(string text)
        {
            _comboText.text = text;
            var rt = (RectTransform)_comboText.transform;
            float t = 0f;
            Color c = _comboText.color;
            while (t < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = new Vector2(0, t * 90f);
                c.a = 1f - t / 0.9f;
                _comboText.color = c;
                yield return null;
            }
            _comboText.text = "";
            _comboCo = null;
        }

        private void OnPerfectStamp(bool perfect)
        {
            // Combo breaks on serve regardless of perfect/pass (supplements 1.3).
            _comboCount = 0;
            if (!perfect) return;
            if (_stampCo != null) StopCoroutine(_stampCo);
            _stampCo = StartCoroutine(StampRoutine());
        }

        private IEnumerator StampRoutine()
        {
            _perfectStamp.text = I18nService.Get("hud_perfect");
            var rt = (RectTransform)_perfectStamp.transform;
            float t = 0f;
            Color c = _perfectStamp.color;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                float pop = Mathf.Clamp01(t / 0.15f);
                rt.localScale = Vector3.one * (0.5f + 0.6f * pop);
                c.a = t < 0.9f ? 1f : 1f - (t - 0.9f) / 0.5f;
                _perfectStamp.color = c;
                yield return null;
            }
            _perfectStamp.text = "";
            _stampCo = null;
        }

        private void OnBuriedHint()
        {
            StartCoroutine(ShowToastRoutine(I18nService.Get("toast_buried"), 0f));
        }

        private IEnumerator ShowToastRoutine(string message, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var toast = Instantiate(_toastTemplate, _toastTemplate.parent);
            toast.gameObject.SetActive(true);
            var txt = toast.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = message;
            yield return new WaitForSeconds(2.2f);
            Destroy(toast.gameObject);
        }

        // ================= Popups =================

        private void ShowQuitConfirm()
        {
            if (_quitConfirm != null) _quitConfirm.SetActive(true);
        }

        // Confirmed 放弃本局: counts as a fail — stamina -3 once, then menu (spec 8.2).
        private void ConfirmQuitRun()
        {
            _lastFailReason = "quit";
            if (!_failPenaltyApplied)
            {
                _failPenaltyApplied = true;
                // Stamina -3 (float) + fail coin penalty (supplements 2.0), both once.
                _game.ConfirmFailPenalty();
                PendingCoinPenaltyToast = _game.LastFailCoinPenalty;
            }
            _game.QuitRun();
        }

        // Leaving the Game Over screen without revive confirms the fail: stamina -3 (spec 8.2).
        private void LeaveGameOver()
        {
            if (reasonIsFail(_lastFailReason) && !_failPenaltyApplied)
            {
                _failPenaltyApplied = true;
                // Stamina -3 (float) + fail coin penalty (supplements 2.0), both once.
                _game.ConfirmFailPenalty();
                PendingCoinPenaltyToast = _game.LastFailCoinPenalty; // shown on the next menu load
            }
            _game.ReturnToMenu();
        }

        private static bool reasonIsFail(string reason)
        {
            return reason == "stars" || reason == "timeout" || reason == "quit";
        }

        private void ShowInsufficientInBuySheet()
        {
            // Keep the sheet open with Cancel live; disable only the +80-ad button (review P1).
            if (_insufficientSheet == null) return;
            var ads = AdServiceLocator.Service;
            bool adReady = ads != null && ads.IsReady(AdPlacement.reward_coins);
            _insufficientAdBtn.interactable = adReady;
            _insufficientSheet.SetActive(true);
        }

        private void OpenBuySheet(PowerUpDefinition def)
        {
            _buySheetDef = def;
            _buyTitle.text = string.Format(I18nService.Get("powerup_buy_title"), def.LocalizedName);
            _buyPrice.text = string.Format(I18nService.Get("powerup_price"), def.buyCost);
            _buyMessage.text = "";
            bool afford = EconomyManager.Coins >= def.buyCost;
            _buyButton.image.color = afford ? Color.white : new Color(0.82f, 0.62f, 0.66f);

            // Spec 14: while an ad/sheet is up, the customer timer pauses (same as Pause).
            if (_game.RunActive && !_game.Paused)
            {
                _game.SetPaused(true);
                _pausedForBuySheet = true;
            }
            _buySheet.SetActive(true);
        }

        private void CloseBuySheet()
        {
            _buySheet.SetActive(false);
            if (_insufficientSheet != null) _insufficientSheet.SetActive(false);
            if (_pausedForBuySheet)
            {
                _pausedForBuySheet = false;
                _game.SetPaused(false);
            }
            RefreshBadges();
        }

        private void ShowTutorial(int step)
        {
            _tutorialStep = step;
            if (_tutorialSkip != null)
                _tutorialSkip.gameObject.SetActive(step >= 2); // no skip on step 1
            _tutorialPopup.SetActive(true);
            switch (step)
            {
                case 1:
                    _tutorialBody.text = I18nService.Get("tutorial_1");
                    SetButtonText(_tutorialNext, I18nService.Get("tutorial_next"));
                    TutorialSpotlightAdapter.Show(_orderChipsRow); // point at order chips + pile
                    break;
                case 2:
                    _tutorialBody.text = I18nService.Get("tutorial_2");
                    TutorialSpotlightAdapter.Show(_starsAnchor.rectTransform); // stars restore rule
                    break;
                default:
                    _tutorialBody.text = I18nService.Get("tutorial_3");
                    TutorialSpotlightAdapter.Show(_bottomBar); // power-up thumb zone
                    break;
            }
        }

        private void FinishTutorial()
        {
            TutorialSpotlightAdapter.Hide();
            SaveDataService.Current.tutorialDone = true;
            SaveDataService.Save();
            _tutorialPopup.SetActive(false);
            BeginRun();
        }

        // ================= Serve / game over =================

        private void OnServeCompleted(string rewardText)
        {
            SpawnConfetti();
            _serveRewardText.text = rewardText;
            var ads = AdServiceLocator.Service;
            bool canDouble = _game.AllowOptionalAds &&
                             _game.DoublesUsedThisRun < _game.adConfig.maxDoubleServePerRun &&
                             ads != null && ads.IsReady(AdPlacement.reward_double_serve);
            _doubleButton.gameObject.SetActive(canDouble);
            _servePopup.SetActive(true);
            if (_autoContinueCo != null) StopCoroutine(_autoContinueCo);
            _autoContinueCo = StartCoroutine(AutoContinue());
        }

        private IEnumerator AutoContinue()
        {
            yield return new WaitForSeconds(_game.orderConfig.doubleRewardAutoContinueSeconds);
            DismissServeChip();
        }

        private void DoDoubleReward()
        {
            var ads = AdServiceLocator.Service;
            if (ads == null || !ads.IsReady(AdPlacement.reward_double_serve)) return;
            ads.ShowRewarded(AdPlacement.reward_double_serve, ok =>
            {
                if (ok)
                {
                    // Double the speed reward only (never the perfect bonus).
                    int lastReward = ParseReward(_serveRewardText.text);
                    EconomyManager.AddCoins(lastReward);
                    _game.RegisterDoubleReward();
                    _serveRewardText.text = "+" + lastReward * 2;
                    RefreshCoins();
                }
                else
                {
                    DismissServeChip();
                }
            });
        }

        private static int ParseReward(string text)
        {
            text = text.Replace("+", "").Trim();
            int v;
            return int.TryParse(text, out v) ? v : 0;
        }

        private void DismissServeChip()
        {
            if (_autoContinueCo != null) StopCoroutine(_autoContinueCo);
            _autoContinueCo = null;
            _servePopup.SetActive(false);
            _comboCount = 0; // combo breaks on serve (supplements 1.3)
            _orders.AdvanceQueue();
        }

        private void SpawnConfetti()
        {
            var host = new GameObject("Confetti");
            var ps = host.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startSpeed = 5f;
            main.startSize = 0.12f;
            main.maxParticles = 80;
            main.startColor = new ParticleSystem.MinMaxGradient(UIKit.SugarPink, UIKit.SkyMint);
            main.gravityModifier = 0.8f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            Camera cam = Camera.main;
            host.transform.position = cam != null
                ? cam.transform.position + cam.transform.forward * 3f + Vector3.up
                : Vector3.up * 2f;
            Destroy(host, 2.5f);
        }

        private void OnRunEnded(string reason)
        {
            if (_autoContinueCo != null) StopCoroutine(_autoContinueCo);
            _servePopup.SetActive(false);
            HideAllPopups();

            // Shift Over: successful serve but no stamina for the next guest.
            // Different screen: no revive, no fail penalty (spec 8.2).
            if (reason == "shift_over")
            {
                _lastFailReason = "shift_over";
                RefreshStaminaLabel();
                FillShiftOverStats();
                RefreshShiftOverAdButton();
                _shiftOverPopup.SetActive(true);
                return;
            }

            // Confirmed 放弃本局 already applied -3 and goes straight to menu:
            // no Game Over popup and never a revive offer (spec 14.4).
            if (reason == "quit")
            {
                _lastFailReason = "quit";
                _game.ReturnToMenu();
                return;
            }

            var save = SaveDataService.Current;
            bool newBest = reason != "aborted" && _game.CustomersServed > _bestAtRunStart && _game.CustomersServed > 0;
            _gameOverStats.text =
                string.Format(I18nService.Get("game_over_served"), _game.CustomersServed) + "\n" +
                string.Format(I18nService.Get("game_over_coins"), _game.CoinsEarnedThisRun) + "\n" +
                string.Format(I18nService.Get("game_over_best"), save.bestCustomersServed) + "\n" +
                string.Format(I18nService.Get("game_over_stamina"), StaminaService.Current) +
                (newBest ? "\n" + I18nService.Get("game_over_record") : "");

            var ads = AdServiceLocator.Service;
            bool canRevive = (reason == "timeout" || reason == "stars") &&
                             !_game.ReviveUsed &&
                             _game.AllowOptionalAds &&
                             ads != null && ads.IsReady(AdPlacement.reward_revive);
            _reviveButton.gameObject.SetActive(canRevive);
            _lastFailReason = reason;

            _gameOverPopup.SetActive(true);
        }

        private void DoRevive()
        {
            var ads = AdServiceLocator.Service;
            if (ads == null) return;
            ads.ShowRewarded(AdPlacement.reward_revive, ok =>
            {
                if (!ok) return;
                _gameOverPopup.SetActive(false);
                _game.ApplyRevive(_lastFailReason);
            });
        }

        // Shift Over mirrors the Game Over stats block (served / coins / best / record).
        private void FillShiftOverStats()
        {
            if (_shiftOverStats == null) return;
            var save = SaveDataService.Current;
            bool newBest = _game.CustomersServed > _bestAtRunStart && _game.CustomersServed > 0;
            _shiftOverStats.text =
                string.Format(I18nService.Get("game_over_served"), _game.CustomersServed) + "\n" +
                string.Format(I18nService.Get("game_over_coins"), _game.CoinsEarnedThisRun) + "\n" +
                string.Format(I18nService.Get("game_over_best"), save.bestCustomersServed) +
                (newBest ? "\n" + I18nService.Get("game_over_record") : "");
        }

        // Ad button only when an ad is ready (never promise stamina, supplements 2.0).
        private void RefreshShiftOverAdButton()
        {
            if (_shiftOverAdBtn == null) return;
            var ads = AdServiceLocator.Service;
            bool ready = ads != null && ads.IsReady(AdPlacement.reward_stamina);
            _shiftOverAdBtn.gameObject.SetActive(ready);
            if (!ready) return;
            var txt = _shiftOverAdBtn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                var cfg = _game.staminaConfig;
                txt.text = I18nService.Get("ad_stamina", cfg != null ? cfg.staminaAdGrant : 5);
            }
        }

        private void HideAllPopups()
        {
            if (_pausePopup != null) _pausePopup.SetActive(false);
            if (_quitConfirm != null) _quitConfirm.SetActive(false);
            if (_gameOverPopup != null) _gameOverPopup.SetActive(false);
            if (_buySheet != null && _buySheet.activeSelf) CloseBuySheet();
            else if (_insufficientSheet != null) _insufficientSheet.SetActive(false);
            if (_shiftOverPopup != null) _shiftOverPopup.SetActive(false);
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            var txt = button.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = value;
        }
    }
}
