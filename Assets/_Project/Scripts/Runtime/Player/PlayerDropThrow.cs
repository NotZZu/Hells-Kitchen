using HellsKitchen.Runtime.Inputs;
using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Player
{
    /// <summary>
    /// 내려놓기와 던지기를 한 버튼으로 처리한다.
    ///
    /// 짧게 누르면 발 앞에 내려놓고, 계속 누르고 있으면 게이지가 차서 멀리 던진다.
    /// 버튼을 하나로 합친 이유는, 급할 때 "일단 손을 비우는" 동작과
    /// "저쪽 조리대로 보내는" 동작이 같은 판단에서 나오기 때문이다.
    /// 손가락이 한 번 더 옮겨 갈 여유가 없는 순간에 둘 다 필요하다.
    /// </summary>
    [RequireComponent(typeof(PlayerHands))]
    public class PlayerDropThrow : MonoBehaviour
    {
        [Header("차징")]
        [Tooltip("이 시간 안에 떼면 던지지 않고 그냥 내려놓는다.")]
        [SerializeField] private float _chargeDelay = 0.22f;

        [Tooltip("최대 세기까지 누르고 있어야 하는 시간(차징 시작 이후)")]
        [SerializeField] private float _fullChargeSeconds = 0.9f;

        [Header("내려놓기")]
        [Tooltip("발 앞 어느 정도 거리에 내려놓을지")]
        [SerializeField] private float _dropDistance = 0.95f;

        [Header("던지기")]
        [SerializeField] private float _minThrowSpeed = 6f;
        [SerializeField] private float _maxThrowSpeed = 14f;

        [Tooltip("던지는 각도(도). 클수록 높이 뜬다.")]
        [Range(0f, 60f)]
        [SerializeField] private float _throwAngle = 24f;

        private PlayerHands _hands;
        private IIntentSource _intentSource;

        private float _holdTime;
        private bool _wasHeld;

        // 취소한 뒤에는 버튼을 뗄 때까지 아무 일도 일어나지 않아야 한다.
        // 그렇지 않으면 취소하자마자 손을 떼는 순간 그대로 던져 버린다.
        private bool _cancelledUntilRelease;

        /// <summary>차징 게이지 0~1. UI가 읽어 간다.</summary>
        public float ChargeRatio
        {
            get
            {
                if (_holdTime <= _chargeDelay) return 0f;
                if (_fullChargeSeconds <= 0f) return 1f;
                return Mathf.Clamp01((_holdTime - _chargeDelay) / _fullChargeSeconds);
            }
        }

        /// <summary>지금 던질 준비를 하고 있는가. 게이지를 띄울지 결정한다.</summary>
        public bool IsCharging => _holdTime > _chargeDelay && !_hands.IsEmpty;

        private void Awake()
        {
            _hands = GetComponent<PlayerHands>();
            _intentSource = GetComponent<IIntentSource>();

            if (_intentSource == null)
            {
                Debug.LogError("[PlayerDropThrow] 같은 오브젝트에 IIntentSource 구현이 필요합니다.", this);
            }
        }

        private void Update()
        {
            if (_intentSource == null) return;

            var intent = _intentSource.Current;
            var held = intent.DropHeld;

            // 뗐는지는 직접 비교해서 판단한다.
            // 인텐트의 '뗌' 플래그는 다른 컴포넌트가 먼저 소비할 수 있어서,
            // 컴포넌트 실행 순서에 따라 입력을 놓칠 수 있기 때문이다.
            var released = (!held && _wasHeld) || intent.DropReleased;
            _wasHeld = held;

            if (!held) _cancelledUntilRelease = false;

            if (_hands.IsEmpty)
            {
                _holdTime = 0f;
                _cancelledUntilRelease = false;
                return;
            }

            // 차징 도중 마음이 바뀌면 물건을 손에 든 채로 되돌린다.
            if (intent.CancelPressed && _holdTime > 0f)
            {
                _holdTime = 0f;
                _cancelledUntilRelease = held;
                return;
            }

            if (_cancelledUntilRelease) return;

            if (held)
            {
                _holdTime += Time.deltaTime;
                return;
            }

            if (!released || _holdTime <= 0f) return;

            if (_holdTime < _chargeDelay) DropAtFeet();
            else Throw(ChargeRatio);

            _holdTime = 0f;
        }

        /// <summary>발 앞에 살짝 내려놓는다.</summary>
        private void DropAtFeet()
        {
            var item = _hands.Release();
            if (item == null) return;

            var position = transform.position + transform.forward * _dropDistance;
            position.y = 0f;

            item.PlaceOnGround(position);
        }

        /// <summary>게이지만큼 세게 던진다.</summary>
        private void Throw(float power)
        {
            var item = _hands.Release();
            if (item == null) return;

            var origin = _hands.HoldAnchor.position;

            // 정면에서 위로 각도를 준 방향.
            var direction = Quaternion.AngleAxis(-_throwAngle, transform.right) * transform.forward;
            var speed = Mathf.Lerp(_minThrowSpeed, _maxThrowSpeed, Mathf.Clamp01(power));

            ThrownItem.Launch(item, origin, direction * speed);
        }
    }
}
