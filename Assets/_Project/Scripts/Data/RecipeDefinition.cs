using System.Collections.Generic;
using HellsKitchen.Core.Recipes;
using UnityEngine;

namespace HellsKitchen.Data
{
    /// <summary>
    /// 레시피 한 건. 인스펙터에서 편집하고, 런타임에는 Core 의 RecipeData 로 변환해 넘긴다.
    /// </summary>
    [CreateAssetMenu(menuName = "Hell's Kitchen/레시피", fileName = "Recipe_")]
    public class RecipeDefinition : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("세이브/네트워크에 실리는 고유 문자열. 한번 정하면 바꾸지 않는다.")]
        public string Id = "";

        public string DisplayName = "";

        [Header("구성")]
        [Tooltip("이 요리를 완성하는 데 필요한 재료와 상태. 담는 순서는 판정에 영향을 주지 않는다.")]
        public List<IngredientRef> Ingredients = new List<IngredientRef>();

        [Header("보상과 시간")]
        [Min(1)] public int BaseReward = 20;

        [Tooltip("난이도 보정 전 제한시간(초)")]
        [Min(5f)] public float BaseTimeLimitSeconds = 60f;

        [Header("주문 변형")]
        [Tooltip("'추가' 변형으로 붙을 수 있는 재료")]
        public List<IngredientRef> OptionalAdditions = new List<IngredientRef>();

        [Tooltip("'빼기' 변형이 허용되는 재료. 빵처럼 빠지면 요리가 성립하지 않는 것은 넣지 않는다.")]
        public List<IngredientRef> RemovableIngredients = new List<IngredientRef>();

        [Header("표시")]
        [Tooltip("완성 요리의 모델. 티켓 아이콘 자동 생성과 서빙 연출에 쓴다.")]
        public GameObject CompletedModel;

        [Tooltip("주문 티켓의 대표 아이콘")]
        public Sprite Icon;

        private RecipeData _cached;

        public string ResolvedId => string.IsNullOrEmpty(Id) ? name : Id;
        public string ResolvedName => string.IsNullOrEmpty(DisplayName) ? ResolvedId : DisplayName;

        private void OnEnable() => _cached = null;
        private void OnValidate() => _cached = null;

        /// <summary>Core 로 넘길 순수 데이터. 값이 바뀌지 않는 한 재사용한다.</summary>
        public RecipeData ToRecipeData()
        {
            if (_cached != null) return _cached;

            _cached = new RecipeData(
                ResolvedId,
                ResolvedName,
                IngredientRef.ToStacks(Ingredients),
                BaseReward,
                BaseTimeLimitSeconds,
                IngredientRef.ToStacks(OptionalAdditions),
                IngredientRef.ToStacks(RemovableIngredients));

            return _cached;
        }
    }
}
