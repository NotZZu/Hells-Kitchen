using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using HellsKitchen.Core.Orders;
using HellsKitchen.Core.Recipes;
using HellsKitchen.Core.Utility;
using Testing.Shared.Asserts;
using Testing.Shared.Fakes;
using Testing.Shared.Traceability;
using NUnit.Framework;

namespace HellsKitchen.Tests
{
    /// <summary>
    /// 주문판(OrderBoard)의 수명 관리. 상태 전이·경계값 기법.
    /// 시간 값은 이진수로 정확히 표현되는 값(0.25, 0.5, 9.75 등)을 써서 부동소수 오차 없이 경계를 시험한다.
    /// </summary>
    [TestFixture]
    public class OrderBoardTests
    {
        private static OrderBoard NewBoard(RecipeData recipe = null, IRandomSource random = null)
            => new OrderBoard(new[] { recipe ?? Menu.DungeonBurger() }, random ?? new FakeRandom());

        /// <summary>활성 주문을 3건 채운다(간격 28초, 제한시간 175초라 만료되지 않는다).</summary>
        private static OrderBoard BoardWithThreeOrders(out HellsKitchen.Core.Progression.DifficultySnapshot snap)
        {
            snap = Difficulty.Snapshot(interval: 28f, timeScale: 1f, modifierChance: 0f, maxActive: 3);
            var board = NewBoard();
            board.Prime(0f, snap);
            board.Tick(28f, snap, true);
            board.Tick(28f, snap, true);
            return board;
        }

        // ---------- 첫 주문 / 생성 간격 ----------

        [Test, Requirement("FR-ORDER-001"), Tc("ORDER-001")]
        public void Tick_첫_주문_지연_8초_직전에는_주문이_없다()
        {
            var snap = Difficulty.Snapshot();
            var board = NewBoard();
            board.Prime(8f, snap);

            board.Tick(7.5f, snap, true);

            Assert.That(board.ActiveOrders, Is.Empty);
        }

        [Test, Requirement("FR-ORDER-001"), Tc("ORDER-002")]
        public void Tick_첫_주문_지연_8초가_되면_주문이_하나_생긴다()
        {
            var snap = Difficulty.Snapshot();
            var board = NewBoard();
            board.Prime(8f, snap);

            board.Tick(8f, snap, true);

            Assert.That(board.ActiveOrders.Count, Is.EqualTo(1));
        }

        [Test, Requirement("FR-ORDER-001"), Tc("ORDER-003")]
        public void Prime_지연이_0이면_즉시_하나_띄우고_다음_틱에_또_뜨지_않는다()
        {
            var snap = Difficulty.Snapshot(interval: 28f);
            var board = NewBoard();

            board.Prime(0f, snap);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(1));

            board.Tick(1f, snap, true);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(1));
        }

        [Test, Requirement("FR-ORDER-002"), Tc("ORDER-004")]
        public void Tick_스폰_간격이_지나면_다음_주문이_생긴다()
        {
            var snap = Difficulty.Snapshot(interval: 28f);
            var board = NewBoard();
            board.Prime(0f, snap);

            board.Tick(27.5f, snap, true);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(1), "간격 직전");

            board.Tick(0.5f, snap, true);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(2), "간격 도달");
        }

        // ---------- 동시 주문 상한 ----------

        [Test, Requirement("FR-ORDER-003"), Tc("ORDER-005")]
        public void Tick_활성_주문이_상한_3건이면_스폰_간격이_지나도_새_주문이_생기지_않는다()
        {
            var board = BoardWithThreeOrders(out var snap);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(3));

            board.Tick(28f, snap, true);

            Assert.That(board.ActiveOrders.Count, Is.EqualTo(3));
        }

        [Test, Requirement("FR-ORDER-003"), Tc("ORDER-006")]
        public void Tick_상한으로_보류된_뒤_자리가_나면_1초_후_다시_시도해_생성한다()
        {
            var board = BoardWithThreeOrders(out var snap);
            board.Tick(28f, snap, true);                 // 가득 찬 상태에서 보류 -> 재시도 타이머 1초
            board.Complete(board.ActiveOrders[0]);       // 자리가 하나 난다

            board.Tick(0.5f, snap, true);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(2), "0.5초 후에는 아직");

            board.Tick(0.5f, snap, true);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(3), "1초 후 재시도 성공");
        }

        // ---------- 제한시간 / 만료 ----------

        [Test, Requirement("FR-ORDER-004"), Tc("ORDER-007")]
        public void Tick_주문의_남은_시간이_경과한_만큼_줄어든다()
        {
            var snap = Difficulty.Snapshot(interval: 1000f);
            var board = NewBoard();
            board.Prime(0f, snap);
            var order = board.ActiveOrders[0];   // 175 * 1.0 = 175초

            board.Tick(10f, snap, true);

            Check.Approx(order.RemainingSeconds, 165f, Tolerance.Default);
        }

        [Test, Requirement("FR-ORDER-004"), Tc("ORDER-008")]
        public void Tick_한참_지나도_남은_시간은_0_아래로_내려가지_않는다()
        {
            var snap = Difficulty.Snapshot(interval: 1000f);
            var board = NewBoard(Menu.Quick(10f));
            board.Prime(0f, snap);
            var order = board.ActiveOrders[0];

            board.Tick(100f, snap, true);

            Assert.That(order.RemainingSeconds, Is.EqualTo(0f));
            Assert.That(order.IsExpired, Is.True);
        }

        [Test, Requirement("FR-ORDER-005"), Tc("ORDER-009")]
        public void Tick_남은_시간이_0점25초일_때는_주문이_아직_유효하다()
        {
            var snap = Difficulty.Snapshot(interval: 1000f);
            var board = NewBoard(Menu.Quick(10f));
            board.Prime(0f, snap);

            board.Tick(9.75f, snap, true);

            Assert.That(board.ActiveOrders.Count, Is.EqualTo(1));
            Check.Approx(board.ActiveOrders[0].RemainingSeconds, 0.25f, Tolerance.Default);
        }

        [Test, Requirement("FR-ORDER-005"), Tc("ORDER-010")]
        public void Tick_남은_시간이_정확히_0이_되면_주문이_제거된다()
        {
            var snap = Difficulty.Snapshot(interval: 1000f);
            var board = NewBoard(Menu.Quick(10f));
            board.Prime(0f, snap);
            board.Tick(9.75f, snap, true);

            board.Tick(0.25f, snap, true);

            Assert.That(board.ActiveOrders, Is.Empty);
        }

        [Test, Requirement("FR-ORDER-005"), Tc("ORDER-011")]
        public void Tick_시간_만료로_제거되면_이유가_Expired로_통지된다()
        {
            var snap = Difficulty.Snapshot(interval: 1000f);
            var board = NewBoard(Menu.Quick(10f));
            var reasons = new List<OrderRemovalReason>();
            board.OrderRemoved += (o, r) => reasons.Add(r);
            board.Prime(0f, snap);

            board.Tick(10f, snap, true);

            Assert.That(reasons, Is.EqualTo(new[] { OrderRemovalReason.Expired }));
        }

        [Test, Requirement("FR-SCORE-001"), Tc("ORDER-012")]
        public void Complete_활성_주문을_완료하면_제거되고_이유가_Served로_통지된다()
        {
            var snap = Difficulty.Snapshot(interval: 1000f);
            var board = NewBoard();
            var reasons = new List<OrderRemovalReason>();
            board.OrderRemoved += (o, r) => reasons.Add(r);
            board.Prime(0f, snap);

            var result = board.Complete(board.ActiveOrders[0]);

            Assert.That(result, Is.True);
            Assert.That(board.ActiveOrders, Is.Empty);
            Assert.That(reasons, Is.EqualTo(new[] { OrderRemovalReason.Served }));
        }

        [Test, Requirement("FR-ROUND-002"), Tc("ORDER-013")]
        public void Clear_활성_주문을_모두_제거하고_이유는_Cleared다()
        {
            var board = BoardWithThreeOrders(out var snap);
            var reasons = new List<OrderRemovalReason>();
            board.OrderRemoved += (o, r) => reasons.Add(r);

            board.Clear();

            Assert.That(board.ActiveOrders, Is.Empty);
            Assert.That(reasons, Is.EqualTo(new[] { OrderRemovalReason.Cleared, OrderRemovalReason.Cleared, OrderRemovalReason.Cleared }));
        }

        [Test, Requirement("FR-ORDER-006"), Tc("ORDER-014")]
        public void Tick_스폰이_허용되지_않으면_간격이_지나도_주문이_생기지_않는다()
        {
            var snap = Difficulty.Snapshot(interval: 28f);
            var board = NewBoard();
            board.Prime(0f, snap);

            board.Tick(28f, snap, false);

            Assert.That(board.ActiveOrders.Count, Is.EqualTo(1));
        }

        // ---------- 변형 ----------

        [Test, Requirement("FR-ORDER-007"), Tc("ORDER-015")]
        public void 변형_확률이_0이면_50건을_만들어도_변형이_붙지_않는다()
        {
            var snap = Difficulty.Snapshot(interval: 1f, modifierChance: 0f, maxActive: 1000);
            var board = NewBoard(Menu.DungeonBurger(), new SeededRandom(42));
            var all = new List<Order>();
            board.OrderAdded += all.Add;

            board.Prime(0f, snap);
            new VirtualClock().RunFor(49f, 1f, dt => board.Tick(dt, snap, true));

            Assert.That(all.Count, Is.EqualTo(50));
            foreach (var order in all) Assert.That(order.HasModifiers, Is.False);
        }

        [Test, Requirement("FR-ORDER-008"), Tc("ORDER-016")]
        public void 변형_확률_굴림이_성공하면_추가_변형_1건이_붙는다()
        {
            // 난수: 메뉴 0번, 확률 굴림 0.0(성공), 추가/제거 굴림 0.0(< 0.5 이므로 추가), 추가 후보 0번(치즈)
            var random = new FakeRandom().WithInts(0, 0).WithFloats(0f, 0f);
            var board = NewBoard(Menu.DungeonBurger(), random);

            board.Prime(0f, Difficulty.Snapshot(modifierChance: 1f));
            var order = board.ActiveOrders[0];

            Assert.That(order.Modifiers.Count, Is.EqualTo(1));
            Assert.That(order.Modifiers[0].Kind, Is.EqualTo(ModifierKind.Add));
            Assert.That(order.Modifiers[0].Target, Is.EqualTo(Ingredients.CheeseChopped));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("ORDER-017")]
        public void 변형_추가_제거_굴림이_0점5_이상이면_제거_변형이_붙는다()
        {
            // 난수: 메뉴 0번, 확률 굴림 0.0, 추가/제거 굴림 0.9(>= 0.5 이므로 제거), 제거 후보 0번(양상추)
            var random = new FakeRandom().WithInts(0, 0).WithFloats(0f, 0.9f);
            var board = NewBoard(Menu.DungeonBurger(), random);

            board.Prime(0f, Difficulty.Snapshot(modifierChance: 1f));
            var order = board.ActiveOrders[0];

            Assert.That(order.Modifiers.Count, Is.EqualTo(1));
            Assert.That(order.Modifiers[0].Kind, Is.EqualTo(ModifierKind.Remove));
            Assert.That(order.Modifiers[0].Target, Is.EqualTo(Ingredients.LettuceChopped));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("ORDER-018")]
        public void 변형_확률_굴림이_확률과_같으면_변형이_붙지_않는다()
        {
            var random = new FakeRandom().WithInts(0).WithFloats(0.25f);
            var board = NewBoard(Menu.DungeonBurger(), random);

            board.Prime(0f, Difficulty.Snapshot(modifierChance: 0.25f));

            Assert.That(board.ActiveOrders[0].HasModifiers, Is.False);
        }

        [Test, Requirement("FR-ORDER-008"), Tc("ORDER-019")]
        public void 변형_확률_굴림이_확률보다_조금_작으면_변형이_붙는다()
        {
            var random = new FakeRandom().WithInts(0).WithFloats(0.2499f, 0f);
            var board = NewBoard(Menu.DungeonBurger(), random);

            board.Prime(0f, Difficulty.Snapshot(modifierChance: 0.25f));

            Assert.That(board.ActiveOrders[0].HasModifiers, Is.True);
        }

        [Test, Requirement("FR-ORDER-009"), Tc("ORDER-020")]
        public void 변형이_붙으면_제한시간은_1점12배_보상은_5_늘어난다()
        {
            var random = new FakeRandom().WithInts(0, 0).WithFloats(0f, 0f);
            var board = NewBoard(Menu.DungeonBurger(), random);

            board.Prime(0f, Difficulty.Snapshot(timeScale: 1f, modifierChance: 1f));
            var order = board.ActiveOrders[0];

            Check.Approx(order.TimeLimitSeconds, 196f, Tolerance.Loose);   // 175 x 1.12
            Assert.That(order.Reward, Is.EqualTo(27));                              // 22 + 5
        }

        [Test, Requirement("FR-ORDER-009"), Tc("ORDER-021")]
        public void 변형이_붙으면_난이도_시간배율에_1점12배가_곱해진다()
        {
            var random = new FakeRandom().WithInts(0, 0).WithFloats(0f, 0f);
            var board = NewBoard(Menu.DungeonBurger(), random);

            board.Prime(0f, Difficulty.Snapshot(timeScale: 0.72f, modifierChance: 1f));

            Check.Approx(board.ActiveOrders[0].TimeLimitSeconds, 141.12f, Tolerance.Loose);   // 175 x 0.72 x 1.12
        }

        [Test, Requirement("FR-ORDER-002"), Tc("ORDER-022")]
        public void 변형이_없으면_제한시간은_기본값_곱하기_시간배율이고_보상은_기본_보상이다()
        {
            var board = NewBoard();

            board.Prime(0f, Difficulty.Snapshot(timeScale: 0.72f, modifierChance: 0f));
            var order = board.ActiveOrders[0];

            Check.Approx(order.TimeLimitSeconds, 126f, Tolerance.Loose);   // 175 x 0.72
            Assert.That(order.Reward, Is.EqualTo(22));
        }

        [Test, Requirement("FR-ORDER-010"), Tc("ORDER-023")]
        public void 던전_버거에서_제거_변형은_양상추_토마토만_대상으로_하고_빵_패티는_제외한다()
        {
            var snap = Difficulty.Snapshot(interval: 1f, modifierChance: 1f, maxActive: 1000);
            var board = NewBoard(Menu.DungeonBurger(), new SeededRandom(1234));
            var all = new List<Order>();
            board.OrderAdded += all.Add;

            board.Prime(0f, snap);
            new VirtualClock().RunFor(499f, 1f, dt => board.Tick(dt, snap, true));

            var removeCount = 0;
            var addCount = 0;
            foreach (var order in all)
            {
                foreach (var m in order.Modifiers)
                {
                    if (m.Kind == ModifierKind.Add) { addCount++; continue; }

                    removeCount++;
                    Assert.That(m.Target.Type, Is.Not.EqualTo(IngredientType.Bun));
                    Assert.That(m.Target.Type, Is.Not.EqualTo(IngredientType.Patty));
                    Assert.That(m.Target == Ingredients.LettuceChopped || m.Target == Ingredients.TomatoChopped, Is.True);
                }
            }

            Assert.That(all.Count, Is.EqualTo(500));
            Assert.That(removeCount, Is.GreaterThan(0), "제거 변형이 한 번도 나오지 않으면 이 시험은 의미가 없다");
            Assert.That(addCount, Is.GreaterThan(0));
        }

        [Test, Requirement("FR-ORDER-010"), Tc("ORDER-024")]
        public void 제거_가능_재료가_없는_레시피는_변형이_붙어도_항상_추가_변형이다()
        {
            var snap = Difficulty.Snapshot(interval: 1f, modifierChance: 1f, maxActive: 1000);
            var board = NewBoard(Menu.GoblinCheeseburger(), new SeededRandom(99));
            var all = new List<Order>();
            board.OrderAdded += all.Add;

            board.Prime(0f, snap);
            new VirtualClock().RunFor(199f, 1f, dt => board.Tick(dt, snap, true));

            Assert.That(all.Count, Is.EqualTo(200));
            foreach (var order in all)
            {
                Assert.That(order.Modifiers.Count, Is.EqualTo(1));
                Assert.That(order.Modifiers[0].Kind, Is.EqualTo(ModifierKind.Add));
            }
        }

        // ---------- 매칭 ----------

        [Test, Requirement("FR-ORDER-011"), Tc("ORDER-025")]
        public void FindMatch_같은_구성이_여럿이면_남은_시간이_가장_적은_주문을_고른다()
        {
            // 먼저 뜬 주문 A(남은 147초)보다, 나중에 떠서 시간배율이 작았던 주문 B(남은 87.5초)가 더 급하다.
            var board = NewBoard();
            board.Prime(0f, Difficulty.Snapshot(interval: 28f, timeScale: 1f));
            var a = board.ActiveOrders[0];

            board.Tick(28f, Difficulty.Snapshot(interval: 28f, timeScale: 0.5f), true);
            var b = board.ActiveOrders[1];

            Assert.That(b.RemainingSeconds, Is.LessThan(a.RemainingSeconds));
            Assert.That(board.FindMatch(PlateOf.Burger()), Is.SameAs(b));
        }

        [Test, Requirement("FR-ORDER-011"), Tc("ORDER-026")]
        public void FindMatch_가장_급한_주문을_완료하면_다음으로_급한_주문이_골라진다()
        {
            var board = NewBoard();
            board.Prime(0f, Difficulty.Snapshot(interval: 28f, timeScale: 1f));
            var a = board.ActiveOrders[0];
            board.Tick(28f, Difficulty.Snapshot(interval: 28f, timeScale: 0.5f), true);
            var b = board.ActiveOrders[1];

            board.Complete(b);

            Assert.That(board.FindMatch(PlateOf.Burger()), Is.SameAs(a));
        }

        [Test, Requirement("FR-SCORE-002"), Tc("ORDER-027")]
        public void FindMatch_재료가_하나_부족한_접시는_일치하지_않는다()
        {
            var board = NewBoard();
            board.Prime(0f, Difficulty.Snapshot());
            var missingTomato = PlateOf.Of(Ingredients.Bun, Ingredients.PattyCooked, Ingredients.LettuceChopped);

            Assert.That(board.FindMatch(missingTomato), Is.Null);
        }

        [Test, Requirement("FR-SCORE-002"), Tc("ORDER-028")]
        public void FindMatch_재료가_하나_더_들어간_접시는_일치하지_않는다()
        {
            var board = NewBoard();
            board.Prime(0f, Difficulty.Snapshot());
            var extraCheese = PlateOf.Of(Ingredients.Bun, Ingredients.PattyCooked, Ingredients.LettuceChopped,
                Ingredients.TomatoChopped, Ingredients.CheeseChopped);

            Assert.That(board.FindMatch(extraCheese), Is.Null);
        }

        [Test, Requirement("FR-SCORE-002"), Tc("ORDER-029")]
        public void FindMatch_가공_상태가_다른_재료가_섞인_접시는_일치하지_않는다()
        {
            var board = NewBoard();
            board.Prime(0f, Difficulty.Snapshot());
            var rawLettuce = PlateOf.Of(Ingredients.Bun, Ingredients.PattyCooked, Ingredients.LettuceRaw, Ingredients.TomatoChopped);
            var burnedPatty = PlateOf.Of(Ingredients.Bun, Ingredients.PattyBurned, Ingredients.LettuceChopped, Ingredients.TomatoChopped);

            Assert.That(board.FindMatch(rawLettuce), Is.Null, "손질 안 된 양상추");
            Assert.That(board.FindMatch(burnedPatty), Is.Null, "탄 패티");
        }

        [Test, Requirement("FR-SCORE-003"), Tc("ORDER-030")]
        public void FindMatch_null_또는_빈_접시는_일치하는_주문이_없다()
        {
            var board = NewBoard();
            board.Prime(0f, Difficulty.Snapshot());

            Assert.That(board.FindMatch(null), Is.Null);
            Assert.That(board.FindMatch(new IngredientMultiset()), Is.Null);
        }

        // ---------- 방어 ----------

        [Test, Requirement("FR-SCORE-001"), Tc("ORDER-031")]
        public void Complete_null_없는_주문_이미_완료한_주문은_false를_돌려준다()
        {
            var board = NewBoard();
            board.Prime(0f, Difficulty.Snapshot());
            var order = board.ActiveOrders[0];

            Assert.That(board.Complete(null), Is.False);
            Assert.That(board.Complete(order), Is.True);
            Assert.That(board.Complete(order), Is.False, "같은 주문 두 번째 완료");
        }

        [Test, Requirement("FR-ORDER-002"), Tc("ORDER-032")]
        public void 생성자는_빈_메뉴와_null_난수를_거부한다()
        {
            Assert.Throws<System.ArgumentException>(() => new OrderBoard(new RecipeData[0], new FakeRandom()));
            Assert.Throws<System.ArgumentException>(() => new OrderBoard(null, new FakeRandom()));
            Assert.Throws<System.ArgumentNullException>(() => new OrderBoard(new[] { Menu.DungeonBurger() }, null));
        }

        [Test, Requirement("FR-ORDER-004"), Tc("ORDER-033")]
        public void Tick_경과_시간이_0_이하이면_아무것도_변하지_않는다()
        {
            var snap = Difficulty.Snapshot(interval: 28f);
            var board = NewBoard();
            board.Prime(0f, snap);
            var order = board.ActiveOrders[0];

            board.Tick(0f, snap, true);
            board.Tick(-100f, snap, true);

            Check.Approx(order.RemainingSeconds, 175f, Tolerance.Default);
            board.Tick(27.5f, snap, true);
            Assert.That(board.ActiveOrders.Count, Is.EqualTo(1), "음수 틱이 스폰 타이머를 되돌리지 않았다");
        }

        [Test, Requirement("FR-ORDER-002"), Tc("ORDER-034")]
        public void 주문_번호는_1부터_차례로_늘어나고_생성마다_통지된다()
        {
            var snap = Difficulty.Snapshot(interval: 28f);
            var board = NewBoard();
            var added = new List<Order>();
            board.OrderAdded += added.Add;

            board.Prime(0f, snap);
            board.Tick(28f, snap, true);
            board.Tick(28f, snap, true);

            Assert.That(added.Count, Is.EqualTo(3));
            Assert.That(added[0].Id, Is.EqualTo(1));
            Assert.That(added[1].Id, Is.EqualTo(2));
            Assert.That(added[2].Id, Is.EqualTo(3));
        }
    }
}
