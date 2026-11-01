using System;
using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using HellsKitchen.Core.Progression;
using HellsKitchen.Core.Recipes;
using HellsKitchen.Core.Utility;

namespace HellsKitchen.Core.Orders
{
    public enum OrderRemovalReason
    {
        Served = 0,
        Expired = 1,
        Cleared = 2,
    }

    /// <summary>
    /// 대기 중인 주문 목록의 수명 관리자.
    /// 시간을 스스로 읽지 않고 Tick(deltaSeconds) 로 받기 때문에,
    /// 나중에 서버 틱이나 테스트에서 시간을 마음대로 흘려보낼 수 있다.
    ///
    /// 점수는 이 클래스의 관심사가 아니다. 여기서는 "무엇이 떠 있고, 무엇이 맞는가"만 판단한다.
    /// </summary>
    public sealed class OrderBoard
    {
        private readonly List<RecipeData> _menu;
        private readonly IRandomSource _random;
        private readonly List<Order> _active = new List<Order>();
        private readonly List<Order> _expiredBuffer = new List<Order>();

        private int _nextOrderId = 1;
        private float _spawnTimer;

        public OrderBoard(IReadOnlyList<RecipeData> menu, IRandomSource random)
        {
            if (menu == null || menu.Count == 0)
                throw new ArgumentException("메뉴에 레시피가 최소 하나는 있어야 합니다.", nameof(menu));

            _menu = new List<RecipeData>(menu);
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public IReadOnlyList<Order> ActiveOrders => _active;

        public event Action<Order> OrderAdded;
        public event Action<Order, OrderRemovalReason> OrderRemoved;

        /// <summary>
        /// 라운드 시작 직후 첫 주문이 뜨기까지의 대기 시간.
        /// 0 이면 즉시 하나 띄운다.
        /// </summary>
        public void Prime(float firstSpawnDelay, DifficultySnapshot difficulty)
        {
            if (firstSpawnDelay > 0f)
            {
                _spawnTimer = firstSpawnDelay;
                return;
            }

            // 즉시 한 장 띄우고, 다음 주문까지의 간격을 새로 잡아 준다.
            // 타이머를 0으로 둔 채 넘기면 바로 다음 틱에 또 스폰된다.
            SpawnOrder(difficulty);
            _spawnTimer = difficulty.SpawnInterval;
        }

        public void Tick(float deltaSeconds, DifficultySnapshot difficulty, bool allowSpawn)
        {
            if (deltaSeconds <= 0f) return;

            AdvanceOrders(deltaSeconds);

            if (!allowSpawn) return;

            _spawnTimer -= deltaSeconds;
            if (_spawnTimer <= 0f)
            {
                if (_active.Count < difficulty.MaxActiveOrders)
                {
                    SpawnOrder(difficulty);
                    _spawnTimer = difficulty.SpawnInterval;
                }
                else
                {
                    // 자리가 없으면 잠깐 뒤에 다시 시도한다.
                    _spawnTimer = 1f;
                }
            }
        }

        private void AdvanceOrders(float deltaSeconds)
        {
            _expiredBuffer.Clear();

            for (int i = 0; i < _active.Count; i++)
            {
                var order = _active[i];
                order.Advance(deltaSeconds);
                if (order.IsExpired) _expiredBuffer.Add(order);
            }

            for (int i = 0; i < _expiredBuffer.Count; i++)
            {
                Remove(_expiredBuffer[i], OrderRemovalReason.Expired);
            }
        }

        private void SpawnOrder(DifficultySnapshot difficulty)
        {
            var recipe = _menu[_random.NextInt(_menu.Count)];
            var modifiers = BuildModifiers(recipe, difficulty.ModifierChance);

            var timeLimit = recipe.BaseTimeLimitSeconds * difficulty.TimeLimitScale;

            // 변형이 붙으면 손이 더 가므로 시간과 보상을 조금 얹어 준다.
            if (modifiers.Count > 0)
            {
                timeLimit *= 1f + 0.12f * modifiers.Count;
            }

            var reward = recipe.BaseReward + 5 * modifiers.Count;

            var order = new Order(_nextOrderId++, recipe, modifiers, timeLimit, reward);
            _active.Add(order);
            OrderAdded?.Invoke(order);
        }

        private List<OrderModifier> BuildModifiers(RecipeData recipe, float chance)
        {
            var modifiers = new List<OrderModifier>();
            if (chance <= 0f || _random.NextFloat() >= chance) return modifiers;

            var canAdd = recipe.OptionalAdditions.Count > 0;
            var canRemove = recipe.RemovableIngredients.Count > 0;
            if (!canAdd && !canRemove) return modifiers;

            // 둘 다 가능하면 반반, 아니면 가능한 쪽으로.
            var useAdd = canAdd && (!canRemove || _random.NextFloat() < 0.5f);

            if (useAdd)
            {
                var pick = recipe.OptionalAdditions[_random.NextInt(recipe.OptionalAdditions.Count)];
                modifiers.Add(OrderModifier.Add(pick));
            }
            else
            {
                var pick = recipe.RemovableIngredients[_random.NextInt(recipe.RemovableIngredients.Count)];
                modifiers.Add(OrderModifier.Remove(pick));
            }

            return modifiers;
        }

        /// <summary>
        /// 접시 내용물과 맞는 주문을 찾는다.
        /// 같은 요리가 여러 개 떠 있으면 가장 급한 것부터 소진한다.
        /// </summary>
        public Order FindMatch(IngredientMultiset plateContents)
        {
            if (plateContents == null || plateContents.IsEmpty) return null;

            Order best = null;
            for (int i = 0; i < _active.Count; i++)
            {
                var order = _active[i];
                if (!order.Matches(plateContents)) continue;
                if (best == null || order.RemainingSeconds < best.RemainingSeconds) best = order;
            }
            return best;
        }

        public bool Complete(Order order) => Remove(order, OrderRemovalReason.Served);

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Remove(_active[i], OrderRemovalReason.Cleared);
            }
            _spawnTimer = 0f;
        }

        private bool Remove(Order order, OrderRemovalReason reason)
        {
            if (order == null) return false;
            if (!_active.Remove(order)) return false;
            OrderRemoved?.Invoke(order, reason);
            return true;
        }
    }
}
