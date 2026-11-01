using System.Collections.Generic;
using HellsKitchen.Core.Orders;
using HellsKitchen.Core.Progression;
using HellsKitchen.Core.Recipes;
using Testing.Shared.Asserts;
using Testing.Shared.Traceability;
using NUnit.Framework;

namespace HellsKitchen.Tests
{
    /// <summary>
    /// 라운드 전체(RoundSimulation = 주문판 + 점수판 + 난이도). 상태 전이·경계값 기법.
    /// 시험용 레시피(빠른 요리: 빵 + 패티(Cooked), 보상 20, 제한시간 10초)와 평탄한 난이도(간격 고정, 시간배율 1)를 쓴다.
    /// </summary>
    [TestFixture]
    public class RoundSimulationTests
    {
        private static RoundSimulation NewRound(RecipeData recipe, float duration, DifficultySettings difficulty)
            => new RoundSimulation(new[] { recipe }, duration, difficulty, null, null, new FakeRandom());

        private static IngredientMultiset QuickPlate() => PlateOf.Of(Ingredients.Bun, Ingredients.PattyCooked);
        private static IngredientMultiset WrongPlate() => PlateOf.Of(Ingredients.Bun, Ingredients.LettuceChopped);

        // ---------- 시작 / 종료 ----------

        [Test, Requirement("FR-ROUND-001"), Tc("ROUND-001")]
        public void Start_시작_전에는_Ready이고_Start하면_Running이_된다()
        {
            var round = NewRound(Menu.Quick(), 240f, Difficulty.Flat());
            Assert.That(round.State, Is.EqualTo(RoundState.Ready));

            round.Start(0f);

            Assert.That(round.State, Is.EqualTo(RoundState.Running));
            Assert.That(round.ElapsedSeconds, Is.EqualTo(0f));
            Assert.That(round.Score.Gold, Is.EqualTo(0));
        }

        [Test, Requirement("FR-ORDER-001"), Tc("ROUND-002")]
        public void Start_첫_주문_지연_8초를_주면_8초_뒤에_첫_주문이_뜬다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat(interval: 28f));
            round.Start(8f);

            round.Tick(7.5f);
            Assert.That(round.Board.ActiveOrders, Is.Empty, "7.5초");

            round.Tick(0.5f);
            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(1), "8초");
        }

        [Test, Requirement("FR-ROUND-002"), Tc("ROUND-003")]
        public void Tick_경과_239점5초에는_라운드가_계속_진행_중이다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat());
            round.Start(0f);

            round.Tick(239.5f);

            Assert.That(round.State, Is.EqualTo(RoundState.Running));
        }

        [Test, Requirement("FR-ROUND-002"), Tc("ROUND-004")]
        public void Tick_경과가_정확히_240초가_되면_라운드가_종료된다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat());
            round.Start(0f);
            round.Tick(239.5f);

            round.Tick(0.5f);

            Assert.That(round.State, Is.EqualTo(RoundState.Finished));
            Assert.That(round.ElapsedSeconds, Is.EqualTo(240f));
        }

        [Test, Requirement("FR-ROUND-002"), Tc("ROUND-005")]
        public void Finish_종료하면_활성_주문이_모두_사라지고_만료_페널티는_없다()
        {
            var round = NewRound(Menu.Quick(10f), 240f, Difficulty.Flat(interval: 5f));
            round.Start(0f);
            round.SubmitPlate(QuickPlate());   // 골드 30
            round.Tick(5f);                    // 새 주문 하나
            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(1));

            round.Finish();

            Assert.That(round.Board.ActiveOrders, Is.Empty);
            Assert.That(round.Score.ExpiredCount, Is.EqualTo(0));
            Assert.That(round.Score.Gold, Is.EqualTo(30));
        }

        [Test, Requirement("FR-ROUND-003"), Tc("ROUND-006")]
        public void Finish_종료_통지는_한_번만_가고_요약에_최종_성적이_담긴다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat(interval: 10f));
            var summaries = new List<RoundSummary>();
            round.RoundFinished += summaries.Add;
            round.Start(0f);
            round.SubmitPlate(QuickPlate());   // 30
            round.Tick(10f);
            round.SubmitPlate(QuickPlate());   // 20 x 1.5 x 1.1 = 33 -> 합계 63

            round.Finish();
            round.Finish();

            Assert.That(summaries.Count, Is.EqualTo(1));
            Assert.That(summaries[0].Gold, Is.EqualTo(63));
            Assert.That(summaries[0].Stars, Is.EqualTo(1));       // 60 이상, 110 미만
            Assert.That(summaries[0].Served, Is.EqualTo(2));
            Assert.That(summaries[0].BestCombo, Is.EqualTo(2));
        }

        [Test, Requirement("FR-ROUND-003"), Tc("ROUND-007")]
        public void Tick_종료_뒤에는_시간이_더_흐르지도_주문이_생기지도_않는다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat(interval: 5f));
            round.Start(0f);
            round.Finish();

            round.Tick(100f);

            Assert.That(round.ElapsedSeconds, Is.EqualTo(240f));
            Assert.That(round.Board.ActiveOrders, Is.Empty);
        }

        [Test, Requirement("FR-ROUND-003"), Tc("ROUND-008")]
        public void SubmitPlate_종료_뒤에_제출해도_점수는_변하지_않는다()
        {
            // 스펙 FR-ROUND-003 "시뮬레이션 일시정지"의 해석: 종료 뒤 제출은 점수에 영향이 없어야 한다.
            var round = NewRound(Menu.Quick(10f), 240f, Difficulty.Flat(interval: 1000f));
            round.Start(0f);
            round.SubmitPlate(QuickPlate());   // 골드 30
            round.Finish();

            round.SubmitPlate(WrongPlate());

            Assert.That(round.Score.Gold, Is.EqualTo(30));
        }

        [Test, Requirement("FR-ROUND-004"), Tc("ROUND-009")]
        public void Start_종료_뒤_다시_시작하면_점수와_시간이_초기화되고_주문이_새로_뜬다()
        {
            var round = NewRound(Menu.Quick(10f), 240f, Difficulty.Flat(interval: 1000f));
            round.Start(0f);
            round.SubmitPlate(QuickPlate());
            round.Finish();

            round.Start(0f);

            Assert.That(round.State, Is.EqualTo(RoundState.Running));
            Assert.That(round.ElapsedSeconds, Is.EqualTo(0f));
            Assert.That(round.Score.Gold, Is.EqualTo(0));
            Assert.That(round.Score.ServedCount, Is.EqualTo(0));
            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(1));
        }

        [Test, Requirement("FR-ROUND-001"), Tc("ROUND-010")]
        public void Start_진행_중에_다시_호출해도_무시된다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat(interval: 1000f));
            round.Start(0f);
            round.Tick(10f);

            round.Start(0f);

            Assert.That(round.ElapsedSeconds, Is.EqualTo(10f));
            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(1));
        }

        [Test, Requirement("FR-ROUND-001"), Tc("ROUND-011")]
        public void Tick_시작_전에는_시간이_흐르지_않고_진행도는_절반_지점에서_0점5다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat());

            round.Tick(50f);
            Assert.That(round.ElapsedSeconds, Is.EqualTo(0f), "Ready 상태에서는 무시");

            round.Start(0f);
            round.Tick(120f);
            Check.Approx(round.Progress01, 0.5f, Tolerance.Tight);
        }

        // ---------- 종료 임박 시 주문 생성 중단 (FR-ORDER-006) ----------

        [Test, Requirement("FR-ORDER-006"), Tc("ROUND-012")]
        public void Tick_잔여_15점5초에는_새_주문이_생성된다()
        {
            // 라운드 100초, 시간배율 1 -> 임계값 15초. 경과 84.5초 = 잔여 15.5초.
            var round = NewRound(Menu.Quick(1000f), 100f, Difficulty.Flat(interval: 10f));
            round.Start(0f);

            round.Tick(84.5f);

            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(2));
        }

        [Test, Requirement("FR-ORDER-006"), Tc("ROUND-013")]
        public void Tick_잔여_14점5초에는_새_주문이_생성되지_않는다()
        {
            var round = NewRound(Menu.Quick(1000f), 100f, Difficulty.Flat(interval: 10f));
            round.Start(0f);

            round.Tick(85.5f);

            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(1));
        }

        [Test, Requirement("FR-ORDER-006"), Tc("ROUND-014")]
        public void Tick_잔여가_정확히_15초일_때는_스펙대로_새_주문이_생성된다()
        {
            // 스펙 FR-ORDER-006: "라운드 잔여 < 15 x 시간배율" 일 때만 중단. 15초 '정확히'는 중단 조건이 아니다.
            var round = NewRound(Menu.Quick(1000f), 100f, Difficulty.Flat(interval: 10f));
            round.Start(0f);

            round.Tick(85f);

            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(2));
        }

        // ---------- 제출 판정 (FR-SCORE-001~004) ----------

        [Test, Requirement("FR-SCORE-001"), Tc("SCORE-014")]
        public void SubmitPlate_주문과_일치하면_성공_보상이_지급되고_주문이_사라진다()
        {
            var round = NewRound(Menu.Quick(10f, 20), 240f, Difficulty.Flat(interval: 1000f));
            round.Start(0f);

            var result = round.SubmitPlate(QuickPlate());

            Assert.That(result.Outcome, Is.EqualTo(ServeOutcome.Success));
            Assert.That(result.GoldDelta, Is.EqualTo(30));              // 20 x 1.5
            Check.Approx(result.Punctuality, 1f, Tolerance.Default);
            Assert.That(round.Score.Gold, Is.EqualTo(30));
            Assert.That(round.Score.Combo, Is.EqualTo(1));
            Assert.That(round.Board.ActiveOrders, Is.Empty);
        }

        [Test, Requirement("FR-SCORE-002"), Tc("SCORE-015")]
        public void SubmitPlate_일치하는_주문이_없으면_골드_5가_줄고_콤보가_0이_되며_주문은_남는다()
        {
            var round = NewRound(Menu.Quick(10f, 20), 240f, Difficulty.Flat(interval: 5f));
            round.Start(0f);
            round.SubmitPlate(QuickPlate());   // 골드 30, 콤보 1
            round.Tick(5f);                    // 새 주문

            var result = round.SubmitPlate(WrongPlate());

            Assert.That(result.Outcome, Is.EqualTo(ServeOutcome.Mismatch));
            Assert.That(result.GoldDelta, Is.EqualTo(-5));
            Assert.That(round.Score.Gold, Is.EqualTo(25));
            Assert.That(round.Score.Combo, Is.EqualTo(0));
            Assert.That(round.Score.MismatchCount, Is.EqualTo(1));
            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(1), "주문은 그대로 남는다");
        }

        [Test, Requirement("FR-SCORE-003"), Tc("SCORE-016")]
        public void SubmitPlate_빈_접시나_null은_무효이고_점수와_콤보가_변하지_않는다()
        {
            var round = NewRound(Menu.Quick(10f, 20), 240f, Difficulty.Flat(interval: 5f));
            round.Start(0f);
            round.SubmitPlate(QuickPlate());   // 골드 30, 콤보 1
            round.Tick(5f);

            var empty = round.SubmitPlate(new IngredientMultiset());
            var nul = round.SubmitPlate(null);

            Assert.That(empty.Outcome, Is.EqualTo(ServeOutcome.EmptyPlate));
            Assert.That(nul.Outcome, Is.EqualTo(ServeOutcome.EmptyPlate));
            Assert.That(empty.GoldDelta, Is.EqualTo(0));
            Assert.That(round.Score.Gold, Is.EqualTo(30));
            Assert.That(round.Score.Combo, Is.EqualTo(1));
            Assert.That(round.Board.ActiveOrders.Count, Is.EqualTo(1));
        }

        [Test, Requirement("FR-SCORE-004"), Tc("SCORE-017")]
        public void Tick_주문이_만료되면_골드_10이_줄고_콤보가_0이_된다()
        {
            var round = NewRound(Menu.Quick(10f, 20), 240f, Difficulty.Flat(interval: 5f));
            round.Start(0f);
            round.SubmitPlate(QuickPlate());   // 골드 30, 콤보 1
            round.Tick(5f);                    // 두 번째 주문(제한 10초)

            round.Tick(10f);                   // 두 번째 주문 만료

            Assert.That(round.Score.Gold, Is.EqualTo(20));
            Assert.That(round.Score.Combo, Is.EqualTo(0));
            Assert.That(round.Score.ExpiredCount, Is.EqualTo(1));
        }

        // ---------- 통지 ----------

        [Test, Requirement("FR-ORDER-002"), Tc("ROUND-015")]
        public void 주문이_생길_때마다_OrderSpawned가_통지된다()
        {
            var round = NewRound(Menu.Quick(1000f), 240f, Difficulty.Flat(interval: 10f));
            var spawned = new List<Order>();
            round.OrderSpawned += spawned.Add;

            round.Start(0f);
            round.Tick(10f);

            Assert.That(spawned.Count, Is.EqualTo(2));
        }

        [Test, Requirement("FR-ORDER-005"), Tc("ROUND-016")]
        public void 주문이_만료되면_OrderExpired가_통지된다()
        {
            var round = NewRound(Menu.Quick(10f), 240f, Difficulty.Flat(interval: 1000f));
            var expired = new List<Order>();
            round.OrderExpired += expired.Add;
            round.Start(0f);

            round.Tick(10f);

            Assert.That(expired.Count, Is.EqualTo(1));
        }

        [Test, Requirement("FR-SCORE-001"), Tc("ROUND-018")]
        public void 완료한_주문은_시간이_지나도_OrderExpired가_통지되지_않는다()
        {
            var round = NewRound(Menu.Quick(10f), 240f, Difficulty.Flat(interval: 1000f));
            var expired = new List<Order>();
            round.OrderExpired += expired.Add;
            round.Start(0f);
            round.SubmitPlate(QuickPlate());

            round.Tick(100f);

            Assert.That(expired, Is.Empty);
        }

        [Test, Requirement("FR-SCORE-001"), Tc("ROUND-017")]
        public void 제출할_때마다_PlateServed가_결과_종류대로_통지된다()
        {
            var round = NewRound(Menu.Quick(10f), 240f, Difficulty.Flat(interval: 5f));
            var outcomes = new List<ServeOutcome>();
            round.PlateServed += r => outcomes.Add(r.Outcome);
            round.Start(0f);

            round.SubmitPlate(QuickPlate());
            round.SubmitPlate(WrongPlate());
            round.SubmitPlate(new IngredientMultiset());

            Assert.That(outcomes, Is.EqualTo(new[] { ServeOutcome.Success, ServeOutcome.Mismatch, ServeOutcome.EmptyPlate }));
        }
    }
}
