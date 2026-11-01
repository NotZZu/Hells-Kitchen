using System;
using HellsKitchen.Core.Ingredients;

namespace HellsKitchen.Core.Recipes
{
    public enum ModifierKind
    {
        /// <summary>기본 레시피에 재료를 하나 더 요구한다. 예: "치즈 추가"</summary>
        Add = 0,
        /// <summary>기본 레시피에서 재료를 하나 뺀다. 예: "양상추 빼주세요"</summary>
        Remove = 1,
    }

    /// <summary>
    /// 주문 단위로 기본 레시피를 비트는 변형 규칙.
    /// 난이도가 올라가면 이 변형이 붙기 시작해서, 플레이어가
    /// "외운 레시피"가 아니라 "지금 티켓"을 읽게 만든다.
    /// </summary>
    [Serializable]
    public readonly struct OrderModifier
    {
        public readonly ModifierKind Kind;
        public readonly IngredientStack Target;

        public OrderModifier(ModifierKind kind, IngredientStack target)
        {
            Kind = kind;
            Target = target;
        }

        public static OrderModifier Add(IngredientStack stack) => new OrderModifier(ModifierKind.Add, stack);
        public static OrderModifier Remove(IngredientStack stack) => new OrderModifier(ModifierKind.Remove, stack);

        public override string ToString()
        {
            return Kind == ModifierKind.Add ? $"{Target.Type} 추가" : $"{Target.Type} 빼기";
        }
    }
}
