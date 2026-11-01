using HellsKitchen.Runtime.Player;
using TMPro;
using UnityEngine;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 화면 하단에 "지금 무엇을 들고 있고, 무엇을 바라보고 있는지"를 한 줄로 보여 준다.
    /// 조작을 익히는 동안에만 필요한 보조 표시라, 옵션에서 끌 수 있게 만들어 두는 것이 좋다.
    /// </summary>
    public class HeldItemHint : MonoBehaviour
    {
        [SerializeField] private PlayerHands _hands;
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private TMP_Text _label;

        private void Awake()
        {
            if (_hands == null) _hands = FindFirstObjectByType<PlayerHands>();
            if (_interaction == null) _interaction = FindFirstObjectByType<PlayerInteraction>();
        }

        private void Update()
        {
            if (_label == null) return;

            var held = _hands != null && _hands.Held != null ? _hands.Held.DisplayName : "빈손";
            var focus = _interaction != null && _interaction.Focused != null ? _interaction.Focused.DisplayName : "-";

            _label.text = $"손: {held}   앞: {focus}";
        }

#if UNITY_EDITOR
        public void EditorConfigure(PlayerHands hands, PlayerInteraction interaction, TMP_Text label)
        {
            _hands = hands;
            _interaction = interaction;
            _label = label;
        }
#endif
    }
}
