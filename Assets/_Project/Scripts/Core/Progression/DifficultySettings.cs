using System;

namespace HellsKitchen.Core.Progression
{
    /// <summary>
    /// 라운드가 진행될수록 압박을 올리는 곡선.
    /// 값 자체는 ScriptableObject 에서 조정하고, 계산은 여기서만 한다.
    ///
    /// 압박을 주는 축은 세 가지다.
    ///  - 주문이 더 자주 들어온다 (spawn interval 감소)
    ///  - 주문 하나당 시간이 짧아진다 (time limit 배율 감소)
    ///  - 레시피 변형이 붙기 시작한다 (modifier 확률 증가)
    /// </summary>
    [Serializable]
    public sealed class DifficultySettings
    {
        public float StartSpawnInterval = 12f;
        public float MinSpawnInterval = 5f;

        /// <summary>제한시간에 곱해지는 배율. 1 에서 시작해 MinTimeLimitScale 까지 줄어든다.</summary>
        public float MinTimeLimitScale = 0.6f;

        /// <summary>변형이 붙기 시작하는 진행도(0~1). 이 지점 전에는 기본 레시피만 나온다.</summary>
        public float ModifierUnlockProgress = 0.35f;

        /// <summary>진행도 1일 때의 변형 확률.</summary>
        public float MaxModifierChance = 0.6f;

        /// <summary>동시에 떠 있을 수 있는 주문 수.</summary>
        public int MaxActiveOrders = 4;

        /// <summary>
        /// 라운드 진행도(0~1)를 받아 그 시점의 난이도 값을 계산한다.
        /// 곡선은 선형이고, 앞부분을 조금 완만하게 하고 싶으면
        /// 여기서 progress 를 제곱하는 정도로 조정하면 된다.
        /// </summary>
        public DifficultySnapshot Evaluate(float progress)
        {
            progress = Clamp01(progress);

            var spawnInterval = Lerp(StartSpawnInterval, MinSpawnInterval, progress);
            var timeScale = Lerp(1f, MinTimeLimitScale, progress);

            float modifierChance;
            if (progress <= ModifierUnlockProgress || ModifierUnlockProgress >= 1f)
            {
                modifierChance = progress <= ModifierUnlockProgress ? 0f : MaxModifierChance;
            }
            else
            {
                var t = (progress - ModifierUnlockProgress) / (1f - ModifierUnlockProgress);
                modifierChance = Lerp(0f, MaxModifierChance, t);
            }

            return new DifficultySnapshot(spawnInterval, timeScale, modifierChance, MaxActiveOrders);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    /// <summary>특정 시점의 난이도 값 묶음.</summary>
    public readonly struct DifficultySnapshot
    {
        public readonly float SpawnInterval;
        public readonly float TimeLimitScale;
        public readonly float ModifierChance;
        public readonly int MaxActiveOrders;

        public DifficultySnapshot(float spawnInterval, float timeLimitScale, float modifierChance, int maxActiveOrders)
        {
            SpawnInterval = spawnInterval;
            TimeLimitScale = timeLimitScale;
            ModifierChance = modifierChance;
            MaxActiveOrders = maxActiveOrders;
        }
    }
}
