using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 쓰레기통. 탄 재료나 잘못 만든 요리를 버린다.
    /// 접시 자체는 버려지지 않고 내용물만 비워진다 — 접시를 잃으면 복구가 불가능해지기 때문이다.
    /// </summary>
    public class TrashStation : Station
    {
        [Header("연출")]
        [SerializeField] private GameObject _trashEffect;

        public override string DisplayName => "쓰레기통";

        /// <summary>탄 재료는 쓰레기통이 유일한 목적지다. 그 외에는 끌어당기지 않는다.</summary>
        public override float GetAttraction(Carryable item)
        {
            if (item is IngredientItem ingredient && ingredient.IsBurned) return 3f;
            return 0f;
        }

        public override bool TryPlace(Carryable held)
        {
            if (held == null) return false;

            if (held is PlateItem plate)
            {
                if (plate.IsEmpty) return false;

                plate.ClearContents();
                PlayEffect();
                // 접시는 플레이어 손에 그대로 남는다.
                return false;
            }

            held.Dispose();
            PlayEffect();
            return true;
        }

        private void PlayEffect()
        {
            if (_trashEffect == null) return;
            _trashEffect.SetActive(false);
            _trashEffect.SetActive(true);
        }
    }
}
