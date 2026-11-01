using HellsKitchen.Core.Ingredients;
using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 화로. 올려 두면 알아서 익고, 그대로 두면 탄다.
    /// 플레이어가 손을 대지 않아도 시간이 흐른다는 점이 도마와의 결정적 차이이고,
    /// 이것이 "여러 일을 동시에 관리한다"는 이 장르의 압박을 만든다.
    /// </summary>
    public class StoveStation : CounterStation
    {
        [Header("연출")]
        [SerializeField] private GameObject _cookingEffect;
        [SerializeField] private GameObject _burningEffect;

        public override string DisplayName => "화로";

        public override bool ShowsProgress
        {
            get
            {
                var ing = CurrentIngredient;
                if (ing == null) return false;
                return ing.State == ProcessState.Raw || ing.State == ProcessState.Cooked;
            }
        }

        public override float Progress01
        {
            get
            {
                var ing = CurrentIngredient;
                if (ing == null) return 0f;

                if (ing.State == ProcessState.Raw) return ing.ProcessProgress;

                if (ing.State == ProcessState.Cooked && ing.Definition != null)
                {
                    // 다 익은 뒤에는 타기까지 남은 시간을 거꾸로 보여 준다.
                    var burn = Mathf.Max(0.1f, ing.Definition.BurnSeconds);
                    return 1f - Mathf.Clamp01(ing.OverCookSeconds / burn);
                }

                return 0f;
            }
        }

        public override bool IsProgressCritical
        {
            get
            {
                var ing = CurrentIngredient;
                return ing != null && ing.State == ProcessState.Cooked;
            }
        }

        private IngredientItem CurrentIngredient => _current as IngredientItem;

        /// <summary>구워야 할 재료(생패티 등)라면 강하게 끌어당긴다.</summary>
        public override float GetAttraction(Carryable item)
        {
            if (!(item is IngredientItem ingredient)) return 0f;
            if (ingredient.Definition == null) return 0f;
            if (_current != null) return 0f;

            return ingredient.Definition.CanCook(ingredient.State) ? 2.6f : 0f;
        }

        protected override bool CanAccept(Carryable item)
        {
            if (!(item is IngredientItem ingredient)) return false;
            return ingredient.Definition != null && ingredient.Definition.CanCook(ingredient.State);
        }

        private void Update()
        {
            var ingredient = CurrentIngredient;
            if (ingredient == null)
            {
                UpdateEffects(false, false);
                return;
            }

            var definition = ingredient.Definition;
            if (definition == null) return;

            var dt = Time.deltaTime;

            switch (ingredient.State)
            {
                case ProcessState.Raw:
                    var cookDuration = Mathf.Max(0.1f, definition.CookSeconds);
                    if (ingredient.AddProgress(dt / cookDuration))
                    {
                        ingredient.SetState(ProcessState.Cooked);
                    }
                    UpdateEffects(cooking: true, burning: false);
                    break;

                case ProcessState.Cooked:
                    if (ingredient.AddOverCook(dt))
                    {
                        ingredient.SetState(ProcessState.Burned);
                    }
                    UpdateEffects(cooking: true, burning: true);
                    break;

                default:
                    // 타 버린 재료는 더 이상 변하지 않는다. 쓰레기통으로 가야 한다.
                    UpdateEffects(cooking: false, burning: false);
                    break;
            }
        }

        protected override void OnItemRemoved(Carryable item) => UpdateEffects(false, false);

        private void UpdateEffects(bool cooking, bool burning)
        {
            if (_cookingEffect != null && _cookingEffect.activeSelf != cooking) _cookingEffect.SetActive(cooking);
            if (_burningEffect != null && _burningEffect.activeSelf != burning) _burningEffect.SetActive(burning);
        }
    }
}
