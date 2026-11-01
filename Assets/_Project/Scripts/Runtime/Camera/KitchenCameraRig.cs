using UnityEngine;

namespace HellsKitchen.Runtime.CameraRig
{
    /// <summary>
    /// 주방을 내려다보는 고정 각도 카메라.
    ///
    /// 화면 비율에 따라 거리를 자동으로 조정한다. 같은 주방이라도
    /// 세로로 긴 휴대폰과 가로로 넓은 모니터에서 보이는 범위가 달라지면
    /// 난이도 자체가 달라지기 때문이다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class KitchenCameraRig : MonoBehaviour
    {
        [Header("추적 대상")]
        [Tooltip("비우면 씬에서 PlayerCharacter 를 찾아 붙는다")]
        [SerializeField] private Transform _target;

        [Tooltip("켜면 대상을 따라간다. 끄면 고정 카메라가 된다.")]
        [SerializeField] private bool _follow = true;

        [Header("배치")]
        [SerializeField] private float _pitchDegrees = 50f;
        [SerializeField] private float _yawDegrees = 45f;
        [SerializeField] private float _distance = 12f;
        [SerializeField] private Vector3 _lookOffset = new Vector3(0f, 0.8f, 0f);

        [Header("추적 감각")]
        [Tooltip("클수록 빠르게 따라붙는다")]
        [SerializeField] private float _followSharpness = 6f;

        [Tooltip("이 반경 안에서는 카메라가 움직이지 않는다. 잔떨림 방지.")]
        [SerializeField] private float _deadZoneRadius = 0.6f;

        [Header("화면 비율 보정")]
        [Tooltip("이 가로세로 비율을 기준으로 거리를 계산한다(16:9)")]
        [SerializeField] private float _referenceAspect = 16f / 9f;

        [SerializeField] private float _maxDistanceMultiplier = 1.6f;

        private Camera _camera;
        private Vector3 _smoothedFocus;
        private float _appliedAspect = -1f;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (_target == null)
            {
                var player = FindFirstObjectByType<Player.PlayerCharacter>();
                if (player != null) _target = player.transform;
            }

            _smoothedFocus = _target != null ? _target.position : transform.position;
            ApplyAspect();
            Reposition(instant: true);
        }

        private void LateUpdate()
        {
            ApplyAspect();
            Reposition(instant: false);
        }

        private void ApplyAspect()
        {
            if (_camera == null) return;

            var aspect = _camera.aspect;
            if (Mathf.Approximately(aspect, _appliedAspect)) return;
            _appliedAspect = aspect;
        }

        /// <summary>세로로 긴 화면일수록 뒤로 물러나 같은 범위를 보이게 한다.</summary>
        private float GetEffectiveDistance()
        {
            if (_appliedAspect <= 0f) return _distance;

            var ratio = _referenceAspect / _appliedAspect;
            var multiplier = Mathf.Clamp(ratio, 1f, _maxDistanceMultiplier);
            return _distance * multiplier;
        }

        private void Reposition(bool instant)
        {
            var focus = _target != null && _follow ? _target.position : _smoothedFocus;

            if (!instant && _target != null && _follow)
            {
                var flatDelta = focus - _smoothedFocus;
                flatDelta.y = 0f;

                if (flatDelta.magnitude > _deadZoneRadius)
                {
                    // 데드존 경계까지만 따라간다.
                    var pull = flatDelta.normalized * (flatDelta.magnitude - _deadZoneRadius);
                    var desired = _smoothedFocus + pull;
                    desired.y = focus.y;

                    var t = 1f - Mathf.Exp(-_followSharpness * Time.deltaTime);
                    _smoothedFocus = Vector3.Lerp(_smoothedFocus, desired, t);
                }
            }
            else
            {
                _smoothedFocus = focus;
            }

            var rotation = Quaternion.Euler(_pitchDegrees, _yawDegrees, 0f);
            var offset = rotation * Vector3.back * GetEffectiveDistance();

            transform.position = _smoothedFocus + _lookOffset + offset;
            transform.rotation = rotation;
        }

#if UNITY_EDITOR
        public void EditorConfigure(Transform target, float distance, float pitch, float yaw, bool follow = true)
        {
            _target = target;
            _distance = distance;
            _pitchDegrees = pitch;
            _yawDegrees = yaw;
            _follow = follow;
        }
#endif
    }
}
