using HellsKitchen.Core.Ingredients;
using UnityEngine;

namespace HellsKitchen.Data
{
    /// <summary>
    /// 재료 한 종류의 외형과 조리 시간을 정의한다.
    /// 상태가 바뀌면 이 정의에서 해당 상태의 모델을 꺼내 교체한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Hell's Kitchen/재료 정의", fileName = "Ingredient_")]
    public class IngredientDefinition : ScriptableObject
    {
        [Header("식별")]
        public IngredientType Type = IngredientType.None;

        [Tooltip("UI와 주문 티켓에 표시할 이름")]
        public string DisplayName = "";

        [Tooltip("주문 티켓에 표시할 아이콘. 에디터 툴이 모델을 렌더해 자동으로 채워 준다.")]
        public Sprite Icon;

        [Header("외형 - 상태별 모델")]
        [Tooltip("손질 전 모델. 필수.")]
        public GameObject RawModel;

        [Tooltip(
            "도마에서 손질하는 '중'에 보여줄 모델. 잘게 흩어진 형태.\n" +
            "비워 두면 손질 전 모델을 그대로 쓴다.")]
        public GameObject ChoppingModel;

        [Tooltip("손질을 마친 모델. 버거에 올릴 수 있는 완성된 형태(슬라이스, 링 등).")]
        public GameObject ChoppedModel;

        [Tooltip("익힌 모델. 구울 수 없는 재료면 비워 둔다.")]
        public GameObject CookedModel;

        [Tooltip("탄 모델. 비워 두면 익힌 모델을 검게 표시한다.")]
        public GameObject BurnedModel;

        [Header("외형 - 크기")]
        [Tooltip("모델을 표시할 배율. 로우폴리 재료는 실제 크기가 작아 그대로 두면 화면에서 식별이 어렵다.")]
        [Min(0.1f)] public float DisplayScale = 1.4f;

        [Header("접시에 담길 때")]
        [Tooltip("접시 위에서 대신 쓸 모델. 비우면 상태별 모델을 그대로 쓴다. 예: 빵 -> 아래빵")]
        public GameObject PlateModel;

        [Tooltip("접시 위에서 이 재료 위에 덮을 모델. 예: 빵 -> 윗빵. 다른 재료가 하나라도 쌓였을 때만 나타난다.")]
        public GameObject PlateTopperModel;

        [Tooltip("접시에 쌓이는 순서. 작을수록 아래에 깔린다.")]
        public int PlateSortOrder = 50;

        [Tooltip("접시 위에 쌓일 때의 높이 간격. 빵처럼 두꺼운 재료는 크게 준다.")]
        public float PlateStackHeight = 0.05f;

        [Header("조리 공정")]
        [Tooltip("도마에서 손질할 수 있는가")]
        public bool Choppable = false;

        [Tooltip("화로에서 구울 수 있는가")]
        public bool Cookable = false;

        [Tooltip(
            "손질하면 아예 다른 재료가 되는 경우 여기에 결과를 넣는다. 예: 고기 -> 패티.\n" +
            "비워 두면 같은 재료의 '썬 상태'가 된다.")]
        public IngredientDefinition ChopResult;

        [Header("조리 시간(초)")]
        [Min(0.1f)] public float ChopSeconds = 2.5f;
        [Min(0.1f)] public float CookSeconds = 6f;

        [Tooltip("다 익은 뒤 이 시간이 더 지나면 탄다.")]
        [Min(0.1f)] public float BurnSeconds = 6f;

        [Header("신선도")]
        [Tooltip("바닥에 떨어진 채로 이 시간이 지나면 재료가 상해서 사라진다.")]
        [Min(1f)] public float GroundLifeSeconds = 20f;

        public string ResolvedName => string.IsNullOrEmpty(DisplayName) ? Type.ToString() : DisplayName;

        /// <summary>지금 상태에서 도마에 올릴 수 있는가.</summary>
        public bool CanChop(ProcessState state) => Choppable && state == ProcessState.Raw;

        /// <summary>지금 상태에서 화로에 올릴 수 있는가.</summary>
        public bool CanCook(ProcessState state) => Cookable && state == ProcessState.Raw;

        /// <summary>
        /// 해당 상태에서 보여줄 모델. 없으면 가장 가까운 대체 모델을 돌려준다.
        /// </summary>
        /// <param name="processing">
        /// 도마에서 손질이 진행 중인가. 손질 전과 손질 완료 사이의 '썰리는 중' 모습을 위해 필요하다.
        /// </param>
        public GameObject GetModel(ProcessState state, bool processing = false)
        {
            switch (state)
            {
                case ProcessState.Chopped: return ChoppedModel != null ? ChoppedModel : RawModel;
                case ProcessState.Cooked: return CookedModel != null ? CookedModel : RawModel;
                case ProcessState.Burned:
                    if (BurnedModel != null) return BurnedModel;
                    return CookedModel != null ? CookedModel : RawModel;
                default:
                    if (processing && ChoppingModel != null) return ChoppingModel;
                    return RawModel;
            }
        }

        /// <summary>접시 위에서 쓸 모델. 전용 모델이 없으면 상태별 모델을 그대로 쓴다.</summary>
        public GameObject GetPlateModel(ProcessState state)
        {
            if (PlateModel != null && state != ProcessState.Burned) return PlateModel;
            return GetModel(state, processing: false);
        }
    }
}
