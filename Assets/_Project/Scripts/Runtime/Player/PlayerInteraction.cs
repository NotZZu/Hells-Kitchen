using HellsKitchen.Runtime.Inputs;
using HellsKitchen.Runtime.Items;
using HellsKitchen.Runtime.Stations;
using UnityEngine;

namespace HellsKitchen.Runtime.Player
{
    /// <summary>
    /// 앞에 있는 설비를 골라내고, 버튼 입력을 그 설비에 전달한다.
    ///
    /// 대상 선택은 "가장 가까운 것"이 아니라 "가장 정면에 가까운 것"에 가중치를 둔다.
    /// 조리대가 다닥다닥 붙은 주방에서 이 차이가 조작 스트레스를 크게 좌우한다.
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    [RequireComponent(typeof(PlayerHands))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("감지 범위")]
        [Tooltip("캐릭터 앞쪽으로 얼마나 떨어진 지점을 중심으로 볼 것인가")]
        [SerializeField] private float _detectionOffset = 0.7f;

        [SerializeField] private float _detectionRadius = 1.4f;

        [Tooltip("이 거리 안의 바닥 물건은 정면이 아니어도 주울 수 있다")]
        [SerializeField] private float _looseGrabRange = 1.2f;

        [Tooltip("정면에서 이 각도 안에 있는 설비만 대상으로 삼는다")]
        [Range(15f, 180f)]
        [SerializeField] private float _maxAngle = 110f;

        [Tooltip("비워 두면 모든 레이어를 검사한 뒤 Station 컴포넌트로 거른다")]
        [SerializeField] private LayerMask _stationMask = ~0;

        private static readonly Collider[] Hits = new Collider[24];

        private PlayerHands _hands;
        private IIntentSource _intentSource;
        private Station _focused;
        private Carryable _focusedLooseItem;
        private Station _workingStation;

        /// <summary>지금 바라보고 있는 설비. UI 힌트에서 읽어 간다.</summary>
        public Station Focused => _focused;

        /// <summary>지금 설비에서 작업(썰기) 중인가. 애니메이션이 읽어 간다.</summary>
        public bool IsWorking => _workingStation != null;

        /// <summary>바라보고 있는 바닥의 물건. 없으면 null.</summary>
        public Carryable FocusedLooseItem => _focusedLooseItem;

        private void Awake()
        {
            _hands = GetComponent<PlayerHands>();

            // 인텐트를 캐릭터가 캐시한 값이 아니라 공급자에서 직접 읽는다.
            // 같은 오브젝트 안에서도 컴포넌트의 Update 순서는 보장되지 않기 때문에,
            // 캐시를 거치면 같은 버튼 입력이 두 프레임에 걸쳐 두 번 처리될 수 있다.
            _intentSource = GetComponent<IIntentSource>();
            if (_intentSource == null)
            {
                Debug.LogError("[PlayerInteraction] 같은 오브젝트에 IIntentSource 구현이 필요합니다.", this);
            }
        }

        private void Update()
        {
            UpdateFocus();

            if (_intentSource == null) return;

            var intent = _intentSource.Current;

            if (intent.InteractPressed) HandleInteract();

            HandleWork(intent.WorkHeld, Time.deltaTime);

            // 입력을 다 쓴 뒤에 눌림 플래그를 지운다.
            _intentSource.ConsumePresses();
        }

        private void UpdateFocus()
        {
            var origin = transform.position + transform.forward * _detectionOffset + Vector3.up * 0.4f;
            var count = Physics.OverlapSphereNonAlloc(origin, _detectionRadius, Hits, _stationMask, QueryTriggerInteraction.Collide);

            Station best = null;
            var bestScore = float.NegativeInfinity;

            Carryable bestItem = null;
            var bestItemScore = float.NegativeInfinity;

            var cosLimit = Mathf.Cos(_maxAngle * 0.5f * Mathf.Deg2Rad);

            for (int i = 0; i < count; i++)
            {
                var collider = Hits[i];
                if (collider == null) continue;

                // 바닥에 떨어진 물건도 후보로 본다. 던져 놓은 재료를 주우려면 필요하다.
                var loose = collider.GetComponentInParent<Carryable>();
                if (loose != null && loose.IsLoose)
                {
                    // 바닥 물건은 발밑에 있을 때 방향이 요동쳐서 각도 판정이 자꾸 어긋난다.
                    // 가까우면 각도를 따지지 않고 무조건 후보로 삼는다.
                    var score = ScoreTarget(loose.transform.position, cosLimit, _looseGrabRange);
                    if (score > bestItemScore)
                    {
                        bestItemScore = score;
                        bestItem = loose;
                    }
                    continue;
                }

                var station = collider.GetComponentInParent<Station>();
                if (station == null) continue;

                var stationScore = ScoreTarget(station.transform.position, cosLimit);
                if (stationScore > bestScore)
                {
                    bestScore = stationScore;
                    best = station;
                }
            }

            _focusedLooseItem = bestItem;

            if (best == _focused) return;

            if (_focused != null) _focused.SetHighlighted(false);
            _focused = best;
            if (_focused != null) _focused.SetHighlighted(true);
        }

        /// <summary>
        /// 정면에 가까울수록, 가까울수록 높은 점수. 범위 밖이면 -무한대.
        /// </summary>
        /// <param name="closeRange">이 거리 안이면 각도를 따지지 않는다. 0이면 항상 각도를 본다.</param>
        private float ScoreTarget(Vector3 targetPosition, float cosLimit, float closeRange = 0f)
        {
            var to = targetPosition - transform.position;
            to.y = 0f;

            var distance = to.magnitude;
            if (distance < 0.001f) return float.MaxValue;

            // 코앞에 있는 것은 어느 쪽을 보고 있든 집을 수 있어야 한다.
            if (closeRange > 0f && distance <= closeRange) return 50f - distance;

            var dot = Vector3.Dot(transform.forward, to / distance);
            if (dot < cosLimit) return float.NegativeInfinity;

            return dot * 2f - distance * 0.5f;
        }

        private void HandleInteract()
        {
            // 0) 빈손인데 발밑에 물건이 있으면 그것부터 줍는다.
            //    던져 놓거나 흘린 재료를 회수하는 것이 설비를 여는 것보다 급하다.
            if (_hands.IsEmpty && _focusedLooseItem != null)
            {
                _hands.Hold(_focusedLooseItem);
                _focusedLooseItem = null;
                return;
            }

            if (_focused == null) return;

            // 1) 빈손이면 집는다.
            if (_hands.IsEmpty)
            {
                var taken = _focused.TryTake();
                if (taken != null) _hands.Hold(taken);
                return;
            }

            // 2) 접시를 들고 있으면 먼저 '재료만 담기'를 시도한다.
            //    접시를 통째로 내려놓는 것보다 이쪽이 훨씬 자주 원하는 동작이다.
            if (_hands.Held is PlateItem plate && _focused.TryTransferTo(plate)) return;

            // 3) 들고 있는 것을 넘긴다.
            if (_focused.TryPlace(_hands.Held)) _hands.Clear();
        }

        private void HandleWork(bool held, float deltaSeconds)
        {
            // 손질이 끝난 설비는 더 이상 작업 대상이 아니다.
            // 버튼을 계속 누르고 있어도 칼질 애니메이션이 멈춰야 한다.
            var canWork = _focused != null && _focused.SupportsWork && _focused.HasWorkPending;
            var target = held && canWork ? _focused : null;

            if (_workingStation != null && _workingStation != target)
            {
                _workingStation.OnWorkReleased();
                _workingStation = null;
            }

            if (target == null) return;

            _workingStation = target;
            target.OnWorkHeld(deltaSeconds);
        }

        private void OnDisable()
        {
            if (_workingStation != null)
            {
                _workingStation.OnWorkReleased();
                _workingStation = null;
            }

            if (_focused != null)
            {
                _focused.SetHighlighted(false);
                _focused = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + transform.forward * _detectionOffset + Vector3.up * 0.4f;
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            Gizmos.DrawSphere(origin, _detectionRadius);
        }
    }
}
