using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 플레이어가 상호작용할 수 있는 설비의 공통 뼈대.
    ///
    /// 상호작용은 세 갈래로만 나뉜다.
    ///  - 빈손으로 다가가 집는다        -> TryTake
    ///  - 무언가를 들고 와서 넘긴다      -> TryPlace
    ///  - 접시를 들고 와서 재료만 옮긴다 -> TryTransferTo
    /// 이 세 가지 밖의 특수 규칙은 두지 않는다. 조작이 단순해야 손이 빨라진다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class Station : MonoBehaviour
    {
        [Header("공통")]
        [Tooltip("물건이 올라갈 위치. 비우면 이 오브젝트의 트랜스폼을 쓴다.")]
        [SerializeField] protected Transform _placePoint;

        [Tooltip("플레이어가 바라볼 때 켜질 표시 오브젝트")]
        [SerializeField] protected GameObject _highlightVisual;

        public Transform PlacePoint => _placePoint != null ? _placePoint : transform;

        /// <summary>UI에 띄울 이 설비의 이름.</summary>
        public virtual string DisplayName => name;

        /// <summary>작업 버튼을 누르고 있을 때 반응하는 설비인가(도마 등).</summary>
        public virtual bool SupportsWork => false;

        /// <summary>
        /// 지금 이 설비에 할 일이 남아 있는가.
        /// 손질이 끝났는데도 계속 칼질하는 것처럼 보이지 않게 하려면,
        /// 설비가 스스로 "이제 할 게 없다"고 말해 줘야 한다.
        /// </summary>
        public virtual bool HasWorkPending => false;

        /// <summary>
        /// 던져진 물건이 이 설비 쪽으로 얼마나 끌려야 하는가.
        /// 1이면 보통, 그보다 크면 적극적으로 끌어당긴다. 0이면 유도하지 않는다.
        ///
        /// 재료의 지금 상태에서 '다음에 가야 할 곳'이 어디인지는 설비가 가장 잘 안다.
        /// 그래서 판단을 각 설비에 맡긴다.
        /// </summary>
        public virtual float GetAttraction(Carryable item) => item != null ? 1f : 0f;

        /// <summary>진행바를 띄워야 하는가.</summary>
        public virtual bool ShowsProgress => false;

        /// <summary>진행도 0~1.</summary>
        public virtual float Progress01 => 0f;

        /// <summary>진행바 색으로 위험 상태(타는 중)를 알릴 때 쓴다.</summary>
        public virtual bool IsProgressCritical => false;

        protected virtual void Awake()
        {
            SetHighlighted(false);
        }

        /// <summary>빈손인 플레이어가 상호작용했다. 가져갈 물건이 있으면 돌려준다.</summary>
        public virtual Carryable TryTake() => null;

        /// <summary>플레이어가 들고 온 물건을 넘긴다. true 면 플레이어의 손이 빈다.</summary>
        public virtual bool TryPlace(Carryable held) => false;

        /// <summary>접시를 든 플레이어가 상호작용했다. 여기 놓인 재료를 접시로 옮긴다.</summary>
        public virtual bool TryTransferTo(PlateItem plate) => false;

        /// <summary>작업 버튼을 누르고 있는 동안 매 프레임 호출된다.</summary>
        public virtual void OnWorkHeld(float deltaSeconds) { }

        /// <summary>작업 버튼을 뗐을 때.</summary>
        public virtual void OnWorkReleased() { }

        public virtual void SetHighlighted(bool highlighted)
        {
            if (_highlightVisual != null) _highlightVisual.SetActive(highlighted);
        }

#if UNITY_EDITOR
        public void EditorSetPlacePoint(Transform placePoint) => _placePoint = placePoint;
        public void EditorSetHighlight(GameObject highlight) => _highlightVisual = highlight;
#endif
    }
}
