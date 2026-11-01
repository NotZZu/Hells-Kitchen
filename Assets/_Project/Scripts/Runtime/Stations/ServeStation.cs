using HellsKitchen.Core.Orders;
using HellsKitchen.Runtime.Items;
using HellsKitchen.Runtime.Session;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 주문 창구. 완성한 접시를 여기에 내면 판정이 이루어진다.
    ///
    /// 판정 자체는 하지 않는다. 접시를 GameSession 에 넘기고 결과를 연출로만 옮긴다.
    /// </summary>
    public class ServeStation : Station
    {
        [Header("연결")]
        [SerializeField] private GameSession _session;
        [SerializeField] private PlateDispenser _plateReturn;

        [Header("연출")]
        [SerializeField] private GameObject _successEffect;
        [SerializeField] private GameObject _failEffect;

        public override string DisplayName => "주문 창구";

        protected override void Awake()
        {
            base.Awake();
            if (_session == null) _session = FindFirstObjectByType<GameSession>();
        }

        /// <summary>내용물이 담긴 접시라면 창구가 최우선 목적지다.</summary>
        public override float GetAttraction(Carryable item)
        {
            if (!(item is PlateItem plate)) return 0f;
            return plate.IsEmpty ? 0f : 3.4f;
        }

        public override bool TryPlace(Carryable held)
        {
            if (!(held is PlateItem plate)) return false;
            if (plate.IsEmpty) return false;

            if (_session == null)
            {
                Debug.LogError("[ServeStation] 씬에 GameSession 이 없습니다.", this);
                return false;
            }

            var result = _session.SubmitPlate(plate);
            PlayFeedback(result);

            // 접시는 창구에서 회수되어 선반으로 돌아간다.
            plate.Dispose();
            if (_plateReturn != null) _plateReturn.ReturnPlate();

            return true;
        }

        private void PlayFeedback(ServeResult result)
        {
            var effect = result.IsSuccess ? _successEffect : _failEffect;
            if (effect == null) return;

            effect.SetActive(false);
            effect.SetActive(true);
        }

#if UNITY_EDITOR
        public void EditorConfigure(GameSession session, PlateDispenser plateReturn)
        {
            _session = session;
            _plateReturn = plateReturn;
        }
#endif
    }
}
