using System;

namespace HellsKitchen.Core.Scoring
{
    /// <summary>
    /// 라운드 종료 시 별 개수 판정. 목표 골드를 기준으로 1~3성을 준다.
    /// </summary>
    [Serializable]
    public sealed class StarThresholds
    {
        public int OneStar = 60;
        public int TwoStar = 110;
        public int ThreeStar = 170;

        public int Evaluate(int gold)
        {
            if (gold >= ThreeStar) return 3;
            if (gold >= TwoStar) return 2;
            if (gold >= OneStar) return 1;
            return 0;
        }

        /// <summary>다음 별까지 얼마나 왔는지(0~1). 결과 화면 게이지용.</summary>
        public float ProgressToNextStar(int gold)
        {
            if (gold >= ThreeStar) return 1f;

            int lower, upper;
            if (gold >= TwoStar) { lower = TwoStar; upper = ThreeStar; }
            else if (gold >= OneStar) { lower = OneStar; upper = TwoStar; }
            else { lower = 0; upper = OneStar; }

            var span = upper - lower;
            if (span <= 0) return 1f;
            return (gold - lower) / (float)span;
        }
    }
}
