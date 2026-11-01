using HellsKitchen.Core.Ingredients;
using HellsKitchen.Runtime.Items;
using UnityEngine;

namespace HellsKitchen.Runtime.Stations
{
    /// <summary>
    /// 도마. 썰 수 있는 재료를 올린 뒤 작업 버튼을 누르고 있으면 진행도가 찬다.
    /// 손을 떼면 진행도는 그 자리에 멈춰 있다(초기화되지 않는다).
    /// </summary>
    public class CuttingStation : CounterStation
    {
        [Header("연출")]
        [SerializeField] private GameObject _choppingEffect;
        [SerializeField] private AudioSource _chopLoop;

        private bool _working;

        public override string DisplayName => "도마";
        public override bool SupportsWork => true;

        public override bool ShowsProgress => CurrentIngredient != null && CanChopCurrent;

        /// <summary>썰 것이 올라와 있고 아직 다 썰지 않았다면 할 일이 남은 것이다.</summary>
        public override bool HasWorkPending => CanChopCurrent;

        /// <summary>손질해야 할 재료라면 강하게 끌어당긴다. 원물이 향할 다음 자리다.</summary>
        public override float GetAttraction(Carryable item)
        {
            if (!(item is IngredientItem ingredient)) return 0f;
            if (ingredient.Definition == null) return 0f;
            if (_current != null) return 0f;

            return ingredient.Definition.CanChop(ingredient.State) ? 2.6f : 0f;
        }

        public override float Progress01 => CurrentIngredient != null ? CurrentIngredient.ProcessProgress : 0f;

        private IngredientItem CurrentIngredient => _current as IngredientItem;

        private bool CanChopCurrent
        {
            get
            {
                var ing = CurrentIngredient;
                return ing != null && ing.Definition != null && ing.Definition.CanChop(ing.State);
            }
        }

        protected override bool CanAccept(Carryable item)
        {
            // 썰 수 있는 재료만 받는다. 다 썬 재료를 다시 올리는 것도 막는다.
            if (!(item is IngredientItem ingredient)) return false;
            return ingredient.Definition != null && ingredient.Definition.CanChop(ingredient.State);
        }

        public override void OnWorkHeld(float deltaSeconds)
        {
            var ingredient = CurrentIngredient;
            if (ingredient == null || !CanChopCurrent)
            {
                StopWorkFeedback();
                return;
            }

            var definition = ingredient.Definition;
            var duration = definition != null ? Mathf.Max(0.1f, definition.ChopSeconds) : 2.5f;

            StartWorkFeedback();

            if (ingredient.AddProgress(deltaSeconds / duration))
            {
                // 손질 결과가 아예 다른 재료인 경우(고기 -> 패티)와
                // 같은 재료의 썬 상태인 경우(양상추 -> 썬 양상추)를 나눠 처리한다.
                if (definition != null && definition.ChopResult != null)
                {
                    ingredient.ChangeTo(definition.ChopResult, ProcessState.Raw);
                }
                else
                {
                    ingredient.SetState(ProcessState.Chopped);
                }

                StopWorkFeedback();
            }
        }

        public override void OnWorkReleased() => StopWorkFeedback();

        protected override void OnItemRemoved(Carryable item) => StopWorkFeedback();

        private void StartWorkFeedback()
        {
            if (_working) return;
            _working = true;

            if (_choppingEffect != null) _choppingEffect.SetActive(true);
            if (_chopLoop != null && !_chopLoop.isPlaying) _chopLoop.Play();
        }

        private void StopWorkFeedback()
        {
            if (!_working) return;
            _working = false;

            if (_choppingEffect != null) _choppingEffect.SetActive(false);
            if (_chopLoop != null && _chopLoop.isPlaying) _chopLoop.Stop();
        }
    }
}
