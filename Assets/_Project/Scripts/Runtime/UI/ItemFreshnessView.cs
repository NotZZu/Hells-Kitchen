using HellsKitchen.Runtime.Items;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 바닥에 떨어진 재료 위에 뜨는 신선도 게이지.
    ///
    /// 설비의 손질 진행바와는 완전히 별개다.
    /// 손질 진행바는 "얼마나 했는가"를, 이 게이지는 "얼마나 남았는가"를 보여 준다.
    /// 둘이 같은 자리에 뜨면 플레이어가 반대로 읽기 때문에, 재료 자신에게 붙여 둔다.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class ItemFreshnessView : MonoBehaviour
    {
        [SerializeField] private IngredientItem _item;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _fill;

        [SerializeField] private Color _freshColor = new Color(0.45f, 0.8f, 0.4f);
        [SerializeField] private Color _spoiledColor = new Color(0.85f, 0.25f, 0.2f);

        private Transform _cameraTransform;

        private void Awake()
        {
            if (_item == null) _item = GetComponentInParent<IngredientItem>();
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_item == null) return;

            var visible = _item.IsSpoiling;
            if (_group != null) _group.alpha = visible ? 1f : 0f;
            if (!visible) return;

            if (_fill != null)
            {
                var ratio = _item.GroundLifeRatio;
                _fill.fillAmount = ratio;
                _fill.color = Color.Lerp(_spoiledColor, _freshColor, ratio);
            }

            if (_cameraTransform != null) transform.rotation = _cameraTransform.rotation;
        }

#if UNITY_EDITOR
        public void EditorConfigure(IngredientItem item, CanvasGroup group, Image fill)
        {
            _item = item;
            _group = group;
            _fill = fill;
        }
#endif
    }
}
