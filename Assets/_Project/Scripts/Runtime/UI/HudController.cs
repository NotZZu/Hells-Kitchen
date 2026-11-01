using System.Collections.Generic;
using HellsKitchen.Core.Orders;
using HellsKitchen.Data;
using HellsKitchen.Runtime.Session;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.Runtime.UI
{
    /// <summary>
    /// 라운드 중 화면 정보를 담당한다.
    /// 게임 규칙은 전혀 모르고, GameSession 이 알려 주는 것만 그린다.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private GameSession _session;
        [SerializeField] private IngredientCatalog _catalog;

        [Header("주문 티켓")]
        [SerializeField] private Transform _ticketRow;
        [SerializeField] private OrderTicketView _ticketPrefab;

        [Header("상단 정보")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private Image _roundProgressFill;

        [Header("피드백")]
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private float _feedbackDuration = 1.2f;

        [SerializeField] private Color _positiveColor = new Color(0.35f, 0.8f, 0.4f);
        [SerializeField] private Color _negativeColor = new Color(0.9f, 0.35f, 0.3f);

        private readonly Dictionary<int, OrderTicketView> _tickets = new Dictionary<int, OrderTicketView>();
        private OrderBoard _boundBoard;
        private float _feedbackTimer;

        private void Awake()
        {
            if (_session == null) _session = FindFirstObjectByType<GameSession>();
        }

        private void OnEnable()
        {
            if (_session == null) return;

            _session.RoundStarted += HandleRoundStarted;
            _session.PlateServed += HandlePlateServed;
            _session.OrderExpired += HandleOrderExpired;

            // 이미 라운드가 돌고 있으면 바로 붙는다(HUD가 나중에 켜진 경우).
            if (_session.Simulation != null) BindBoard(_session.Simulation.Board);
        }

        private void OnDisable()
        {
            if (_session != null)
            {
                _session.RoundStarted -= HandleRoundStarted;
                _session.PlateServed -= HandlePlateServed;
                _session.OrderExpired -= HandleOrderExpired;
            }
            UnbindBoard();
        }

        private void HandleRoundStarted()
        {
            ClearTickets();
            if (_session.Simulation != null) BindBoard(_session.Simulation.Board);
        }

        private void BindBoard(OrderBoard board)
        {
            if (_boundBoard == board) return;

            UnbindBoard();
            _boundBoard = board;
            if (_boundBoard == null) return;

            _boundBoard.OrderAdded += HandleOrderAdded;
            _boundBoard.OrderRemoved += HandleOrderRemoved;

            // 이미 떠 있는 주문을 따라잡는다.
            for (int i = 0; i < _boundBoard.ActiveOrders.Count; i++) HandleOrderAdded(_boundBoard.ActiveOrders[i]);
        }

        private void UnbindBoard()
        {
            if (_boundBoard == null) return;

            _boundBoard.OrderAdded -= HandleOrderAdded;
            _boundBoard.OrderRemoved -= HandleOrderRemoved;
            _boundBoard = null;
        }

        private void HandleOrderAdded(Order order)
        {
            if (order == null || _ticketPrefab == null || _ticketRow == null) return;
            if (_tickets.ContainsKey(order.Id)) return;

            var ticket = Instantiate(_ticketPrefab, _ticketRow);
            ticket.Bind(order, _catalog);
            _tickets[order.Id] = ticket;
        }

        private void HandleOrderRemoved(Order order, OrderRemovalReason reason)
        {
            if (order == null) return;
            if (!_tickets.TryGetValue(order.Id, out var ticket)) return;

            _tickets.Remove(order.Id);
            if (ticket != null) Destroy(ticket.gameObject);
        }

        private void ClearTickets()
        {
            foreach (var pair in _tickets)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            }
            _tickets.Clear();
        }

        private void Update()
        {
            var simulation = _session != null ? _session.Simulation : null;
            if (simulation == null) return;

            foreach (var pair in _tickets)
            {
                if (pair.Value != null) pair.Value.Refresh();
            }

            if (_timerText != null)
            {
                var remaining = Mathf.CeilToInt(simulation.RemainingSeconds);
                _timerText.text = $"{remaining / 60:0}:{remaining % 60:00}";
            }

            if (_roundProgressFill != null)
            {
                _roundProgressFill.fillAmount = 1f - simulation.Progress01;
            }

            if (_goldText != null) _goldText.text = $"{simulation.Score.Gold} G";

            if (_comboText != null)
            {
                var combo = simulation.Score.Combo;
                _comboText.gameObject.SetActive(combo >= 2);
                if (combo >= 2) _comboText.text = $"x{simulation.Score.CurrentComboMultiplier:0.0} 콤보 {combo}";
            }

            UpdateFeedback();
        }

        private void HandlePlateServed(ServeResult result)
        {
            switch (result.Outcome)
            {
                case ServeOutcome.Success:
                    ShowFeedback($"+{result.GoldDelta} G", _positiveColor);
                    break;
                case ServeOutcome.Mismatch:
                    ShowFeedback($"주문에 없는 요리! {result.GoldDelta} G", _negativeColor);
                    break;
                case ServeOutcome.EmptyPlate:
                    ShowFeedback("빈 접시입니다", _negativeColor);
                    break;
            }
        }

        private void HandleOrderExpired(Order order)
        {
            ShowFeedback($"{order.Recipe.DisplayName} 주문을 놓쳤습니다", _negativeColor);
        }

        private void ShowFeedback(string message, Color color)
        {
            if (_feedbackText == null) return;

            _feedbackText.text = message;
            _feedbackText.color = color;
            _feedbackText.gameObject.SetActive(true);
            _feedbackTimer = _feedbackDuration;
        }

        private void UpdateFeedback()
        {
            if (_feedbackText == null || _feedbackTimer <= 0f) return;

            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f) _feedbackText.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            GameSession session, IngredientCatalog catalog, Transform ticketRow, OrderTicketView ticketPrefab,
            TMP_Text timerText, TMP_Text goldText, TMP_Text comboText, Image roundFill, TMP_Text feedbackText)
        {
            _session = session;
            _catalog = catalog;
            _ticketRow = ticketRow;
            _ticketPrefab = ticketPrefab;
            _timerText = timerText;
            _goldText = goldText;
            _comboText = comboText;
            _roundProgressFill = roundFill;
            _feedbackText = feedbackText;
        }
#endif
    }
}
