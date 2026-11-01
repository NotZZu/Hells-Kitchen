using HellsKitchen.Core.Progression;
using Testing.Shared.Asserts;
using Testing.Shared.Traceability;
using NUnit.Framework;

namespace HellsKitchen.Tests
{
    /// <summary>
    /// 난이도 곡선(DifficultySettings.Evaluate). 경계값 분석 기법.
    /// FUNCTIONAL_SPEC 4.3 스테이지 값(간격 28→14, 시간배율 1→0.72, 변형 0.35 이후 최대 0.6, 동시 주문 3)을 사용한다.
    /// </summary>
    [TestFixture]
    public class DifficultySettingsTests
    {
        private DifficultySettings _spec;

        [SetUp]
        public void SetUp() => _spec = Difficulty.Spec();

        [Test, Requirement("FR-ORDER-007"), Tc("DIFF-001")]
        public void Evaluate_진행도_0에서는_시작값이고_변형_확률은_0이다()
        {
            var s = _spec.Evaluate(0f);

            Check.Approx(s.SpawnInterval, 28f, Tolerance.Default);
            Check.Approx(s.TimeLimitScale, 1f, Tolerance.Default);
            Assert.That(s.ModifierChance, Is.EqualTo(0f));
            Assert.That(s.MaxActiveOrders, Is.EqualTo(3));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("DIFF-002")]
        public void Evaluate_진행도_1에서는_종료값이다()
        {
            var s = _spec.Evaluate(1f);

            Check.Approx(s.SpawnInterval, 14f, Tolerance.Default);
            Check.Approx(s.TimeLimitScale, 0.72f, Tolerance.Default);
            Check.Approx(s.ModifierChance, 0.6f, Tolerance.Default);
        }

        [Test, Requirement("FR-ORDER-007"), Tc("DIFF-003")]
        public void Evaluate_진행도_0점349에서는_변형_확률이_0이다()
        {
            Assert.That(_spec.Evaluate(0.349f).ModifierChance, Is.EqualTo(0f));
        }

        [Test, Requirement("FR-ORDER-007"), Tc("DIFF-004")]
        public void Evaluate_진행도_정확히_0점35에서는_변형_확률이_0이다_경계_포함()
        {
            Assert.That(_spec.Evaluate(0.35f).ModifierChance, Is.EqualTo(0f));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("DIFF-005")]
        public void Evaluate_진행도_0점351에서는_변형_확률이_0보다_크다()
        {
            // (0.351 - 0.35) / (1 - 0.35) * 0.6 = 약 0.000923
            var chance = _spec.Evaluate(0.351f).ModifierChance;

            Assert.That(chance, Is.GreaterThan(0f));
            Check.Approx(chance, 0.000923f, Tolerance.Default);
        }

        [Test, Requirement("FR-ORDER-008"), Tc("DIFF-006")]
        public void Evaluate_진행도_0점5에서는_선형_보간_값이다()
        {
            var s = _spec.Evaluate(0.5f);

            Check.Approx(s.SpawnInterval, 21f, Tolerance.Default);
            Check.Approx(s.TimeLimitScale, 0.86f, Tolerance.Default);
            Check.Approx(s.ModifierChance, 0.13846f, Tolerance.Default);
        }

        [TestCase(-0.1f, 28f, 1f, 0f)]
        [TestCase(-100f, 28f, 1f, 0f)]
        [TestCase(1.5f, 14f, 0.72f, 0.6f)]
        [TestCase(100f, 14f, 0.72f, 0.6f)]
        [Requirement("FR-ORDER-008"), Tc("DIFF-007")]
        public void Evaluate_범위_밖_진행도는_0과_1로_보정된다(float progress, float interval, float scale, float chance)
        {
            var s = _spec.Evaluate(progress);

            Check.Approx(s.SpawnInterval, interval, Tolerance.Default);
            Check.Approx(s.TimeLimitScale, scale, Tolerance.Default);
            Check.Approx(s.ModifierChance, chance, Tolerance.Default);
        }

        [TestCase(0f)]
        [TestCase(0.35f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        [Requirement("FR-ORDER-003"), Tc("DIFF-008")]
        public void Evaluate_동시_주문_상한은_진행도와_무관하게_고정이다(float progress)
        {
            Assert.That(_spec.Evaluate(progress).MaxActiveOrders, Is.EqualTo(3));
        }

        [Test, Requirement("FR-ORDER-008"), Tc("DIFF-009")]
        public void Evaluate_진행도가_늘어도_간격과_시간배율은_줄기만_하고_변형_확률은_늘기만_한다()
        {
            var prev = _spec.Evaluate(0f);
            for (int i = 1; i <= 100; i++)
            {
                var cur = _spec.Evaluate(i / 100f);

                Assert.That(cur.SpawnInterval, Is.LessThanOrEqualTo(prev.SpawnInterval + 0.00001f), "간격 @" + i);
                Assert.That(cur.TimeLimitScale, Is.LessThanOrEqualTo(prev.TimeLimitScale + 0.00001f), "시간배율 @" + i);
                Assert.That(cur.ModifierChance, Is.GreaterThanOrEqualTo(prev.ModifierChance - 0.00001f), "변형확률 @" + i);
                prev = cur;
            }
        }
    }
}
