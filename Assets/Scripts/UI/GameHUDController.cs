using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CandyShop
{
    // Builds and drives the in-run portrait HUD (spec section 10.4 + supplements section 1).
    public class GameHUDController : MonoBehaviour
    {
        private GameManager _game;
        private CustomerOrderManager _orders;
        private PowerUpManager _powerUps;

        private RectTransform _safeRoot;
        private Text _starsText;
        private readonly Image[] _starIcons = new Image[3];
        private Text _coinsText;
        private RectTransform _timerFill;
        private Image _timerFillImage;

        private RectTransform _queueStrip;
        private RectTransform _orderChipsRow;
        private Text _challengeChipText;

        private class PowerButtonEntry
        {
            public PowerUpDefinition def;
            public Button button;   // the thumb-zone button itself
            public Text labelText;  // the name label (not the count badge)
            public Text badgeText;  // the count badge
        }

        private readonly List<PowerButtonEntry> _powerButtons =
            new List<PowerButtonEntry>();

        private GameObject _pausePopup;
        private GameObject _quitConfirm;
        private GameObject _gameOverPopup;
        private GameObject _servePopup;
        private Text _serveRewardText;
        private Button _doubleButton;
        private Button _reviveButton;
        private Text _gameOverStats;
        private GameObject _buySheet;
        private Text _buySheetMessage;
        private Button _buyButton;
        private GameObject _insufficientSheet;
        private GameObject _tutorialPopup;
        private Text _tutorialBody;
        private Button _tutorialNext;
        private Button _tutorialSkip;

        private Image _vignette;
        private Text _comboText;
        private Text _perfectStamp;
        private Text _staminaText;
        private GameObject _shiftOverPopup;
        private Text _staminaFloatText;
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
        private Coroutine _autoContinueCo;
        private PowerUpDefinition _buySheetDef;

        public static string PendingRecipeUnlockToast; // set by RecipeShopController

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
            BuildUI();
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

        // ================= UI construction =================

        private void BuildUI()
        {
            var canvas = UIKit.CreateCanvas(null, "GameHUD");
            UIKit.CreateBackground(canvas.transform, "bg_game_shop");
            _safeRoot = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            _safeRoot.SetParent(canvas.transform, false);
            _safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            // Vignette (soft low-time warning)
            var vigRect = UIKit.CreatePanel(canvas.transform, "Vignette", new Color(0, 0, 0, 0));
            UIKit.Stretch(vigRect, canvas.transform);
            _vignette = vigRect.GetComponent<Image>();
            _vignette.color = new Color(UIKit.Berry.r, UIKit.Berry.g, UIKit.Berry.b, 0f);

            // ---- Top cluster: stars / coins / pause ----
            var top = UIKit.CreatePanel(_safeRoot, "Top", new Color(0, 0, 0, 0));
            UIKit.Place(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -180), new Vector2(-24, -24));

            _starsText = UIKit.CreateText(top, "", 1, Color.clear, TextAnchor.MiddleLeft);
            var starRow = UIKit.CreatePanel(top, "Stars", new Color(0, 0, 0, 0));
            UIKit.Place(starRow, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -48), new Vector2(280, 48));
            for (int i = 0; i < 3; i++)
            {
                var star = UIKit.CreateIcon(starRow, "icon_star", Vector2.one * 84f);
                var srt = (RectTransform)star.transform;
                srt.anchorMin = new Vector2(0, 0.5f);
                srt.anchorMax = new Vector2(0, 0.5f);
                srt.anchoredPosition = new Vector2(42 + i * 88, 0);
                _starIcons[i] = star;
            }

            var coinIcon = UIKit.CreateIcon(top, "icon_coin", Vector2.one * 72f);
            var coinRt = (RectTransform)coinIcon.transform;
            coinRt.anchorMin = new Vector2(1, 0.5f);
            coinRt.anchorMax = new Vector2(1, 0.5f);
            coinRt.anchoredPosition = new Vector2(-300, 0);

            _coinsText = UIKit.CreateText(top, "", 40, UIKit.Cocoa, TextAnchor.MiddleRight);
            UIKit.Place((RectTransform)_coinsText.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-280, -42), new Vector2(-130, 42));

            // Stamina n/20 next to coins (spec 8.2 HUD)
            var staminaChip = UIKit.CreatePanel(top, "StaminaChip", UIKit.Cream);
            UIKit.Place((RectTransform)staminaChip.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-280, 52), new Vector2(-20, 136));
            var stamIcon = UIKit.CreateIcon(staminaChip, "icon_stamina", Vector2.one * 56f);
            var stamIconRt = (RectTransform)stamIcon.transform;
            stamIconRt.anchorMin = new Vector2(0, 0.5f);
            stamIconRt.anchorMax = new Vector2(0, 0.5f);
            stamIconRt.anchoredPosition = new Vector2(36, 0);
            _staminaText = UIKit.CreateText(staminaChip, "", 32, UIKit.Cocoa, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)_staminaText.transform,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(72, 0), new Vector2(-8, 0));

            var pauseBtn = UIKit.CreateButton(top, "", Vector2.one * 96f, Color.white);
            var pauseIcon = UIKit.CreateIcon(pauseBtn.transform, "icon_pause", Vector2.one * 80f);
            pauseIcon.raycastTarget = false;
            UIKit.Place((RectTransform)pauseBtn.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-116, -48), new Vector2(-20, 48));
            pauseBtn.onClick.AddListener(() =>
            {
                _game.SetPaused(true);
                _pausePopup.SetActive(true);
            });

            // ---- Timer bar ----
            var timerBg = UIKit.CreatePanel(top, "TimerBg", Color.white);
            timerBg.GetComponent<Image>().sprite = UIKit.LoadSprite("bar_timer_bg");
            timerBg.GetComponent<Image>().type = Image.Type.Sliced;
            UIKit.Place(timerBg, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -120), new Vector2(0, -76));
            var fillGo = new GameObject("TimerFill", typeof(Image));
            fillGo.transform.SetParent(timerBg, false);
            _timerFill = (RectTransform)fillGo.transform;
            _timerFill.anchorMin = Vector2.zero;
            _timerFill.anchorMax = Vector2.one;
            _timerFill.offsetMin = new Vector2(8, 8);
            _timerFill.offsetMax = new Vector2(-8, -8);
            _timerFillImage = fillGo.GetComponent<Image>();
            _timerFillImage.sprite = UIKit.LoadSprite("bar_timer_fill");
            _timerFillImage.type = Image.Type.Sliced;
            _timerFillImage.color = Color.white;

            // ---- Queue strip ----
            _queueStrip = UIKit.CreatePanel(top, "Queue", new Color(0, 0, 0, 0));
            UIKit.Place(_queueStrip, new Vector2(0, 0), new Vector2(1, 0), new Vector2(60, -350), new Vector2(-60, -160));

            // ---- Order chips ----
            _orderChipsRow = UIKit.CreatePanel(top, "OrderChips", new Color(0, 0, 0, 0));
            UIKit.Place(_orderChipsRow, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, -540), new Vector2(-40, -380));

            // ---- Daily challenge chip ----
            var challengeChip = UIKit.CreatePanel(top, "ChallengeChip", UIKit.Lemon);
            UIKit.Place(challengeChip, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-280, -620), new Vector2(280, -556));
            _challengeChipText = UIKit.CreateText(challengeChip, "", 34, UIKit.Cocoa);
            UIKit.Stretch((RectTransform)_challengeChipText.transform, challengeChip);

            // ---- Combo text / perfect stamp ----
            _comboText = UIKit.CreateText(_safeRoot, "", 64, UIKit.SugarPink);
            var comboRt = (RectTransform)_comboText.transform;
            comboRt.anchorMin = new Vector2(0.62f, 0.5f);
            comboRt.anchorMax = new Vector2(0.92f, 0.56f);
            comboRt.offsetMin = Vector2.zero;
            comboRt.offsetMax = Vector2.zero;
            _comboText.text = "";

            _perfectStamp = UIKit.CreateText(_safeRoot, "", 100, UIKit.Berry);
            var stampRt = (RectTransform)_perfectStamp.transform;
            stampRt.rotation = Quaternion.Euler(0, 0, -12);
            stampRt.anchorMin = new Vector2(0.25f, 0.45f);
            stampRt.anchorMax = new Vector2(0.75f, 0.58f);
            stampRt.offsetMin = Vector2.zero;
            stampRt.offsetMax = Vector2.zero;
            _perfectStamp.text = "";

            // ---- Bottom thumb zone ----
            var bottom = UIKit.CreatePanel(_safeRoot, "Bottom", new Color(0, 0, 0, 0));
            UIKit.Place(bottom, new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 50), new Vector2(-24, 290));

            AddPowerButton(bottom, _game.magnetDef, 0);
            AddPowerButton(bottom, _game.tornadoDef, 1);
            AddPowerButton(bottom, _game.freezeDef, 2);

            BuildPausePopup(canvas.transform);
            BuildGameOverPopup(canvas.transform);
            BuildServePopup(_safeRoot);
            BuildBuySheet(canvas.transform);
            BuildTutorial(canvas.transform);
            BuildShiftOverPopup(canvas.transform);

            RefreshLocalizedTexts();
            RefreshCoins();
            RefreshStars(3);
            RefreshStaminaLabel();
            RefreshChallengeChip();
            RefreshBadges();
        }

        // Re-applies every static label after a language switch (i18n spec section 4).
        private void RefreshLocalizedTexts()
        {
            if (_pauseTitle != null) _pauseTitle.text = I18nService.Get("pause_title");
            if (_pauseContinueBtn != null) SetButtonText(_pauseContinueBtn, I18nService.Get("btn_continue"));
            if (_pauseQuitBtn != null) SetButtonText(_pauseQuitBtn, I18nService.Get("btn_quit_run"));
            if (_pauseQuitHint != null) _pauseQuitHint.text = I18nService.Get("pause_quit_hint");
            if (_quitConfirmBody != null) _quitConfirmBody.text = I18nService.Get("quit_confirm_body");
            if (_quitYesBtn != null) SetButtonText(_quitYesBtn, I18nService.Get("quit_yes"));
            if (_quitNoBtn != null) SetButtonText(_quitNoBtn, I18nService.Get("quit_no"));
            if (_gameOverTitle != null) _gameOverTitle.text = I18nService.Get("game_over_title");
            if (_reviveButton != null) SetButtonText(_reviveButton, I18nService.Get("ad_revive"));
            if (_menuButton != null) SetButtonText(_menuButton, I18nService.Get("btn_main_menu"));
            if (_doubleButton != null) SetButtonText(_doubleButton, I18nService.Get("ad_double"));
            if (_buyButton != null) SetButtonText(_buyButton, I18nService.Get("ad_buy_cta"));
            if (_buyCancelBtn != null) SetButtonText(_buyCancelBtn, I18nService.Get("btn_cancel"));
            if (_shiftOverMenuBtn != null) SetButtonText(_shiftOverMenuBtn, I18nService.Get("btn_main_menu"));
            if (_shiftOverPopup != null && _shiftOverPopup.activeSelf)
            {
                SetTextSafe(_shiftOverPopup.transform, "ShiftOverPanel", "ShiftOverBody",
                    I18nService.Get("stamina_shift_body"));
            }
            var buySheetRoot = _buySheet != null ? _buySheet.transform : null;
            if (buySheetRoot != null)
            {
                SetTextSafe(buySheetRoot, "BuySheet", "BuyNote", I18nService.Get("ad_buy_need"));
                var insuf = buySheetRoot.Find("Insufficient");
                if (insuf != null)
                {
                    var msgTxt = insuf.Find("InsufMsg");
                    if (msgTxt != null) msgTxt.GetComponent<Text>().text = I18nService.Get("coins_short");
                }
            }
            if (_tutorialNext != null)
                SetButtonText(_tutorialNext, I18nService.Get("tutorial_next"));
            if (_pauseLangButton != null)
                SetButtonText(_pauseLangButton,
                    I18nService.Language == "en" ? I18nService.Get("lang_zh") : I18nService.Get("lang_en"));
            if (_shiftOverTitle != null)
                _shiftOverTitle.text = I18nService.Get("stamina_shift_title");
            if (_shiftOverBody != null)
                _shiftOverBody.text = I18nService.Get("stamina_shift_body");
            // Pause toggle labels (music/sfx/haptics), tracked at build time.
            foreach (var entry in _pauseToggleLabels)
                if (entry.label != null)
                    entry.label.text = I18nService.Get(entry.key);
            // Power-up button labels: stored references, no hierarchy walking.
            foreach (var entry in _powerButtons)
            {
                if (entry.labelText != null)
                    entry.labelText.text = entry.def.LocalizedName;
            }

            // Order chips show candy names — rebuild them so they switch locale mid-run.
            if (_orders != null && _orders.Current != null)
                RebuildOrderChips(_orders.Current);
            var langLabel = _pausePopup != null
                ? _pausePopup.transform.Find("PausePanel/Toggle_Language/LangLabel") : null;
            if (langLabel != null)
                langLabel.GetComponent<Text>().text =
                    I18nService.Get("label_language") + " / Language";
            RefreshChallengeChip();
        }

        private void AddPowerButton(RectTransform bottom, PowerUpDefinition def, int index)
        {
            const float w = 300f, h = 200f, gap = 40f;
            float x = -(w * 3 + gap * 2) / 2f + index * (w + gap) + w / 2f;

            var btn = UIKit.CreateButton(bottom, def.LocalizedName, new Vector2(w, h), Color.white, 36, UIKit.Cocoa);
            var rt = (RectTransform)btn.transform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0);
            btn.image.sprite = UIKit.LoadSprite("btn_primary");
            btn.image.color = Color.white;
            var pIcon = UIKit.CreateIcon(btn.transform, PowerIconPath(def.powerUpId), Vector2.one * 72f);
            var pRt = (RectTransform)pIcon.transform;
            pRt.anchorMin = new Vector2(0.5f, 1);
            pRt.anchorMax = new Vector2(0.5f, 1);
            pRt.anchoredPosition = new Vector2(0, -48);
            var label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                var lrt = (RectTransform)label.transform;
                lrt.offsetMin = new Vector2(8, 8);
                lrt.offsetMax = new Vector2(-8, -70);
            }

            var badgeGo = new GameObject("Badge", typeof(Image));
            badgeGo.transform.SetParent(btn.transform, false);
            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.sprite = UIKit.RoundedSprite();
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = UIKit.Cream;
            badgeImg.raycastTarget = false;
            var badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(1, 1);
            badgeRt.anchorMax = new Vector2(1, 1);
            badgeRt.sizeDelta = new Vector2(72, 72);
            badgeRt.anchoredPosition = new Vector2(14, -14);

            var badgeText = UIKit.CreateText(badgeGo.transform, "0", 38, UIKit.Cocoa);
            UIKit.Stretch((RectTransform)badgeText.transform, badgeRt);

            btn.onClick.AddListener(() => { Haptics.Light(); _powerUps.TapUse(def); });
            _powerButtons.Add(new PowerButtonEntry
            {
                def = def,
                button = btn,
                labelText = btn.GetComponentInChildren<Text>(),
                badgeText = badgeText
            });
        }

        // ================= Popups =================

        private Text _pauseTitle;
        private Text _pauseQuitHint;
        private Button _pauseLangButton;
        private readonly List<(string key, Text label)> _pauseToggleLabels =
            new List<(string, Text)>();
        private Button _pauseContinueBtn;
        private Button _pauseQuitBtn;
        private Text _quitConfirmBody;
        private Button _quitYesBtn;
        private Button _quitNoBtn;
        private Text _gameOverTitle;
        private Button _menuButton;
        private Text _shiftOverTitle;
        private Text _shiftOverBody;
        private Text _shiftOverStats;
        private Button _shiftOverMenuBtn;

        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            var txt = button.GetComponentInChildren<Text>();
            if (txt != null) txt.text = value;
        }

        private static void SetTextSafe(Transform parent, string childName, string textChildName, string value)
        {
            if (parent == null) return;
            var child = parent.Find(childName);
            if (child == null) return;
            var t = child.Find(textChildName);
            if (t != null) t.GetComponent<Text>().text = value;
        }

        private void BuildPausePopup(Transform parent)
        {
            var dim = UIKit.CreatePanel(parent, "PauseDim", new Color(0, 0, 0, 0.45f));
            UIKit.Stretch(dim, parent);
            var dimImg = dim.GetComponent<Image>();
            dimImg.raycastTarget = true;
            _pausePopup = dim.gameObject;

            var panel = UIKit.CreatePanel(dim, "PausePanel", UIKit.Cream);
            panel.sizeDelta = new Vector2(800, 980);
            panel.anchoredPosition = Vector2.zero;

            _pauseTitle = UIKit.CreateText(panel, "", 64, UIKit.Cocoa);
            _pauseTitle.rectTransform.anchoredPosition = new Vector2(0, 400);

            _pauseContinueBtn = UIKit.CreateButton(panel, "", new Vector2(600, 140), UIKit.SugarPink);
            _pauseContinueBtn.transform.localPosition = new Vector3(0, 240, 0);
            _pauseContinueBtn.onClick.AddListener(() => { HideAllPopups(); _game.SetPaused(false); });

            _pauseQuitBtn = UIKit.CreateButton(panel, "", new Vector2(600, 140), UIKit.Grape);
            _pauseQuitBtn.transform.localPosition = new Vector3(0, 70, 0);
            _pauseQuitBtn.onClick.AddListener(ShowQuitConfirm);

            CreateToggle(panel, I18nService.Get("label_music"), SaveDataService.Current.musicEnabled, v =>
            {
                SaveDataService.Current.musicEnabled = v;
                SaveDataService.Save();
            }, new Vector2(0, -80));

            CreateToggle(panel, I18nService.Get("label_sfx"), SaveDataService.Current.sfxEnabled, v =>
            {
                SaveDataService.Current.sfxEnabled = v;
                SaveDataService.Save();
            }, new Vector2(0, -190));

            CreateToggle(panel, I18nService.Get("label_haptics"), SaveDataService.Current.hapticsEnabled, v =>
            {
                SaveDataService.Current.hapticsEnabled = v;
                SaveDataService.Save();
            }, new Vector2(0, -300));

            // Language row (i18n spec section 4: Main Menu and Pause). Bilingual labels.
            AddPauseLanguageRow(panel, new Vector2(0, -360));

            var hint = UIKit.CreateText(panel, "", 30, UIKit.Grape);
            hint.rectTransform.anchoredPosition = new Vector2(0, -420);
            _pauseQuitHint = hint;

            _pausePopup.SetActive(false);

            // Quit confirm layer
            var confirmDim = UIKit.CreatePanel(parent, "QuitConfirmDim", new Color(0, 0, 0, 0.55f));
            UIKit.Stretch(confirmDim, parent);
            confirmDim.GetComponent<Image>().raycastTarget = true;
            var cPanel = UIKit.CreatePanel(confirmDim, "Confirm", UIKit.Cream);
            cPanel.sizeDelta = new Vector2(820, 520);
            cPanel.anchoredPosition = Vector2.zero;

            _quitConfirmBody = UIKit.CreateText(cPanel, "", 44, UIKit.Cocoa);
            _quitConfirmBody.rectTransform.anchoredPosition = new Vector2(0, 100);

            _quitYesBtn = UIKit.CreateButton(cPanel, "", new Vector2(300, 130), UIKit.MagnetRed);
            _quitYesBtn.transform.localPosition = new Vector3(-180, -140, 0);
            _quitYesBtn.onClick.AddListener(() => { HideAllPopups(); ConfirmQuitRun(); });

            _quitNoBtn = UIKit.CreateButton(cPanel, "", new Vector2(300, 130), UIKit.SkyMint, 40, UIKit.Cocoa);
            _quitNoBtn.transform.localPosition = new Vector3(180, -140, 0);
            _quitNoBtn.onClick.AddListener(() => confirmDim.gameObject.SetActive(false));

            _quitConfirm = confirmDim.gameObject;
            _quitConfirm.SetActive(false);
        }

        // Bilingual 中文/English switcher for the pause panel (persisted via SetLanguage).
        private void AddPauseLanguageRow(Transform parent, Vector2 pos)
        {
            var row = new GameObject("Toggle_Language", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rr = (RectTransform)row.transform;
            rr.sizeDelta = new Vector2(620, 84);
            rr.localPosition = pos;

            var t = UIKit.CreateText(row.transform, "", 40, UIKit.Cocoa, TextAnchor.MiddleLeft);
            t.name = "LangLabel";
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = new Vector2(0, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = new Vector2(360, 0);

            _pauseLangButton = UIKit.CreateButton(row.transform,
                I18nService.Language == "en" ? I18nService.Get("lang_zh") : I18nService.Get("lang_en"),
                new Vector2(150, 78), UIKit.Grape, 32);
            _pauseLangButton.name = "LangSwitch";
            var brt = (RectTransform)_pauseLangButton.transform;
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-90, 0);
            _pauseLangButton.onClick.AddListener(() => I18nService.ToggleLanguage());
        }

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
                // ApplyFailPenalty itself fires FloatingTextRequested -> HUD shows 体力-3 once.
                _game.ConfirmFailPenalty();
            }
            _game.QuitRun();
        }

        private void CreateToggle(Transform parent, string label, bool value, System.Action<bool> onChanged, Vector2 pos)
        {
            var row = new GameObject("ToggleRow_" + _pauseToggleLabels.Count, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rr = (RectTransform)row.transform;
            rr.sizeDelta = new Vector2(620, 84);
            rr.localPosition = pos;

            var t = UIKit.CreateText(row.transform, label, 40, UIKit.Cocoa, TextAnchor.MiddleLeft);
            // Remember which i18n key drives this label so language switches re-apply it.
            if (label == I18nService.Get("label_music")) _pauseToggleLabels.Add(("label_music", t));
            else if (label == I18nService.Get("label_sfx")) _pauseToggleLabels.Add(("label_sfx", t));
            else if (label == I18nService.Get("label_haptics")) _pauseToggleLabels.Add(("label_haptics", t));
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = new Vector2(0, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = new Vector2(360, 0);

            var btn = UIKit.CreateButton(row.transform,
                value ? I18nService.Get("toggle_on") : I18nService.Get("toggle_off"), new Vector2(150, 78),
                value ? UIKit.SkyMint : new Color(0.72f, 0.72f, 0.72f), 36, UIKit.Cocoa);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-90, 0);

            btn.onClick.AddListener(() =>
            {
                var txt = btn.GetComponentInChildren<Text>();
                bool isOn = txt.text == I18nService.Get("toggle_on");
                isOn = !isOn;
                txt.text = isOn ? I18nService.Get("toggle_on") : I18nService.Get("toggle_off");
                btn.image.color = isOn ? UIKit.SkyMint : new Color(0.72f, 0.72f, 0.72f);
                onChanged(isOn);
            });
        }

        private void BuildGameOverPopup(Transform parent)
        {
            var dim = UIKit.CreatePanel(parent, "GameOverDim", new Color(0, 0, 0, 0.6f));
            UIKit.Stretch(dim, parent);
            dim.GetComponent<Image>().raycastTarget = true;
            _gameOverPopup = dim.gameObject;

            var panel = UIKit.CreatePanel(dim, "GameOverPanel", UIKit.Cream);
            panel.sizeDelta = new Vector2(860, 1050);
            panel.anchoredPosition = Vector2.zero;

            _gameOverTitle = UIKit.CreateText(panel, "", 80, UIKit.Cocoa);
            _gameOverTitle.rectTransform.anchoredPosition = new Vector2(0, 420);

            _gameOverStats = UIKit.CreateText(panel, "", 44, UIKit.Cocoa, TextAnchor.UpperCenter);
            var srt = (RectTransform)_gameOverStats.transform;
            srt.sizeDelta = new Vector2(800, 420);
            srt.anchoredPosition = new Vector2(0, 100);

            _reviveButton = UIKit.CreateButton(panel, "", new Vector2(700, 140), UIKit.Grape, 42);
            _reviveButton.transform.localPosition = new Vector3(0, -250, 0);
            _reviveButton.onClick.AddListener(DoRevive);

            _menuButton = UIKit.CreateButton(panel, "", new Vector2(700, 140), UIKit.SugarPink);
            _menuButton.transform.localPosition = new Vector3(0, -430, 0);
            _menuButton.onClick.AddListener(LeaveGameOver);

            _gameOverPopup.SetActive(false);
        }

        // Leaving the Game Over screen without revive confirms the fail: stamina -3 (spec 8.2).
        private void LeaveGameOver()
        {
            if (reasonIsFail(_lastFailReason) && !_failPenaltyApplied)
            {
                _failPenaltyApplied = true;
                // ApplyFailPenalty itself fires FloatingTextRequested -> HUD shows 体力-3 once.
                _game.ConfirmFailPenalty();
            }
            _game.ReturnToMenu();
        }

        private static bool reasonIsFail(string reason)
        {
            return reason == "stars" || reason == "timeout" || reason == "quit";
        }

        private void BuildServePopup(RectTransform parent)
        {
            var chip = UIKit.CreatePanel(parent, "ServeChip", UIKit.SkyMint);
            chip.sizeDelta = new Vector2(940, 230);
            chip.anchorMin = new Vector2(0.5f, 0.5f);
            chip.anchorMax = new Vector2(0.5f, 0.5f);
            chip.anchoredPosition = new Vector2(0, 480);

            _serveRewardText = UIKit.CreateText(chip, "+10", 72, UIKit.Cocoa);
            ((RectTransform)_serveRewardText.transform).anchoredPosition = new Vector2(-200, 0);

            _doubleButton = UIKit.CreateButton(chip, "", new Vector2(360, 110), UIKit.Lemon, 36, UIKit.Cocoa);
            var drt = (RectTransform)_doubleButton.transform;
            drt.anchorMin = new Vector2(1, 0.5f);
            drt.anchorMax = new Vector2(1, 0.5f);
            drt.anchoredPosition = new Vector2(-220, 0);
            _doubleButton.onClick.AddListener(DoDoubleReward);

            _servePopup = chip.gameObject;
            _servePopup.SetActive(false);
        }

        private void BuildBuySheet(Transform parent)
        {
            var dim = UIKit.CreatePanel(parent, "BuySheetDim", new Color(0, 0, 0, 0.45f));
            UIKit.Stretch(dim, parent);
            dim.GetComponent<Image>().raycastTarget = true;
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);
            _buySheet = dim.gameObject;

            var panel = UIKit.CreatePanel(dim, "BuySheet", UIKit.Cream);
            panel.sizeDelta = new Vector2(860, 780);
            panel.anchoredPosition = Vector2.zero;

            var title = UIKit.CreateText(panel, "", 54, UIKit.Cocoa);
            title.rectTransform.anchoredPosition = new Vector2(0, 280);
            title.name = "Title";

            var price = UIKit.CreateText(panel, "", 44, UIKit.Cocoa);
            price.rectTransform.anchoredPosition = new Vector2(0, 190);
            price.name = "Price";

            var note = UIKit.CreateText(panel, "", 32, UIKit.Grape);
            note.name = "BuyNote";
            note.rectTransform.anchoredPosition = new Vector2(0, 130);

            _buySheetMessage = UIKit.CreateText(panel, "", 34, UIKit.MagnetRed);
            _buySheetMessage.rectTransform.anchoredPosition = new Vector2(0, 60);
            _buySheetMessage.text = "";

            _buyButton = UIKit.CreateButton(panel, "", new Vector2(680, 140), UIKit.SugarPink, 40);
            _buyButton.transform.localPosition = new Vector3(0, -60, 0);
            _buyButton.onClick.AddListener(() =>
            {
                if (_buySheetDef == null) return;
                if (EconomyManager.Coins < _buySheetDef.buyCost)
                {
                    ShowInsufficientInBuySheet();
                    return;
                }
                _powerUps.TryPurchaseAndAutoUse(_buySheetDef, msg =>
                {
                    _buySheetMessage.text = msg ?? "";
                    if (msg == null) CloseBuySheet();
                    RefreshCoins();
                    RefreshBadges();
                });
            });

            _buyCancelBtn = UIKit.CreateButton(panel, "", new Vector2(680, 120),
                new Color(0.75f, 0.72f, 0.68f), 38, UIKit.Cocoa);
            _buyCancelBtn.transform.localPosition = new Vector3(0, -230, 0);
            _buyCancelBtn.onClick.AddListener(CloseBuySheet);

            _buyTitle = title;
            _buyPrice = price;

            // Insufficient-coins inner sheet
            var insufPanel = UIKit.CreatePanel(dim, "Insufficient", UIKit.Lemon);
            insufPanel.sizeDelta = new Vector2(820, 500);
            insufPanel.anchoredPosition = Vector2.zero;
            insufPanel.SetAsLastSibling();

            var insufMsg = UIKit.CreateText(insufPanel, "", 48, UIKit.Cocoa);
            insufMsg.name = "InsufMsg";
            insufMsg.rectTransform.anchoredPosition = new Vector2(0, 160);

            var adBtn = UIKit.CreateButton(insufPanel, I18nService.Get("ad_coins_80"), new Vector2(660, 130), UIKit.SugarPink, 38);
            adBtn.name = "AdButton"; // distinct name so only the ad button is disabled when not ready
            adBtn.transform.localPosition = new Vector3(0, 10, 0);
            adBtn.onClick.AddListener(() =>
            {
                if (_buySheetDef == null) return;
                _powerUps.WatchAdForCoins(_buySheetDef, () =>
                {
                    RefreshCoins();
                    RefreshBadges();
                    insufPanel.gameObject.SetActive(false);
                });
            });

            var closeInsuf = UIKit.CreateButton(insufPanel, I18nService.Get("btn_cancel"), new Vector2(660, 110),
                new Color(0.75f, 0.72f, 0.68f), 36, UIKit.Cocoa);
            closeInsuf.transform.localPosition = new Vector3(0, -160, 0);
            closeInsuf.onClick.AddListener(() => insufPanel.gameObject.SetActive(false));

            _insufficientSheet = insufPanel.gameObject;
            _insufficientSheet.SetActive(false);
            _buySheet.SetActive(false);
        }

        private Text _buyTitle;
        private Text _buyPrice;
        private Button _buyCancelBtn;

        private void ShowInsufficientInBuySheet()
        {
            // Keep the sheet open with Cancel live; disable only the +80-ad button (review P1).
            if (_insufficientSheet == null) return;
            var ads = AdServiceLocator.Service;
            bool adReady = ads != null && ads.IsReady(AdPlacement.reward_coins);
            var adBtnTr = _insufficientSheet.transform.Find("AdButton");
            if (adBtnTr != null)
                adBtnTr.GetComponent<Button>().interactable = adReady;
            _insufficientSheet.SetActive(true);
        }

        private void OpenBuySheet(PowerUpDefinition def)
        {
            _buySheetDef = def;
            _buyTitle.text = string.Format(I18nService.Get("powerup_buy_title"), def.LocalizedName);
            _buyPrice.text = string.Format(I18nService.Get("powerup_price"), def.buyCost);
            _buySheetMessage.text = "";
            bool afford = EconomyManager.Coins >= def.buyCost;
            _buyButton.image.color = afford ? UIKit.SugarPink : new Color(0.82f, 0.62f, 0.66f);
            _buySheet.SetActive(true);
        }

        private void CloseBuySheet()
        {
            _buySheet.SetActive(false);
            if (_insufficientSheet != null) _insufficientSheet.SetActive(false);
            RefreshBadges();
        }

        private void BuildTutorial(Transform parent)
        {
            var dim = UIKit.CreatePanel(parent, "TutorialDim", new Color(0, 0, 0, 0.6f));
            UIKit.Stretch(dim, parent);
            dim.GetComponent<Image>().raycastTarget = true;
            _tutorialPopup = dim.gameObject;

            var card = UIKit.CreatePanel(dim, "TutorialCard", UIKit.Cream);
            card.sizeDelta = new Vector2(880, 900);
            card.anchoredPosition = Vector2.zero;

            _tutorialBody = UIKit.CreateText(card, "", 46, UIKit.Cocoa, TextAnchor.UpperCenter);
            var brt = (RectTransform)_tutorialBody.transform;
            brt.sizeDelta = new Vector2(780, 420);
            brt.anchoredPosition = new Vector2(0, 140);

            _tutorialNext = UIKit.CreateButton(card, I18nService.Get("tutorial_next"), new Vector2(560, 140), UIKit.SugarPink);
            _tutorialNext.transform.localPosition = new Vector3(0, -240, 0);
            _tutorialNext.onClick.AddListener(() =>
            {
                if (_tutorialStep >= 3) FinishTutorial();
                else ShowTutorial(_tutorialStep + 1);
            });

            _tutorialSkip = UIKit.CreateButton(card, I18nService.Get("tutorial_skip"), new Vector2(300, 100),
                new Color(0.78f, 0.75f, 0.7f), 34, UIKit.Cocoa);
            _tutorialSkip.transform.localPosition = new Vector3(0, -370, 0);
            _tutorialSkip.onClick.AddListener(FinishTutorial);

            _tutorialPopup.SetActive(false);
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
                    break;
                case 2:
                    _tutorialBody.text = I18nService.Get("tutorial_2");
                    break;
                default:
                    _tutorialBody.text = I18nService.Get("tutorial_3");
                    break;
            }
        }

        private void FinishTutorial()
        {
            SaveDataService.Current.tutorialDone = true;
            SaveDataService.Save();
            _tutorialPopup.SetActive(false);
            BeginRun();
        }

        // ================= Event wiring =================

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

            const float chipW = 320f, gap = 24f;
            int n = order.types.Count;
            for (int i = 0; i < n; i++)
            {
                var type = order.types[i];
                var chip = UIKit.CreatePanel(_orderChipsRow, "Chip_" + type.typeId, UIKit.Cream);
                chip.sizeDelta = new Vector2(chipW, 140);
                chip.anchorMin = new Vector2(0.5f, 0.5f);
                chip.anchorMax = new Vector2(0.5f, 0.5f);
                float x = -(n - 1) * (chipW + gap) / 2f + i * (chipW + gap);
                chip.anchoredPosition = new Vector2(x, 0);

                var candyIcon = UIKit.CreateIcon(chip, UIKit.CandyIconPath(type.typeId), Vector2.one * 88f);
                candyIcon.name = "Dot";
                var candyRt = (RectTransform)candyIcon.transform;
                candyRt.anchorMin = new Vector2(0, 0.5f);
                candyRt.anchorMax = new Vector2(0, 0.5f);
                candyRt.anchoredPosition = new Vector2(72, 0);

                var label = UIKit.CreateText(chip, type.LocalizedName + " x" + order.remaining[i], 36, UIKit.Cocoa,
                    TextAnchor.MiddleLeft);
                label.name = "Count";
                var lrt = (RectTransform)label.transform;
                lrt.anchorMin = new Vector2(0, 0.5f);
                lrt.anchorMax = new Vector2(1, 0.5f);
                lrt.offsetMin = new Vector2(136, -50);
                lrt.offsetMax = new Vector2(-16, 50);
            }
        }

        private void RebuildQueueStrip()
        {
            if (_orders.Current == null) return;
            foreach (Transform child in _queueStrip)
                Destroy(child.gameObject);

            var current = UIKit.CreatePanel(_queueStrip, "Customer_Current", UIKit.Cream);
            current.sizeDelta = new Vector2(300, 180);
            current.anchorMin = new Vector2(0, 0.5f);
            current.anchorMax = new Vector2(0, 0.5f);
            current.anchoredPosition = new Vector2(170, 10);
            PlacePortrait(current, PortraitPath(0), new Vector2(0, 18), Vector2.one * 140f);
            var curLabel = UIKit.CreateText(current, I18nService.Get("queue_current"), 28, UIKit.Cocoa);
            curLabel.rectTransform.anchoredPosition = new Vector2(0, -70);

            for (int i = 0; i < _game.orderConfig.waitingCount; i++)
            {
                var waitingState = _orders.GetWaiting(i);
                var card = UIKit.CreatePanel(_queueStrip, "Customer_Wait" + i, UIKit.Cream);
                card.sizeDelta = new Vector2(230, 140);
                card.anchorMin = new Vector2(0, 0.5f);
                card.anchorMax = new Vector2(0, 0.5f);
                card.anchoredPosition = new Vector2(355 + i * 255, -10);
                if (waitingState != null)
                    PlacePortrait(card, PortraitPath(i + 1), new Vector2(0, 12), Vector2.one * 96f);
                var lbl = UIKit.CreateText(card, I18nService.Get("queue_waiting"), 24, UIKit.Cocoa);
                lbl.rectTransform.anchoredPosition = new Vector2(0, -52);
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
            RefreshCoins();

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
            if (_staminaFloatText == null)
            {
                var host = UIKit.CreatePanel(_safeRoot, "StaminaFloat", new Color(0, 0, 0, 0));
                host.anchorMin = new Vector2(1, 1);
                host.anchorMax = new Vector2(1, 1);
                host.pivot = new Vector2(0.5f, 0.5f);
                host.sizeDelta = new Vector2(320, 90);
                host.anchoredPosition = new Vector2(-290, -240);
                _staminaFloatText = UIKit.CreateText(host, "", 44, UIKit.SkyMint);
                UIKit.Stretch((RectTransform)_staminaFloatText.transform, host);
            }

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
            {
                int count = _powerUps.CountOf(entry.def.powerUpId);
                entry.badgeText.text = count > 0 ? count.ToString() : "+";
            }
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

        private static string PowerIconPath(string id)
        {
            if (id == "magnet") return "icon_magnet";
            if (id == "tornado") return "icon_tornado";
            return "icon_freeze";
        }

        private static void PlacePortrait(RectTransform parent, string path, Vector2 pos, Vector2 size)
        {
            var img = UIKit.CreateIcon(parent, path, size);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
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

        // ================= Pick feedback =================

        private void OnCorrectPick(CandyTypeDefinition type)
        {
            Haptics.Light();
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
            Transform chip = _orderChipsRow.Find("Chip_" + type.typeId);
            if (chip != null)
                StartCoroutine(PunchScale(chip));
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
                Transform chip = _orderChipsRow.Find("Chip_" + cur.types[i].typeId);
                if (chip == null) continue;
                Transform count = chip.Find("Count");
                if (count != null)
                    count.GetComponent<Text>().text = cur.types[i].LocalizedName + " x" + cur.remaining[i];
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
            var toast = UIKit.CreatePanel(null, "Toast", UIKit.Grape);
            toast.SetParent(_safeRoot, false);
            toast.anchorMin = new Vector2(0.5f, 0.35f);
            toast.anchorMax = new Vector2(0.5f, 0.35f);
            toast.sizeDelta = new Vector2(900, 130);
            toast.anchoredPosition = Vector2.zero;
            var txt = UIKit.CreateText(toast, message, 38, Color.white);
            UIKit.Stretch((RectTransform)txt.transform, toast);
            yield return new WaitForSeconds(2.2f);
            Destroy(toast.gameObject);
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
                    // Double the speed reward only (never the perfect +5).
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

        // Shift Over screen: after a successful serve with stamina < 1 for the next guest.
        // No revive, no fail -3 (spec 8.2).
        private void BuildShiftOverPopup(Transform parent)
        {
            var dim = UIKit.CreatePanel(parent, "ShiftOverDim", new Color(0, 0, 0, 0.6f));
            UIKit.Stretch(dim, parent);
            dim.GetComponent<Image>().raycastTarget = true;
            _shiftOverPopup = dim.gameObject;

            var panel = UIKit.CreatePanel(dim, "ShiftOverPanel", UIKit.Cream);
            panel.sizeDelta = new Vector2(860, 760);
            panel.anchoredPosition = Vector2.zero;

            _shiftOverTitle = UIKit.CreateText(panel, "", 80, UIKit.SkyMint);
            _shiftOverTitle.name = "ShiftOverTitle";
            _shiftOverTitle.rectTransform.anchoredPosition = new Vector2(0, 220);

            _shiftOverBody = UIKit.CreateText(panel, "", 44, UIKit.Cocoa);
            _shiftOverBody.name = "ShiftOverBody";
            var brt = (RectTransform)_shiftOverBody.transform;
            brt.sizeDelta = new Vector2(760, 120);
            brt.anchoredPosition = new Vector2(0, 90);

            // Same stat lines as Game Over: served / coins / best (+ new record) — spec 10.5.
            _shiftOverStats = UIKit.CreateText(panel, "", 40, UIKit.Cocoa, TextAnchor.UpperCenter);
            _shiftOverStats.name = "ShiftOverStats";
            var srt = (RectTransform)_shiftOverStats.transform;
            srt.sizeDelta = new Vector2(760, 260);
            srt.anchoredPosition = new Vector2(0, -80);

            _shiftOverMenuBtn = UIKit.CreateButton(panel, "", new Vector2(700, 140), UIKit.SugarPink);
            _shiftOverMenuBtn.transform.localPosition = new Vector3(0, -240, 0);
            _shiftOverMenuBtn.onClick.AddListener(() => _game.ReturnToMenu());

            _shiftOverTitle.text = I18nService.Get("stamina_shift_title");
            _shiftOverBody.text = I18nService.Get("stamina_shift_body");
            SetButtonText(_shiftOverMenuBtn, I18nService.Get("btn_main_menu"));

            _shiftOverPopup.SetActive(false);
        }

        private void HideAllPopups()
        {
            if (_pausePopup != null) _pausePopup.SetActive(false);
            if (_quitConfirm != null) _quitConfirm.SetActive(false);
            if (_gameOverPopup != null) _gameOverPopup.SetActive(false);
            if (_buySheet != null) _buySheet.SetActive(false);
            if (_insufficientSheet != null) _insufficientSheet.SetActive(false);
            if (_shiftOverPopup != null) _shiftOverPopup.SetActive(false);
        }
    }
}
