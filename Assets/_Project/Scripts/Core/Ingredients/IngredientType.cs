namespace HellsKitchen.Core.Ingredients
{
    /// <summary>
    /// 게임에 등장하는 재료의 종류.
    /// 값(숫자)은 세이브 데이터와 네트워크 패킷에 그대로 실릴 수 있으므로
    /// 한 번 정한 숫자는 바꾸지 말고, 새 재료는 항상 뒤에 추가한다.
    /// </summary>
    public enum IngredientType
    {
        None = 0,
        Bun = 1,        // 빵
        Patty = 2,      // 고기 패티
        Lettuce = 3,    // 양상추
        Tomato = 4,     // 토마토
        Cheese = 5,     // 치즈
        Onion = 6,      // 양파
        Potato = 7,     // 감자
        Carrot = 8,     // 당근
        Steak = 9,      // 스테이크
        Ham = 10,       // 햄
    }
}
