using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 설비 위에 떠 있는 이름표. 재료 상자가 무엇을 담고 있는지 알려 준다.
    ///
    /// 로우폴리 모델은 위에서 내려다보면 양파인지 감자인지 구분이 어렵다.
    /// 아이콘과 이름을 같이 띄우면 주방을 한 번 훑는 것만으로 위치를 외울 수 있다.
    /// </summary>
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _icon;

        [Tooltip("켜면 항상 카메라를 향한다")]
        [SerializeField] private bool _faceCamera = true;

        private Transform _cameraTransform;

        private void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (!_faceCamera || _cameraTransform == null) return;
            transform.rotation = _cameraTransform.rotation;
        }

        public void SetText(string text)
        {
            if (_label != null) _label.text = text;
        }

        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;

            _icon.sprite = sprite;
            _icon.gameObject.SetActive(sprite != null);
        }

#if UNITY_EDITOR
        public void EditorConfigure(string text, Sprite icon)
        {
            SetText(text);
            SetIcon(icon);
        }

        public void EditorSetParts(TMP_Text label, Image icon)
        {
            _label = label;
            _icon = icon;
        }
#endif
    }
}
