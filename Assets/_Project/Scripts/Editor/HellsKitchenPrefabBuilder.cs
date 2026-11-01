using HellsKitchen.Data;
using HellsKitchen.Runtime.Items;
using HellsKitchen.Runtime.Player;
using HellsKitchen.Runtime.UI;
using HellsKitchen.Runtime.Inputs;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// 재료/접시/플레이어/UI 프리팹을 코드로 조립한다.
    ///
    /// 손으로 만들어도 되지만, 툴로 만들어 두면 구조가 문서처럼 남고
    /// 나중에 구조를 바꿀 때 프리팹을 다시 찍어내면 되므로 유지보수가 쉽다.
    /// </summary>
    public static class HellsKitchenPrefabBuilder
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs";
        public const string IngredientPrefabPath = PrefabRoot + "/Items/Ingredient_Base.prefab";
        public const string PlatePrefabPath = PrefabRoot + "/Items/Plate.prefab";
        public const string PlayerPrefabPath = PrefabRoot + "/Characters/Player.prefab";
        public const string TicketPrefabPath = PrefabRoot + "/UI/OrderTicket.prefab";
        public const string TicketIconPrefabPath = PrefabRoot + "/UI/TicketIcon.prefab";
        public const string ProgressPrefabPath = PrefabRoot + "/UI/StationProgress.prefab";
        public const string LabelPrefabPath = PrefabRoot + "/UI/StationLabel.prefab";

        private const string InputAssetPath = "Assets/_Project/Input/GameInput.inputactions";

        [MenuItem("Hell's Kitchen/2. 프리팹 생성", priority = 20)]
        public static void BuildAll()
        {
            EditorAssetUtility.EnsureFolder(PrefabRoot + "/Items");
            EditorAssetUtility.EnsureFolder(PrefabRoot + "/Characters");
            EditorAssetUtility.EnsureFolder(PrefabRoot + "/UI");

            BuildIngredientPrefab();
            BuildPlatePrefab();
            var iconPrefab = BuildTicketIconPrefab();
            BuildTicketPrefab(iconPrefab);
            BuildProgressPrefab();
            BuildLabelPrefab();
            BuildPlayerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hell's Kitchen] 프리팹 생성 완료.");
        }

        // ------------------------------------------------------------------ 아이템

        private static void BuildIngredientPrefab()
        {
            var root = new GameObject("Ingredient");
            var modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.SetParent(root.transform, false);

            var item = root.AddComponent<IngredientItem>();
            item.EditorConfigure(modelRoot.transform);

            // 바닥에 떨어진 재료를 플레이어가 찾아낼 수 있게 하는 감지용 콜라이더.
            // 트리거라 물리 충돌은 일으키지 않는다.
            var pickup = root.AddComponent<SphereCollider>();
            pickup.isTrigger = true;
            pickup.radius = 0.35f;
            pickup.center = new Vector3(0f, 0.2f, 0f);

            BuildFreshnessGauge(root, item);

            SaveAndCleanup(root, IngredientPrefabPath);
        }

        /// <summary>재료 위에 뜨는 신선도 게이지. 바닥에 떨어져 있을 때만 보인다.</summary>
        private static void BuildFreshnessGauge(GameObject root, IngredientItem item)
        {
            var gaugeGo = new GameObject("FreshnessGauge", typeof(RectTransform));
            gaugeGo.transform.SetParent(root.transform, false);
            gaugeGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            var canvas = gaugeGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var group = gaugeGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var rect = gaugeGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90f, 12f);
            rect.localScale = Vector3.one * 0.005f;

            var back = UiBuildUtility.CreateImage(
                "Back", rect, new Color(0.1f, 0.09f, 0.08f, 0.8f), UiBuildUtility.GaugeBackSprite);
            UiBuildUtility.Stretch(back.rectTransform);
            back.raycastTarget = false;

            var fill = UiBuildUtility.CreateImage(
                "Fill", back.transform, new Color(0.45f, 0.8f, 0.4f), UiBuildUtility.GaugeFillSprite);
            UiBuildUtility.Stretch(fill.rectTransform, 1.5f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            var view = gaugeGo.AddComponent<ItemFreshnessView>();
            view.EditorConfigure(item, group, fill);
        }

        /// <summary>
        /// 접시 배율. 재료를 키우면 접시도 같이 키워야 재료가 접시 밖으로 삐져나오지 않는다.
        /// 재료의 DisplayScale 과 같은 값으로 맞춰 둔다.
        /// </summary>
        private const float PlateScale = 1.5f;

        private static void BuildPlatePrefab()
        {
            var root = new GameObject("Plate");

            var plateModel = KayKitPaths.Restaurant("plate");
            float topHeight = 0.02f;

            if (plateModel != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(plateModel);
                model.name = "Model";
                model.transform.SetParent(root.transform, false);
                model.transform.localScale *= PlateScale;

                var bounds = EditorAssetUtility.MeasureBounds(plateModel);
                topHeight = Mathf.Max(0.02f, bounds.max.y * PlateScale);
            }

            var stackRoot = new GameObject("StackRoot");
            stackRoot.transform.SetParent(root.transform, false);
            stackRoot.transform.localPosition = new Vector3(0f, topHeight, 0f);

            var plate = root.AddComponent<PlateItem>();
            plate.EditorConfigure(stackRoot.transform, capacity: 6);

            var pickup = root.AddComponent<SphereCollider>();
            pickup.isTrigger = true;
            pickup.radius = 0.45f;
            pickup.center = new Vector3(0f, 0.15f, 0f);

            SaveAndCleanup(root, PlatePrefabPath);
        }

        // ------------------------------------------------------------------ UI

        private static TicketIconView BuildTicketIconPrefab()
        {
            // 칸 배경 위에 재료 그림을 얹는다. 밝은 티켓 위에서 재료 실루엣이 묻히는 것을 막는다.
            var slot = UiBuildUtility.CreateImage(
                "TicketIcon", null, new Color(1f, 1f, 1f, 0.85f),
                KenneyUi.IsAvailable ? KenneyUi.Slot(KenneyUi.Grey) : UiBuildUtility.FlatSprite);
            slot.rectTransform.sizeDelta = new Vector2(58f, 58f);

            var layout = slot.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 58f;
            layout.preferredHeight = 58f;

            var image = UiBuildUtility.CreateImage("Art", slot.transform, Color.white);
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            UiBuildUtility.Stretch(image.rectTransform, 4f);

            var iconView = slot.gameObject.AddComponent<TicketIconView>();
            iconView.EditorConfigure(image);

            var saved = SaveAndCleanup(slot.gameObject, TicketIconPrefabPath);
            return saved != null ? saved.GetComponent<TicketIconView>() : null;
        }

        private static void BuildTicketPrefab(TicketIconView iconPrefab)
        {
            var normalPanel = KenneyUi.IsAvailable ? KenneyUi.Panel(KenneyUi.Yellow) : UiBuildUtility.RoundedSprite;
            var urgentPanel = KenneyUi.IsAvailable ? KenneyUi.Panel(KenneyUi.Red) : UiBuildUtility.RoundedSprite;

            var background = UiBuildUtility.CreateImage("OrderTicket", null, Color.white, normalPanel);

            var rootRect = background.rectTransform;
            rootRect.sizeDelta = new Vector2(240f, 176f);

            var layoutElement = background.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 240f;
            layoutElement.preferredHeight = 176f;

            var vertical = background.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(10, 10, 8, 8);
            vertical.spacing = 4f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            var title = UiBuildUtility.CreateText("Title", rootRect, "요리 이름", 22f, TextAlignmentOptions.Left);
            title.color = new Color(0.16f, 0.13f, 0.1f);
            title.fontStyle = FontStyles.Bold;
            AddHeight(title.gameObject, 28f);

            var iconRowRect = UiBuildUtility.CreateRect("IconRow", rootRect);
            var horizontal = iconRowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 4f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;
            AddHeight(iconRowRect.gameObject, 60f);

            var modifier = UiBuildUtility.CreateText("Modifier", rootRect, "", 17f, TextAlignmentOptions.Left);
            modifier.color = new Color(0.78f, 0.32f, 0.12f);
            modifier.fontStyle = FontStyles.Bold;
            AddHeight(modifier.gameObject, 22f);

            var timerBack = UiBuildUtility.CreateImage(
                "TimerBar", rootRect, new Color(0.25f, 0.22f, 0.2f, 0.6f), UiBuildUtility.GaugeBackSprite);
            AddHeight(timerBack.gameObject, 14f);

            var fill = UiBuildUtility.CreateImage(
                "Fill", timerBack.transform, new Color(0.42f, 0.72f, 0.38f), UiBuildUtility.GaugeFillSprite);
            UiBuildUtility.Stretch(fill.rectTransform, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            var view = background.gameObject.AddComponent<OrderTicketView>();
            view.EditorConfigure(title, modifier, fill, background, iconRowRect, iconPrefab);
            view.EditorSetBackgrounds(normalPanel, urgentPanel);

            SaveAndCleanup(background.gameObject, TicketPrefabPath);
        }

        private static void AddHeight(GameObject target, float height)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
        }

        private static void BuildProgressPrefab()
        {
            var root = new GameObject("StationProgress", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var group = root.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            group.alpha = 0f;

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(110f, 16f);
            rootRect.localScale = Vector3.one * 0.006f;

            var back = UiBuildUtility.CreateImage(
                "Back", rootRect, new Color(0.1f, 0.09f, 0.08f, 0.75f), UiBuildUtility.RoundedSprite);
            UiBuildUtility.Stretch(back.rectTransform);
            back.raycastTarget = false;

            var fill = UiBuildUtility.CreateImage(
                "Fill", back.transform, new Color(0.45f, 0.8f, 0.4f), UiBuildUtility.RoundedSprite);
            UiBuildUtility.Stretch(fill.rectTransform, 2f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            var view = root.AddComponent<StationProgressView>();
            view.EditorConfigure(null, fill, group);

            SaveAndCleanup(root, ProgressPrefabPath);
        }

        private static void BuildLabelPrefab()
        {
            var root = new GameObject("StationLabel", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(210f, 62f);
            // 월드 스페이스 캔버스는 픽셀 단위라, 배율로 실제 크기를 맞춘다.
            rootRect.localScale = Vector3.one * 0.0055f;

            var back = UiBuildUtility.CreateImage(
                "Back", rootRect, new Color(0.11f, 0.1f, 0.09f, 0.82f), UiBuildUtility.RoundedSprite);
            UiBuildUtility.Stretch(back.rectTransform);
            back.raycastTarget = false;

            var icon = UiBuildUtility.CreateImage("Icon", back.transform, Color.white);
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UiBuildUtility.Anchor(icon.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(46f, 46f));

            var label = UiBuildUtility.CreateText("Text", back.transform, "재료", 30f, TextAlignmentOptions.Left);
            label.color = new Color(0.98f, 0.96f, 0.9f);
            label.fontStyle = FontStyles.Bold;
            UiBuildUtility.Anchor(label.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(0f, 0f));
            label.rectTransform.offsetMin = new Vector2(60f, 4f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);

            var view = root.AddComponent<WorldLabel>();
            view.EditorSetParts(label, icon);

            SaveAndCleanup(root, LabelPrefabPath);
        }

        // ------------------------------------------------------------------ 플레이어

        private static void BuildPlayerPrefab()
        {
            var root = new GameObject("Player");

            var characterModel = KayKitPaths.Adventurer("Knight");
            float height = 1.7f;
            GameObject modelInstance = null;

            if (characterModel != null)
            {
                modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterModel);
                modelInstance.name = "Model";
                modelInstance.transform.SetParent(root.transform, false);

                var bounds = EditorAssetUtility.MeasureBounds(characterModel);
                if (bounds.size.y > 0.2f) height = bounds.size.y;
            }

            // 측정값은 T 포즈로 팔을 벌린 상태의 바운드라 실제 몸보다 크다.
            // 그대로 캡슐로 쓰면 조리대에 닿기도 전에 막히므로 몸통 기준으로 줄인다.
            var bodyHeight = height * 0.78f;

            var controller = root.AddComponent<CharacterController>();
            controller.height = bodyHeight;
            controller.radius = Mathf.Max(0.25f, bodyHeight * 0.18f);
            controller.center = new Vector3(0f, bodyHeight * 0.5f, 0f);
            controller.slopeLimit = 60f;
            controller.stepOffset = 0.2f;
            controller.skinWidth = 0.02f;

            // 든 물건은 가슴 높이에서 몸 앞으로 확실히 떨어뜨린다.
            // 몸 안쪽에 두면 카메라 각도상 캐릭터에 가려 아무것도 안 보인다.
            var holdAnchor = new GameObject("HoldAnchor");
            holdAnchor.transform.SetParent(root.transform, false);
            holdAnchor.transform.localPosition = new Vector3(0f, bodyHeight * 0.62f, controller.radius + 0.55f);

            var intentSource = root.AddComponent<LocalIntentSource>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset != null) intentSource.EditorConfigure(inputAsset);
            else Debug.LogWarning($"[Hell's Kitchen] 입력 에셋을 찾지 못했습니다: {InputAssetPath}");

            var character = root.AddComponent<PlayerCharacter>();

            var hands = root.AddComponent<PlayerHands>();
            hands.EditorSetAnchor(holdAnchor.transform);

            var interaction = root.AddComponent<PlayerInteraction>();
            root.AddComponent<PlayerDropThrow>();

            // Animator 는 캐릭터 루트에 둔다.
            // Generic 리그에서는 애니메이터가 본 계층의 조상이기만 하면 되고,
            // 모델 FBX 인스턴스 안쪽에 두면 중첩 프리팹이라 설정이 저장되지 않는 경우가 있다.
            var animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.applyRootMotion = false;

            // 모델 쪽에 딸려 온 애니메이터가 있으면 둘이 충돌하므로 하나만 남긴다.
            if (modelInstance != null)
            {
                var nested = modelInstance.GetComponent<Animator>();
                if (nested != null) Object.DestroyImmediate(nested, true);
            }

            var animatorDriver = root.AddComponent<PlayerAnimatorDriver>();
            animatorDriver.EditorConfigure(animator, character, hands, interaction);

            SaveAndCleanup(root, PlayerPrefabPath);
        }

        // ------------------------------------------------------------------ 공통

        private static GameObject SaveAndCleanup(GameObject root, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }
    }
}
