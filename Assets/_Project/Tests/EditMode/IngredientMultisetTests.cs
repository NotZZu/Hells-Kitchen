using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using HellsKitchen.Core.Recipes;
using Testing.Shared.Traceability;
using NUnit.Framework;

namespace HellsKitchen.Tests
{
    /// <summary>
    /// 접시 내용물 판정의 핵심인 IngredientMultiset(순서 무관·개수 포함 집합). 동등분할·에러추측 기법.
    /// </summary>
    [TestFixture]
    public class IngredientMultisetTests
    {
        private static IEnumerable<IngredientStack[]> Permutations(IngredientStack[] items)
        {
            var list = new List<IngredientStack>(items);
            return Permute(list, 0);
        }

        private static IEnumerable<IngredientStack[]> Permute(List<IngredientStack> a, int k)
        {
            if (k == a.Count - 1) { yield return a.ToArray(); yield break; }
            for (int i = k; i < a.Count; i++)
            {
                (a[k], a[i]) = (a[i], a[k]);
                foreach (var p in Permute(a, k + 1)) yield return p;
                (a[k], a[i]) = (a[i], a[k]);
            }
        }

        [Test, Requirement("FR-PLATE-002"), Tc("MS-001")]
        public void ContentEquals_담는_순서가_달라도_같은_구성이면_같다_4개_재료_24가지_순열()
        {
            var requirement = PlateOf.Burger();
            var count = 0;

            foreach (var order in Permutations(new[]
            {
                Ingredients.Bun, Ingredients.PattyCooked, Ingredients.LettuceChopped, Ingredients.TomatoChopped,
            }))
            {
                Assert.That(new IngredientMultiset(order).ContentEquals(requirement), Is.True);
                count++;
            }

            Assert.That(count, Is.EqualTo(24));
        }

        [Test, Requirement("FR-PLATE-002"), Tc("MS-002")]
        public void ContentEquals_총_개수와_종류_수가_같아도_개수_분포가_다르면_다르다()
        {
            var a = PlateOf.Of(Ingredients.Bun, Ingredients.Bun, Ingredients.PattyCooked);
            var b = PlateOf.Of(Ingredients.Bun, Ingredients.PattyCooked, Ingredients.PattyCooked);

            Assert.That(a.TotalCount, Is.EqualTo(b.TotalCount));
            Assert.That(a.DistinctCount, Is.EqualTo(b.DistinctCount));
            Assert.That(a.ContentEquals(b), Is.False);
        }

        [Test, Requirement("FR-PLATE-002"), Tc("MS-003")]
        public void ContentEquals_같은_재료_두_장과_한_장은_다르다()
        {
            var two = PlateOf.Of(Ingredients.CheeseChopped, Ingredients.CheeseChopped);
            var one = PlateOf.Of(Ingredients.CheeseChopped);

            Assert.That(two.ContentEquals(one), Is.False);
            Assert.That(two.ContentEquals(PlateOf.Of(Ingredients.CheeseChopped, Ingredients.CheeseChopped)), Is.True);
        }

        [Test, Requirement("FR-SCORE-002"), Tc("MS-004")]
        public void ContentEquals_같은_재료라도_가공_상태가_다르면_다르다()
        {
            Assert.That(PlateOf.Of(Ingredients.LettuceChopped).ContentEquals(PlateOf.Of(Ingredients.LettuceRaw)), Is.False);
            Assert.That(PlateOf.Of(Ingredients.PattyCooked).ContentEquals(PlateOf.Of(Ingredients.PattyBurned)), Is.False);
        }

        [Test, Requirement("FR-PLATE-002"), Tc("MS-005")]
        public void ContentEquals_null과는_다르고_자기_자신과는_같다()
        {
            var set = PlateOf.Burger();

            Assert.That(set.ContentEquals(null), Is.False);
            Assert.That(set.ContentEquals(set), Is.True);
        }

        [Test, Requirement("FR-PLATE-001"), Tc("MS-006")]
        public void Add_개수가_0_이하이면_무시된다()
        {
            var set = new IngredientMultiset();

            set.Add(Ingredients.Bun, 0);
            set.Add(Ingredients.Bun, -2);

            Assert.That(set.IsEmpty, Is.True);
            Assert.That(set.TotalCount, Is.EqualTo(0));
        }

        [Test, Requirement("FR-PLATE-001"), Tc("MS-007")]
        public void Remove_없거나_부족하면_false를_돌려주고_내용은_그대로다()
        {
            var set = PlateOf.Of(Ingredients.Bun, Ingredients.CheeseChopped);

            Assert.That(set.Remove(Ingredients.OnionChopped), Is.False, "없는 재료");
            Assert.That(set.Remove(Ingredients.Bun, 2), Is.False, "개수 부족");
            Assert.That(set.TotalCount, Is.EqualTo(2));
            Assert.That(set.Remove(Ingredients.Bun), Is.True);
            Assert.That(set.DistinctCount, Is.EqualTo(1), "개수가 0이 되면 종류에서도 빠진다");
        }

        [Test, Requirement("FR-PLATE-001"), Tc("MS-008")]
        public void 복사_생성자로_만든_집합은_원본과_독립이다()
        {
            var original = PlateOf.Burger();
            var copy = new IngredientMultiset(original);

            copy.Add(Ingredients.CheeseChopped);

            Assert.That(original.TotalCount, Is.EqualTo(4));
            Assert.That(copy.TotalCount, Is.EqualTo(5));
        }

        [Test, Requirement("FR-SCORE-002"), Tc("MS-009")]
        public void MissingFrom_과_ExtrasOver_부족한_재료와_잘못_넣은_재료를_알려준다()
        {
            var plate = PlateOf.Of(Ingredients.Bun, Ingredients.PattyCooked, Ingredients.CheeseChopped);
            var requirement = PlateOf.Burger();

            var missing = plate.MissingFrom(requirement);
            var extras = plate.ExtrasOver(requirement);

            Assert.That(missing.ContentEquals(PlateOf.Of(Ingredients.LettuceChopped, Ingredients.TomatoChopped)), Is.True);
            Assert.That(extras.ContentEquals(PlateOf.Of(Ingredients.CheeseChopped)), Is.True);
        }
    }
}
