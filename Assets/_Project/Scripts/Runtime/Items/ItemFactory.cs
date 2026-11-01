using HellsKitchen.Core.Ingredients;
using HellsKitchen.Data;
using UnityEngine;

namespace HellsKitchen.Runtime.Items
{
    /// <summary>
    /// 재료와 접시를 만들어 내는 단일 창구.
    /// 스테이션들이 프리팹을 각자 들고 있으면 나중에 오브젝트 풀이나
    /// 네트워크 스폰으로 바꿀 때 손댈 곳이 흩어진다. 여기 하나만 고치면 되도록 모아 둔다.
    /// </summary>
    public class ItemFactory : MonoBehaviour
    {
        public static ItemFactory Instance { get; private set; }

        [Header("프리팹")]
        [Tooltip("IngredientItem 컴포넌트가 붙은 빈 껍데기. 모델은 재료 정의에서 채운다.")]
        [SerializeField] private IngredientItem _ingredientPrefab;

        [SerializeField] private PlateItem _platePrefab;

        [Header("데이터")]
        [SerializeField] private IngredientCatalog _catalog;

        public IngredientCatalog Catalog => _catalog;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public IngredientItem CreateIngredient(IngredientType type, ProcessState state = ProcessState.Raw)
        {
            if (_catalog == null || _ingredientPrefab == null)
            {
                Debug.LogError("[ItemFactory] 재료 프리팹 또는 카탈로그가 비어 있습니다.", this);
                return null;
            }

            var definition = _catalog.Get(type);
            if (definition == null)
            {
                Debug.LogError($"[ItemFactory] 카탈로그에 {type} 정의가 없습니다.", this);
                return null;
            }

            return CreateIngredient(definition, state);
        }

        public IngredientItem CreateIngredient(IngredientDefinition definition, ProcessState state = ProcessState.Raw)
        {
            if (definition == null || _ingredientPrefab == null) return null;

            var item = Instantiate(_ingredientPrefab);
            item.name = $"Ingredient_{definition.Type}";
            item.Initialize(definition, state);
            return item;
        }

        public PlateItem CreatePlate()
        {
            if (_platePrefab == null)
            {
                Debug.LogError("[ItemFactory] 접시 프리팹이 비어 있습니다.", this);
                return null;
            }

            var plate = Instantiate(_platePrefab);
            plate.name = "Plate";
            return plate;
        }

#if UNITY_EDITOR
        public void EditorConfigure(IngredientItem ingredientPrefab, PlateItem platePrefab, IngredientCatalog catalog)
        {
            _ingredientPrefab = ingredientPrefab;
            _platePrefab = platePrefab;
            _catalog = catalog;
        }
#endif
    }
}
