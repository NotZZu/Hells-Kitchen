namespace HellsKitchen.Core.Ingredients
{
    /// <summary>
    /// 재료의 가공 상태. 하나의 재료는 항상 정확히 하나의 상태를 가진다.
    /// Raw -> Chopped -> Cooked -> Burned 순으로 진행되지만,
    /// 모든 재료가 모든 상태를 가질 수 있는 것은 아니다(IngredientDefinition 참고).
    /// </summary>
    public enum ProcessState
    {
        Raw = 0,        // 손질 전
        Chopped = 1,    // 썬 상태
        Cooked = 2,     // 익힌 상태
        Burned = 3,     // 탄 상태(실패)
    }
}
