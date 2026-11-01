using HellsKitchen.Core.Orders;
using HellsKitchen.Runtime.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 라운드가 끝났을 때 뜨는 결과 화면.
    /// 별 개수와 함께 놓친 주문 수를 같이 보여 준다 — 다음 판에 무엇을 고쳐야 하는지가
    /// 총 골드보다 이쪽에서 더 잘 드러나기 때문이다.
    /// </summary>
    public class RoundResultView : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private GameSession _session;
        [SerializeField] private GameObject _panel;

        [Header("표시")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private Image[] _starImages;

        [SerializeField] private Color _starOnColor = Color.white;
        [SerializeField] private Color _starOffColor = new Color(1f, 1f, 1f, 0.55f);

        [Header("별 스프라이트")]
        [SerializeField] private Sprite _starOnSprite;
        [SerializeField] private Sprite _starOffSprite;

        [Header("버튼")]
        [SerializeField] private Button _retryButton;

        private void Awake()
        {
            if (_session == null) _session = FindFirstObjectByType<GameSession>();
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (_session != null) _session.RoundFinished += Show;
            if (_retryButton != null) _retryButton.onClick.AddListener(Retry);
        }

        private void OnDisable()
        {
            if (_session != null) _session.RoundFinished -= Show;
            if (_retryButton != null) _retryButton.onClick.RemoveListener(Retry);
        }

        private void Show(RoundSummary summary)
        {
            if (_panel != null) _panel.SetActive(true);

            if (_titleText != null)
            {
                _titleText.text = summary.Stars > 0 ? "영업 종료!" : "손님을 너무 많이 놓쳤습니다";
            }

            if (_goldText != null) _goldText.text = $"{summary.Gold} G";

            if (_detailText != null)
            {
                _detailText.text =
                    $"완료 {summary.Served}\n" +
                    $"놓침 {summary.Expired}\n" +
                    $"오배송 {summary.Mismatched}\n" +
                    $"최고 콤보 {summary.BestCombo}";
            }

            if (_starImages != null)
            {
                for (int i = 0; i < _starImages.Length; i++)
                {
                    if (_starImages[i] == null) continue;

                    var earned = i < summary.Stars;
                    var sprite = earned ? _starOnSprite : _starOffSprite;
                    if (sprite != null) _starImages[i].sprite = sprite;

                    _starImages[i].color = earned ? _starOnColor : _starOffColor;
                }
            }
        }

        public void Retry()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_session != null) _session.StartRound();
        }

#if UNITY_EDITOR
        public void EditorSetStarSprites(Sprite on, Sprite off)
        {
            _starOnSprite = on;
            _starOffSprite = off;
        }

        public void EditorConfigure(GameSession session, GameObject panel, TMP_Text title, TMP_Text gold, TMP_Text detail, Image[] stars, Button retry)
        {
            _session = session;
            _panel = panel;
            _titleText = title;
            _goldText = gold;
            _detailText = detail;
            _starImages = stars;
            _retryButton = retry;
        }
#endif
    }
}
