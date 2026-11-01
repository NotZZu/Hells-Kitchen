using System.Collections.Generic;

namespace HellsKitchen.Core.Recipes
{
    /// <summary>
    /// 기본 레시피 + 변형 목록 -> 이 주문이 실제로 요구하는 재료 집합.
    /// 순수 함수라 테스트하기 쉽고, 서버/클라 양쪽에서 같은 결과가 나온다.
    /// </summary>
    public static class RecipeResolver
    {
        public static IngredientMultiset Resolve(RecipeData recipe, IReadOnlyList<OrderModifier> modifiers)
        {
            var requirement = recipe.BuildBaseRequirement();
            if (modifiers == null) return requirement;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                switch (modifier.Kind)
                {
                    case ModifierKind.Add:
                        requirement.Add(modifier.Target);
                        break;

                    case ModifierKind.Remove:
                        // 없는 재료를 빼라는 변형은 조용히 무시한다.
                        // (레시피가 바뀌어도 저장된 주문 데이터가 깨지지 않도록)
                        requirement.Remove(modifier.Target);
                        break;
                }
            }

            return requirement;
        }
    }
}
