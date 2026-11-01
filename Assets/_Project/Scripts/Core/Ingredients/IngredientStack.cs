using System;

namespace HellsKitchen.Core.Ingredients
{
    /// <summary>
    /// "무슨 재료가 어떤 상태인가"를 나타내는 값 객체.
    /// 레시피 요구 사항과 접시에 담긴 내용물을 같은 타입으로 표현해
    /// 비교를 단순한 집합 연산으로 만든다.
    /// </summary>
    [Serializable]
    public readonly struct IngredientStack : IEquatable<IngredientStack>
    {
        public readonly IngredientType Type;
        public readonly ProcessState State;

        public IngredientStack(IngredientType type, ProcessState state)
        {
            Type = type;
            State = state;
        }

        public static IngredientStack Raw(IngredientType type) => new IngredientStack(type, ProcessState.Raw);

        /// <summary>상태만 바꾼 새 값을 돌려준다(원본은 불변).</summary>
        public IngredientStack WithState(ProcessState state) => new IngredientStack(Type, state);

        public bool IsNone => Type == IngredientType.None;
        public bool IsRuined => State == ProcessState.Burned;

        public bool Equals(IngredientStack other) => Type == other.Type && State == other.State;
        public override bool Equals(object obj) => obj is IngredientStack other && Equals(other);

        public override int GetHashCode()
        {
            // 재료 종류와 상태를 하나의 정수로 접는다. 상태 종류가 적으므로 8비트면 충분하다.
            return ((int)Type << 8) ^ (int)State;
        }

        public static bool operator ==(IngredientStack a, IngredientStack b) => a.Equals(b);
        public static bool operator !=(IngredientStack a, IngredientStack b) => !a.Equals(b);

        public override string ToString() => $"{Type}({State})";
    }
}
