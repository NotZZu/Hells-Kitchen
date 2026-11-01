using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using UnityEngine;

namespace HellsKitchen.Data
{
    /// <summary>
    /// 재료 정의를 한곳에 모아 두고 타입으로 찾을 수 있게 하는 목록.
    /// 런타임 코드가 개별 에셋을 직접 참조하지 않도록 하는 단일 진입점이다.
    /// </summary>
    [CreateAssetMenu(menuName = "Hell's Kitchen/재료 카탈로그", fileName = "IngredientCatalog")]
    public class IngredientCatalog : ScriptableObject
    {
        [SerializeField] private List<IngredientDefinition> _definitions = new List<IngredientDefinition>();

        private Dictionary<IngredientType, IngredientDefinition> _lookup;

        public IReadOnlyList<IngredientDefinition> Definitions => _definitions;

        private void OnEnable() => _lookup = null;

        private void EnsureLookup()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<IngredientType, IngredientDefinition>();
            for (int i = 0; i < _definitions.Count; i++)
            {
                var def = _definitions[i];
                if (def == null || def.Type == IngredientType.None) continue;
                _lookup[def.Type] = def;
            }
        }

        public IngredientDefinition Get(IngredientType type)
        {
            EnsureLookup();
            return _lookup.TryGetValue(type, out var def) ? def : null;
        }

        public bool TryGet(IngredientType type, out IngredientDefinition definition)
        {
            EnsureLookup();
            return _lookup.TryGetValue(type, out definition);
        }

#if UNITY_EDITOR
        /// <summary>에디터 자동 생성 툴에서만 사용한다.</summary>
        public void EditorSetDefinitions(List<IngredientDefinition> definitions)
        {
            _definitions = definitions;
            _lookup = null;
        }
#endif
    }
}
