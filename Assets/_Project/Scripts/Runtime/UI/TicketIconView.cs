using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 주문 티켓의 재료 아이콘 한 칸.
    /// 칸 배경과 재료 그림이 분리돼 있어서, 바꿔 끼울 대상이 무엇인지
    /// 밖에서 헷갈리지 않도록 이 컴포넌트를 통해서만 접근하게 한다.
    /// </summary>
    public class TicketIconView : MonoBehaviour
    {
        [SerializeField] private Image _art;

        public void SetIcon(Sprite sprite)
        {
            if (_art == null) return;

            _art.sprite = sprite;
            _art.enabled = sprite != null;
        }

#if UNITY_EDITOR
        public void EditorConfigure(Image art) => _art = art;
#endif
    }
}
