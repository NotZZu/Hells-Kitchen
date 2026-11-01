using UnityEngine;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 화면 위 조이스틱과 버튼을 언제 보여줄지 결정한다.
    ///
    /// 터치가 되는 기기에서만 켜는 것이 기본이고,
    /// 에디터에서 모바일 조작을 시험해 볼 수 있도록 강제 표시 옵션을 둔다.
    /// </summary>
    public class MobileControlsVisibility : MonoBehaviour
    {
        public enum DisplayMode
        {
            /// <summary>터치 기기에서만 표시</summary>
            Auto = 0,
            AlwaysOn = 1,
            AlwaysOff = 2,
        }

        [SerializeField] private DisplayMode _mode = DisplayMode.Auto;

        [Tooltip("보이거나 숨길 대상. 보통 가상 조이스틱과 버튼이 담긴 패널.")]
        [SerializeField] private GameObject _controlsRoot;

        private void Start() => Apply();

        public void SetMode(DisplayMode mode)
        {
            _mode = mode;
            Apply();
        }

        private void Apply()
        {
            if (_controlsRoot == null) return;
            _controlsRoot.SetActive(ShouldShow());
        }

        private bool ShouldShow()
        {
            switch (_mode)
            {
                case DisplayMode.AlwaysOn: return true;
                case DisplayMode.AlwaysOff: return false;
                default:
                    // 터치스크린이 있고, 키보드가 없는 기기를 모바일로 본다.
                    // 터치 지원 노트북에서 조이스틱이 뜨는 것을 막기 위한 조건이다.
                    var hasTouch = UnityEngine.InputSystem.Touchscreen.current != null;
                    var hasKeyboard = UnityEngine.InputSystem.Keyboard.current != null;
                    return hasTouch && !hasKeyboard;
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(GameObject controlsRoot, DisplayMode mode)
        {
            _controlsRoot = controlsRoot;
            _mode = mode;
        }
#endif
    }
}
