namespace HellsKitchen.Core.Utility
{
    /// <summary>
    /// 난수 공급자. UnityEngine.Random 을 직접 쓰지 않는 이유는 두 가지다.
    /// 1) 테스트에서 결과를 고정할 수 있어야 하고,
    /// 2) 나중에 온라인 협동으로 갈 때 호스트와 클라이언트가 같은 주문 순서를 봐야 한다.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>[0, exclusiveMax) 범위의 정수.</summary>
        int NextInt(int exclusiveMax);

        /// <summary>[0, 1) 범위의 실수.</summary>
        float NextFloat();
    }
}
