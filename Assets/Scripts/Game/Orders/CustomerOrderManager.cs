using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyShop
{
    // Generates the customer queue, runs per-customer timers, and resolves candy picks.
    public class CustomerOrderManager : MonoBehaviour
    {
        public static CustomerOrderManager Instance { get; private set; }

        [Header("References")]
        public CandyPileRestock pile;
        public PowerUpManager powerUps;

        // Current order + waiting queue (current + N waiting per config).
        public CustomerOrderState Current { get; private set; }
        private readonly List<CustomerOrderState> _waiting = new List<CustomerOrderState>();

        public bool TimerRunning { get; private set; }
        public bool Frozen { get; private set; }
        public bool AwaitingServeUi => _awaitingServeUi;
        private float _frozenUntil = -1f;

        public event Action<CustomerOrderState> OrderStarted;
        public event Action QueueChanged;
        public event Action<float, float> TimerTick;      // timeLeft, totalTime
        public event Action<CandyTypeDefinition> CorrectPick;
        public event Action WrongPick;
        public event Action<string> ServeCompletedRewardText; // "+N"
        public event Action<bool> ServePerfectStamp;          // true = show the perfect stamp
        public event Action BuriedHintRequested;

        private float _secondsSinceCorrectPick;
        private bool _buriedHintShown;
        private bool _awaitingServeUi;
        private int _servedCountForScaling;

        public GameManager game => GameManager.Instance;

        private void Awake() => Instance = this;

        public void BeginQueue()
        {
            // The first guest of a run becomes current here; this start spends 1 stamina.
            if (!StaminaService.SpendForCurrentGuest())
            {
                game.EndRun("aborted"); // should not happen: menu gates at stamina < 1
                return;
            }
            Current = GenerateOrder();
            _waiting.Clear();
            for (int i = 0; i < (game.orderConfig.waitingCount); i++)
                _waiting.Add(GenerateOrder());
            _servedCountForScaling = 0;
            StartTimerFor(Current);
            OrderStarted?.Invoke(Current);
            QueueChanged?.Invoke();
            if (pile != null) pile.RestockForOrder(Current);
        }

        private void Update()
        {
            if (game == null || !game.RunActive || Current == null) return;
            if (game.Paused) return;              // Pause freezes the timer (no VFX)
            if (_awaitingServeUi) return;

            if (Frozen)
            {
                if (Time.unscaledTime >= _frozenUntil) Frozen = false;
                else return;                       // Freeze pauses the countdown only
            }

            Current.timeLeft -= Time.deltaTime;
            TimerTick?.Invoke(Mathf.Max(0f, Current.timeLeft), Current.totalTime);

            _secondsSinceCorrectPick += Time.deltaTime;
            if (!_buriedHintShown && _secondsSinceCorrectPick >= game.orderConfig.buriedHintSeconds)
            {
                _buriedHintShown = true;
                BuriedHintRequested?.Invoke();
            }

            if (Current.timeLeft <= 0f)
            {
                TimerRunning = false;
                game.OnTimerExpired();
            }
        }

        // ---- Order generation (spec 5.2) ----
        public CustomerOrderState GenerateOrder()
        {
            var save = SaveDataService.Current;
            var cfg = game.orderConfig;
            var unlocked = GetUnlockedTypes();

            int typeCount = UnityEngine.Random.Range(cfg.minTypes, Mathf.Min(cfg.maxTypes, unlocked.Count) + 1);

            var chosen = new List<CandyTypeDefinition>();
            var pool = new List<CandyTypeDefinition>(unlocked);

            var featured = DailySignInService.GetFeatured(game.catalog, save);
            bool featuredUsable = featured != null && unlocked.Contains(featured) &&
                                  UnityEngine.Random.value < game.dailyChallengeConfig.biasChance;
            if (featuredUsable) chosen.Add(featured);

            while (chosen.Count < typeCount && pool.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                var pick = pool[idx];
                if (!chosen.Contains(pick)) chosen.Add(pick);
                pool.RemoveAt(idx);
            }
            if (chosen.Count == 0 && unlocked.Count > 0) chosen.Add(unlocked[0]);

            int total = UnityEngine.Random.Range(cfg.minTotal, cfg.maxTotal + 1);
            // Mild difficulty scaling: once past each N-served milestone, bias toward the upper half.
            if (_servedCountForScaling >= cfg.scaleEveryCustomers &&
                UnityEngine.Random.value < 0.5f)
                total = UnityEngine.Random.Range((cfg.minTotal + cfg.maxTotal) / 2, cfg.maxTotal + 1);
            total = Mathf.Min(total, cfg.maxTotal);

            if (chosen.Count == 0)
            {
                // No unlocked types (empty catalog / bad data): fail safe with a minimal order
                // instead of indexing into an empty list.
                var empty = new CustomerOrderState { totalCandies = 0 };
                empty.totalTime = Mathf.Clamp(cfg.baseSeconds, cfg.minSeconds, cfg.maxSeconds);
                empty.timeLeft = empty.totalTime;
                return empty;
            }

            var order = new CustomerOrderState { totalCandies = total };
            foreach (var t in chosen)
            {
                order.types.Add(t);
                order.remaining.Add(1); // every chosen type gets at least 1
            }
            int leftToDistribute = total - chosen.Count;
            for (int i = 0; i < leftToDistribute; i++)
            {
                int idx = UnityEngine.Random.Range(0, order.remaining.Count);
                order.remaining[idx]++;
            }

            float t2 = cfg.baseSeconds + total * cfg.secondsPerCandy;
            order.totalTime = Mathf.Clamp(t2, cfg.minSeconds, cfg.maxSeconds);
            order.timeLeft = order.totalTime;
            return order;
        }

        public List<CandyTypeDefinition> GetUnlockedTypes()
        {
            var result = new List<CandyTypeDefinition>();
            if (game?.catalog == null) return result;
            foreach (var c in game.catalog)
            {
                if (c == null) continue;
                if (c.isStarter || IsUnlocked(c)) result.Add(c);
            }
            return result;
        }

        private static bool IsUnlocked(CandyTypeDefinition type)
        {
            return Array.IndexOf(SaveDataService.Current.unlockedRecipeIds, type.typeId) >= 0;
        }

        private void StartTimerFor(CustomerOrderState order)
        {
            TimerRunning = true;
            _secondsSinceCorrectPick = 0f;
            _buriedHintShown = false;
        }

        // ---- Picking (spec 6) ----
        public PickResult Pick(CandyInstance candy)
        {
            if (game == null || !game.RunActive || Current == null || _awaitingServeUi)
                return PickResult.Ignored;
            if (candy == null || candy.Picked) return PickResult.Ignored; // already-taken: ignore

            string typeId = candy.candyTypeId;
            int remainingNow = Current.RemainingOf(typeId);

            bool correct = remainingNow > 0;
            candy.MarkRemoved();

            if (correct)
            {
                DecrementRemaining(typeId);
                _secondsSinceCorrectPick = 0f;
                _buriedHintShown = false;
                CorrectPick?.Invoke(candy.definition);
                game.NotifyCorrectPick(typeId);

                if (Current.IsComplete)
                    CompleteCustomer();
                else
                    pile?.RestockIfNeeded(Current, typeId);
                return PickResult.Correct;
            }

            // Wrong tap: remove the candy and cost one star.
            Current.wrongPicksThisCustomer++;
            WrongPick?.Invoke();
            game.OnWrongPick();
            return PickResult.Wrong;
        }

        private void DecrementRemaining(string typeId)
        {
            for (int i = 0; i < Current.types.Count; i++)
            {
                if (Current.types[i].typeId == typeId)
                {
                    Current.remaining[i] = Mathf.Max(0, Current.remaining[i] - 1);
                    return;
                }
            }
        }

        public int RemainingOf(CandyTypeDefinition type)
        {
            return Current != null ? Current.RemainingOf(type.typeId) : -1;
        }

        // ---- Serve / rewards (spec 6 & 7.2) ----
        public void CompleteCustomer()
        {
            var econ = game.economyConfig;
            float speedRatio = Current.totalTime > 0f ? Mathf.Clamp01(Current.timeLeft / Current.totalTime) : 0f;
            float rewardF = econ.baseReward + Current.totalCandies * econ.perCandy + speedRatio * econ.speedBonusMax;
            int reward = Mathf.Max(econ.minReward, Mathf.RoundToInt(rewardF));
            bool wasPerfect = Current.perfect;

            EconomyManager.AddCoins(reward + (wasPerfect ? econ.perfectBonus : 0));
            game.OnCustomerServed(reward, wasPerfect);
            _servedCountForScaling++;

            // Pause the run until the serve chip resolves (double / skip / 2.5s timeout).
            NotifyServeChipShown();
            ServePerfectStamp?.Invoke(wasPerfect);
            ServeCompletedRewardText?.Invoke("+" + reward);
        }

        // Called by HUD after the double-reward chip resolves (or is skipped).
        public void AdvanceQueue()
        {
            if (Current == null) return;
            _awaitingServeUi = false;

            // Stamina gate (spec 8.2): the next guest becomes current only if we can pay.
            // Otherwise the shift is over — no revive, no fail penalty.
            if (!StaminaService.SpendForCurrentGuest())
            {
                game.EndRun("shift_over");
                return;
            }

            Current = _waiting.Count > 0 ? ShiftQueue() : GenerateOrder();
            while (_waiting.Count < game.orderConfig.waitingCount)
                _waiting.Add(GenerateOrder());

            StartTimerFor(Current);
            OrderStarted?.Invoke(Current);
            QueueChanged?.Invoke();
            pile?.RestockForOrder(Current);
        }

        private CustomerOrderState ShiftQueue()
        {
            var next = _waiting[0];
            _waiting.RemoveAt(0);
            return next;
        }

        public void NotifyServeChipShown()
        {
            _awaitingServeUi = true;
            TimerRunning = false;
        }

        public CustomerOrderState GetWaiting(int index)
        {
            return index >= 0 && index < _waiting.Count ? _waiting[index] : null;
        }

        // ---- Freeze power-up: pause countdown only ----
        public void FreezeTimer(float seconds)
        {
            Frozen = true;
            _frozenUntil = Time.unscaledTime + seconds;
        }

        // ---- Magnet helper: up to N unpicked candies whose type is still required ----
        public List<CandyInstance> GetRequiredCandies(int maxCount)
        {
            var result = new List<CandyInstance>();
            if (pile == null || Current == null) return result;
            foreach (var inst in pile.GetUnpickedInstances())
            {
                if (result.Count >= maxCount) break;
                if (Current.RemainingOf(inst.candyTypeId) > 0)
                    result.Add(inst);
            }
            return result;
        }

        public void ApplyMagnetPicks(List<CandyInstance> candies)
        {
            foreach (var c in candies)
                Pick(c);
        }

        public void ResumeAfterRevive(bool restoreTimeout)
        {
            if (restoreTimeout && Current != null)
            {
                Current.timeLeft = Current.totalTime; // full timer back
                TimerRunning = true;
            }
            _awaitingServeUi = false;
        }
    }
}
