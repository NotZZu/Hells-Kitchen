using System;

namespace HellsKitchen.Core.Utility
{
    /// <summary>
    /// 시드 하나로 완전히 재현되는 난수 생성기(xorshift32).
    /// System.Random 을 쓰지 않는 것은 .NET 런타임 버전에 따라
    /// 같은 시드에서도 다른 수열이 나올 수 있기 때문이다.
    /// </summary>
    public sealed class SeededRandom : IRandomSource
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            // 0 은 xorshift 에서 고정점이라 절대 나오면 안 된다.
            _state = seed == 0 ? 0x9E3779B9u : unchecked((uint)seed);
        }

        public int Seed => unchecked((int)_state);

        private uint NextBits()
        {
            var x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0) return 0;
            return (int)(NextBits() % (uint)exclusiveMax);
        }

        public float NextFloat()
        {
            // 상위 24비트만 써서 [0,1) 을 균등하게 만든다.
            return (NextBits() >> 8) * (1.0f / 16777216.0f);
        }

        /// <summary>목록에서 하나를 고른다. 비어 있으면 default 를 돌려준다.</summary>
        public T Pick<T>(System.Collections.Generic.IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0) return default;
            return items[NextInt(items.Count)];
        }
    }
}
