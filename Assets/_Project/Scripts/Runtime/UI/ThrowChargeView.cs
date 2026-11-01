using HellsKitchen.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 던지기 세기 게이지.
    ///
    /// 게이지가 차기 시작해야 비로소 "던지기"라는 걸 플레이어가 알 수 있다.
    /// 그 전에는 아무것도 보이지 않아야 짧게 눌러 내려놓는 동작이 방해받지 않는다.
    /// </summary>
    public class ThrowChargeView : MonoBehaviour
    {
        [SerializeField] private PlayerDropThrow _source;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _fill;

        [SerializeField] private Color _lowColor = new Color(0.95f, 0.85f, 0.35f);
        [SerializeField] private Color _fullColor = new Color(0.95f, 0.42f, 0.25f);

        private void Awake()
        {
            if (_source == null) _source = FindFirstObjectByType<PlayerDropThrow>();
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (_source == null) return;

            var charging = _source.IsCharging;

            if (_group != null) _group.alpha = charging ? 1f : 0f;
            if (!charging || _fill == null) return;

            var ratio = _source.ChargeRatio;
            _fill.fillAmount = ratio;
            _fill.color = Color.Lerp(_lowColor, _fullColor, ratio);
        }

#if UNITY_EDITOR
        public void EditorConfigure(CanvasGroup group, Image fill)
        {
            _group = group;
            _fill = fill;
        }
#endif
    }
}
