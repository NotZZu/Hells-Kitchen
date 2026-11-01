using HellsKitchen.Runtime.Stations;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 설비 위에 떠 있는 진행바. 썰기 진행도와 타기까지 남은 시간을 같은 자리에 보여 준다.
    ///
    /// 색으로 의미를 구분한다. 초록은 진행 중, 빨강은 곧 탄다.
    /// 플레이어가 주방을 한눈에 훑을 때 읽어야 하는 정보는 이 두 가지뿐이다.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class StationProgressView : MonoBehaviour
    {
        [SerializeField] private Station _station;
        [SerializeField] private Image _fill;
        [SerializeField] private CanvasGroup _group;

        [SerializeField] private Color _normalColor = new Color(0.45f, 0.8f, 0.4f);
        [SerializeField] private Color _criticalColor = new Color(0.9f, 0.3f, 0.25f);

        [Tooltip("켜면 항상 카메라를 향한다")]
        [SerializeField] private bool _faceCamera = true;

        private Transform _cameraTransform;

        private void Awake()
        {
            if (_station == null) _station = GetComponentInParent<Station>();
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_station == null) return;

            var visible = _station.ShowsProgress;

            if (_group != null) _group.alpha = visible ? 1f : 0f;
            else if (_fill != null) _fill.enabled = visible;

            if (!visible) return;

            if (_fill != null)
            {
                _fill.fillAmount = Mathf.Clamp01(_station.Progress01);
                _fill.color = _station.IsProgressCritical ? _criticalColor : _normalColor;
            }

            if (_faceCamera && _cameraTransform != null)
            {
                transform.rotation = _cameraTransform.rotation;
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(Station station, Image fill, CanvasGroup group)
        {
            _station = station;
            _fill = fill;
            _group = group;
        }
#endif
    }
}
