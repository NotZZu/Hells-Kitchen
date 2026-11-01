using UnityEngine;

namespace HellsKitchen.Runtime.Items
{
    /// <summary>
    /// 플레이어가 들 수 있는 물건의 공통 뼈대.
    /// 물리 시뮬레이션은 쓰지 않는다. 들리는 순간 부모를 바꾸고 로컬 좌표를 0으로 맞추는
    /// 방식이 오버쿡드류의 빠른 손놀림에 훨씬 안정적이다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class Carryable : MonoBehaviour
    {
        [Header("배치")]
        [Tooltip("놓였을 때 표면에서 띄울 높이")]
        [SerializeField] protected float _placeHeightOffset = 0f;

        /// <summary>현재 이 물건을 들고 있거나 올려놓은 주체. 아무 데도 없으면 null.</summary>
        public Transform CurrentAnchor { get; private set; }

        public bool IsHeld { get; private set; }

        /// <summary>손에 들었을 때의 표시 이름. UI 디버그용.</summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// 지정한 앵커에 붙인다. 손이면 held=true, 조리대면 false.
        /// </summary>
        public virtual void AttachTo(Transform anchor, bool held)
        {
            CurrentAnchor = anchor;
            IsHeld = held;

            transform.SetParent(anchor, false);
            transform.localPosition = held ? Vector3.zero : new Vector3(0f, _placeHeightOffset, 0f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            gameObject.SetActive(true);
        }

        /// <summary>앵커에서 떼어낸다. 다음 AttachTo 까지는 어디에도 속하지 않는다.</summary>
        public virtual void Detach()
        {
            CurrentAnchor = null;
            IsHeld = false;
            transform.SetParent(null, true);
        }

        /// <summary>날아가는 중인가. ThrownItem 이 켜고 끈다.</summary>
        public bool IsFlying { get; internal set; }

        /// <summary>
        /// 바닥에 굴러다니는 상태인가.
        /// 손에도 설비에도 속하지 않고 날아가는 중도 아닌 물건만 주울 수 있다.
        /// </summary>
        public bool IsLoose => CurrentAnchor == null && !IsHeld && !IsFlying;

        /// <summary>바닥에 내려놓는다.</summary>
        public virtual void PlaceOnGround(Vector3 position)
        {
            Detach();
            transform.position = position;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        /// <summary>버려지거나 소비될 때. 오브젝트 풀을 도입하면 여기만 고치면 된다.</summary>
        public virtual void Dispose()
        {
            Detach();
            Destroy(gameObject);
        }
    }
}
