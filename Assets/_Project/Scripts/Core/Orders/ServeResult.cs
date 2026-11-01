namespace HellsKitchen.Core.Orders
{
    public enum ServeOutcome
    {
        /// <summary>주문과 정확히 일치. 보상 지급.</summary>
        Success = 0,
        /// <summary>대기 중인 어떤 주문과도 맞지 않음. 페널티.</summary>
        Mismatch = 1,
        /// <summary>빈 접시를 냈다.</summary>
        EmptyPlate = 2,
    }

    /// <summary>서빙 창구에 접시를 냈을 때의 판정 결과.</summary>
    public readonly struct ServeResult
    {
        public readonly ServeOutcome Outcome;
        public readonly Order MatchedOrder;
        public readonly int GoldDelta;
        public readonly int ScoreDelta;

        /// <summary>얼마나 빨리 냈는가(1 = 즉시, 0 = 시간 다 씀). 팁 계산과 연출에 쓴다.</summary>
        public readonly float Punctuality;

        public ServeResult(ServeOutcome outcome, Order matchedOrder, int goldDelta, int scoreDelta, float punctuality)
        {
            Outcome = outcome;
            MatchedOrder = matchedOrder;
            GoldDelta = goldDelta;
            ScoreDelta = scoreDelta;
            Punctuality = punctuality;
        }

        public bool IsSuccess => Outcome == ServeOutcome.Success;
    }
}
