using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.EditorTools
{
    /// <summary>UI 오브젝트를 코드로 조립할 때 반복되는 부분을 모아 둔 도우미.</summary>
    public static class UiBuildUtility
    {
        // Kenney UI 팩이 있으면 그쪽을 쓰고, 없으면 유니티 기본 스킨으로 물러선다.
        // 팩이 빠진 프로젝트에서도 UI 조립이 통째로 실패하지는 않게 하기 위해서다.

        public static Sprite RoundedSprite =>
            KenneyUi.IsAvailable
                ? KenneyUi.Panel(KenneyUi.Grey)
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static Sprite FlatSprite =>
            KenneyUi.IsAvailable
                ? KenneyUi.FlatPanel(KenneyUi.Grey)
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        public static Sprite KnobSprite =>
            KenneyUi.IsAvailable
                ? KenneyUi.RoundButton(KenneyUi.Grey)
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        public static Sprite GaugeBackSprite =>
            KenneyUi.IsAvailable
                ? KenneyUi.GaugeBack()
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        public static Sprite GaugeFillSprite =>
            KenneyUi.IsAvailable
                ? KenneyUi.GaugeFill()
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null) image.type = Image.Type.Sliced;
            return image;
        }

        public static TextMeshProUGUI CreateText(
            string name, Transform parent, string content, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            var font = KoreanFontSetup.GetUiFont();
            if (font != null) text.font = font;

            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.14f, 0.12f, 0.1f);
            text.raycastTarget = false;
            return text;
        }

        /// <summary>앵커를 화면 모서리 기준으로 잡는다.</summary>
        public static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
