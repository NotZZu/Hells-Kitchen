using System.Collections.Generic;
using HellsKitchen.Runtime.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// KayKit 애니메이션을 플레이어 캐릭터에 붙인다.
    ///
    /// KayKit 캐릭터와 애니메이션은 같은 뼈대(Rig_Medium)를 쓰도록 만들어져 있다.
    /// 그래서 캐릭터 모델에서 아바타를 하나 만들고, 애니메이션 파일들이
    /// 그 아바타를 그대로 복사해 쓰게 하면 리타게팅 없이 바로 맞물린다.
    /// </summary>
    public static class CharacterAnimationSetup
    {
        private const string ControllerFolder = "Assets/_Project/Animation";
        private const string ControllerPath = ControllerFolder + "/PlayerAnimator.controller";

        private const string CharacterModel = "Knight";

        /// <summary>클립이 들어 있는 애니메이션 파일들.</summary>
        private static readonly string[] AnimationFiles =
        {
            "Rig_Medium_General",
            "Rig_Medium_MovementBasic",
            "Rig_Medium_Tools",
        };

        /// <summary>계속 반복돼야 하는 클립. 나머지는 한 번만 재생된다.</summary>
        private static readonly HashSet<string> LoopingClips = new HashSet<string>
        {
            "Idle_A", "Idle_B",
            "Walking_A", "Walking_B", "Walking_C",
            "Running_A", "Running_B",
            "Holding_A", "Holding_B", "Holding_C",
            "Chopping", "Working_A", "Working_B",
        };

        // 애니메이터에서 실제로 쓰는 클립들.
        private const string IdleClip = "Idle_A";
        private const string WalkClip = "Walking_B";
        private const string RunClip = "Running_A";
        private const string CarryIdleClip = "Holding_B";
        private const string ChopClip = "Chopping";

        [MenuItem("Hell's Kitchen/5. 캐릭터 애니메이션 설정", priority = 50)]
        public static void Setup()
        {
            Debug.Log("[Hell's Kitchen] 애니메이션 설정을 시작합니다. FBX 재임포트 때문에 시간이 걸립니다.");

            var avatar = PrepareCharacterAvatar();
            if (avatar == null)
            {
                Debug.LogError("[Hell's Kitchen] 1/4 아바타 준비 실패. 여기서 중단합니다.");
                return;
            }
            Debug.Log($"[Hell's Kitchen] 1/4 아바타 준비 완료: {avatar.name}");

            var clips = PrepareAnimationClips(avatar);
            if (clips.Count == 0)
            {
                Debug.LogError(
                    "[Hell's Kitchen] 2/4 애니메이션 클립을 하나도 찾지 못했습니다.\n" +
                    $"경로를 확인해 주세요: {KayKitPaths.MediumRigAnimations}");
                return;
            }
            Debug.Log($"[Hell's Kitchen] 2/4 클립 {clips.Count}개 확보: {string.Join(", ", clips.Keys)}");

            var controller = BuildController(clips);
            if (controller == null)
            {
                Debug.LogError("[Hell's Kitchen] 3/4 애니메이터 컨트롤러 생성 실패.");
                return;
            }
            Debug.Log($"[Hell's Kitchen] 3/4 컨트롤러 생성 완료: {ControllerPath}");

            var assigned = AssignToPlayerPrefab(controller, avatar);

            // 이미 씬에 배치된 캐릭터는 프리팹을 고쳐도 갱신되지 않는 경우가 있다.
            // (프리팹 인스턴스가 해당 값을 자기 것으로 덮어쓰고 있을 때)
            // 그래서 열려 있는 씬의 캐릭터도 직접 손봐 준다.
            var refreshed = RefreshOpenScenes(controller, avatar);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (assigned)
            {
                Debug.Log($"[Hell's Kitchen] 4/4 플레이어 프리팹 연결 완료. 씬의 캐릭터 {refreshed}개도 갱신했습니다.");
            }
            else
            {
                Debug.LogError("[Hell's Kitchen] 4/4 플레이어 프리팹 연결 실패. 위 경고를 확인해 주세요.");
            }
        }

        // ------------------------------------------------------------------ 아바타

        private static Avatar PrepareCharacterAvatar()
        {
            var path = KayKitPaths.AdventurerCharacters + CharacterModel + ".fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null)
            {
                Debug.LogError($"[Hell's Kitchen] 캐릭터 모델을 찾지 못했습니다: {path}");
                return null;
            }

            // 이미 아바타가 만들어져 있으면 다시 임포트하지 않는다. 재임포트는 느리다.
            var existing = FindAvatar(path);
            if (existing != null &&
                importer.animationType == ModelImporterAnimationType.Generic &&
                importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
            {
                return existing;
            }

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();

            var avatar = FindAvatar(path);
            if (avatar == null)
            {
                Debug.LogError($"[Hell's Kitchen] 아바타 생성에 실패했습니다: {path}");
            }
            return avatar;
        }

        private static Avatar FindAvatar(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar) return avatar;
            }
            return null;
        }

        // ------------------------------------------------------------------ 클립

        private static Dictionary<string, AnimationClip> PrepareAnimationClips(Avatar avatar)
        {
            var result = new Dictionary<string, AnimationClip>();

            for (int i = 0; i < AnimationFiles.Length; i++)
            {
                var path = KayKitPaths.MediumRigAnimations + AnimationFiles[i] + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer == null)
                {
                    Debug.LogWarning($"[Hell's Kitchen] 애니메이션 파일을 찾지 못했습니다: {path}");
                    continue;
                }

                if (NeedsReimport(importer, avatar))
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;

                    // 반복돼야 하는 클립에 루프를 켠다.
                    // 이걸 빼먹으면 걷기가 한 걸음 만에 멈춰 선다.
                    var clipSettings = importer.defaultClipAnimations;
                    for (int c = 0; c < clipSettings.Length; c++)
                    {
                        if (!LoopingClips.Contains(clipSettings[c].name)) continue;
                        clipSettings[c].loopTime = true;
                    }
                    importer.clipAnimations = clipSettings;

                    importer.SaveAndReimport();
                }

                CollectClips(path, result);
            }

            return result;
        }

        private static bool NeedsReimport(ModelImporter importer, Avatar avatar)
        {
            if (importer.animationType != ModelImporterAnimationType.Generic) return true;
            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther) return true;
            if (importer.sourceAvatar != avatar) return true;

            // 루프 설정이 아직 반영되지 않았으면 다시 임포트한다.
            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0) return true;

            for (int i = 0; i < clips.Length; i++)
            {
                if (LoopingClips.Contains(clips[i].name) && !clips[i].loopTime) return true;
            }
            return false;
        }

        private static void CollectClips(string assetPath, Dictionary<string, AnimationClip> into)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (!(assets[i] is AnimationClip clip)) continue;

                // 유니티가 내부적으로 만드는 미리보기 클립은 건너뛴다.
                if (clip.name.StartsWith("__preview__")) continue;

                into[clip.name] = clip;
            }
        }

        // ------------------------------------------------------------------ 컨트롤러

        private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips)
        {
            EditorAssetUtility.EnsureFolder(ControllerFolder);

            // 구조가 바뀌었을 수 있으므로 항상 새로 만든다.
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Carrying", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Working", AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;

            // 맨손 이동: 서기 -> 걷기 -> 달리기
            var locomotion = CreateLocomotion(controller, clips, "Locomotion", Get(clips, IdleClip));

            // 들고 이동: 들고 서기 -> 걷기 -> 달리기
            // 들고 걷는 전용 클립이 없어 이동 구간은 같은 클립을 쓴다.
            // 손에 든 물건이 앞쪽 앵커에 붙어 있어서 실루엣은 충분히 구분된다.
            var carrying = CreateLocomotion(controller, clips, "Carrying", Get(clips, CarryIdleClip));

            machine.defaultState = locomotion;

            // 썰기는 어느 상태에서든 즉시 들어가야 한다.
            var chopClip = Get(clips, ChopClip);
            AnimatorState chopping = null;
            if (chopClip != null)
            {
                chopping = machine.AddState("Chopping");
                chopping.motion = chopClip;

                // 원본 클립이 느긋해서 실제 손질 속도와 따로 논다.
                // 2배로 돌리면 진행바가 차는 속도와 얼추 맞는다.
                chopping.speed = 2f;
            }

            Connect(locomotion, carrying, "Carrying", true);
            Connect(carrying, locomotion, "Carrying", false);

            if (chopping != null)
            {
                var anyToChop = machine.AddAnyStateTransition(chopping);
                anyToChop.hasExitTime = false;
                anyToChop.duration = 0.08f;
                anyToChop.canTransitionToSelf = false;
                anyToChop.AddCondition(AnimatorConditionMode.If, 0f, "Working");

                var chopToCarry = chopping.AddTransition(carrying);
                chopToCarry.hasExitTime = false;
                chopToCarry.duration = 0.12f;
                chopToCarry.AddCondition(AnimatorConditionMode.IfNot, 0f, "Working");
                chopToCarry.AddCondition(AnimatorConditionMode.If, 0f, "Carrying");

                var chopToLocomotion = chopping.AddTransition(locomotion);
                chopToLocomotion.hasExitTime = false;
                chopToLocomotion.duration = 0.12f;
                chopToLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0f, "Working");
                chopToLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0f, "Carrying");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        /// <summary>서기/걷기/달리기를 Speed 로 섞는 상태를 만든다.</summary>
        private static AnimatorState CreateLocomotion(
            AnimatorController controller, Dictionary<string, AnimationClip> clips, string name, AnimationClip idle)
        {
            var state = controller.CreateBlendTreeInController(name, out var tree, 0);

            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;

            var walk = Get(clips, WalkClip);
            var run = Get(clips, RunClip);

            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, 0.55f);
            if (run != null) tree.AddChild(run, 1f);

            return state;
        }

        private static void Connect(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.15f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static AnimationClip Get(Dictionary<string, AnimationClip> clips, string name)
        {
            if (clips.TryGetValue(name, out var clip)) return clip;

            Debug.LogWarning($"[Hell's Kitchen] 애니메이션 클립을 찾지 못했습니다: {name}");
            return null;
        }

        // ------------------------------------------------------------------ 프리팹 연결

        /// <summary>
        /// 열려 있는 씬에 놓인 캐릭터의 애니메이터를 갱신한다.
        /// 프리팹만 고치고 끝내면, 씬 인스턴스가 값을 덮어쓰고 있을 때
        /// 플레이 버튼을 눌러도 여전히 T 포즈로 서 있게 된다.
        /// </summary>
        private static int RefreshOpenScenes(AnimatorController controller, Avatar avatar)
        {
            var drivers = Object.FindObjectsByType<PlayerAnimatorDriver>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (drivers == null || drivers.Length == 0) return 0;

            var touched = 0;
            foreach (var driver in drivers)
            {
                if (driver == null) continue;

                var go = driver.gameObject;

                var animator = go.GetComponent<Animator>();
                if (animator == null) animator = go.AddComponent<Animator>();

                animator.runtimeAnimatorController = controller;
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                driver.EditorConfigure(
                    animator,
                    go.GetComponent<PlayerCharacter>(),
                    go.GetComponent<PlayerHands>(),
                    go.GetComponent<PlayerInteraction>());

                EditorUtility.SetDirty(driver);
                EditorUtility.SetDirty(animator);
                EditorSceneManager.MarkSceneDirty(go.scene);
                touched++;
            }

            if (touched > 0) EditorSceneManager.SaveOpenScenes();
            return touched;
        }

        private static bool AssignToPlayerPrefab(AnimatorController controller, Avatar avatar)
        {
            var path = HellsKitchenPrefabBuilder.PlayerPrefabPath;
            var root = PrefabUtility.LoadPrefabContents(path);

            if (root == null)
            {
                Debug.LogWarning(
                    $"[Hell's Kitchen] 플레이어 프리팹을 찾지 못했습니다: {path}\n" +
                    "'2. 프리팹 생성' 을 먼저 실행해 주세요.");
                return false;
            }

            try
            {
                // 애니메이터는 캐릭터 루트에 둔다. 모델 안쪽(중첩 프리팹)에 있으면
                // 설정이 저장되지 않고 조용히 되돌아가는 경우가 있다.
                var animator = root.GetComponent<Animator>();
                if (animator == null) animator = root.AddComponent<Animator>();

                var nested = root.transform.Find("Model");
                if (nested != null)
                {
                    var nestedAnimator = nested.GetComponent<Animator>();
                    if (nestedAnimator != null) Object.DestroyImmediate(nestedAnimator, true);
                }

                animator.runtimeAnimatorController = controller;
                animator.avatar = avatar;

                // 이동은 CharacterController 가 담당하므로 애니메이션이 위치를 밀면 안 된다.
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var driver = root.GetComponent<PlayerAnimatorDriver>();
                if (driver == null) driver = root.AddComponent<PlayerAnimatorDriver>();

                driver.EditorConfigure(
                    animator,
                    root.GetComponent<PlayerCharacter>(),
                    root.GetComponent<PlayerHands>(),
                    root.GetComponent<PlayerInteraction>());

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
