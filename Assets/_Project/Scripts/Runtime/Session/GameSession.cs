using System;
using HellsKitchen.Core.Orders;
using HellsKitchen.Core.Utility;
using HellsKitchen.Data;
using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Session
{
    /// <summary>
    /// 씬과 순수 로직 사이의 유일한 다리.
    ///
    /// 이 클래스가 하는 일은 딱 두 가지다.
    ///  - 매 프레임 흐른 시간을 RoundSimulation 에 넘긴다
    ///  - 서빙 창구가 낸 접시를 시뮬레이션에 전달하고 결과를 씬에 돌려준다
    ///
    /// 규칙 판단은 전혀 하지 않는다. 나중에 온라인으로 갈 때
    /// 이 클래스만 "서버가 시뮬레이션을 돌리고 결과를 받아 재생한다"로 바뀌면 된다.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("스테이지")]
        [SerializeField] private LevelDefinition _level;

        [Tooltip("켜면 씬 진입과 동시에 라운드를 시작한다.")]
        [SerializeField] private bool _autoStart = true;

        private RoundSimulation _simulation;
        private bool _paused;

        public RoundSimulation Simulation => _simulation;
        public LevelDefinition Level => _level;
        public bool IsRunning => _simulation != null && _simulation.State == RoundState.Running && !_paused;
        public bool IsPaused => _paused;

        public event Action<Order> OrderSpawned;
        public event Action<Order> OrderExpired;
        public event Action<ServeResult> PlateServed;
        public event Action<RoundSummary> RoundFinished;
        public event Action RoundStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DetachSimulation();
        }

        private void Start()
        {
            if (_autoStart) StartRound();
        }

        public void StartRound()
        {
            if (_level == null)
            {
                Debug.LogError("[GameSession] 스테이지 정의가 지정되지 않았습니다.", this);
                return;
            }

            var menu = _level.BuildMenu();
            if (menu.Count == 0)
            {
                Debug.LogError("[GameSession] 스테이지 메뉴가 비어 있습니다.", this);
                return;
            }

            DetachSimulation();

            var seed = _level.RandomSeed != 0 ? _level.RandomSeed : Environment.TickCount;

            _simulation = new RoundSimulation(
                menu,
                _level.DurationSeconds,
                _level.Difficulty,
                _level.ScoreRules,
                _level.Stars,
                new SeededRandom(seed));

            _simulation.OrderSpawned += HandleOrderSpawned;
            _simulation.OrderExpired += HandleOrderExpired;
            _simulation.PlateServed += HandlePlateServed;
            _simulation.RoundFinished += HandleRoundFinished;

            _paused = false;
            _simulation.Start(_level.FirstOrderDelay);
            RoundStarted?.Invoke();
        }

        private void DetachSimulation()
        {
            if (_simulation == null) return;

            _simulation.OrderSpawned -= HandleOrderSpawned;
            _simulation.OrderExpired -= HandleOrderExpired;
            _simulation.PlateServed -= HandlePlateServed;
            _simulation.RoundFinished -= HandleRoundFinished;
            _simulation = null;
        }

        private void Update()
        {
            if (_simulation == null || _paused) return;
            _simulation.Tick(Time.deltaTime);
        }

        public void SetPaused(bool paused) => _paused = paused;

        /// <summary>
        /// 서빙 창구에서 호출한다. 접시 내용물을 판정하고, 성공이든 실패든 접시는 비워진다.
        /// </summary>
        public ServeResult SubmitPlate(PlateItem plate)
        {
            if (_simulation == null || plate == null)
            {
                return new ServeResult(ServeOutcome.EmptyPlate, null, 0, 0, 0f);
            }

            var contents = plate.BuildContents();
            var result = _simulation.SubmitPlate(contents);

            // 빈 접시를 낸 경우가 아니면 접시는 비워서 되돌린다.
            if (result.Outcome != ServeOutcome.EmptyPlate) plate.ClearContents();

            return result;
        }

        private void HandleOrderSpawned(Order order) => OrderSpawned?.Invoke(order);
        private void HandleOrderExpired(Order order) => OrderExpired?.Invoke(order);
        private void HandlePlateServed(ServeResult result) => PlateServed?.Invoke(result);

        private void HandleRoundFinished(RoundSummary summary)
        {
            _paused = true;
            RoundFinished?.Invoke(summary);
        }

#if UNITY_EDITOR
        public void EditorSetLevel(LevelDefinition level) => _level = level;
#endif
    }
}
