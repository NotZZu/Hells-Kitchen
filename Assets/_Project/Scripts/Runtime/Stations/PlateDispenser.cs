using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 접시 선반. 접시를 꺼내고, 빈 접시를 되돌려 놓을 수 있다.
    /// 설거지는 세로 슬라이스 범위 밖이므로 지금은 접시가 항상 깨끗하다.
    /// </summary>
    public class PlateDispenser : Station
    {
        [Header("선반")]
        [Tooltip("0 이하면 무제한")]
        [SerializeField] private int _maxPlates = 0;

        [SerializeField] private int _availablePlates = 0;

        public override string DisplayName => "접시 선반";

        public bool HasPlate => _maxPlates <= 0 || _availablePlates > 0;

        private void Start()
        {
            if (_maxPlates > 0 && _availablePlates <= 0) _availablePlates = _maxPlates;
        }

        /// <summary>빈 접시를 돌려놓는 정도의 약한 유도만 둔다.</summary>
        public override float GetAttraction(Carryable item)
        {
            return item is PlateItem plate && plate.IsEmpty ? 1.2f : 0f;
        }

        public override Carryable TryTake()
        {
            if (!HasPlate) return null;

            var factory = ItemFactory.Instance;
            if (factory == null)
            {
                Debug.LogError("[PlateDispenser] 씬에 ItemFactory 가 없습니다.", this);
                return null;
            }

            var plate = factory.CreatePlate();
            if (plate == null) return null;

            if (_maxPlates > 0) _availablePlates--;
            return plate;
        }

        public override bool TryPlace(Carryable held)
        {
            if (!(held is PlateItem plate)) return false;
            if (!plate.IsEmpty) return false;

            plate.Dispose();
            if (_maxPlates > 0) _availablePlates = Mathf.Min(_maxPlates, _availablePlates + 1);
            return true;
        }

        /// <summary>서빙 창구가 접시를 되돌려 줄 때 호출한다.</summary>
        public void ReturnPlate()
        {
            if (_maxPlates > 0) _availablePlates = Mathf.Min(_maxPlates, _availablePlates + 1);
        }
    }
}
