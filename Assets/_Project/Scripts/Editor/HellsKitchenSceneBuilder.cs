using System.Collections.Generic;
using HellsKitchen.Core.Ingredients;
using HellsKitchen.Data;
using HellsKitchen.Runtime.CameraRig;
using HellsKitchen.Runtime.Items;
using HellsKitchen.Runtime.Session;
using HellsKitchen.Runtime.Stations;
using HellsKitchen.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// 플레이 가능한 주방 씬을 통째로 만든다.
    ///
    /// 설비를 한 덩어리로 몰지 않고 세 구역으로 떼어 놓는다.
    /// 재료를 집는 곳, 손질하고 굽는 곳, 내주는 곳이 물리적으로 떨어져 있어야
    /// "무엇을 먼저 가지러 갈까"라는 판단이 생기고, 그 판단이 이 장르의 재미다.
    ///
    ///                    [서빙]
    ///              접시  주문창구  쓰레기
    ///
    ///   [재료 창고]                    [조리 구역]
    ///   상자 6개(ㄷ자)                도마2 화로2 조립대2(ㄷ자)
    ///
    ///                   (플레이어 시작)
    /// </summary>
    public static class HellsKitchenSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Kitchen.unity";

        [MenuItem("Hell's Kitchen/4. 주방 씬 생성", priority = 40)]
        public static void BuildScene()
        {
            if (!EditorUtility.DisplayDialog(
                    "주방 씬 생성",
                    "새 씬을 만들어 주방을 세 구역으로 배치합니다.\n저장하지 않은 현재 씬의 변경 사항은 확인 후 처리됩니다.\n계속할까요?",
                    "생성", "취소"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var catalog = AssetDatabase.LoadAssetAtPath<IngredientCatalog>(HellsKitchenDataSetup.CatalogPath);
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(HellsKitchenDataSetup.LevelPath);

            if (catalog == null || level == null)
            {
                EditorUtility.DisplayDialog("먼저 데이터를 만들어 주세요",
                    "메뉴에서 '1. 데이터 에셋 생성' 을 먼저 실행해 주세요.", "확인");
                return;
            }

            var ingredientPrefab = AssetDatabase.LoadAssetAtPath<IngredientItem>(HellsKitchenPrefabBuilder.IngredientPrefabPath);
            var platePrefab = AssetDatabase.LoadAssetAtPath<PlateItem>(HellsKitchenPrefabBuilder.PlatePrefabPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HellsKitchenPrefabBuilder.PlayerPrefabPath);
            var ticketPrefab = AssetDatabase.LoadAssetAtPath<OrderTicketView>(HellsKitchenPrefabBuilder.TicketPrefabPath);
            var progressPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HellsKitchenPrefabBuilder.ProgressPrefabPath);
            var labelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HellsKitchenPrefabBuilder.LabelPrefabPath);

            if (ingredientPrefab == null || platePrefab == null || playerPrefab == null)
            {
                EditorUtility.DisplayDialog("먼저 프리팹을 만들어 주세요",
                    "메뉴에서 '2. 프리팹 생성' 을 먼저 실행해 주세요.", "확인");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            var session = BuildSystems(level, ingredientPrefab, platePrefab, catalog);

            // 조리대 폭을 재서 모든 간격의 기준으로 삼는다. 에셋을 바꿔도 배치가 어긋나지 않는다.
            var counterModel = KayKitPaths.Restaurant("kitchencounter_straight_A");
            var counterBounds = EditorAssetUtility.MeasureBounds(counterModel);
            var unit = EditorAssetUtility.LooksReasonable(counterBounds)
                ? Mathf.Max(0.8f, counterBounds.size.x) + 0.02f
                : 1.02f;

            var layout = new KitchenLayout(unit);

            BuildEnvironment(layout);
            BuildStations(session, catalog, layout, counterModel, progressPrefab, labelPrefab);
            BuildPlayer(playerPrefab, layout);
            BuildCamera(layout);

            HellsKitchenUiBuilder.Build(session, catalog, ticketPrefab);

            EditorAssetUtility.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();

            Debug.Log($"[Hell's Kitchen] 주방 씬을 만들었습니다: {ScenePath}\n" +
                      $"설비 한 칸 {unit:0.00} / 구역 간격 {layout.SideOffset:0.00} / 카메라 거리 {layout.CameraDistance:0.00}\n" +
                      "WASD 이동, E 집기/놓기, Space 썰기.");
        }

        // ------------------------------------------------------------------ 배치 계산

        /// <summary>
        /// 주방 전체 치수를 한곳에서 계산한다.
        ///
        /// 주의: KayKit Restaurant 팩은 1유닛이 아니라 2유닛 그리드다.
        /// 조리대 폭이 2, 바닥 타일이 4다. 그래서 Unit(설비 한 칸)을
        /// 구역 사이 거리에 그대로 배수로 쓰면 주방이 순식간에 수십 미터가 된다.
        /// 구역 간격은 "ㄷ자 하나가 차지하는 폭"을 기준으로 잡는다.
        /// </summary>
        private class KitchenLayout
        {
            public readonly float Unit;

            public KitchenLayout(float unit)
            {
                Unit = unit;
            }

            /// <summary>ㄷ자 구역 하나가 차지하는 폭. 뒤 2칸 + 좌우 날개 1칸씩.</summary>
            public float ZoneWidth => Unit * 3f;

            /// <summary>
            /// 좌우 구역 중심의 x 좌표.
            /// 두 구역이 서로 닿지 않을 만큼만 벌린다. 더 벌리면 왕복이 지루해진다.
            /// </summary>
            public float SideOffset => ZoneWidth * 0.72f;

            public Vector3 PantryCenter => new Vector3(-SideOffset, 0f, -Unit * 0.8f);
            public Vector3 CookingCenter => new Vector3(SideOffset, 0f, -Unit * 0.8f);
            public Vector3 ServingCenter => new Vector3(0f, 0f, ZoneWidth * 0.88f);

            /// <summary>세 구역의 무게중심. 카메라가 바라보는 지점이다.</summary>
            public Vector3 Focus => new Vector3(0f, 0f, ZoneWidth * 0.16f);

            public Vector3 PlayerStart => new Vector3(0f, 0.1f, 0f);

            /// <summary>바닥과 경계벽이 덮어야 할 반경.</summary>
            public float FloorReach => SideOffset + ZoneWidth * 0.9f;

            /// <summary>
            /// 카메라 거리. 캐릭터(높이 약 2.5)가 화면에서 너무 작아지지 않으면서
            /// 세 구역이 한 화면에 들어오는 지점.
            /// </summary>
            public float CameraDistance => ZoneWidth * 2.6f;
        }

        /// <summary>설비 한 자리. 위치와 바라보는 방향.</summary>
        private readonly struct Slot
        {
            public readonly Vector3 Position;
            public readonly float Yaw;

            public Slot(Vector3 position, float yaw)
            {
                Position = position;
                Yaw = yaw;
            }
        }

        /// <summary>
        /// ㄷ자 모양으로 자리를 만든다. 열린 쪽이 openYaw 방향을 향하고,
        /// 모든 설비는 그 열린 공간(=플레이어가 서는 곳)을 바라본다.
        /// </summary>
        private static List<Slot> BuildUSlots(Vector3 center, float openYaw, float spacing, int backCount, int wingCount)
        {
            var slots = new List<Slot>(backCount + wingCount * 2);
            var rotation = Quaternion.Euler(0f, openYaw, 0f);

            // 로컬 좌표계에서는 열린 쪽이 +Z 다.
            var halfWidth = (backCount - 1) * 0.5f * spacing;
            var backZ = -(wingCount + 1) * 0.5f * spacing;

            for (int i = 0; i < backCount; i++)
            {
                var local = new Vector3(-halfWidth + i * spacing, 0f, backZ);
                slots.Add(new Slot(center + rotation * local, openYaw));
            }

            for (int i = 0; i < wingCount; i++)
            {
                var z = backZ + spacing * (i + 1);

                var left = new Vector3(-halfWidth - spacing, 0f, z);
                slots.Add(new Slot(center + rotation * left, openYaw + 90f));

                var right = new Vector3(halfWidth + spacing, 0f, z);
                slots.Add(new Slot(center + rotation * right, openYaw + 270f));
            }

            return slots;
        }

        // ------------------------------------------------------------------ 조명

        private static void BuildLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.89f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.45f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.33f, 0.31f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.17f, 0.18f);
        }

        // ------------------------------------------------------------------ 시스템

        private static GameSession BuildSystems(
            LevelDefinition level, IngredientItem ingredientPrefab, PlateItem platePrefab, IngredientCatalog catalog)
        {
            var systems = new GameObject("Systems");

            var sessionGo = new GameObject("GameSession");
            sessionGo.transform.SetParent(systems.transform, false);
            var session = sessionGo.AddComponent<GameSession>();
            session.EditorSetLevel(level);

            var factoryGo = new GameObject("ItemFactory");
            factoryGo.transform.SetParent(systems.transform, false);
            var factory = factoryGo.AddComponent<ItemFactory>();
            factory.EditorConfigure(ingredientPrefab, platePrefab, catalog);

            return session;
        }

        // ------------------------------------------------------------------ 환경

        private static void BuildEnvironment(KitchenLayout layout)
        {
            var root = new GameObject("Environment");

            var floorModel = KayKitPaths.Restaurant("floor_kitchen");
            var floorBounds = EditorAssetUtility.MeasureBounds(floorModel);
            var tile = Mathf.Max(0.5f, floorBounds.size.x);

            var tileCount = Mathf.CeilToInt(layout.FloorReach / tile);

            var floorRoot = new GameObject("Floor");
            floorRoot.transform.SetParent(root.transform, false);

            if (floorModel != null)
            {
                for (int x = -tileCount; x <= tileCount; x++)
                {
                    for (int z = -tileCount; z <= tileCount; z++)
                    {
                        var piece = (GameObject)PrefabUtility.InstantiatePrefab(floorModel);
                        piece.name = $"Floor_{x}_{z}";
                        piece.transform.SetParent(floorRoot.transform, false);
                        piece.transform.position = new Vector3(x * tile, 0f, z * tile);
                    }
                }
            }

            var span = (tileCount * 2 + 1) * tile;

            // 물리 바닥. 타일 모델에 콜라이더가 없어도 캐릭터가 떨어지지 않게 한다.
            var ground = new GameObject("GroundCollider");
            ground.transform.SetParent(root.transform, false);
            var groundBox = ground.AddComponent<BoxCollider>();
            groundBox.size = new Vector3(span, 0.4f, span);
            groundBox.center = new Vector3(0f, -0.2f, 0f);

            BuildBoundaryWalls(root.transform, span);
        }

        /// <summary>
        /// 주방 밖으로 걸어 나가지 못하게 막는 보이지 않는 벽.
        /// 설비가 서로 떨어져 있어 틈이 많아졌기 때문에 경계는 따로 필요하다.
        /// </summary>
        private static void BuildBoundaryWalls(Transform parent, float span)
        {
            var walls = new GameObject("Boundary");
            walls.transform.SetParent(parent, false);

            var half = span * 0.5f;
            const float thickness = 1f;
            const float height = 4f;

            var offsets = new[]
            {
                new Vector3(0f, height * 0.5f, half),
                new Vector3(0f, height * 0.5f, -half),
                new Vector3(half, height * 0.5f, 0f),
                new Vector3(-half, height * 0.5f, 0f),
            };

            var sizes = new[]
            {
                new Vector3(span + thickness, height, thickness),
                new Vector3(span + thickness, height, thickness),
                new Vector3(thickness, height, span + thickness),
                new Vector3(thickness, height, span + thickness),
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                var wall = new GameObject($"Boundary_{i}");
                wall.transform.SetParent(walls.transform, false);
                wall.transform.localPosition = offsets[i];

                var box = wall.AddComponent<BoxCollider>();
                box.size = sizes[i];
            }
        }

        // ------------------------------------------------------------------ 설비

        private static void BuildStations(
            GameSession session,
            IngredientCatalog catalog,
            KitchenLayout layout,
            GameObject counterModel,
            GameObject progressPrefab,
            GameObject labelPrefab)
        {
            var root = new GameObject("Stations");

            var unit = layout.Unit;

            // --- 재료 창고: 상자 6개를 ㄷ자로. 열린 쪽은 주방 중앙(+x) ---
            var pantry = BuildUSlots(layout.PantryCenter, 90f, unit, backCount: 2, wingCount: 2);
            var pantryRoot = NewZone(root.transform, "Zone_Pantry", layout.PantryCenter, "재료 창고", labelPrefab, unit);

            var crates = new (IngredientType type, string model)[]
            {
                (IngredientType.Bun, "crate_buns"),
                (IngredientType.Steak, "crate_steak"),
                (IngredientType.Lettuce, "crate_lettuce"),
                (IngredientType.Tomato, "crate_tomatoes"),
                (IngredientType.Cheese, "crate_cheese"),
                (IngredientType.Onion, "crate_onions"),
            };

            for (int i = 0; i < crates.Length && i < pantry.Count; i++)
            {
                PlaceCrate(pantryRoot, pantry[i], crates[i].type, crates[i].model, catalog, labelPrefab);
            }

            // --- 조리 구역: 조립대 2, 도마 2, 화로 2. 열린 쪽은 주방 중앙(-x) ---
            var cooking = BuildUSlots(layout.CookingCenter, 270f, unit, backCount: 2, wingCount: 2);
            var cookingRoot = NewZone(root.transform, "Zone_Cooking", layout.CookingCenter, "조리 구역", labelPrefab, unit);

            var cuttingBoard = KayKitPaths.Restaurant("cuttingboard");
            var stoveModel = KayKitPaths.Restaurant("stove_single_countertop");
            var panModel = KayKitPaths.Restaurant("pan_A");

            // 뒤 변에는 접시를 올려 두고 조립하는 자리.
            StationBuildUtility.Create<CounterStation>(
                "Assembly_1", cookingRoot, cooking[0].Position, cooking[0].Yaw, counterModel);
            StationBuildUtility.Create<CounterStation>(
                "Assembly_2", cookingRoot, cooking[1].Position, cooking[1].Yaw, counterModel);

            // 양 날개에 도마와 화로를 마주 보게 놓는다.
            StationBuildUtility.Create<CuttingStation>(
                "Cutting_1", cookingRoot, cooking[2].Position, cooking[2].Yaw, counterModel,
                new StationBuildUtility.Options
                {
                    TopModel = cuttingBoard,
                    ProgressPrefab = progressPrefab,
                    LabelPrefab = labelPrefab,
                    LabelText = "도마",
                });

            StationBuildUtility.Create<StoveStation>(
                "Stove_1", cookingRoot, cooking[3].Position, cooking[3].Yaw, stoveModel,
                new StationBuildUtility.Options
                {
                    TopModel = panModel,
                    ProgressPrefab = progressPrefab,
                    LabelPrefab = labelPrefab,
                    LabelText = "화로",
                });

            StationBuildUtility.Create<CuttingStation>(
                "Cutting_2", cookingRoot, cooking[4].Position, cooking[4].Yaw, counterModel,
                new StationBuildUtility.Options
                {
                    TopModel = cuttingBoard,
                    ProgressPrefab = progressPrefab,
                    LabelPrefab = labelPrefab,
                    LabelText = "도마",
                });

            StationBuildUtility.Create<StoveStation>(
                "Stove_2", cookingRoot, cooking[5].Position, cooking[5].Yaw, stoveModel,
                new StationBuildUtility.Options
                {
                    TopModel = panModel,
                    ProgressPrefab = progressPrefab,
                    LabelPrefab = labelPrefab,
                    LabelText = "화로",
                });

            // --- 서빙 구역: 주문 창구를 가운데 두고 접시와 쓰레기통 ---
            var serving = BuildUSlots(layout.ServingCenter, 180f, unit, backCount: 1, wingCount: 1);
            var servingRoot = NewZone(root.transform, "Zone_Serving", layout.ServingCenter, "주문 창구", labelPrefab, unit);

            var serve = StationBuildUtility.Create<ServeStation>(
                "ServeWindow", servingRoot, serving[0].Position, serving[0].Yaw,
                KayKitPaths.Restaurant("wall_orderwindow"));

            var dispenser = StationBuildUtility.Create<PlateDispenser>(
                "PlateDispenser", servingRoot, serving[1].Position, serving[1].Yaw,
                KayKitPaths.Restaurant("dishrack_plates"),
                new StationBuildUtility.Options { LabelPrefab = labelPrefab, LabelText = "접시" });

            StationBuildUtility.Create<TrashStation>(
                "Trash", servingRoot, serving[2].Position, serving[2].Yaw,
                KayKitPaths.Restaurant("crate"),
                new StationBuildUtility.Options { LabelPrefab = labelPrefab, LabelText = "쓰레기통" });

            serve.EditorConfigure(session, dispenser);
        }

        /// <summary>구역을 묶는 빈 오브젝트와 그 위에 뜨는 큰 이름표를 만든다.</summary>
        private static Transform NewZone(
            Transform parent, string name, Vector3 center, string label, GameObject labelPrefab, float unit)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(parent, false);

            if (labelPrefab == null || string.IsNullOrEmpty(label)) return zone.transform;

            var banner = (GameObject)PrefabUtility.InstantiatePrefab(labelPrefab);
            banner.name = "ZoneLabel";
            banner.transform.SetParent(zone.transform, false);
            banner.transform.position = center + new Vector3(0f, unit * 2.6f, 0f);
            banner.transform.localScale *= 2.2f;

            var view = banner.GetComponent<WorldLabel>();
            if (view != null) view.EditorConfigure(label, null);

            return zone.transform;
        }

        private static void PlaceCrate(
            Transform root, Slot slot, IngredientType type, string modelName,
            IngredientCatalog catalog, GameObject labelPrefab)
        {
            var definition = catalog != null ? catalog.Get(type) : null;

            var station = StationBuildUtility.Create<CrateStation>(
                $"Crate_{type}", root, slot.Position, slot.Yaw,
                KayKitPaths.Restaurant(modelName),
                new StationBuildUtility.Options
                {
                    LabelPrefab = labelPrefab,
                    LabelText = definition != null ? definition.ResolvedName : type.ToString(),
                    LabelIcon = definition != null ? definition.Icon : null,
                });

            station.EditorSetIngredient(type);
        }

        // ------------------------------------------------------------------ 플레이어와 카메라

        private static GameObject BuildPlayer(GameObject playerPrefab, KitchenLayout layout)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = layout.PlayerStart;

            // 애니메이션 설정을 아직 돌리지 않았거나 참조가 끊긴 경우를 여기서 잡는다.
            var animator = player.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Animation/PlayerAnimator.controller");

                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
                else
                {
                    Debug.LogWarning(
                        "[Hell's Kitchen] 애니메이터 컨트롤러가 아직 없습니다. " +
                        "'5. 캐릭터 애니메이션 설정' 을 실행하면 캐릭터가 움직입니다.");
                }
            }

            return player;
        }

        private static void BuildCamera(KitchenLayout layout)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.08f, 0.11f);
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 150f;

            cameraGo.AddComponent<AudioListener>();

            // URP 파이프라인 에셋의 MSAA 만으로는 로우폴리 모델의 대각선 모서리가
            // 여전히 거칠게 보인다. 카메라 단계의 SMAA 를 함께 켜 준다.
            var cameraData = cameraGo.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null) cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            cameraData.renderPostProcessing = true;

            // 세 구역이 한 화면에 다 들어오는 크기라 카메라를 따라다니게 하지 않는다.
            // 화면이 고정돼야 어느 구역이 어디 있는지 몸으로 외워진다.
            var anchor = new GameObject("CameraFocus");
            anchor.transform.position = layout.Focus;

            var rig = cameraGo.AddComponent<KitchenCameraRig>();
            rig.EditorConfigure(anchor.transform, layout.CameraDistance, pitch: 50f, yaw: 0f, follow: false);
        }

        private static void RegisterSceneInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == ScenePath) return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
