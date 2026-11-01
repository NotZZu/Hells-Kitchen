using System;

namespace HellsKitchen.Core.Scoring
{
    /// <summary>
    /// 보상과 페널티를 계산하는 규칙. 값만 담고 상태는 갖지 않는다.
    /// "얼마를 벌었나"의 판단 근거가 이 한 곳에만 있어야 밸런싱이 쉽다.
    /// </summary>
    [Serializable]
    public sealed class ScoreRules
    {
        /// <summary>제출 즉시(남은 시간 100%)일 때 기본 보상에 추가로 붙는 비율.</summary>
        public float MaxTipRatio = 0.5f;

        /// <summary>주문을 놓쳤을 때 잃는 골드.</summary>
        public int ExpirePenaltyGold = 10;

        /// <summary>엉뚱한 접시를 냈을 때 잃는 골드.</summary>
        public int MismatchPenaltyGold = 5;

        /// <summary>연속 성공 하나당 붙는 배율 증가분.</summary>
        public float ComboStepMultiplier = 0.1f;

        /// <summary>콤보 배율 상한.</summary>
        public float MaxComboMultiplier = 2f;

        /// <summary>
        /// 성공 보상. 기본 보상에 신속 팁과 콤보 배율을 곱해서 얹는다.
        /// </summary>
        /// <param name="baseReward">레시피의 기본 보상</param>
        /// <param name="punctuality">남은 시간 비율 0~1</param>
        /// <param name="comboCount">이번 성공을 포함하지 않은 직전까지의 연속 성공 수</param>
        public int CalculateReward(int baseReward, float punctuality, int comboCount)
        {
            punctuality = Clamp01(punctuality);
            var tipped = baseReward * (1f + MaxTipRatio * punctuality);
            var total = tipped * GetComboMultiplier(comboCount);
            return (int)Math.Round(total, MidpointRounding.AwayFromZero);
        }

        public float GetComboMultiplier(int comboCount)
        {
            if (comboCount <= 0) return 1f;
            var multiplier = 1f + ComboStepMultiplier * comboCount;
            return multiplier > MaxComboMultiplier ? MaxComboMultiplier : multiplier;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
