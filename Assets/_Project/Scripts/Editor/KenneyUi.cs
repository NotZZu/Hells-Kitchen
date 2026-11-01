using UnityEditor;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// Kenney UI 팩 스프라이트를 이름으로 꺼내 쓰는 창구.
    ///
    /// 팩에는 800장이 넘는 이미지가 들어 있는데, 실제로 쓰는 건 열 장 남짓이다.
    /// 여기서 용도별 이름을 붙여 두면 UI 코드가 파일 경로를 몰라도 되고,
    /// 나중에 다른 UI 팩으로 갈아탈 때 이 파일만 고치면 된다.
    ///
    /// 9-slice 경계값도 여기서 한 번에 지정한다. 이미지마다 손으로 맞추다 보면
    /// 늘어난 패널의 모서리가 뭉개지는 것을 뒤늦게 발견하게 된다.
    /// </summary>
    public static class KenneyUi
    {
        private const string Root = "Assets/kenney_ui-pack/PNG/";

        public const string Grey = "Grey";
        public const string Blue = "Blue";
        public const string Green = "Green";
        public const string Red = "Red";
        public const string Yellow = "Yellow";

        /// <summary>팩이 프로젝트에 들어와 있는가. 없으면 기본 유니티 스프라이트로 물러선다.</summary>
        public static bool IsAvailable => AssetDatabase.IsValidFolder("Assets/kenney_ui-pack");

        // border 는 (왼쪽, 아래, 오른쪽, 위) 순서다.

        /// <summary>패널과 티켓 배경. 아래쪽에 두께가 있어 살짝 입체적으로 보인다.</summary>
        public static Sprite Panel(string color) =>
            Get(color, "button_rectangle_depth_flat", new Vector4(16f, 20f, 16f, 16f));

        /// <summary>두께 없는 평평한 패널.</summary>
        public static Sprite FlatPanel(string color) =>
            Get(color, "button_rectangle_flat", new Vector4(16f, 16f, 16f, 16f));

        /// <summary>아이콘이 들어갈 정사각 칸.</summary>
        public static Sprite Slot(string color) =>
            Get(color, "button_square_flat", new Vector4(12f, 12f, 12f, 12f));

        /// <summary>화면 위 원형 버튼. 원형이라 늘리지 않고 그대로 쓴다.</summary>
        public static Sprite RoundButton(string color) =>
            Get(color, "button_round_depth_flat", Vector4.zero);

        /// <summary>게이지 배경. 가로로만 늘어난다.</summary>
        public static Sprite GaugeBack() =>
            Get(Grey, "slide_horizontal_grey", new Vector4(8f, 0f, 8f, 0f));

        /// <summary>게이지 채움.</summary>
        public static Sprite GaugeFill() =>
            Get(Grey, "slide_horizontal_color", new Vector4(8f, 0f, 8f, 0f));

        /// <summary>결과 화면의 별.</summary>
        public static Sprite Star(string color) => Get(color, "star", Vector4.zero);

        public static Sprite StarOutline(string color) => Get(color, "star_outline", Vector4.zero);

        private static Sprite Get(string color, string name, Vector4 border)
        {
            var path = $"{Root}{color}/Default/{name}.png";
            EnsureSpriteImport(path, border);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[Hell's Kitchen] UI 스프라이트를 찾지 못했습니다: {path}");
            }
            return sprite;
        }

        /// <summary>
        /// PNG 를 스프라이트로 임포트하고 9-slice 경계를 지정한다.
        /// 이미 같은 설정이면 아무것도 하지 않는다. 재임포트는 느리다.
        /// </summary>
        private static void EnsureSpriteImport(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            var needsUpdate =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.spriteBorder != border ||
                importer.mipmapEnabled;

            if (!needsUpdate) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
