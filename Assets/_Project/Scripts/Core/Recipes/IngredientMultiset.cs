using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HellsKitchen.Core.Ingredients;

namespace HellsKitchen.Core.Recipes
{
    /// <summary>
    /// 재료의 "중복을 허용하는 집합". 치즈버거처럼 같은 재료가 두 장 들어가는 경우를
    /// 표현해야 하므로 HashSet이 아니라 (재료 -> 개수) 사전으로 관리한다.
    ///
    /// 오버쿡드 계열 게임과 마찬가지로 담는 순서는 판정에 영향을 주지 않는다.
    /// 즉 [빵, 패티] 와 [패티, 빵] 은 같은 것으로 본다.
    /// </summary>
    public sealed class IngredientMultiset : IEnumerable<KeyValuePair<IngredientStack, int>>
    {
        private readonly Dictionary<IngredientStack, int> _counts;

        public IngredientMultiset()
        {
            _counts = new Dictionary<IngredientStack, int>();
        }

        public IngredientMultiset(IEnumerable<IngredientStack> items) : this()
        {
            if (items == null) return;
            foreach (var item in items) Add(item);
        }

        public IngredientMultiset(IngredientMultiset other) : this()
        {
            if (other == null) return;
            foreach (var pair in other._counts) _counts[pair.Key] = pair.Value;
            TotalCount = other.TotalCount;
        }

        /// <summary>담긴 재료의 총 개수(중복 포함).</summary>
        public int TotalCount { get; private set; }

        /// <summary>서로 다른 재료 종류의 수.</summary>
        public int DistinctCount => _counts.Count;

        public bool IsEmpty => TotalCount == 0;

        public int CountOf(IngredientStack stack)
        {
            return _counts.TryGetValue(stack, out var n) ? n : 0;
        }

        public void Add(IngredientStack stack, int amount = 1)
        {
            if (amount <= 0) return;
            _counts.TryGetValue(stack, out var current);
            _counts[stack] = current + amount;
            TotalCount += amount;
        }

        /// <summary>한 개를 덜어낸다. 실제로 덜어냈으면 true.</summary>
        public bool Remove(IngredientStack stack, int amount = 1)
        {
            if (amount <= 0) return false;
            if (!_counts.TryGetValue(stack, out var current) || current < amount) return false;

            if (current == amount) _counts.Remove(stack);
            else _counts[stack] = current - amount;

            TotalCount -= amount;
            return true;
        }

        public void Clear()
        {
            _counts.Clear();
            TotalCount = 0;
        }

        /// <summary>내용물이 완전히 같은가. 레시피 판정의 핵심 연산.</summary>
        public bool ContentEquals(IngredientMultiset other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (TotalCount != other.TotalCount) return false;
            if (_counts.Count != other._counts.Count) return false;

            foreach (var pair in _counts)
            {
                if (!other._counts.TryGetValue(pair.Key, out var n) || n != pair.Value) return false;
            }
            return true;
        }

        /// <summary>
        /// 이 집합이 target 을 만들기 위해 아직 부족한 재료들.
        /// UI에서 "무엇이 더 필요한지" 표시하거나 부분 점수를 줄 때 쓴다.
        /// </summary>
        public IngredientMultiset MissingFrom(IngredientMultiset target)
        {
            var result = new IngredientMultiset();
            if (target == null) return result;

            foreach (var pair in target._counts)
            {
                var shortfall = pair.Value - CountOf(pair.Key);
                if (shortfall > 0) result.Add(pair.Key, shortfall);
            }
            return result;
        }

        /// <summary>target 에는 없는데 이 집합에만 들어있는 재료들(=잘못 넣은 것).</summary>
        public IngredientMultiset ExtrasOver(IngredientMultiset target)
        {
            var result = new IngredientMultiset();
            foreach (var pair in _counts)
            {
                var surplus = pair.Value - (target?.CountOf(pair.Key) ?? 0);
                if (surplus > 0) result.Add(pair.Key, surplus);
            }
            return result;
        }

        public IEnumerator<KeyValuePair<IngredientStack, int>> GetEnumerator() => _counts.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            if (IsEmpty) return "(비어 있음)";
            var sb = new StringBuilder();
            foreach (var pair in _counts)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(pair.Key);
                if (pair.Value > 1) sb.Append(" x").Append(pair.Value);
            }
            return sb.ToString();
        }
    }
}
