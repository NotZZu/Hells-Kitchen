using System;
using System.Collections.Generic;
using HellsKitchen.Core.Recipes;

namespace HellsKitchen.Core.Orders
{
    /// <summary>
    /// 손님이 낸 주문 한 건. 화면의 티켓 하나에 대응한다.
    /// 남은 시간은 OrderBoard 가 매 틱 깎아 준다.
    /// </summary>
    public sealed class Order
    {
        public Order(int id, RecipeData recipe, IReadOnlyList<OrderModifier> modifiers, float timeLimitSeconds, int reward)
        {
            Id = id;
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            Modifiers = modifiers ?? Array.Empty<OrderModifier>();
            Requirement = RecipeResolver.Resolve(recipe, Modifiers);
            TimeLimitSeconds = timeLimitSeconds;
            RemainingSeconds = timeLimitSeconds;
            Reward = reward;
        }

        public int Id { get; }
        public RecipeData Recipe { get; }
        public IReadOnlyList<OrderModifier> Modifiers { get; }

        /// <summary>변형까지 반영한, 이 주문이 실제로 원하는 재료 집합.</summary>
        public IngredientMultiset Requirement { get; }

        public float TimeLimitSeconds { get; }
        public float RemainingSeconds { get; private set; }
        public int Reward { get; }

        /// <summary>변형이 하나라도 붙었으면 true. 티켓 UI에서 강조 표시용.</summary>
        public bool HasModifiers => Modifiers.Count > 0;

        /// <summary>남은 시간 비율(1 = 방금 들어옴, 0 = 시간 초과).</summary>
        public float RemainingRatio => TimeLimitSeconds <= 0f ? 0f : Clamp01(RemainingSeconds / TimeLimitSeconds);

        public bool IsExpired => RemainingSeconds <= 0f;

        internal void Advance(float deltaSeconds)
        {
            RemainingSeconds -= deltaSeconds;
            if (RemainingSeconds < 0f) RemainingSeconds = 0f;
        }

        /// <summary>이 접시가 주문과 일치하는가.</summary>
        public bool Matches(IngredientMultiset plateContents) => Requirement.ContentEquals(plateContents);

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        public override string ToString() => $"#{Id} {Recipe.DisplayName} [{Requirement}]";
    }
}
