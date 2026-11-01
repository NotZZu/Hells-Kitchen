using UnityEngine;

namespace HellsKitchen.Runtime.Player
{
    /// <summary>
    /// 캐릭터의 상태를 애니메이터 파라미터로 옮긴다.
    ///
    /// 이동, 손, 상호작용이 각자 애니메이터를 건드리면 누가 무엇을 덮어썼는지
    /// 추적이 어려워진다. 애니메이터에 값을 쓰는 곳은 이 클래스 하나로 유지한다.
    /// </summary>
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerCharacter _character;
        [SerializeField] private PlayerHands _hands;
        [SerializeField] private PlayerInteraction _interaction;

        [Header("파라미터 이름")]
        [SerializeField] private string _speedParameter = "Speed";
        [SerializeField] private string _carryingParameter = "Carrying";
        [SerializeField] private string _workingParameter = "Working";

        [Tooltip("속도 변화가 애니메이션에 반영되는 부드러움. 작을수록 즉각적이다.")]
        [SerializeField] private float _speedDamping = 0.1f;

        private int _speedHash;
        private int _carryingHash;
        private int _workingHash;
        private bool _ready;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_character == null) _character = GetComponent<PlayerCharacter>();
            if (_hands == null) _hands = GetComponent<PlayerHands>();
            if (_interaction == null) _interaction = GetComponent<PlayerInteraction>();

            _speedHash = Animator.StringToHash(_speedParameter);
            _carryingHash = Animator.StringToHash(_carryingParameter);
            _workingHash = Animator.StringToHash(_workingParameter);

            _ready = _animator != null && _animator.runtimeAnimatorController != null;
            if (_ready) return;

            if (_animator == null)
            {
                Debug.LogError(
                    "[PlayerAnimatorDriver] 캐릭터에서 Animator 를 찾지 못했습니다. " +
                    "'2. 프리팹 생성' 부터 다시 실행해 주세요.", this);
                return;
            }

            Debug.LogWarning(
                $"[PlayerAnimatorDriver] '{_animator.gameObject.name}' 의 Animator 에 컨트롤러가 비어 있습니다.\n" +
                $"아바타: {(_animator.avatar != null ? _animator.avatar.name : "없음")}\n" +
                "메뉴에서 'Hell's Kitchen → 5. 캐릭터 애니메이션 설정' 을 실행하면 씬에 놓인 캐릭터까지 함께 갱신됩니다.",
                this);
        }

        private void Update()
        {
            if (!_ready) return;

            var speed = _character != null ? _character.NormalizedSpeed : 0f;
            _animator.SetFloat(_speedHash, speed, _speedDamping, Time.deltaTime);

            _animator.SetBool(_carryingHash, _hands != null && !_hands.IsEmpty);
            _animator.SetBool(_workingHash, _interaction != null && _interaction.IsWorking);
        }

#if UNITY_EDITOR
        public void EditorConfigure(Animator animator, PlayerCharacter character, PlayerHands hands, PlayerInteraction interaction)
        {
            _animator = animator;
            _character = character;
            _hands = hands;
            _interaction = interaction;
        }
#endif
    }
}
