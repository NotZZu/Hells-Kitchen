using HellsKitchen.Data;
using HellsKitchen.Runtime.Player;
using HellsKitchen.Runtime.Session;
using HellsKitchen.Runtime.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HellsKitchen.EditorTools
{
    /// <summary>화면 UI 전체를 코드로 조립한다.</summary>
    public static class HellsKitchenUiBuilder
    {
        private static readonly Color Ink = new Color(0.98f, 0.96f, 0.9f);

        public static GameObject Build(GameSession session, IngredientCatalog catalog, OrderTicketView ticketPrefab)
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("UI", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // 세로로 긴 화면에서 UI가 터무니없이 커지지 않도록 높이 쪽에 무게를 둔다.
            scaler.matchWidthOrHeight = 0.6f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var canvasRect = canvasGo.GetComponent<RectTransform>();

            var ticketRow = BuildTicketRow(canvasRect);
            BuildTopBar(canvasRect, out var timerText, out var goldText, out var comboText, out var roundFill);
            var feedback = BuildFeedback(canvasRect);
            var hint = BuildHint(canvasRect);
            var mobile = BuildMobileControls(canvasRect);
            var resultPanel = BuildResultPanel(canvasRect, out var resultParts);

            var hud = canvasGo.AddComponent<HudController>();
            hud.EditorConfigure(session, catalog, ticketRow, ticketPrefab, timerText, goldText, comboText, roundFill, feedback);

            var hintView = canvasGo.AddComponent<HeldItemHint>();
            hintView.EditorConfigure(null, null, hint);

            BuildThrowGauge(canvasRect, canvasGo);

            var visibility = canvasGo.AddComponent<MobileControlsVisibility>();
            visibility.EditorConfigure(mobile, MobileControlsVisibility.DisplayMode.Auto);

            // 뷰는 캔버스에 붙인다. 패널 자신에 붙이면 패널을 끄는 순간
            // 컴포넌트도 꺼져서 라운드 종료 이벤트를 받지 못한다.
            var resultView = canvasGo.AddComponent<RoundResultView>();
            resultView.EditorSetStarSprites(resultParts.StarOn, resultParts.StarOff);
            resultView.EditorConfigure(
                session, resultPanel, resultParts.Title, resultParts.Gold,
                resultParts.Detail, resultParts.Stars, resultParts.Retry);

            // 결과 화면은 항상 맨 위에 오도록 마지막 자식으로 둔다.
            resultPanel.transform.SetAsLastSibling();

            return canvasGo;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ------------------------------------------------------------------ 상단

        private static RectTransform BuildTopBar(
            RectTransform parent,
            out TMP_Text timerText, out TMP_Text goldText, out TMP_Text comboText, out Image roundFill)
        {
            var bar = UiBuildUtility.CreateImage(
                "TopBar", parent, new Color(0.85f, 0.85f, 0.88f, 0.95f),
                KenneyUi.IsAvailable ? KenneyUi.Panel(KenneyUi.Grey) : UiBuildUtility.RoundedSprite);
            UiBuildUtility.Anchor(bar.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(520f, 86f));

            timerText = UiBuildUtility.CreateText("Timer", bar.transform, "3:00", 44f, TextAlignmentOptions.Center);
            timerText.color = new Color(0.15f, 0.13f, 0.11f);
            timerText.fontStyle = FontStyles.Bold;
            UiBuildUtility.Anchor(timerText.rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -6f), new Vector2(150f, 54f));

            goldText = UiBuildUtility.CreateText("Gold", bar.transform, "0 G", 38f, TextAlignmentOptions.Right);
            goldText.color = new Color(0.72f, 0.5f, 0.05f);
            goldText.fontStyle = FontStyles.Bold;
            UiBuildUtility.Anchor(goldText.rectTransform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-20f, -6f), new Vector2(220f, 54f));

            var barBack = UiBuildUtility.CreateImage(
                "RoundBarBack", bar.transform, new Color(0.35f, 0.33f, 0.32f, 0.8f), UiBuildUtility.GaugeBackSprite);
            UiBuildUtility.Anchor(barBack.rectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(470f, 10f));

            roundFill = UiBuildUtility.CreateImage(
                "RoundBarFill", barBack.transform, new Color(0.95f, 0.7f, 0.25f), UiBuildUtility.GaugeFillSprite);
            UiBuildUtility.Stretch(roundFill.rectTransform, 1f);
            roundFill.type = Image.Type.Filled;
            roundFill.fillMethod = Image.FillMethod.Horizontal;
            roundFill.fillAmount = 1f;

            comboText = UiBuildUtility.CreateText("Combo", parent, "", 30f, TextAlignmentOptions.Center);
            comboText.color = new Color(1f, 0.6f, 0.25f);
            comboText.fontStyle = FontStyles.Bold;
            UiBuildUtility.Anchor(comboText.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -118f), new Vector2(400f, 40f));
            comboText.gameObject.SetActive(false);

            return bar.rectTransform;
        }

        private static Transform BuildTicketRow(RectTransform parent)
        {
            var row = UiBuildUtility.CreateRect("TicketRow", parent);
            UiBuildUtility.Anchor(row,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(800f, 140f));

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return row;
        }

        private static TMP_Text BuildFeedback(RectTransform parent)
        {
            var text = UiBuildUtility.CreateText("Feedback", parent, "", 46f, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            UiBuildUtility.Anchor(text.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 180f), new Vector2(900f, 70f));
            text.gameObject.SetActive(false);
            return text;
        }

        private static TMP_Text BuildHint(RectTransform parent)
        {
            var text = UiBuildUtility.CreateText("HeldHint", parent, "", 26f, TextAlignmentOptions.Center);
            text.color = new Color(1f, 1f, 1f, 0.75f);
            UiBuildUtility.Anchor(text.rectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(900f, 40f));
            return text;
        }

        /// <summary>던지기 세기 게이지. 차징이 시작될 때만 나타난다.</summary>
        private static void BuildThrowGauge(RectTransform parent, GameObject canvasGo)
        {
            var back = UiBuildUtility.CreateImage(
                "ThrowGauge", parent, new Color(0.3f, 0.28f, 0.27f, 0.85f), UiBuildUtility.GaugeBackSprite);
            UiBuildUtility.Anchor(back.rectTransform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 86f), new Vector2(320f, 20f));
            back.raycastTarget = false;

            var fill = UiBuildUtility.CreateImage(
                "Fill", back.transform, new Color(0.95f, 0.85f, 0.35f), UiBuildUtility.GaugeFillSprite);
            UiBuildUtility.Stretch(fill.rectTransform, 3f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;

            var group = back.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var view = canvasGo.AddComponent<ThrowChargeView>();
            view.EditorConfigure(group, fill);
        }

        // ------------------------------------------------------------------ 모바일 조작

        private static GameObject BuildMobileControls(RectTransform parent)
        {
            var root = UiBuildUtility.CreateRect("MobileControls", parent);
            UiBuildUtility.Stretch(root);

            // 왼쪽 아래 조이스틱
            var stickBack = UiBuildUtility.CreateImage("StickBackground", root, new Color(1f, 1f, 1f, 0.22f), UiBuildUtility.KnobSprite);
            stickBack.type = Image.Type.Simple;
            UiBuildUtility.Anchor(stickBack.rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(220f, 220f), new Vector2(300f, 300f));

            var knob = UiBuildUtility.CreateImage("Handle", stickBack.transform, new Color(1f, 1f, 1f, 0.62f), UiBuildUtility.KnobSprite);
            knob.type = Image.Type.Simple;
            UiBuildUtility.Anchor(knob.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(140f, 140f));

            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.movementRange = 90f;
            SetControlPath(stick, "<Gamepad>/leftStick");

            // 오른쪽 아래 버튼들. 자주 쓰는 것일수록 엄지에 가깝게 둔다.
            CreateButton(root, "InteractButton", "집기 / 놓기", new Vector2(-200f, 190f), 190f,
                KenneyUi.Green, "<Gamepad>/buttonSouth");

            CreateButton(root, "WorkButton", "썰기", new Vector2(-380f, 350f), 150f,
                KenneyUi.Yellow, "<Gamepad>/buttonWest");

            CreateButton(root, "DropButton", "놓기 / 던지기", new Vector2(-195f, 400f), 140f,
                KenneyUi.Red, "<Gamepad>/buttonEast");

            // 달리기는 왼손 엄지로 조이스틱과 함께 쓰므로 왼쪽에 둔다.
            CreateButton(root, "SprintButton", "달리기", new Vector2(430f, 150f), 130f,
                KenneyUi.Blue, "<Gamepad>/rightTrigger");

            // 던지기 취소. 게이지가 차는 동안 마음을 바꿀 수 있어야 한다.
            CreateButton(root, "CancelButton", "취소", new Vector2(430f, 330f), 120f,
                KenneyUi.Grey, "<Gamepad>/buttonNorth");

            return root.gameObject;
        }

        private static void CreateButton(
            Transform parent, string name, string label, Vector2 offset, float size, string kenneyColor, string controlPath)
        {
            var sprite = KenneyUi.IsAvailable ? KenneyUi.RoundButton(kenneyColor) : UiBuildUtility.KnobSprite;

            var image = UiBuildUtility.CreateImage(name, parent, new Color(1f, 1f, 1f, 0.92f), sprite);
            image.type = Image.Type.Simple;

            // offset.x 가 양수면 화면 왼쪽 기준, 음수면 오른쪽 기준으로 붙인다.
            var anchor = offset.x >= 0f ? new Vector2(0f, 0f) : new Vector2(1f, 0f);

            UiBuildUtility.Anchor(image.rectTransform,
                anchor, anchor, new Vector2(0.5f, 0.5f),
                offset, new Vector2(size, size));

            var text = UiBuildUtility.CreateText("Label", image.transform, label, size * 0.16f, TextAlignmentOptions.Center);
            text.color = Ink;
            text.fontStyle = FontStyles.Bold;
            UiBuildUtility.Stretch(text.rectTransform, 8f);

            var button = image.gameObject.AddComponent<OnScreenButton>();
            SetControlPath(button, controlPath);
        }

        /// <summary>
        /// OnScreenControl 의 controlPath 는 인스펙터 전용 비공개 필드라
        /// SerializedObject 로만 설정할 수 있다.
        /// </summary>
        private static void SetControlPath(OnScreenControl control, string path)
        {
            var so = new SerializedObject(control);
            var property = so.FindProperty("m_ControlPath");
            if (property == null)
            {
                Debug.LogWarning($"[Hell's Kitchen] OnScreenControl 의 controlPath 필드를 찾지 못했습니다: {control.name}");
                return;
            }
            property.stringValue = path;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ 결과 화면

        /// <summary>결과 화면을 조립하고, 나중에 연결할 부품들을 함께 돌려준다.</summary>
        public struct ResultParts
        {
            public Sprite StarOn;
            public Sprite StarOff;
            public TMP_Text Title;
            public TMP_Text Gold;
            public TMP_Text Detail;
            public Image[] Stars;
            public Button Retry;
        }

        private static GameObject BuildResultPanel(RectTransform parent, out ResultParts parts)
        {
            var panel = UiBuildUtility.CreateImage("ResultPanel", parent, new Color(0.08f, 0.07f, 0.06f, 0.9f));
            UiBuildUtility.Stretch(panel.rectTransform);

            var title = UiBuildUtility.CreateText("Title", panel.transform, "영업 종료!", 68f, TextAlignmentOptions.Center);
            title.color = Ink;
            title.fontStyle = FontStyles.Bold;
            UiBuildUtility.Anchor(title.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 250f), new Vector2(1000f, 90f));

            var starOn = KenneyUi.IsAvailable ? KenneyUi.Star(KenneyUi.Yellow) : UiBuildUtility.KnobSprite;
            var starOff = KenneyUi.IsAvailable ? KenneyUi.StarOutline(KenneyUi.Grey) : UiBuildUtility.KnobSprite;

            var stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var star = UiBuildUtility.CreateImage($"Star{i + 1}", panel.transform, new Color(1f, 1f, 1f, 0.55f), starOff);
                star.type = Image.Type.Simple;
                star.preserveAspect = true;
                UiBuildUtility.Anchor(star.rectTransform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - 1) * 130f, 120f), new Vector2(110f, 110f));
                stars[i] = star;
            }

            var gold = UiBuildUtility.CreateText("Gold", panel.transform, "0 G", 60f, TextAlignmentOptions.Center);
            gold.color = new Color(1f, 0.85f, 0.35f);
            gold.fontStyle = FontStyles.Bold;
            UiBuildUtility.Anchor(gold.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(600f, 80f));

            var detail = UiBuildUtility.CreateText("Detail", panel.transform, "", 30f, TextAlignmentOptions.Center);
            detail.color = new Color(1f, 1f, 1f, 0.85f);
            detail.lineSpacing = 8f;
            UiBuildUtility.Anchor(detail.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -110f), new Vector2(600f, 190f));

            var buttonImage = UiBuildUtility.CreateImage(
                "RetryButton", panel.transform, Color.white,
                KenneyUi.IsAvailable ? KenneyUi.Panel(KenneyUi.Green) : UiBuildUtility.RoundedSprite);
            UiBuildUtility.Anchor(buttonImage.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -280f), new Vector2(300f, 84f));

            var buttonLabel = UiBuildUtility.CreateText("Label", buttonImage.transform, "다시 도전", 34f, TextAlignmentOptions.Center);
            buttonLabel.color = Ink;
            buttonLabel.fontStyle = FontStyles.Bold;
            UiBuildUtility.Stretch(buttonLabel.rectTransform);

            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            parts = new ResultParts
            {
                StarOn = starOn,
                StarOff = starOff,
                Title = title,
                Gold = gold,
                Detail = detail,
                Stars = stars,
                Retry = button,
            };

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }
    }
}
