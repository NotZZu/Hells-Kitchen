using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Player
{
    /// <summary>
    /// 플레이어의 손. 한 번에 하나만 들 수 있다는 규칙이 이 게임 난이도의 뼈대다.
    /// </summary>
    public class PlayerHands : MonoBehaviour
    {
        [Tooltip("들고 있는 물건이 붙을 위치. 보통 가슴 앞쪽.")]
        [SerializeField] private Transform _holdAnchor;

        public Carryable Held { get; private set; }
        public bool IsEmpty => Held == null;
        public Transform HoldAnchor => _holdAnchor != null ? _holdAnchor : transform;

        public void Hold(Carryable item)
        {
            if (item == null) return;

            Held = item;
            item.AttachTo(HoldAnchor, held: true);
        }

        /// <summary>손에서 놓되 물건은 그대로 돌려준다. 넘겨받을 쪽이 처리한다.</summary>
        public Carryable Release()
        {
            var item = Held;
            Held = null;
            return item;
        }

        /// <summary>이미 다른 쪽이 물건을 가져갔을 때 손만 비운다.</summary>
        public void Clear() => Held = null;

#if UNITY_EDITOR
        public void EditorSetAnchor(Transform anchor) => _holdAnchor = anchor;
#endif
    }
}
