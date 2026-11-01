using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using HellsKitchen.Core.Recipes;
using UnityEngine;

namespace HellsKitchen.Runtime.Items
{
    /// <summary>
    /// 재료를 담는 접시. 담긴 재료는 IngredientItem 인스턴스 그대로 자식으로 쌓아 둔다.
    /// 이렇게 하면 시각 표현과 판정 데이터가 항상 같은 원본을 보게 된다.
    ///
    /// 쌓는 순서는 판정과 무관하지만 보기에는 중요하다.
    /// 빵이 맨 아래에 깔리고 위에 재료가 올라가야 버거로 보이기 때문에,
    /// 재료 정의의 PlateSortOrder 로 항상 같은 순서로 다시 정렬한다.
    /// </summary>
    public class PlateItem : Carryable
    {
        [Header("담기 규칙")]
        [Tooltip("접시 하나에 올릴 수 있는 최대 재료 수")]
        [SerializeField] private int _capacity = 6;

        [Tooltip("재료가 쌓이는 기준점")]
        [SerializeField] private Transform _stackRoot;

        private readonly List<IngredientItem> _contents = new List<IngredientItem>();
        private GameObject _topper;

        public override string DisplayName => _contents.Count == 0 ? "빈 접시" : $"접시({_contents.Count})";

        public IReadOnlyList<IngredientItem> Contents => _contents;
        public bool IsEmpty => _contents.Count == 0;
        public bool IsFull => _contents.Count >= _capacity;

        /// <summary>담긴 재료가 하나라도 탔는가. UI에서 경고를 띄우는 데 쓴다.</summary>
        public bool HasBurnedIngredient
        {
            get
            {
                for (int i = 0; i < _contents.Count; i++)
                {
                    if (_contents[i] != null && _contents[i].IsBurned) return true;
                }
                return false;
            }
        }

        /// <summary>주문 판정에 넘길 내용물 집합.</summary>
        public IngredientMultiset BuildContents()
        {
            var set = new IngredientMultiset();
            for (int i = 0; i < _contents.Count; i++)
            {
                var item = _contents[i];
                if (item == null || item.Type == IngredientType.None) continue;
                set.Add(item.Stack);
            }
            return set;
        }

        /// <summary>재료를 담을 수 있는지 검사만 한다(실제로 담지는 않는다).</summary>
        public bool CanAccept(IngredientItem ingredient)
        {
            if (ingredient == null || IsFull) return false;
            if (ingredient.Type == IngredientType.None) return false;
            return true;
        }

        /// <summary>재료를 접시에 올린다. 성공하면 재료의 소유권도 접시로 넘어간다.</summary>
        public bool TryAdd(IngredientItem ingredient)
        {
            if (!CanAccept(ingredient)) return false;

            ingredient.Detach();
            ingredient.SetPlateMode(true);
            _contents.Add(ingredient);
            LayoutStack();
            return true;
        }

        /// <summary>가장 위에 보이는 재료를 다시 꺼낸다. 실수로 담았을 때를 위한 여지.</summary>
        public IngredientItem TakeTop()
        {
            if (_contents.Count == 0) return null;

            // 화면에서 맨 위에 보이는 것은 정렬 기준이 가장 큰 재료다.
            var index = 0;
            for (int i = 1; i < _contents.Count; i++)
            {
                if (_contents[i] != null && _contents[i].PlateSortOrder >= _contents[index].PlateSortOrder) index = i;
            }

            var item = _contents[index];
            _contents.RemoveAt(index);

            if (item != null)
            {
                item.Detach();
                item.SetPlateMode(false);
            }

            LayoutStack();
            return item;
        }

        /// <summary>서빙이 끝났을 때. 담긴 재료를 모두 없애고 빈 접시로 되돌린다.</summary>
        public void ClearContents()
        {
            for (int i = 0; i < _contents.Count; i++)
            {
                if (_contents[i] != null) _contents[i].Dispose();
            }
            _contents.Clear();
            ClearTopper();
        }

        public override void Dispose()
        {
            ClearContents();
            base.Dispose();
        }

        private void LayoutStack()
        {
            var root = _stackRoot != null ? _stackRoot : transform;

            // 담은 순서와 무관하게 항상 같은 모양이 되도록 다시 정렬한다.
            _contents.Sort(CompareStackOrder);

            var height = 0f;
            for (int i = 0; i < _contents.Count; i++)
            {
                var item = _contents[i];
                if (item == null) continue;

                // 부모만 바꾸지 말고 소유 관계까지 넘겨야 한다.
                //
                // SetParent 만 하면 재료의 '주인'이 비어 있는 상태로 남아,
                // 접시에 담긴 재료가 바닥에 굴러다니는 물건으로 취급된다.
                // 그러면 접시를 집으려 할 때 재료가 먼저 뽑혀 나온다.
                item.AttachTo(root, held: false);
                item.transform.localPosition = new Vector3(0f, height, 0f);

                height += item.StackHeight;
            }

            RefreshTopper(root, height);
        }

        private static int CompareStackOrder(IngredientItem a, IngredientItem b)
        {
            if (a == null) return b == null ? 0 : 1;
            if (b == null) return -1;
            return a.PlateSortOrder.CompareTo(b.PlateSortOrder);
        }

        /// <summary>
        /// 맨 아래 재료가 덮개를 가지고 있으면(빵) 그 위에 덮개를 얹는다.
        ///
        /// 빵만 올려 둔 상태에서도 덮개가 보이고, 재료를 하나 넣을 때마다
        /// 그 재료의 두께만큼 덮개가 올라간다. 그래야 접시를 보는 것만으로
        /// "지금 몇 개나 들어갔는지"가 읽힌다. 완성 판정과는 무관한 시각 규칙이다.
        /// </summary>
        private void RefreshTopper(Transform root, float height)
        {
            ClearTopper();

            if (_contents.Count == 0) return;

            var bottom = _contents[0];
            if (bottom == null) return;

            var topperPrefab = bottom.PlateTopperModel;
            if (topperPrefab == null) return;

            _topper = Instantiate(topperPrefab, root);
            _topper.name = "Topper";

            // 재료 모델과 같은 이유로 프리팹의 회전과 배율을 유지한 채 위치만 맞춘다.
            var scale = bottom.Definition != null ? bottom.Definition.DisplayScale : 1f;
            _topper.transform.localPosition = new Vector3(0f, height, 0f);
            _topper.transform.localScale *= scale;
        }

        private void ClearTopper()
        {
            if (_topper == null) return;

            Destroy(_topper);
            _topper = null;
        }

#if UNITY_EDITOR
        public void EditorConfigure(Transform stackRoot, int capacity)
        {
            _stackRoot = stackRoot;
            _capacity = capacity;
        }
#endif
    }
}
