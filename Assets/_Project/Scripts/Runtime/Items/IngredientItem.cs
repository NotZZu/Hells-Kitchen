using HellsKitchen.Core.Ingredients;
using HellsKitchen.Data;
using UnityEngine;

namespace HellsKitchen.Runtime.Items
{
    /// <summary>
    /// 씬에 존재하는 재료 하나. 상태가 바뀌면 모델을 갈아 끼운다.
    ///
    /// 가공 진행도를 스테이션이 아니라 재료 쪽에 들고 있는 이유는,
    /// 반쯤 썰던 재료를 다른 도마로 옮겨도 진행도가 이어지게 하기 위해서다.
    /// </summary>
    public class IngredientItem : Carryable
    {
        [SerializeField] private IngredientDefinition _definition;
        [SerializeField] private ProcessState _state = ProcessState.Raw;
        [SerializeField] private Transform _modelRoot;

        private GameObject _currentModel;
        private bool _plateMode;
        private bool _groundMode;

        // 슬라이스처럼 얇은 재료를 바닥에 눕히면 위에서 내려다보는 시점에서는
        // 선 한 줄로만 보인다. 바닥이나 공중에 있을 때는 카메라 쪽으로 세우고
        // 살짝 띄워서, 무엇이 떨어져 있는지 한눈에 알아보게 한다.
        private const float GroundTiltDegrees = -58f;
        private const float GroundLift = 0.2f;
        private const float GroundScaleBoost = 1.3f;

        public IngredientDefinition Definition => _definition;
        public IngredientType Type => _definition != null ? _definition.Type : IngredientType.None;
        public ProcessState State => _state;
        public IngredientStack Stack => new IngredientStack(Type, _state);

        public override string DisplayName =>
            _definition != null ? $"{_definition.ResolvedName}({_state})" : "알 수 없는 재료";

        /// <summary>현재 가공 진행도 0~1. 상태가 바뀌면 0으로 돌아간다.</summary>
        public float ProcessProgress { get; private set; }

        /// <summary>익은 뒤 타기까지의 경과 시간. 화로가 관리한다.</summary>
        public float OverCookSeconds { get; private set; }

        public bool IsBurned => _state == ProcessState.Burned;

        /// <summary>
        /// 도마에서 썰리는 중인가. 손질 전과 손질 완료 사이의 중간 모습을 보여 주기 위한 판단이다.
        /// 손을 떼도 진행도가 남아 있으면 계속 '썰다 만' 모습으로 남는다.
        /// </summary>
        public bool IsBeingChopped => _state == ProcessState.Raw && ProcessProgress > 0f;

        /// <summary>바닥에 놓인 채로 남은 신선도 시간(초).</summary>
        public float GroundLifeRemaining { get; private set; } = -1f;

        /// <summary>신선도 비율 0~1. 게이지 표시에 쓴다.</summary>
        public float GroundLifeRatio
        {
            get
            {
                if (_definition == null || _definition.GroundLifeSeconds <= 0f) return 1f;
                if (GroundLifeRemaining < 0f) return 1f;
                return Mathf.Clamp01(GroundLifeRemaining / _definition.GroundLifeSeconds);
            }
        }

        /// <summary>신선도가 줄어드는 중인가. 게이지를 띄울지 결정한다.</summary>
        public bool IsSpoiling => IsLoose && GroundLifeRemaining >= 0f;

        /// <summary>접시에 쌓이는 순서. 작을수록 아래.</summary>
        public int PlateSortOrder => _definition != null ? _definition.PlateSortOrder : 50;

        /// <summary>표시 배율이 반영된 접시 위 두께.</summary>
        public float StackHeight
        {
            get
            {
                if (_definition == null) return 0.045f;
                return Mathf.Max(0.01f, _definition.PlateStackHeight) * _definition.DisplayScale;
            }
        }

        /// <summary>접시 위에서 이 재료 위에 덮을 모델(윗빵 등). 없으면 null.</summary>
        public GameObject PlateTopperModel =>
            _definition != null && _state != ProcessState.Burned ? _definition.PlateTopperModel : null;

        /// <summary>스폰 직후 초기화. 프리팹 인스턴스를 만든 쪽에서 반드시 한 번 호출한다.</summary>
        public void Initialize(IngredientDefinition definition, ProcessState state = ProcessState.Raw)
        {
            _definition = definition;
            _state = state;
            ProcessProgress = 0f;
            OverCookSeconds = 0f;
            RefreshModel();
        }

        /// <summary>
        /// 아예 다른 재료로 바뀐다. 고기를 다져 패티가 되는 것처럼
        /// 손질 결과가 같은 재료의 '썬 상태'가 아니라 별개의 재료일 때 쓴다.
        /// 오브젝트는 그대로 쓰고 정의와 모델만 갈아 끼운다.
        /// </summary>
        public void ChangeTo(IngredientDefinition definition, ProcessState state = ProcessState.Raw)
        {
            if (definition == null) return;
            Initialize(definition, state);
        }

        public void SetState(ProcessState state)
        {
            if (_state == state) return;

            _state = state;
            ProcessProgress = 0f;
            OverCookSeconds = 0f;
            RefreshModel();
        }

        /// <summary>접시에 담기면 전용 모델로 바꾼다. 빵이 아래빵으로 보이게 하는 용도.</summary>
        public void SetPlateMode(bool onPlate)
        {
            // 접시에 올라가면 눕혀야 버거 단면이 된다. 세워 두는 표시는 해제한다.
            if (onPlate) SetGroundMode(false);

            if (_plateMode == onPlate) return;

            _plateMode = onPlate;
            RefreshModel();
        }

        /// <summary>
        /// 바닥이나 공중에 있을 때의 표시 자세로 전환한다.
        /// 손이나 설비로 돌아가면 원래 자세로 되돌린다.
        /// </summary>
        public void SetGroundMode(bool inWorld)
        {
            if (_groundMode == inWorld) return;

            _groundMode = inWorld;
            ApplyModelPose();
        }

        /// <summary>모델 루트의 자세를 현재 표시 모드에 맞춘다.</summary>
        private void ApplyModelPose()
        {
            var root = _modelRoot != null ? _modelRoot : transform;
            if (root == transform) return;

            if (_groundMode)
            {
                root.localRotation = Quaternion.Euler(GroundTiltDegrees, 0f, 0f);
                root.localPosition = new Vector3(0f, GroundLift, 0f);
                root.localScale = Vector3.one * GroundScaleBoost;
            }
            else
            {
                root.localRotation = Quaternion.identity;
                root.localPosition = Vector3.zero;
                root.localScale = Vector3.one;
            }
        }

        public override void AttachTo(Transform anchor, bool held)
        {
            base.AttachTo(anchor, held);

            // 손이든 설비든 접시든, 어딘가에 얹히면 세워 두는 표시는 해제한다.
            SetGroundMode(false);
        }

        public override void PlaceOnGround(Vector3 position)
        {
            base.PlaceOnGround(position);
            SetGroundMode(true);
        }

        /// <summary>가공 진행도를 더한다. 1에 도달하면 true 를 돌려준다.</summary>
        public bool AddProgress(float normalizedDelta)
        {
            if (normalizedDelta <= 0f) return false;

            var wasUntouched = ProcessProgress <= 0f;
            ProcessProgress += normalizedDelta;

            // 칼이 처음 닿는 순간 '썰리는 중' 모습으로 바꾼다.
            if (wasUntouched && _state == ProcessState.Raw) RefreshModel();

            if (ProcessProgress < 1f) return false;

            ProcessProgress = 1f;
            return true;
        }

        public void ResetProgress()
        {
            var hadProgress = ProcessProgress > 0f;
            ProcessProgress = 0f;
            if (hadProgress && _state == ProcessState.Raw) RefreshModel();
        }

        /// <summary>화로 위에서 다 익은 뒤의 방치 시간을 누적한다. 탈 때가 되면 true.</summary>
        public bool AddOverCook(float deltaSeconds)
        {
            if (_definition == null) return false;

            OverCookSeconds += deltaSeconds;
            return OverCookSeconds >= _definition.BurnSeconds;
        }

        private void Update()
        {
            // 바닥에 놓여 있는 동안에만 상한다.
            // 손에 들거나 설비에 올려 두면 타이머가 멈추되, 남은 시간은 그대로 유지된다.
            // 주웠다 놓기를 반복해도 시간이 되돌아오지 않아야 하기 때문이다.
            if (!IsLoose) return;
            if (_definition == null) return;

            if (GroundLifeRemaining < 0f) GroundLifeRemaining = _definition.GroundLifeSeconds;

            GroundLifeRemaining -= Time.deltaTime;
            if (GroundLifeRemaining > 0f) return;

            GroundLifeRemaining = 0f;
            Dispose();
        }

        private void RefreshModel()
        {
            if (_modelRoot == null) _modelRoot = transform;

            if (_currentModel != null) Destroy(_currentModel);
            if (_definition == null) return;

            var prefab = _plateMode
                ? _definition.GetPlateModel(_state)
                : _definition.GetModel(_state, IsBeingChopped);
            if (prefab == null) return;

            _currentModel = Instantiate(prefab, _modelRoot);

            // FBX 프리팹 루트에는 축을 바로잡는 회전과 단위 보정 배율이 들어 있다.
            // (KayKit 모델은 Z-up 으로 만들어져 있고 Bake Axis Conversion 이 꺼져 있다)
            // 이 값을 Quaternion.identity / Vector3.one 으로 덮어쓰면
            // 모델이 눕거나 크기가 완전히 달라져 화면에서 사라진 것처럼 보인다.
            // 위치만 원점으로 맞추고, 배율은 원래 값에 곱한다.
            var model = _currentModel.transform;
            model.localPosition = Vector3.zero;
            model.localScale *= _definition.DisplayScale;

            // 탄 상태인데 전용 모델이 없으면 색으로 구분해 준다.
            if (_state == ProcessState.Burned && _definition.BurnedModel == null)
            {
                TintDark(_currentModel);
            }

            // 모델을 갈아 끼운 뒤에도 지금 표시 자세를 유지한다.
            ApplyModelPose();
        }

#if UNITY_EDITOR
        public void EditorConfigure(Transform modelRoot) => _modelRoot = modelRoot;
#endif

        private static void TintDark(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var block = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(0.18f, 0.14f, 0.12f));
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
