using UnityEngine;
using UnityEngine.InputSystem;

namespace HellsKitchen.Runtime.Inputs
{
    /// <summary>
    /// 이 기기의 실제 입력(키보드, 게임패드, 화면 위 가상 컨트롤)을 인텐트로 번역한다.
    ///
    /// 화면 위 조이스틱과 버튼은 Input System 의 On-Screen Control 로 가상 게임패드를
    /// 흉내 내기 때문에, 여기서 터치를 따로 다룰 필요가 없다.
    /// 즉 모바일 대응이 이 클래스 바깥에서 끝난다.
    /// </summary>
    public class LocalIntentSource : MonoBehaviour, IIntentSource
    {
        [Header("입력 에셋")]
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _actionMapName = "Player";

        [Header("액션 이름")]
        [SerializeField] private string _moveActionName = "Move";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private string _workActionName = "Work";
        [SerializeField] private string _sprintActionName = "Sprint";
        [SerializeField] private string _dropActionName = "Drop";
        [SerializeField] private string _cancelActionName = "Cancel";

        private InputAction _move;
        private InputAction _interact;
        private InputAction _work;
        private InputAction _sprint;
        private InputAction _drop;
        private InputAction _cancel;

        private PlayerIntent _intent;

        public PlayerIntent Current => _intent;

        private void Awake()
        {
            if (_actions == null)
            {
                Debug.LogError("[LocalIntentSource] 입력 에셋이 지정되지 않았습니다.", this);
                enabled = false;
                return;
            }

            var map = _actions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogError($"[LocalIntentSource] '{_actionMapName}' 액션 맵을 찾을 수 없습니다.", this);
                enabled = false;
                return;
            }

            _move = map.FindAction(_moveActionName, throwIfNotFound: false);
            _interact = map.FindAction(_interactActionName, throwIfNotFound: false);
            _work = map.FindAction(_workActionName, throwIfNotFound: false);
            _sprint = map.FindAction(_sprintActionName, throwIfNotFound: false);
            _drop = map.FindAction(_dropActionName, throwIfNotFound: false);
            _cancel = map.FindAction(_cancelActionName, throwIfNotFound: false);

            map.Enable();
        }

        private void OnEnable()
        {
            _actions?.Enable();
        }

        private void OnDisable()
        {
            _intent = PlayerIntent.None;
        }

        private void Update()
        {
            var move = _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

            // 아날로그 스틱이 1을 살짝 넘길 수 있으므로 잘라 준다.
            if (move.sqrMagnitude > 1f) move.Normalize();
            _intent.Move = move;

            // 이미 눌림이 쌓여 있으면 유지한다. 캐릭터가 읽고 ConsumePresses 로 지운다.
            if (_interact != null && _interact.WasPressedThisFrame()) _intent.InteractPressed = true;
            if (_work != null && _work.WasPressedThisFrame()) _intent.WorkPressed = true;

            if (_drop != null && _drop.WasReleasedThisFrame()) _intent.DropReleased = true;
            if (_cancel != null && _cancel.WasPressedThisFrame()) _intent.CancelPressed = true;

            _intent.WorkHeld = _work != null && _work.IsPressed();
            _intent.SprintHeld = _sprint != null && _sprint.IsPressed();
            _intent.DropHeld = _drop != null && _drop.IsPressed();
        }

        public void ConsumePresses() => _intent.ConsumePresses();

#if UNITY_EDITOR
        public void EditorConfigure(InputActionAsset actions) => _actions = actions;
#endif
    }
}
