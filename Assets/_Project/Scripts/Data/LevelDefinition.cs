using System.Collections.Generic;
using HellsKitchen.Core.Progression;
using HellsKitchen.Core.Recipes;
using HellsKitchen.Core.Scoring;
using UnityEngine;

namespace HellsKitchen.Data
{
    /// <summary>
    /// 스테이지 하나의 설정. 라운드 길이, 나오는 메뉴, 난이도 곡선, 별 기준을 담는다.
    /// 새 스테이지를 만든다는 것은 이 에셋을 하나 더 만드는 일이 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Hell's Kitchen/스테이지", fileName = "Level_")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("식별")]
        public string DisplayName = "던전 1층 - 모험가 식당";

        [Header("라운드")]
        [Tooltip("한 판의 길이(초)")]
        [Min(30f)] public float DurationSeconds = 180f;

        [Tooltip("라운드 시작 후 첫 주문까지의 여유 시간")]
        [Min(0f)] public float FirstOrderDelay = 3f;

        [Header("메뉴")]
        public List<RecipeDefinition> Menu = new List<RecipeDefinition>();

        [Header("난이도")]
        public DifficultySettings Difficulty = new DifficultySettings();

        [Header("보상 규칙")]
        public ScoreRules ScoreRules = new ScoreRules();

        [Header("별 기준")]
        public StarThresholds Stars = new StarThresholds();

        [Header("난수")]
        [Tooltip("0이면 매번 다른 주문 순서. 값을 넣으면 항상 같은 순서가 나와 테스트에 쓰기 좋다.")]
        public int RandomSeed = 0;

        /// <summary>Core 가 이해하는 형태의 메뉴 목록.</summary>
        public List<RecipeData> BuildMenu()
        {
            var list = new List<RecipeData>(Menu.Count);
            for (int i = 0; i < Menu.Count; i++)
            {
                if (Menu[i] == null) continue;
                list.Add(Menu[i].ToRecipeData());
            }
            return list;
        }
    }
}
