using HellsKitchen.Runtime.Inputs;
using UnityEngine;

namespace HellsKitchen.Runtime.Player
{
    /// <summary>
    /// 캐릭터의 이동과 회전. 인텐트만 읽고 입력 장치는 모른다.
    ///
    /// 속도를 목표값으로 곧장 세팅하지 않고 가속도로 접근시킨다.
    /// 그래야 출발이 굼뜨고 멈출 때 미끄러지는 무게감이 생기고,
    /// "달리다가 조리대 앞에서 못 서는" 실수가 게임의 일부가 된다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("속도")]
        [Tooltip("평상시 걷는 속도")]
        [SerializeField] private float _walkSpeed = 4.2f;

        [Tooltip("달리기 버튼을 눌렀을 때의 최고 속도")]
        [SerializeField] private float _runSpeed = 8.5f;

        [Header("가속과 관성")]
        [Tooltip("초당 속도 증가량. 클수록 즉각적으로 튀어나간다.")]
        [SerializeField] private float _acceleration = 16f;

        [Tooltip("걷다가 입력을 놓았을 때의 초당 감속량")]
        [SerializeField] private float _deceleration = 22f;

        [Tooltip("달리다가 입력을 놓았을 때의 초당 감속량. 작을수록 많이 미끄러진다.")]
        [SerializeField] private float _runDeceleration = 7f;

        [Header("조향")]
        [Tooltip("걸을 때 이동 방향을 바꾸는 속도(초당 각도)")]
        [SerializeField] private float _steerSpeedWalk = 900f;

        [Tooltip("달릴 때 이동 방향을 바꾸는 속도. 낮을수록 크게 돌아야 한다.")]
        [SerializeField] private float _steerSpeedRun = 300f;

        [Tooltip("몸이 이동 방향을 따라 도는 속도. 조향보다 빠르게 두면 진행 방향을 먼저 바라본다.")]
        [SerializeField] private float _bodyTurnSpeed = 900f;

        [Header("중력")]
        [SerializeField] private float _gravity = -20f;

        [Header("카메라 기준")]
        [Tooltip("켜면 화면 기준으로 이동한다. 쿼터뷰에서는 켜 두는 것이 자연스럽다.")]
        [SerializeField] private bool _cameraRelative = true;

        private CharacterController _controller;
        private IIntentSource _intentSource;
        private Transform _cameraTransform;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        /// <summary>이번 프레임의 인텐트.</summary>
        public PlayerIntent Intent { get; private set; }

        /// <summary>지금 달리는 중인가. 달리기 버튼을 누른 채 실제로 움직이고 있어야 true.</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>현재 속력(m/s).</summary>
        public float CurrentSpeed => _horizontalVelocity.magnitude;

        /// <summary>
        /// 애니메이션 블렌드용 0~1 값.
        /// 걷기 속도에서 0.55 부근, 달리기 최고 속도에서 1이 되도록 맞춘다.
        /// </summary>
        public float NormalizedSpeed => _runSpeed <= 0f ? 0f : Mathf.Clamp01(CurrentSpeed / _runSpeed);

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _intentSource = GetComponent<IIntentSource>();

            if (_intentSource == null)
            {
                Debug.LogError("[PlayerCharacter] 같은 오브젝트에 IIntentSource 구현이 필요합니다.", this);
            }
        }

        private void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            Intent = _intentSource != null ? _intentSource.Current : PlayerIntent.None;
            Move(Time.deltaTime);
        }

        private void Move(float dt)
        {
            var desired = ResolveDirection(Intent.Move);
            var hasInput = desired.sqrMagnitude > 0.0001f;

            var wantsSprint = Intent.SprintHeld && hasInput;
            var targetSpeed = wantsSprint ? _runSpeed : _walkSpeed;

            // 달리는 중이었는지는 '지금 속도'로 판단한다.
            // 버튼을 뗀 직후에도 관성이 남아 있어야 하기 때문이다.
            var movingFast = CurrentSpeed > _walkSpeed * 1.05f;
            IsSprinting = wantsSprint || movingFast;

            // 속도를 '방향'과 '속력'으로 나눠서 다룬다.
            //
            // 속도 벡터를 목표 벡터로 곧장 보간하면, 방향을 꺾을 때 벡터의 크기가
            // 일단 줄었다가 다시 커진다. 즉 코너마다 멈췄다 출발하는 느낌이 난다.
            // 속력은 유지한 채 방향만 돌리면 달리면서 자연스럽게 호를 그린다.
            var speed = _horizontalVelocity.magnitude;
            var direction = speed > 0.01f ? _horizontalVelocity / speed : transform.forward;

            if (hasInput)
            {
                var steerSpeed = Mathf.Lerp(
                    _steerSpeedWalk, _steerSpeedRun,
                    _runSpeed > _walkSpeed ? Mathf.InverseLerp(_walkSpeed, _runSpeed, speed) : 0f);

                direction = Vector3.RotateTowards(
                    direction, desired, steerSpeed * Mathf.Deg2Rad * dt, 0f).normalized;

                speed = Mathf.MoveTowards(speed, targetSpeed, _acceleration * dt);
            }
            else
            {
                // 빠르게 달리다 놓으면 천천히, 걷다 놓으면 빠르게 선다.
                var rate = movingFast ? _runDeceleration : _deceleration;
                speed = Mathf.MoveTowards(speed, 0f, rate * dt);
            }

            _horizontalVelocity = direction * speed;

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += _gravity * dt;

            var motion = _horizontalVelocity;
            motion.y = _verticalVelocity;
            _controller.Move(motion * dt);

            ApplyRotation(dt);
        }

        private Vector3 ResolveDirection(Vector2 input)
        {
            var desired = new Vector3(input.x, 0f, input.y);

            if (_cameraRelative && _cameraTransform != null)
            {
                // 카메라의 좌우/앞뒤만 뽑아 쓴다. 기울기는 무시한다.
                var forward = _cameraTransform.forward;
                forward.y = 0f;
                forward.Normalize();

                var right = _cameraTransform.right;
                right.y = 0f;
                right.Normalize();

                desired = right * input.x + forward * input.y;
            }

            if (desired.sqrMagnitude > 1f) desired.Normalize();
            return desired;
        }

        private void ApplyRotation(float dt)
        {
            // 이동 중일 때만 몸을 튼다. 멈췄을 때 제자리에서 도는 것을 막는다.
            if (_horizontalVelocity.sqrMagnitude <= 0.01f) return;

            // 몸은 실제 진행 방향을 따라간다. 진행 방향 자체가 이미 부드럽게
            // 꺾이고 있으므로, 여기서는 그것을 빠르게 뒤따르기만 하면 된다.
            var look = Quaternion.LookRotation(new Vector3(_horizontalVelocity.x, 0f, _horizontalVelocity.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, _bodyTurnSpeed * dt);
        }
    }
}
