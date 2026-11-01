using System;
using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using HellsKitchen.Core.Progression;
using HellsKitchen.Core.Recipes;
using HellsKitchen.Core.Utility;
using Testing.Shared.Builders;
using Testing.Shared.Fakes;

namespace HellsKitchen.Tests
{
    /// <summary>
    /// 테스트 전용 데이터와 도우미. FUNCTIONAL_SPEC.md 4.2(레시피), 4.3(난이도 곡선)의 값을 그대로 옮겼다.
    /// 게임 코드는 수정하지 않고, 테스트 어셈블리 안에서만 쓴다.
    /// </summary>
    internal static class Ingredients
    {
        public static readonly IngredientStack Bun = new IngredientStack(IngredientType.Bun, ProcessState.Raw);
        public static readonly IngredientStack PattyCooked = new IngredientStack(IngredientType.Patty, ProcessState.Cooked);
        public static readonly IngredientStack PattyBurned = new IngredientStack(IngredientType.Patty, ProcessState.Burned);
        public static readonly IngredientStack LettuceChopped = new IngredientStack(IngredientType.Lettuce, ProcessState.Chopped);
        public static readonly IngredientStack LettuceRaw = new IngredientStack(IngredientType.Lettuce, ProcessState.Raw);
        public static readonly IngredientStack TomatoChopped = new IngredientStack(IngredientType.Tomato, ProcessState.Chopped);
        public static readonly IngredientStack OnionChopped = new IngredientStack(IngredientType.Onion, ProcessState.Chopped);
        public static readonly IngredientStack CheeseChopped = new IngredientStack(IngredientType.Cheese, ProcessState.Chopped);
    }

    /// <summary>레시피 빌더. 기본값은 시험용 레시피(빵 + 패티(Cooked), 보상 20, 제한시간 10초)다.</summary>
    internal sealed class RecipeBuilder : Builder<RecipeBuilder, RecipeData>
    {
        public string Id = "quick";
        public string Name = "빠른 요리";
        public IngredientStack[] Required = { Ingredients.Bun, Ingredients.PattyCooked };
        public int Reward = 20;
        public float TimeLimit = 10f;
        public IngredientStack[] Additions;
        public IngredientStack[] Removables;

        protected override RecipeData Create() => new RecipeData(
            Id, Name, Required, baseReward: Reward, baseTimeLimitSeconds: TimeLimit,
            optionalAdditions: Additions, removableIngredients: Removables);
    }

    internal static class Menu
    {
        /// <summary>던전 버거: 빵 + 패티(Cooked) + 양상추 + 토마토. 기본 보상 22, 제한시간 175s.</summary>
        public static RecipeData DungeonBurger() => new RecipeBuilder().With(b =>
        {
            b.Id = "dungeon_burger"; b.Name = "던전 버거";
            b.Required = new[] { Ingredients.Bun, Ingredients.PattyCooked, Ingredients.LettuceChopped, Ingredients.TomatoChopped };
            b.Reward = 22; b.TimeLimit = 175f;
            b.Additions = new[] { Ingredients.CheeseChopped, Ingredients.OnionChopped };
            b.Removables = new[] { Ingredients.LettuceChopped, Ingredients.TomatoChopped };
        }).Build();

        /// <summary>고블린 치즈버거: 빵 + 패티(Cooked) + 치즈. 기본 보상 18, 제한시간 145s. 제거 가능 재료 없음.</summary>
        public static RecipeData GoblinCheeseburger() => new RecipeBuilder().With(b =>
        {
            b.Id = "goblin_cheeseburger"; b.Name = "고블린 치즈버거";
            b.Required = new[] { Ingredients.Bun, Ingredients.PattyCooked, Ingredients.CheeseChopped };
            b.Reward = 18; b.TimeLimit = 145f;
            b.Additions = new[] { Ingredients.OnionChopped, Ingredients.TomatoChopped, Ingredients.LettuceChopped };
        }).Build();

        /// <summary>제한시간을 짧게 둔 시험용 레시피(만료 경계 시험).</summary>
        public static RecipeData Quick(float timeLimit = 10f, int reward = 20)
            => new RecipeBuilder().With(b => { b.TimeLimit = timeLimit; b.Reward = reward; }).Build();
    }

    /// <summary>난이도 빌더. 기본값은 FUNCTIONAL_SPEC 4.3 스테이지 값이다.</summary>
    internal sealed class DifficultyBuilder : Builder<DifficultyBuilder, DifficultySettings>
    {
        public float StartInterval = 28f;
        public float MinInterval = 14f;
        public float MinTimeScale = 0.72f;
        public float ModifierUnlock = 0.35f;
        public float MaxModifierChance = 0.6f;
        public int MaxActive = 3;

        protected override DifficultySettings Create() => new DifficultySettings
        {
            StartSpawnInterval = StartInterval,
            MinSpawnInterval = MinInterval,
            MinTimeLimitScale = MinTimeScale,
            ModifierUnlockProgress = ModifierUnlock,
            MaxModifierChance = MaxModifierChance,
            MaxActiveOrders = MaxActive,
        };
    }

    internal static class Difficulty
    {
        /// <summary>FUNCTIONAL_SPEC 4.3 스테이지 값: 간격 28→14, 시간배율 1→0.72, 변형 0.35 이후 최대 0.6, 동시 주문 3.</summary>
        public static DifficultySettings Spec() => new DifficultyBuilder().Build();

        /// <summary>시간 흐름과 무관하게 값이 변하지 않는 설정(라운드 시험을 단순화하기 위함).</summary>
        public static DifficultySettings Flat(float interval = 10f, int maxActive = 3) => new DifficultyBuilder().With(b =>
        {
            b.StartInterval = interval; b.MinInterval = interval;
            b.MinTimeScale = 1f; b.MaxModifierChance = 0f; b.MaxActive = maxActive;
        }).Build();

        public static DifficultySnapshot Snapshot(float interval = 28f, float timeScale = 1f, float modifierChance = 0f, int maxActive = 3)
            => new DifficultySnapshot(interval, timeScale, modifierChance, maxActive);
    }

    /// <summary>
    /// 미리 정해 둔 값을 순서대로 돌려주는 난수 공급자. 값이 바닥나면 마지막 값을 반복한다.
    /// 값 공급은 Testing.Shared.Fakes.ScriptedSequence 가 맡고, 여기서는 IRandomSource 모양만 맞춘다.
    /// </summary>
    internal sealed class FakeRandom : IRandomSource
    {
        private readonly ScriptedSequence<int> _ints = new ScriptedSequence<int>();
        private readonly ScriptedSequence<float> _floats = new ScriptedSequence<float>();

        public FakeRandom WithInts(params int[] values) { _ints.Enqueue(values); return this; }
        public FakeRandom WithFloats(params float[] values) { _floats.Enqueue(values); return this; }

        public int NextInt(int exclusiveMax)
        {
            var value = _ints.Next();   // 범위가 0 이하여도 값은 하나 소비한다(기존 동작 유지)
            return exclusiveMax <= 0 ? 0 : Math.Min(value, exclusiveMax - 1);
        }

        public float NextFloat() => _floats.Next();
    }

    internal static class PlateOf
    {
        public static IngredientMultiset Burger() => new IngredientMultiset(new[]
        {
            Ingredients.Bun, Ingredients.PattyCooked, Ingredients.LettuceChopped, Ingredients.TomatoChopped,
        });

        public static IngredientMultiset Of(params IngredientStack[] items) => new IngredientMultiset(items);
    }
}
