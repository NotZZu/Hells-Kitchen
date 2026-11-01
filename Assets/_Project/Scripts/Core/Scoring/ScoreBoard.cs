using System;

namespace HellsKitchen.Core.Scoring
{
    /// <summary>
    /// 한 라운드 동안의 성적. 골드가 곧 점수인 구조지만,
    /// 별점(스타) 판정을 위해 누적 골드와 실패 횟수를 따로 들고 있는다.
    /// </summary>
    public sealed class ScoreBoard
    {
        private readonly ScoreRules _rules;

        public ScoreBoard(ScoreRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public int Gold { get; private set; }
        public int ServedCount { get; private set; }
        public int ExpiredCount { get; private set; }
        public int MismatchCount { get; private set; }

        /// <summary>현재 연속 성공 수.</summary>
        public int Combo { get; private set; }

        /// <summary>이번 라운드에서 도달한 최고 콤보.</summary>
        public int BestCombo { get; private set; }

        public float CurrentComboMultiplier => _rules.GetComboMultiplier(Combo);

        public event Action Changed;

        /// <summary>성공 서빙을 기록하고 실제 지급 골드를 돌려준다.</summary>
        public int RegisterSuccess(int baseReward, float punctuality)
        {
            var reward = _rules.CalculateReward(baseReward, punctuality, Combo);
            Gold += reward;
            ServedCount++;
            Combo++;
            if (Combo > BestCombo) BestCombo = Combo;
            Changed?.Invoke();
            return reward;
        }

        /// <summary>주문 놓침. 잃은 골드를 돌려준다(음수).</summary>
        public int RegisterExpired()
        {
            var delta = -Math.Min(_rules.ExpirePenaltyGold, Gold);
            Gold += delta;
            ExpiredCount++;
            Combo = 0;
            Changed?.Invoke();
            return delta;
        }

        /// <summary>오배송. 잃은 골드를 돌려준다(음수).</summary>
        public int RegisterMismatch()
        {
            var delta = -Math.Min(_rules.MismatchPenaltyGold, Gold);
            Gold += delta;
            MismatchCount++;
            Combo = 0;
            Changed?.Invoke();
            return delta;
        }

        public void Reset()
        {
            Gold = 0;
            ServedCount = 0;
            ExpiredCount = 0;
            MismatchCount = 0;
            Combo = 0;
            BestCombo = 0;
            Changed?.Invoke();
        }
    }
}
