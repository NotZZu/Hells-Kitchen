using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using HellsKitchen.Data;
using UnityEditor;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// 재료 정의, 카탈로그, 레시피, 스테이지 에셋을 한 번에 만들어 준다.
    ///
    /// 이미 있는 에셋은 덮어쓰지 않고 비어 있는 칸만 채운다.
    /// 그래야 인스펙터에서 밸런스를 손본 뒤 툴을 다시 돌려도 값이 날아가지 않는다.
    /// </summary>
    public static class HellsKitchenDataSetup
    {
        public const string DataRoot = "Assets/_Project/Data";
        private const string IngredientFolder = DataRoot + "/Ingredients";
        private const string RecipeFolder = DataRoot + "/Recipes";
        private const string LevelFolder = DataRoot + "/Levels";

        public const string CatalogPath = DataRoot + "/IngredientCatalog.asset";
        public const string BurgerRecipePath = RecipeFolder + "/Recipe_DungeonBurger.asset";
        public const string CheeseburgerRecipePath = RecipeFolder + "/Recipe_GoblinCheeseburger.asset";
        public const string LevelPath = LevelFolder + "/Level_Dungeon01.asset";

        [MenuItem("Hell's Kitchen/1. 데이터 에셋 생성", priority = 10)]
        public static void CreateAll()
        {
            EditorAssetUtility.EnsureFolder(IngredientFolder);
            EditorAssetUtility.EnsureFolder(RecipeFolder);
            EditorAssetUtility.EnsureFolder(LevelFolder);

            var definitions = CreateIngredients();
            var catalog = CreateCatalog(definitions);
            var recipes = CreateRecipes();
            CreateLevel(recipes);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Hell's Kitchen] 데이터 에셋 생성 완료. 재료 {definitions.Count}종, 레시피 {recipes.Count}종.\n" +
                      $"카탈로그: {catalog.name}");
        }

        private static List<IngredientDefinition> CreateIngredients()
        {
            // 접시 위 쌓는 순서(sortOrder)가 곧 버거의 단면이 된다.
            // 아래에서부터 빵 - 패티 - 양상추 - 토마토 - 양파 - 치즈 - 윗빵.
            //
            // 손질 모델이 두 개로 나뉜다.
            //   chopping = 도마 위에서 썰리는 중의 흩어진 모습
            //   chopped  = 손질을 마친 완성형(슬라이스, 링). 버거에 올라가는 것은 이쪽이다.

            var bun = Define(IngredientType.Bun, "빵",
                raw: "food_ingredient_bun",
                chopping: null, chopped: null, cooked: null, burned: null,
                stackHeight: 0.18f,
                sortOrder: 0,
                // 손에 들 때는 통짜 빵, 접시에 담기면 아래빵으로 바뀌고
                // 그 위에 윗빵이 덮인다.
                plateModel: "food_ingredient_bun_bottom",
                topperModel: "food_ingredient_bun_top");

            // 패티는 상자에서 나오지 않는다. 고기를 도마에서 다져야 비로소 생긴다.
            var patty = Define(IngredientType.Patty, "패티",
                raw: "food_ingredient_burger_uncooked",
                chopping: null, chopped: null,
                cooked: "food_ingredient_burger_cooked",
                burned: "food_ingredient_burger_trash",
                stackHeight: 0.18f,
                sortOrder: 10,
                cookable: true,
                cookSeconds: 6f,
                burnSeconds: 7f);

            // 고기는 굽는 재료가 아니라 다지는 재료다.
            // 다지면 생패티가 되고, 그 패티를 화로에 올려야 먹을 수 있는 상태가 된다.
            // 손질은 짧게(2초 미만) 끝나고, 시간이 걸리는 쪽은 화로다.
            // 손이 묶이는 도마와 손을 뗄 수 있는 화로의 역할을 나누기 위해서다.
            var meat = Define(IngredientType.Steak, "고기",
                raw: "food_ingredient_steak",
                chopping: "food_ingredient_steak_pieces",
                chopped: null, cooked: null, burned: null,
                stackHeight: 0.16f,
                sortOrder: 10,
                choppable: true,
                chopSeconds: 1.9f);

            // 다지기 결과를 연결한다. 패티 정의가 먼저 있어야 해서 여기서 묶는다.
            if (meat.ChopResult != patty)
            {
                meat.ChopResult = patty;
                EditorUtility.SetDirty(meat);
            }

            var lettuce = Define(IngredientType.Lettuce, "양상추",
                raw: "food_ingredient_lettuce",
                chopping: "food_ingredient_lettuce_chopped",
                chopped: "food_ingredient_lettuce_slice",
                cooked: null, burned: null,
                stackHeight: 0.085f,
                sortOrder: 20,
                choppable: true,
                chopSeconds: 1.5f);

            var tomato = Define(IngredientType.Tomato, "토마토",
                raw: "food_ingredient_tomato",
                chopping: "food_ingredient_tomato_slices",
                chopped: "food_ingredient_tomato_slice",
                cooked: null, burned: null,
                stackHeight: 0.095f,
                sortOrder: 30,
                choppable: true,
                chopSeconds: 1.6f);

            var onion = Define(IngredientType.Onion, "양파",
                raw: "food_ingredient_onion",
                chopping: "food_ingredient_onion_chopped",
                chopped: "food_ingredient_onion_rings",
                cooked: null, burned: null,
                stackHeight: 0.10f,
                sortOrder: 40,
                choppable: true,
                chopSeconds: 1.7f);

            var cheese = Define(IngredientType.Cheese, "치즈",
                raw: "food_ingredient_cheese",
                chopping: "food_ingredient_cheese_chopped",
                chopped: "food_ingredient_cheese_slice",
                cooked: null, burned: null,
                stackHeight: 0.10f,
                sortOrder: 50,
                choppable: true,
                chopSeconds: 1.4f);

            return new List<IngredientDefinition> { bun, meat, patty, lettuce, tomato, onion, cheese };
        }

        private static IngredientDefinition Define(
            IngredientType type, string displayName,
            string raw, string chopping, string chopped, string cooked, string burned,
            float stackHeight,
            int sortOrder,
            string plateModel = null,
            string topperModel = null,
            bool choppable = false,
            bool cookable = false,
            float chopSeconds = 2.5f,
            float cookSeconds = 6f,
            float burnSeconds = 6f,
            float displayScale = 1.5f)
        {
            var path = $"{IngredientFolder}/Ingredient_{type}.asset";
            var definition = EditorAssetUtility.LoadOrCreate<IngredientDefinition>(path);

            definition.Type = type;
            if (string.IsNullOrEmpty(definition.DisplayName)) definition.DisplayName = displayName;

            // 모델 참조는 매번 최신 값으로 덮어쓴다.
            //
            // "비어 있을 때만 채운다"로 두었더니, 모델 슬롯을 재배치했을 때
            // 예전 값이 그대로 남아 손질 중과 손질 완료가 같은 모델을 가리키는
            // 일이 있었다. 어느 모델을 쓸지는 이 툴이 정하는 것이 맞다.
            definition.RawModel = Resolve(raw, definition.RawModel);
            definition.ChoppingModel = Resolve(chopping, definition.ChoppingModel);
            definition.ChoppedModel = Resolve(chopped, definition.ChoppedModel);
            definition.CookedModel = Resolve(cooked, definition.CookedModel);
            definition.BurnedModel = Resolve(burned, definition.BurnedModel);
            definition.PlateModel = Resolve(plateModel, definition.PlateModel);
            definition.PlateTopperModel = Resolve(topperModel, definition.PlateTopperModel);

            definition.PlateStackHeight = stackHeight;
            definition.PlateSortOrder = sortOrder;
            definition.Choppable = choppable;
            definition.Cookable = cookable;
            definition.ChopSeconds = chopSeconds;
            definition.CookSeconds = cookSeconds;
            definition.BurnSeconds = burnSeconds;

            // 이전 버전에서 만들어진 에셋은 배율이 0으로 남아 있을 수 있다.
            // 배율은 툴을 돌릴 때마다 최신 값으로 맞춘다.
            // 접시 배율과 어긋나면 재료가 접시 밖으로 삐져나온다.
            definition.DisplayScale = displayScale;

            EditorUtility.SetDirty(definition);
            return definition;
        }

        /// <summary>
        /// 지정한 모델 이름을 불러온다. 이름이 없으면(해당 단계를 쓰지 않는 재료면)
        /// 슬롯을 비워 둔다. 예전 값이 남아 잘못된 모델이 표시되는 것을 막기 위해서다.
        /// </summary>
        private static GameObject Resolve(string modelName, GameObject current)
        {
            if (string.IsNullOrEmpty(modelName)) return null;

            var loaded = KayKitPaths.Restaurant(modelName);
            return loaded != null ? loaded : current;
        }

        private static IngredientCatalog CreateCatalog(List<IngredientDefinition> definitions)
        {
            var catalog = EditorAssetUtility.LoadOrCreate<IngredientCatalog>(CatalogPath);
            catalog.EditorSetDefinitions(definitions);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static List<RecipeDefinition> CreateRecipes()
        {
            var burger = EditorAssetUtility.LoadOrCreate<RecipeDefinition>(BurgerRecipePath);
            if (string.IsNullOrEmpty(burger.Id))
            {
                burger.Id = "dungeon_burger";
                burger.DisplayName = "던전 버거";

                burger.Ingredients = new List<IngredientRef>
                {
                    new IngredientRef(IngredientType.Bun, ProcessState.Raw),
                    new IngredientRef(IngredientType.Patty, ProcessState.Cooked),
                    new IngredientRef(IngredientType.Lettuce, ProcessState.Chopped),
                    new IngredientRef(IngredientType.Tomato, ProcessState.Chopped),
                };

                burger.OptionalAdditions = new List<IngredientRef>
                {
                    new IngredientRef(IngredientType.Cheese, ProcessState.Chopped),
                    new IngredientRef(IngredientType.Onion, ProcessState.Chopped),
                };

                // 빵과 패티는 빠지면 버거가 아니므로 제외 후보에 넣지 않는다.
                burger.RemovableIngredients = new List<IngredientRef>
                {
                    new IngredientRef(IngredientType.Lettuce, ProcessState.Chopped),
                    new IngredientRef(IngredientType.Tomato, ProcessState.Chopped),
                };

                burger.CompletedModel = KayKitPaths.Restaurant("food_burger");
            }

            // 보상과 제한시간은 밸런스 값이라 툴을 돌릴 때마다 최신으로 맞춘다.
            // 고기를 다지고 굽는 두 공정이 추가됐고 구역 사이를 오가야 하므로
            // 시간을 넉넉히 준다.
            burger.BaseReward = 22;
            burger.BaseTimeLimitSeconds = 175f;
            EditorUtility.SetDirty(burger);

            var cheeseburger = EditorAssetUtility.LoadOrCreate<RecipeDefinition>(CheeseburgerRecipePath);
            if (string.IsNullOrEmpty(cheeseburger.Id))
            {
                cheeseburger.Id = "goblin_cheeseburger";
                cheeseburger.DisplayName = "고블린 치즈버거";

                cheeseburger.Ingredients = new List<IngredientRef>
                {
                    new IngredientRef(IngredientType.Bun, ProcessState.Raw),
                    new IngredientRef(IngredientType.Patty, ProcessState.Cooked),
                    new IngredientRef(IngredientType.Cheese, ProcessState.Chopped),
                };

                cheeseburger.OptionalAdditions = new List<IngredientRef>
                {
                    new IngredientRef(IngredientType.Onion, ProcessState.Chopped),
                    new IngredientRef(IngredientType.Tomato, ProcessState.Chopped),
                    new IngredientRef(IngredientType.Lettuce, ProcessState.Chopped),
                };

                cheeseburger.RemovableIngredients = new List<IngredientRef>();
                cheeseburger.CompletedModel = KayKitPaths.Restaurant("food_burger");
            }

            cheeseburger.BaseReward = 18;
            cheeseburger.BaseTimeLimitSeconds = 145f;
            EditorUtility.SetDirty(cheeseburger);

            return new List<RecipeDefinition> { burger, cheeseburger };
        }

        private static LevelDefinition CreateLevel(List<RecipeDefinition> recipes)
        {
            var level = EditorAssetUtility.LoadOrCreate<LevelDefinition>(LevelPath);

            if (level.Menu == null || level.Menu.Count == 0)
            {
                level.DisplayName = "던전 1층 - 모험가 식당";
                level.Menu = new List<RecipeDefinition>(recipes);
                level.RandomSeed = 0;
            }

            // 아래 값들은 밸런스라 툴을 돌릴 때마다 최신으로 맞춘다.
            level.DurationSeconds = 240f;
            level.FirstOrderDelay = 8f;

            // 주문이 몰아치면 무엇을 먼저 할지 판단할 틈이 없다.
            // 한 건을 여유 있게 끝내고 다음을 받는 간격으로 벌려 둔다.
            level.Difficulty.StartSpawnInterval = 28f;
            level.Difficulty.MinSpawnInterval = 14f;
            level.Difficulty.MaxActiveOrders = 3;
            level.Difficulty.MinTimeLimitScale = 0.72f;

            EditorUtility.SetDirty(level);
            return level;
        }
    }
}
