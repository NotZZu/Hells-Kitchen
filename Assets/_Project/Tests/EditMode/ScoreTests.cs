using HellsKitchen.Core.Scoring;
using Testing.Shared.Asserts;
using Testing.Shared.Traceability;
using NUnit.Framework;

namespace HellsKitchen.Tests
{
    /// <summary>
    /// 보상·페널티·콤보(ScoreRules / ScoreBoard)와 별점(StarThresholds). 경계값·상태 전이 기법.
    /// </summary>
    [TestFixture]
    public class ScoreTests
    {
        /// <summary>정확히 gold 만큼 가진 점수판을 만든다(팁·콤보 가산을 꺼서 지급액이 기본 보상 그대로가 되게 한다).</summary>
        private static ScoreBoard BoardWithGold(int gold, ScoreRules rules = null)
        {
            var r = rules ?? new ScoreRules { MaxTipRatio = 0f, ComboStepMultiplier = 0f };
            var board = new ScoreBoard(r);
            if (gold > 0) board.RegisterSuccess(gold, 0f);
            return board;
        }

        // ---------- 보상 계산 (FR-SCORE-005) ----------

        [TestCase(22, 1.0f, 0, 33)]    // 22 x 1.5 x 1.0
        [TestCase(22, 0.0f, 0, 22)]    // 팁 없음
        [TestCase(18, 0.5f, 0, 23)]    // 18 x 1.25 = 22.5 -> 반올림 23 (0.5는 올림)
        [TestCase(22, 0.5f, 1, 30)]    // 22 x 1.25 x 1.1 = 30.25
        [TestCase(22, 0.0f, 3, 29)]    // 22 x 1.0 x 1.3 = 28.6
        [TestCase(22, 1.0f, 10, 66)]   // 22 x 1.5 x 2.0 (콤보 상한)
        [Requirement("FR-SCORE-005"), Tc("SCORE-001")]
        public void CalculateReward_기본보상_팁_콤보_공식대로_계산한다(int baseReward, float punctuality, int combo, int expected)
        {
            Assert.That(new ScoreRules().CalculateReward(baseReward, punctuality, combo), Is.EqualTo(expected));
        }

        [TestCase(1.5f, 33)]
        [TestCase(100f, 33)]
        [TestCase(-0.5f, 22)]
        [TestCase(-100f, 22)]
        [Requirement("FR-SCORE-005"), Tc("SCORE-002")]
        public void CalculateReward_남은시간_비율은_0과_1로_보정한다(float punctuality, int expected)
        {
            Assert.That(new ScoreRules().CalculateReward(22, punctuality, 0), Is.EqualTo(expected));
        }

        // ---------- 콤보 배율 (FR-SCORE-006) ----------

        [TestCase(0, 1.0f)]
        [TestCase(1, 1.1f)]
        [TestCase(5, 1.5f)]
        [TestCase(9, 1.9f)]
        [TestCase(10, 2.0f)]
        [TestCase(11, 2.0f)]
        [TestCase(100, 2.0f)]
        [TestCase(-3, 1.0f)]
        [Requirement("FR-SCORE-006"), Tc("SCORE-003")]
        public void GetComboMultiplier_연속_성공수에_따라_배율이_늘고_상한은_2점0이다(int combo, float expected)
        {
            Check.Approx(new ScoreRules().GetComboMultiplier(combo), expected, Tolerance.Tight);
        }

        // ---------- 점수판 상태 전이 ----------

        [Test, Requirement("FR-SCORE-001"), Tc("SCORE-004")]
        public void RegisterSuccess_골드_성공수_콤보가_함께_늘어난다()
        {
            var board = new ScoreBoard(new ScoreRules());

            var reward = board.RegisterSuccess(22, 1f);

            Assert.That(reward, Is.EqualTo(33));
            Assert.That(board.Gold, Is.EqualTo(33));
            Assert.That(board.ServedCount, Is.EqualTo(1));
            Assert.That(board.Combo, Is.EqualTo(1));
            Assert.That(board.BestCombo, Is.EqualTo(1));
        }

        [Test, Requirement("FR-SCORE-006"), Tc("SCORE-005")]
        public void RegisterSuccess_연속으로_성공하면_두번째_보상에_콤보_배율이_붙는다()
        {
            var board = new ScoreBoard(new ScoreRules());

            board.RegisterSuccess(22, 1f);                   // 33
            var second = board.RegisterSuccess(22, 1f);      // 33 x 1.1 = 36.3 -> 36

            Assert.That(second, Is.EqualTo(36));
            Assert.That(board.Gold, Is.EqualTo(69));
        }

        [Test, Requirement("FR-SCORE-004"), Tc("SCORE-006")]
        public void RegisterExpired_골드가_10_줄고_콤보가_0이_된다()
        {
            var board = BoardWithGold(50, new ScoreRules());
            board.RegisterSuccess(0, 0f);   // 콤보 1 로 만들기(보상 0)
            var before = board.Gold;

            var delta = board.RegisterExpired();

            Assert.That(delta, Is.EqualTo(-10));
            Assert.That(board.Gold, Is.EqualTo(before - 10));
            Assert.That(board.Combo, Is.EqualTo(0));
            Assert.That(board.ExpiredCount, Is.EqualTo(1));
        }

        [Test, Requirement("FR-SCORE-002"), Tc("SCORE-007")]
        public void RegisterMismatch_골드가_5_줄고_콤보가_0이_된다()
        {
            var board = BoardWithGold(50, new ScoreRules());
            board.RegisterSuccess(0, 0f);
            var before = board.Gold;

            var delta = board.RegisterMismatch();

            Assert.That(delta, Is.EqualTo(-5));
            Assert.That(board.Gold, Is.EqualTo(before - 5));
            Assert.That(board.Combo, Is.EqualTo(0));
            Assert.That(board.MismatchCount, Is.EqualTo(1));
        }

        // 골드, 실패 종류(M=오배송 5, E=만료 10), 기대 변화량, 기대 골드
        [TestCase(5, "M", -5, 0)]    // 정확히 0이 되는 경계
        [TestCase(4, "M", -4, 0)]    // 페널티가 보유 골드보다 큼
        [TestCase(0, "M", 0, 0)]     // 이미 0
        [TestCase(10, "E", -10, 0)]
        [TestCase(7, "E", -7, 0)]
        [TestCase(0, "E", 0, 0)]
        [TestCase(11, "E", -10, 1)]  // 하한에 걸리지 않는 경계 바로 위
        [Requirement("FR-SCORE-007"), Tc("SCORE-008")]
        public void 페널티는_골드를_0_아래로_내리지_않는다(int gold, string kind, int expectedDelta, int expectedGold)
        {
            var board = BoardWithGold(gold);

            var delta = kind == "M" ? board.RegisterMismatch() : board.RegisterExpired();

            Assert.That(delta, Is.EqualTo(expectedDelta));
            Assert.That(board.Gold, Is.EqualTo(expectedGold));
        }

        [Test, Requirement("FR-SCORE-006"), Tc("SCORE-009")]
        public void 연속_성공_뒤_실패하면_콤보는_0이_되지만_최고_콤보는_유지된다()
        {
            var board = new ScoreBoard(new ScoreRules());
            board.RegisterSuccess(20, 1f);
            board.RegisterSuccess(20, 1f);
            board.RegisterSuccess(20, 1f);

            board.RegisterMismatch();

            Assert.That(board.Combo, Is.EqualTo(0));
            Assert.That(board.BestCombo, Is.EqualTo(3));
        }

        [Test, Requirement("FR-ROUND-004"), Tc("SCORE-010")]
        public void Reset_모든_수치가_0으로_돌아간다()
        {
            var board = new ScoreBoard(new ScoreRules());
            board.RegisterSuccess(20, 1f);
            board.RegisterMismatch();
            board.RegisterExpired();

            board.Reset();

            Assert.That(board.Gold, Is.EqualTo(0));
            Assert.That(board.ServedCount, Is.EqualTo(0));
            Assert.That(board.ExpiredCount, Is.EqualTo(0));
            Assert.That(board.MismatchCount, Is.EqualTo(0));
            Assert.That(board.Combo, Is.EqualTo(0));
            Assert.That(board.BestCombo, Is.EqualTo(0));
        }

        [Test, Requirement("FR-SCORE-001"), Tc("SCORE-011")]
        public void 점수판_생성자는_null_규칙을_거부한다()
        {
            Assert.Throws<System.ArgumentNullException>(() => new ScoreBoard(null));
        }

        // ---------- 별점 (SPEC 4.4) ----------

        [TestCase(-5, 0)]
        [TestCase(0, 0)]
        [TestCase(59, 0)]
        [TestCase(60, 1)]
        [TestCase(109, 1)]
        [TestCase(110, 2)]
        [TestCase(169, 2)]
        [TestCase(170, 3)]
        [TestCase(1000, 3)]
        [Requirement("SPEC-4.4"), Tc("SCORE-012")]
        public void StarThresholds_골드에_따라_0에서_3성을_판정하고_경계값은_높은_쪽이다(int gold, int stars)
        {
            Assert.That(new StarThresholds().Evaluate(gold), Is.EqualTo(stars));
        }

        [TestCase(0, 0f)]
        [TestCase(30, 0.5f)]
        [TestCase(60, 0f)]
        [TestCase(85, 0.5f)]
        [TestCase(110, 0f)]
        [TestCase(140, 0.5f)]
        [TestCase(170, 1f)]
        [TestCase(500, 1f)]
        [Requirement("SPEC-4.4"), Tc("SCORE-013")]
        public void StarThresholds_다음_별까지의_진행률을_0에서_1로_돌려준다(int gold, float progress)
        {
            Check.Approx(new StarThresholds().ProgressToNextStar(gold), progress, Tolerance.Tight);
        }
    }
}
