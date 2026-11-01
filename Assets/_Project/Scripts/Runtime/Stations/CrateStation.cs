using HellsKitchen.Core.Ingredients;
using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 재료 상자. 상호작용할 때마다 지정된 재료를 새로 하나 꺼내 준다.
    /// 재고 개념은 두지 않는다. 이 장르의 압박은 재료 부족이 아니라 시간에서 와야 한다.
    /// </summary>
    public class CrateStation : Station
    {
        [Header("공급 재료")]
        [SerializeField] private IngredientType _ingredientType = IngredientType.None;

        public IngredientType IngredientType => _ingredientType;

        public override string DisplayName => $"{_ingredientType} 상자";

        /// <summary>상자는 꺼내는 곳이지 넣는 곳이 아니다. 유도하지 않는다.</summary>
        public override float GetAttraction(Carryable item) => 0f;

        public override Carryable TryTake()
        {
            var factory = ItemFactory.Instance;
            if (factory == null)
            {
                Debug.LogError("[CrateStation] 씬에 ItemFactory 가 없습니다.", this);
                return null;
            }

            var item = factory.CreateIngredient(_ingredientType);
            return item;
        }

        public override bool TryPlace(Carryable held)
        {
            // 방금 꺼낸 재료를 잘못 집었을 때 되돌려 넣을 수 있게 해 준다.
            if (!(held is IngredientItem ingredient)) return false;
            if (ingredient.Type != _ingredientType) return false;
            if (ingredient.State != ProcessState.Raw) return false;

            ingredient.Dispose();
            return true;
        }

#if UNITY_EDITOR
        public void EditorSetIngredient(IngredientType type) => _ingredientType = type;
#endif
    }
}
