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
        private Text _coinsText;
        private RectTransform _timerFill;
        private Image _timerFillImage;

        private RectTransform _queueStrip;
        private RectTransform _orderChipsRow;
        private Text _challengeChipText;

        private readonly List<(PowerUpDefinition def, Text badge)> _powerButtons =
            new List<(PowerUpDefinition, Text)>();

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

        private Image _vignette;
        private Text _comboText;
        private Text _perfectStamp;
        private Coroutine _comboCo;
        private Coroutine _stampCo;

        private int _comboCount;
        private int _tutorialStep;
        private bool _runStarted;
        private int _bestAtRunStart;
        private string _lastFailReason;
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

            if (PendingRecipeUnlockToast != null)
            {
                StartCoroutine(ShowToastRoutine("新糖果上架：" + PendingRecipeUnlockToast, 0.6f));
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
        }

        private void BeginRun()
        {
            if (_runStarted) return;
            _runStarted = true;
            _game.StartRun();
        }

        // ================= UI construction =================

        private void BuildUI()
        {
            var canvas = UIKit.CreateCanvas(null, "GameHUD");
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

            _starsText = UIKit.CreateText(top, "★★★", 56, UIKit.Lemon, TextAnchor.MiddleLeft);
            UIKit.Place((RectTransform)_starsText.transform,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -45), new Vector2(320, 45));

            _coinsText = UIKit.CreateText(top, "金币 0", 46, UIKit.Lemon, TextAnchor.MiddleRight);
            UIKit.Place((RectTransform)_coinsText.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-280, -42), new Vector2(-130, 42));

            var pauseBtn = UIKit.CreateButton(top, "⏸", Vector2.one * 96f, UIKit.Grape);
            UIKit.Place((RectTransform)pauseBtn.transform,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-116, -48), new Vector2(-20, 48));
            pauseBtn.onClick.AddListener(() =>
            {
                _game.SetPaused(true);
                _pausePopup.SetActive(true);
            });

            // ---- Timer bar ----
            var timerBg = UIKit.CreatePanel(top, "TimerBg", UIKit.Ice);
            UIKit.Place(timerBg, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -120), new Vector2(0, -76));
            var fillGo = new GameObject("TimerFill", typeof(Image));
            fillGo.transform.SetParent(timerBg, false);
            _timerFill = (RectTransform)fillGo.transform;
            _timerFill.anchorMin = Vector2.zero;
            _timerFill.anchorMax = Vector2.one;
            _timerFill.offsetMin = new Vector2(4, 4);
            _timerFill.offsetMax = new Vector2(-4, -4);
            _timerFillImage = fillGo.GetComponent<Image>();
            _timerFillImage.sprite = UIKit.RoundedSprite();
            _timerFillImage.type = Image.Type.Sliced;
            _timerFillImage.color = UIKit.SkyMint;

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

            _perfectStamp = UIKit.CreateText(_safeRoot, "完美", 100, UIKit.Berry);
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

            RefreshCoins();
            RefreshStars(3);
            RefreshChallengeChip();
            RefreshBadges();
        }

        private void AddPowerButton(RectTransform bottom, PowerUpDefinition def, int index)
        {
            const float w = 300f, h = 200f, gap = 40f;
            float x = -(w * 3 + gap * 2) / 2f + index * (w + gap) + w / 2f;

            var btn = UIKit.CreateButton(bottom, def.displayNameZh, new Vector2(w, h), def.accentColor, 44);
            var rt = (RectTransform)btn.transform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0);

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
            _powerButtons.Add((def, badgeText));
        }

        // ================= Popups =================

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

            var title = UIKit.CreateText(panel, "暂停", 64, UIKit.Cocoa);
            title.rectTransform.anchoredPosition = new Vector2(0, 400);

            var contBtn = UIKit.CreateButton(panel, "继续", new Vector2(600, 140), UIKit.SugarPink);
            contBtn.transform.localPosition = new Vector3(0, 240, 0);
            contBtn.onClick.AddListener(() => { HideAllPopups(); _game.SetPaused(false); });

            var quitBtn = UIKit.CreateButton(panel, "放弃本局", new Vector2(600, 140), UIKit.Grape);
            quitBtn.transform.localPosition = new Vector3(0, 70, 0);
            quitBtn.onClick.AddListener(ShowQuitConfirm);

            CreateToggle(panel, "音乐", SaveDataService.Current.musicEnabled, v =>
            {
                SaveDataService.Current.musicEnabled = v;
                SaveDataService.Save();
            }, new Vector2(0, -80));

            CreateToggle(panel, "音效", SaveDataService.Current.sfxEnabled, v =>
            {
                SaveDataService.Current.sfxEnabled = v;
                SaveDataService.Save();
            }, new Vector2(0, -190));

            CreateToggle(panel, "振动", SaveDataService.Current.hapticsEnabled, v =>
            {
                SaveDataService.Current.hapticsEnabled = v;
                SaveDataService.Save();
            }, new Vector2(0, -300));

            UIKit.CreateText(panel, "真的要打烊吗？点「放弃本局」确认", 30, UIKit.Grape)
                .rectTransform.anchoredPosition = new Vector2(0, -420);

            _pausePopup.SetActive(false);

            // Quit confirm layer
            var confirmDim = UIKit.CreatePanel(parent, "QuitConfirmDim", new Color(0, 0, 0, 0.55f));
            UIKit.Stretch(confirmDim, parent);
            confirmDim.GetComponent<Image>().raycastTarget = true;
            var cPanel = UIKit.CreatePanel(confirmDim, "Confirm", UIKit.Cream);
            cPanel.sizeDelta = new Vector2(820, 520);
            cPanel.anchoredPosition = Vector2.zero;

            var msg = UIKit.CreateText(cPanel, "真的要打烊吗？\n本局星星和进度会结束", 44, UIKit.Cocoa);
            msg.rectTransform.anchoredPosition = new Vector2(0, 100);

            var yes = UIKit.CreateButton(cPanel, "打烊", new Vector2(300, 130), UIKit.MagnetRed);
            yes.transform.localPosition = new Vector3(-180, -140, 0);
            yes.onClick.AddListener(() => { HideAllPopups(); _game.QuitRun(); });

            var no = UIKit.CreateButton(cPanel, "继续营业", new Vector2(300, 130), UIKit.SkyMint, 40, UIKit.Cocoa);
            no.transform.localPosition = new Vector3(180, -140, 0);
            no.onClick.AddListener(() => confirmDim.gameObject.SetActive(false));

            _quitConfirm = confirmDim.gameObject;
            _quitConfirm.SetActive(false);
        }

        private void ShowQuitConfirm()
        {
            if (_quitConfirm != null) _quitConfirm.SetActive(true);
        }

        private void CreateToggle(Transform parent, string label, bool value, System.Action<bool> onChanged, Vector2 pos)
        {
            var row = new GameObject("Toggle_" + label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rr = (RectTransform)row.transform;
            rr.sizeDelta = new Vector2(620, 84);
            rr.localPosition = pos;

            var t = UIKit.CreateText(row.transform, label, 40, UIKit.Cocoa, TextAnchor.MiddleLeft);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = new Vector2(0, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = new Vector2(360, 0);

            var btn = UIKit.CreateButton(row.transform, value ? "开" : "关", new Vector2(150, 78),
                value ? UIKit.SkyMint : new Color(0.72f, 0.72f, 0.72f), 36, UIKit.Cocoa);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-90, 0);

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

        private void BuildGameOverPopup(Transform parent)
        {
            var dim = UIKit.CreatePanel(parent, "GameOverDim", new Color(0, 0, 0, 0.6f));
            UIKit.Stretch(dim, parent);
            dim.GetComponent<Image>().raycastTarget = true;
            _gameOverPopup = dim.gameObject;

            var panel = UIKit.CreatePanel(dim, "GameOverPanel", UIKit.Cream);
            panel.sizeDelta = new Vector2(860, 1050);
            panel.anchoredPosition = Vector2.zero;

            var title = UIKit.CreateText(panel, "营业结束", 80, UIKit.Cocoa);
            title.rectTransform.anchoredPosition = new Vector2(0, 420);

            _gameOverStats = UIKit.CreateText(panel, "", 44, UIKit.Cocoa, TextAnchor.UpperCenter);
            var srt = (RectTransform)_gameOverStats.transform;
            srt.sizeDelta = new Vector2(800, 420);
            srt.anchoredPosition = new Vector2(0, 100);

            _reviveButton = UIKit.CreateButton(panel, "看广告再试一次", new Vector2(700, 140), UIKit.Grape, 42);
            _reviveButton.transform.localPosition = new Vector3(0, -250, 0);
            _reviveButton.onClick.AddListener(DoRevive);

            var menu = UIKit.CreateButton(panel, "回到主菜单", new Vector2(700, 140), UIKit.SugarPink);
            menu.transform.localPosition = new Vector3(0, -430, 0);
            menu.onClick.AddListener(() => _game.ReturnToMenu());

            _gameOverPopup.SetActive(false);
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

            _doubleButton = UIKit.CreateButton(chip, "看广告翻倍", new Vector2(360, 110), UIKit.Lemon, 36, UIKit.Cocoa);
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

            var note = UIKit.CreateText(panel, "购买需扣金币并看广告", 32, UIKit.Grape);
            note.rectTransform.anchoredPosition = new Vector2(0, 130);

            _buySheetMessage = UIKit.CreateText(panel, "", 34, UIKit.MagnetRed);
            _buySheetMessage.rectTransform.anchoredPosition = new Vector2(0, 60);
            _buySheetMessage.text = "";

            _buyButton = UIKit.CreateButton(panel, "购买并观看广告", new Vector2(680, 140), UIKit.SugarPink, 40);
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

            var cancel = UIKit.CreateButton(panel, "取消", new Vector2(680, 120),
                new Color(0.75f, 0.72f, 0.68f), 38, UIKit.Cocoa);
            cancel.transform.localPosition = new Vector3(0, -230, 0);
            cancel.onClick.AddListener(CloseBuySheet);

            _buyTitle = title;
            _buyPrice = price;

            // Insufficient-coins inner sheet
            var insufPanel = UIKit.CreatePanel(dim, "Insufficient", UIKit.Lemon);
            insufPanel.sizeDelta = new Vector2(820, 500);
            insufPanel.anchoredPosition = Vector2.zero;
            insufPanel.SetAsLastSibling();

            var insufMsg = UIKit.CreateText(insufPanel, "金币不足", 48, UIKit.Cocoa);
            insufMsg.rectTransform.anchoredPosition = new Vector2(0, 160);

            var adBtn = UIKit.CreateButton(insufPanel, "看广告获得 80 金币", new Vector2(660, 130), UIKit.SugarPink, 38);
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

            var closeInsuf = UIKit.CreateButton(insufPanel, "取消", new Vector2(660, 110),
                new Color(0.75f, 0.72f, 0.68f), 36, UIKit.Cocoa);
            closeInsuf.transform.localPosition = new Vector3(0, -160, 0);
            closeInsuf.onClick.AddListener(() => insufPanel.gameObject.SetActive(false));

            _insufficientSheet = insufPanel.gameObject;
            _insufficientSheet.SetActive(false);
            _buySheet.SetActive(false);
        }

        private Text _buyTitle;
        private Text _buyPrice;

        private void ShowInsufficientInBuySheet()
        {
            var ads = AdServiceLocator.Service;
            if (_insufficientSheet != null)
                _insufficientSheet.SetActive(ads != null && ads.IsReady(AdPlacement.reward_coins));
        }

        private void OpenBuySheet(PowerUpDefinition def)
        {
            _buySheetDef = def;
            _buyTitle.text = "购买 " + def.displayNameZh + " +1";
            _buyPrice.text = def.buyCost + " 金币";
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

            _tutorialNext = UIKit.CreateButton(card, "下一步", new Vector2(560, 140), UIKit.SugarPink);
            _tutorialNext.transform.localPosition = new Vector3(0, -240, 0);
            _tutorialNext.onClick.AddListener(() =>
            {
                if (_tutorialStep >= 3) FinishTutorial();
                else ShowTutorial(_tutorialStep + 1);
            });

            var skip = UIKit.CreateButton(card, "跳过", new Vector2(300, 100),
                new Color(0.78f, 0.75f, 0.7f), 34, UIKit.Cocoa);
            skip.transform.localPosition = new Vector3(0, -370, 0);
            skip.onClick.AddListener(FinishTutorial);

            _tutorialPopup.SetActive(false);
        }

        private void ShowTutorial(int step)
        {
            _tutorialStep = step;
            _tutorialPopup.SetActive(true);
            switch (step)
            {
                case 1:
                    _tutorialBody.text = "客人要的糖，\n点堆里的就可以";
                    _tutorialNext.GetComponentInChildren<Text>().text = "下一步";
                    break;
                case 2:
                    _tutorialBody.text = "点错会扣星星；\n完美接待可以补回一颗（最多三颗）";
                    break;
                default:
                    _tutorialBody.text = "道具有库存就能用；\n没了要花金币并看广告补充";
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

                var dot = UIKit.CreatePanel(chip, "Dot", type.chipColor);
                dot.sizeDelta = Vector2.one * 88;
                dot.anchoredPosition = new Vector2(-chipW / 2f + 72, 0);

                var label = UIKit.CreateText(chip, type.displayNameZh + " x" + order.remaining[i], 36, UIKit.Cocoa,
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

            var current = UIKit.CreatePanel(_queueStrip, "Customer_Current", UIKit.SugarPink);
            current.sizeDelta = new Vector2(300, 180);
            current.anchorMin = new Vector2(0, 0.5f);
            current.anchorMax = new Vector2(0, 0.5f);
            current.anchoredPosition = new Vector2(170, 10);
            var curLabel = UIKit.CreateText(current, "当前客人", 34, Color.white);
            curLabel.rectTransform.anchoredPosition = Vector2.zero;

            for (int i = 0; i < _game.orderConfig.waitingCount; i++)
            {
                var waitingState = _orders.GetWaiting(i);
                var card = UIKit.CreatePanel(_queueStrip, "Customer_Wait" + i,
                    waitingState != null ? new Color(0.87f, 0.83f, 0.79f) : new Color(0.92f, 0.92f, 0.92f, 0.5f));
                card.sizeDelta = new Vector2(230, 140);
                card.anchorMin = new Vector2(0, 0.5f);
                card.anchorMax = new Vector2(0, 0.5f);
                card.anchoredPosition = new Vector2(355 + i * 255, -10);
                var lbl = UIKit.CreateText(card, "排队中", 26, UIKit.Cocoa);
                lbl.rectTransform.anchoredPosition = Vector2.zero;
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
            if (_coinsText != null) _coinsText.text = "金币 " + EconomyManager.Coins;
        }

        private void RefreshBadges()
        {
            foreach (var entry in _powerButtons)
            {
                int count = _powerUps.CountOf(entry.def.powerUpId);
                entry.badge.text = count > 0 ? count.ToString() : "+";
            }
        }

        private void RefreshStars(int stars)
        {
            if (_starsText == null) return;
            string filled = new string('★', stars);
            string empty = new string('☆', Mathf.Max(0, 3 - stars));
            _starsText.text = filled + empty;
            _starsText.color = stars <= 1 ? UIKit.MagnetRed : UIKit.Lemon;
        }

        private void RefreshChallengeChip()
        {
            var save = SaveDataService.Current;
            var cfg = _game.dailyChallengeConfig;
            if (cfg == null || string.IsNullOrEmpty(save.dailyChallengeTypeId))
                return;
            _challengeChipText.text = save.dailyChallengeClaimed
                ? "今日配方：已完成"
                : "今日配方 进度 " + save.dailyChallengeProgress + "/" + cfg.quota;
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
                ShowCombo("连击 x" + _comboCount);
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
                    count.GetComponent<Text>().text = cur.types[i].displayNameZh + " x" + cur.remaining[i];
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
            if (!perfect) return;
            if (_stampCo != null) StopCoroutine(_stampCo);
            _stampCo = StartCoroutine(StampRoutine());
        }

        private IEnumerator StampRoutine()
        {
            _perfectStamp.text = "完美";
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
            StartCoroutine(ShowToastRoutine("找不到？用龙卷风翻一翻", 0f));
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

            var save = SaveDataService.Current;
            bool newBest = reason != "aborted" && _game.CustomersServed > _bestAtRunStart && _game.CustomersServed > 0;
            _gameOverStats.text = "本局服务 " + _game.CustomersServed + " 位\n" +
                                  "赚到 " + _game.CoinsEarnedThisRun + " 金币\n" +
                                  "历史最佳 " + save.bestCustomersServed +
                                  (newBest ? "\n新纪录!" : "");

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

        private void HideAllPopups()
        {
            if (_pausePopup != null) _pausePopup.SetActive(false);
            if (_quitConfirm != null) _quitConfirm.SetActive(false);
            if (_gameOverPopup != null) _gameOverPopup.SetActive(false);
            if (_buySheet != null) _buySheet.SetActive(false);
            if (_insufficientSheet != null) _insufficientSheet.SetActive(false);
        }
    }
}
