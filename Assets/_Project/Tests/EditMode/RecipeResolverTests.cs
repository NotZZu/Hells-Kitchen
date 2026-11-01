using HellsKitchen.Core.Ingredients;
using HellsKitchen.Core.Recipes;
using Testing.Shared.Traceability;
using NUnit.Framework;

namespace HellsKitchen.Tests
{
    /// <summary>
    /// 기본 레시피 + 변형 -> 실제 요구 재료 집합(RecipeResolver). 결정 테이블 기법.
    /// 기준 레시피는 던전 버거: 빵 + 패티(Cooked) + 양상추(Chopped) + 토마토(Chopped).
    /// </summary>
    [TestFixture]
    public class RecipeResolverTests
    {
        private RecipeData _burger;

        [SetUp]
        public void SetUp() => _burger = Menu.DungeonBurger();

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-001")]
        public void Resolve_변형이_null이면_기본_구성을_돌려준다()
        {
            // Given: 던전 버거  When: 변형 목록 없이(null) 해석  Then: 기본 4개 재료 그대로
            var result = RecipeResolver.Resolve(_burger, null);

            Assert.That(result.ContentEquals(PlateOf.Burger()), Is.True);
            Assert.That(result.TotalCount, Is.EqualTo(4));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-002")]
        public void Resolve_변형이_빈_목록이면_기본_구성을_돌려준다()
        {
            var result = RecipeResolver.Resolve(_burger, new OrderModifier[0]);

            Assert.That(result.ContentEquals(PlateOf.Burger()), Is.True);
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-003")]
        public void Resolve_추가_변형이면_재료가_하나_늘어난다()
        {
            var result = RecipeResolver.Resolve(_burger, new[] { OrderModifier.Add(Ingredients.CheeseChopped) });

            Assert.That(result.TotalCount, Is.EqualTo(5));
            Assert.That(result.CountOf(Ingredients.CheeseChopped), Is.EqualTo(1));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-004")]
        public void Resolve_이미_있는_재료를_추가하면_개수가_2가_된다()
        {
            var result = RecipeResolver.Resolve(_burger, new[] { OrderModifier.Add(Ingredients.LettuceChopped) });

            Assert.That(result.CountOf(Ingredients.LettuceChopped), Is.EqualTo(2));
            Assert.That(result.TotalCount, Is.EqualTo(5));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-005")]
        public void Resolve_제거_변형이면_해당_재료가_빠진다()
        {
            var result = RecipeResolver.Resolve(_burger, new[] { OrderModifier.Remove(Ingredients.LettuceChopped) });

            Assert.That(result.TotalCount, Is.EqualTo(3));
            Assert.That(result.CountOf(Ingredients.LettuceChopped), Is.EqualTo(0));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-006")]
        public void Resolve_레시피에_없는_재료를_제거하면_조용히_무시한다()
        {
            // 양파는 던전 버거 기본 구성에 없다.
            var result = RecipeResolver.Resolve(_burger, new[] { OrderModifier.Remove(Ingredients.OnionChopped) });

            Assert.That(result.ContentEquals(PlateOf.Burger()), Is.True);
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-007")]
        public void Resolve_가공_상태가_다른_재료를_제거하면_무시한다()
        {
            // 레시피에는 Chopped 양상추가 있고, 변형은 Raw 양상추를 빼라고 한다.
            var result = RecipeResolver.Resolve(_burger, new[] { OrderModifier.Remove(Ingredients.LettuceRaw) });

            Assert.That(result.ContentEquals(PlateOf.Burger()), Is.True);
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-008")]
        public void Resolve_추가한_재료를_같은_변형_목록에서_제거하면_기본_구성이_된다()
        {
            var result = RecipeResolver.Resolve(_burger, new[]
            {
                OrderModifier.Add(Ingredients.CheeseChopped),
                OrderModifier.Remove(Ingredients.CheeseChopped),
            });

            Assert.That(result.ContentEquals(PlateOf.Burger()), Is.True);
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-009")]
        public void Resolve_하나뿐인_재료를_두_번_제거하면_두번째는_무시한다()
        {
            var result = RecipeResolver.Resolve(_burger, new[]
            {
                OrderModifier.Remove(Ingredients.LettuceChopped),
                OrderModifier.Remove(Ingredients.LettuceChopped),
            });

            Assert.That(result.CountOf(Ingredients.LettuceChopped), Is.EqualTo(0));
            Assert.That(result.TotalCount, Is.EqualTo(3));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("RECIPE-010")]
        public void Resolve_해석해도_원본_레시피는_바뀌지_않는다()
        {
            RecipeResolver.Resolve(_burger, new[]
            {
                OrderModifier.Add(Ingredients.CheeseChopped),
                OrderModifier.Remove(Ingredients.TomatoChopped),
            });

            Assert.That(_burger.RequiredIngredients.Count, Is.EqualTo(4));
            Assert.That(_burger.BuildBaseRequirement().ContentEquals(PlateOf.Burger()), Is.True);
        }

        [Test, Requirement("FR-ORDER-010"), Tc("RECIPE-011")]
        public void Resolve_제거_가능_목록에_없는_재료도_변형이_오면_제거한다_판단은_OrderBoard_몫()
        {
            // 특성화 테스트: Resolver 는 "제거 가능 재료" 여부를 검사하지 않는다.
            // 빵/패티를 제거 대상에서 빼는 책임은 OrderBoard 의 변형 생성에 있다(ORDER-023 참고).
            var result = RecipeResolver.Resolve(_burger, new[] { OrderModifier.Remove(Ingredients.Bun) });

            Assert.That(result.CountOf(Ingredients.Bun), Is.EqualTo(0));
        }
    }
}
