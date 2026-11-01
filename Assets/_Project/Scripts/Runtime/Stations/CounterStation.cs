using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 물건 하나를 올려 둘 수 있는 조리대. 다른 설비들의 기반이 된다.
    ///
    /// 여기서 접시와 재료의 합치기 규칙이 결정된다.
    ///  - 접시가 놓인 대에 재료를 들고 오면 -> 재료가 접시로 들어간다
    ///  - 재료가 놓인 대에 접시를 들고 오면 -> 재료가 접시로 들어가고 접시는 손에 남는다
    /// </summary>
    public class CounterStation : Station
    {
        [SerializeField] protected Carryable _current;

        public Carryable Current => _current;
        public bool HasItem => _current != null;

        public override string DisplayName => "조리대";

        public override Carryable TryTake()
        {
            if (_current == null) return null;

            var item = _current;
            _current = null;
            item.Detach();
            OnItemRemoved(item);
            return item;
        }

        public override bool TryPlace(Carryable held)
        {
            if (held == null) return false;

            // 1) 빈 조리대: 그냥 올려 둔다.
            if (_current == null)
            {
                if (!CanAccept(held)) return false;
                Attach(held);
                return true;
            }

            // 2) 접시가 놓여 있고 재료를 들고 왔다: 접시에 담는다.
            if (_current is PlateItem plate && held is IngredientItem ingredient)
            {
                if (!plate.TryAdd(ingredient)) return false;
                OnPlateFilled(plate, ingredient);
                return true;
            }

            return false;
        }

        public override bool TryTransferTo(PlateItem plate)
        {
            if (plate == null) return false;
            if (!(_current is IngredientItem ingredient)) return false;
            if (!plate.CanAccept(ingredient)) return false;

            _current = null;
            if (!plate.TryAdd(ingredient))
            {
                // 담기에 실패하면 원상 복구한다.
                _current = ingredient;
                Attach(ingredient);
                return false;
            }

            OnItemRemoved(ingredient);
            OnPlateFilled(plate, ingredient);
            return true;
        }

        /// <summary>
        /// 손질을 마친 재료는 접시로 가야 한다.
        /// 접시가 놓인 조리대를 다른 무엇보다 강하게 끌어당기는 이유다.
        /// </summary>
        public override float GetAttraction(Carryable item)
        {
            if (item == null) return 0f;

            if (_current is PlateItem plate && item is IngredientItem ingredient)
            {
                return plate.CanAccept(ingredient) ? 3.2f : 0f;
            }

            if (_current != null) return 0f;

            // 빈 조리대는 접시를 내려놓기에 가장 자연스러운 자리다.
            // 접시를 조리대에 올려야 재료를 담을 수 있으므로 확실히 받아 준다.
            if (item is PlateItem) return 2.6f;

            // 재료에게 빈 조리대는 갈 곳이 마땅치 않을 때의 차선책이다.
            return 1.1f;
        }

        /// <summary>이 설비가 해당 물건을 받아도 되는지. 특수 설비가 좁혀서 재정의한다.</summary>
        protected virtual bool CanAccept(Carryable item) => true;

        protected void Attach(Carryable item)
        {
            _current = item;
            item.AttachTo(PlacePoint, held: false);
            OnItemPlaced(item);
        }

        protected virtual void OnItemPlaced(Carryable item) { }
        protected virtual void OnItemRemoved(Carryable item) { }
        protected virtual void OnPlateFilled(PlateItem plate, IngredientItem ingredient) { }
    }
}
