using System.Collections.Generic;
using HellsKitchen.Core.Orders;
using HellsKitchen.Core.Recipes;
using HellsKitchen.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 주문 하나를 나타내는 티켓.
    ///
    /// 재료 아이콘을 항상 보여 주는 이유는, 난이도가 올라가면
    /// "치즈 추가" 같은 변형이 붙어 요리 이름만으로는 판단할 수 없게 되기 때문이다.
    /// </summary>
    public class OrderTicketView : MonoBehaviour
    {
        [Header("구성")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _modifierText;
        [SerializeField] private Image _timerFill;
        [SerializeField] private Image _background;

        [Tooltip("재료 아이콘이 들어갈 부모")]
        [SerializeField] private Transform _iconRow;

        [Tooltip("아이콘 한 칸의 프리팹")]
        [SerializeField] private TicketIconView _iconPrefab;

        [Header("배경")]
        [Tooltip("평소 티켓 배경")]
        [SerializeField] private Sprite _normalSprite;

        [Tooltip("시간이 얼마 남지 않았을 때의 배경")]
        [SerializeField] private Sprite _urgentSprite;

        [Header("색")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _urgentColor = Color.white;
        [SerializeField] private Color _timerNormal = new Color(0.42f, 0.72f, 0.38f);
        [SerializeField] private Color _timerUrgent = new Color(0.88f, 0.27f, 0.23f);

        [Tooltip("남은 비율이 이 값 아래로 내려가면 급한 상태로 표시한다")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _urgentThreshold = 0.3f;

        private readonly List<TicketIconView> _spawnedIcons = new List<TicketIconView>();

        public Order Order { get; private set; }

        public void Bind(Order order, IngredientCatalog catalog)
        {
            Order = order;
            if (order == null) return;

            if (_titleText != null) _titleText.text = order.Recipe.DisplayName;

            if (_modifierText != null)
            {
                if (order.HasModifiers)
                {
                    _modifierText.gameObject.SetActive(true);
                    _modifierText.text = BuildModifierLabel(order);
                }
                else
                {
                    _modifierText.gameObject.SetActive(false);
                }
            }

            BuildIcons(order, catalog);
            Refresh();
        }

        private static string BuildModifierLabel(Order order)
        {
            if (order.Modifiers.Count == 1) return order.Modifiers[0].ToString();

            var parts = new string[order.Modifiers.Count];
            for (int i = 0; i < order.Modifiers.Count; i++) parts[i] = order.Modifiers[i].ToString();
            return string.Join(", ", parts);
        }

        private void BuildIcons(Order order, IngredientCatalog catalog)
        {
            ClearIcons();
            if (_iconRow == null || _iconPrefab == null || catalog == null) return;

            foreach (var entry in order.Requirement)
            {
                var definition = catalog.Get(entry.Key.Type);
                if (definition == null || definition.Icon == null) continue;

                for (int i = 0; i < entry.Value; i++)
                {
                    var icon = Instantiate(_iconPrefab, _iconRow);
                    icon.SetIcon(definition.Icon);
                    icon.gameObject.SetActive(true);
                    _spawnedIcons.Add(icon);
                }
            }
        }

        private void ClearIcons()
        {
            for (int i = 0; i < _spawnedIcons.Count; i++)
            {
                if (_spawnedIcons[i] != null) Destroy(_spawnedIcons[i].gameObject);
            }
            _spawnedIcons.Clear();
        }

        /// <summary>남은 시간 표시를 갱신한다. HUD가 매 프레임 호출한다.</summary>
        public void Refresh()
        {
            if (Order == null) return;

            var ratio = Order.RemainingRatio;
            var urgent = ratio <= _urgentThreshold;

            if (_timerFill != null)
            {
                _timerFill.fillAmount = ratio;
                _timerFill.color = urgent ? _timerUrgent : _timerNormal;
            }

            if (_background != null)
            {
                var sprite = urgent ? _urgentSprite : _normalSprite;
                if (sprite != null) _background.sprite = sprite;

                _background.color = urgent ? _urgentColor : _normalColor;
            }
        }

        private void OnDestroy() => ClearIcons();

#if UNITY_EDITOR
        public void EditorConfigure(TMP_Text title, TMP_Text modifier, Image timerFill, Image background, Transform iconRow, TicketIconView iconPrefab)
        {
            _titleText = title;
            _modifierText = modifier;
            _timerFill = timerFill;
            _background = background;
            _iconRow = iconRow;
            _iconPrefab = iconPrefab;
        }

        public void EditorSetBackgrounds(Sprite normal, Sprite urgent)
        {
            _normalSprite = normal;
            _urgentSprite = urgent;
        }
#endif
    }
}
