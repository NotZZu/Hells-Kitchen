using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// TextMeshPro 의 기본 폰트에는 한글 글리프가 없어서, 그대로 두면
    /// 주문 티켓의 요리 이름이 전부 네모로 표시된다.
    ///
    /// 이 툴은 운영체제에 설치된 한글 폰트를 프로젝트로 복사한 뒤
    /// 동적 TMP 폰트 에셋을 만들어 준다. 동적 모드라 쓰이는 글자만
    /// 아틀라스에 채워지므로 용량도 크지 않다.
    /// </summary>
    public static class KoreanFontSetup
    {
        private const string FontFolder = "Assets/_Project/Fonts";
        private const string CopiedFontPath = FontFolder + "/HK_KoreanFont.ttf";
        public const string FontAssetPath = FontFolder + "/HK_KoreanFont SDF.asset";

        /// <summary>후보 경로. 위에서부터 찾아서 처음 발견되는 것을 쓴다.</summary>
        private static readonly string[] CandidateFonts =
        {
            @"C:\Windows\Fonts\malgun.ttf",          // 맑은 고딕
            @"C:\Windows\Fonts\NanumGothic.ttf",
            @"C:\Windows\Fonts\gulim.ttc",
            "/System/Library/Fonts/AppleSDGothicNeo.ttc",
            "/usr/share/fonts/truetype/nanum/NanumGothic.ttf",
        };

        [MenuItem("Hell's Kitchen/0. 한글 폰트 준비", priority = 5)]
        public static void Setup()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                Debug.Log($"[Hell's Kitchen] 한글 폰트 에셋이 이미 있습니다: {FontAssetPath}");
                Selection.activeObject = existing;
                return;
            }

            EditorAssetUtility.EnsureFolder(FontFolder);

            var sourcePath = FindSystemFont();
            if (sourcePath == null)
            {
                Debug.LogError(
                    "[Hell's Kitchen] 한글 폰트를 찾지 못했습니다.\n" +
                    "한글 TTF 파일을 Assets/_Project/Fonts 에 직접 넣은 뒤,\n" +
                    "그 파일을 우클릭 > Create > Text > FontAsset 으로 변환해 주세요.");
                return;
            }

            var projectFontPath = CopiedFontPath;
            if (Path.GetExtension(sourcePath).ToLowerInvariant() == ".ttc")
            {
                // ttc(폰트 모음)는 유니티가 읽지 못한다.
                Debug.LogError(
                    $"[Hell's Kitchen] 찾은 폰트가 ttc 형식이라 사용할 수 없습니다: {sourcePath}\n" +
                    "나눔고딕 같은 ttf 폰트를 설치하거나, ttf 파일을 Assets/_Project/Fonts 에 넣어 주세요.");
                return;
            }

            try
            {
                File.Copy(sourcePath, projectFontPath, overwrite: true);
            }
            catch (IOException e)
            {
                Debug.LogError($"[Hell's Kitchen] 폰트 복사에 실패했습니다: {e.Message}");
                return;
            }

            AssetDatabase.ImportAsset(projectFontPath, ImportAssetOptions.ForceUpdate);

            var font = AssetDatabase.LoadAssetAtPath<Font>(projectFontPath);
            if (font == null)
            {
                Debug.LogError($"[Hell's Kitchen] 복사한 폰트를 불러오지 못했습니다: {projectFontPath}");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 48,
                atlasPadding: 6,
                renderMode: UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError("[Hell's Kitchen] TMP 폰트 에셋 생성에 실패했습니다.");
                return;
            }

            fontAsset.name = "HK_KoreanFont SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            // 아틀라스 텍스처와 머티리얼을 같은 에셋 안에 넣어 준다.
            if (fontAsset.atlasTextures != null)
            {
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture == null) continue;
                    texture.name = "HK_KoreanFont Atlas";
                    AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "HK_KoreanFont Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Hell's Kitchen] 한글 폰트 에셋을 만들었습니다: {FontAssetPath} (원본: {sourcePath})");
            Selection.activeObject = fontAsset;
        }

        private static string FindSystemFont()
        {
            for (int i = 0; i < CandidateFonts.Length; i++)
            {
                if (File.Exists(CandidateFonts[i])) return CandidateFonts[i];
            }
            return null;
        }

        /// <summary>
        /// UI를 만들 때 쓸 폰트. 한글 폰트가 준비되어 있으면 그것을,
        /// 없으면 TMP 기본 폰트를 돌려준다.
        /// </summary>
        public static TMP_FontAsset GetUiFont()
        {
            var korean = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (korean != null) return korean;

            if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;

            Debug.LogWarning(
                "[Hell's Kitchen] TMP 폰트를 찾지 못했습니다. " +
                "Window > TextMeshPro > Import TMP Essential Resources 를 먼저 실행해 주세요.");
            return null;
        }
    }
}
