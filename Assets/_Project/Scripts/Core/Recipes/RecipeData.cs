using System;
using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;

namespace HellsKitchen.Core.Recipes
{
    /// <summary>
    /// 레시피 한 건의 순수 데이터. ScriptableObject(RecipeDefinition)가
    /// 이 타입으로 변환해서 Core 로 넘겨주기 때문에, Core 는 유니티를 몰라도 된다.
    /// </summary>
    public sealed class RecipeData
    {
        private static readonly IngredientStack[] Empty = Array.Empty<IngredientStack>();

        public RecipeData(
            string id,
            string displayName,
            IReadOnlyList<IngredientStack> requiredIngredients,
            int baseReward,
            float baseTimeLimitSeconds,
            IReadOnlyList<IngredientStack> optionalAdditions = null,
            IReadOnlyList<IngredientStack> removableIngredients = null)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("레시피 id 는 비어 있을 수 없습니다.", nameof(id));

            Id = id;
            DisplayName = displayName ?? id;
            RequiredIngredients = requiredIngredients ?? Empty;
            BaseReward = baseReward;
            BaseTimeLimitSeconds = baseTimeLimitSeconds;
            OptionalAdditions = optionalAdditions ?? Empty;
            RemovableIngredients = removableIngredients ?? Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>변형이 붙기 전의 기본 구성.</summary>
        public IReadOnlyList<IngredientStack> RequiredIngredients { get; }

        /// <summary>제한시간을 거의 다 쓰고 냈을 때 받는 기본 보상(골드).</summary>
        public int BaseReward { get; }

        /// <summary>난이도 보정이 없을 때의 제한시간.</summary>
        public float BaseTimeLimitSeconds { get; }

        /// <summary>"추가" 변형으로 붙을 수 있는 재료 후보. 예: 치즈 추가, 양파 추가.</summary>
        public IReadOnlyList<IngredientStack> OptionalAdditions { get; }

        /// <summary>
        /// "빼기" 변형이 허용되는 재료. 빵처럼 빠지면 요리가 성립하지 않는 재료는
        /// 여기에 넣지 않는다.
        /// </summary>
        public IReadOnlyList<IngredientStack> RemovableIngredients { get; }

        public IngredientMultiset BuildBaseRequirement() => new IngredientMultiset(RequiredIngredients);
    }
}
