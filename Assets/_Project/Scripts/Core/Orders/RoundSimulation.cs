using System;
using System.Collections.Generic;
using HellsKitchen.Core.Progression;
using HellsKitchen.Core.Recipes;
using HellsKitchen.Core.Scoring;
using HellsKitchen.Core.Utility;

namespace HellsKitchen.Core.Orders
{
    public enum RoundState
    {
        Ready = 0,
        Running = 1,
        Finished = 2,
    }

    /// <summary>
    /// 한 라운드 전체를 굴리는 시뮬레이션.
    /// 주문판(OrderBoard) + 성적표(ScoreBoard) + 난이도 곡선을 하나로 묶는다.
    ///
    /// 유니티에 전혀 의존하지 않으므로 그대로 서버에 올릴 수 있고,
    /// 테스트에서는 Tick 을 원하는 만큼 호출해 몇 분짜리 라운드를 즉시 돌려볼 수 있다.
    /// </summary>
    public sealed class RoundSimulation
    {
        private readonly DifficultySettings _difficulty;
        private readonly ScoreRules _scoreRules;
        private readonly StarThresholds _stars;

        public RoundSimulation(
            IReadOnlyList<RecipeData> menu,
            float durationSeconds,
            DifficultySettings difficulty,
            ScoreRules scoreRules,
            StarThresholds stars,
            IRandomSource random)
        {
            DurationSeconds = durationSeconds;
            _difficulty = difficulty ?? new DifficultySettings();
            _scoreRules = scoreRules ?? new ScoreRules();
            _stars = stars ?? new StarThresholds();

            Score = new ScoreBoard(_scoreRules);
            Board = new OrderBoard(menu, random ?? new SeededRandom(Environment.TickCount));
            Board.OrderRemoved += HandleOrderRemoved;
        }

        public OrderBoard Board { get; }
        public ScoreBoard Score { get; }

        public float DurationSeconds { get; }
        public float ElapsedSeconds { get; private set; }
        public float RemainingSeconds => Math.Max(0f, DurationSeconds - ElapsedSeconds);
        public RoundState State { get; private set; } = RoundState.Ready;

        /// <summary>라운드 진행도 0~1. 난이도 곡선의 입력값이다.</summary>
        public float Progress01 => DurationSeconds <= 0f ? 1f : Clamp01(ElapsedSeconds / DurationSeconds);

        public DifficultySnapshot CurrentDifficulty => _difficulty.Evaluate(Progress01);

        public event Action<Order> OrderSpawned;
        public event Action<Order> OrderExpired;
        public event Action<ServeResult> PlateServed;
        public event Action<RoundSummary> RoundFinished;

        public void Start(float firstSpawnDelay = 2f)
        {
            if (State == RoundState.Running) return;

            ElapsedSeconds = 0f;
            Score.Reset();
            Board.Clear();
            Board.OrderAdded -= HandleOrderAdded;
            Board.OrderAdded += HandleOrderAdded;

            State = RoundState.Running;
            Board.Prime(firstSpawnDelay, CurrentDifficulty);
        }

        public void Tick(float deltaSeconds)
        {
            if (State != RoundState.Running || deltaSeconds <= 0f) return;

            ElapsedSeconds += deltaSeconds;

            var difficulty = CurrentDifficulty;

            // 라운드 종료가 임박하면 새 주문을 띄우지 않는다.
            // 플레이어가 도저히 완성할 수 없는 주문으로 페널티를 먹는 건 부당하다.
            var allowSpawn = RemainingSeconds > MinimumTimeForNewOrder(difficulty);

            Board.Tick(deltaSeconds, difficulty, allowSpawn);

            if (ElapsedSeconds >= DurationSeconds) Finish();
        }

        private float MinimumTimeForNewOrder(DifficultySnapshot difficulty)
        {
            // 가장 짧은 주문 하나를 소화할 시간은 남겨 둔다.
            return 15f * difficulty.TimeLimitScale;
        }

        /// <summary>서빙 창구에 접시를 냈을 때 호출한다.</summary>
        public ServeResult SubmitPlate(IngredientMultiset plateContents)
        {
            if (plateContents == null || plateContents.IsEmpty)
            {
                var empty = new ServeResult(ServeOutcome.EmptyPlate, null, 0, 0, 0f);
                PlateServed?.Invoke(empty);
                return empty;
            }

            var match = Board.FindMatch(plateContents);
            if (match == null)
            {
                var delta = Score.RegisterMismatch();
                var miss = new ServeResult(ServeOutcome.Mismatch, null, delta, delta, 0f);
                PlateServed?.Invoke(miss);
                return miss;
            }

            var punctuality = match.RemainingRatio;
            var reward = Score.RegisterSuccess(match.Reward, punctuality);
            Board.Complete(match);

            var result = new ServeResult(ServeOutcome.Success, match, reward, reward, punctuality);
            PlateServed?.Invoke(result);
            return result;
        }

        public void Finish()
        {
            if (State == RoundState.Finished) return;

            State = RoundState.Finished;
            ElapsedSeconds = DurationSeconds;
            Board.Clear();

            var summary = new RoundSummary(
                Score.Gold,
                _stars.Evaluate(Score.Gold),
                Score.ServedCount,
                Score.ExpiredCount,
                Score.MismatchCount,
                Score.BestCombo,
                _stars);

            RoundFinished?.Invoke(summary);
        }

        private void HandleOrderAdded(Order order) => OrderSpawned?.Invoke(order);

        private void HandleOrderRemoved(Order order, OrderRemovalReason reason)
        {
            if (reason != OrderRemovalReason.Expired) return;

            Score.RegisterExpired();
            OrderExpired?.Invoke(order);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>라운드 결과 화면에 그대로 꽂아 쓰는 요약.</summary>
    public readonly struct RoundSummary
    {
        public readonly int Gold;
        public readonly int Stars;
        public readonly int Served;
        public readonly int Expired;
        public readonly int Mismatched;
        public readonly int BestCombo;
        public readonly StarThresholds Thresholds;

        public RoundSummary(int gold, int stars, int served, int expired, int mismatched, int bestCombo, StarThresholds thresholds)
        {
            Gold = gold;
            Stars = stars;
            Served = served;
            Expired = expired;
            Mismatched = mismatched;
            BestCombo = bestCombo;
            Thresholds = thresholds;
        }
    }
}
