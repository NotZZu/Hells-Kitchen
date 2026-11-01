using UnityEditor;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// 전체 셋업을 순서대로 실행하는 창.
    /// 새 컴퓨터에서 저장소를 받아 왔을 때 여기 버튼 하나로 플레이 가능한 상태가 되게 하는 것이 목적이다.
    /// </summary>
    public class HellsKitchenSetupWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Hell's Kitchen/셋업 창 열기", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<HellsKitchenSetupWindow>(true, "Hell's Kitchen 셋업");
            window.minSize = new Vector2(440f, 420f);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("프로젝트 셋업", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "아래 '전체 실행' 한 번이면 0~5단계가 순서대로 돌아갑니다.\n" +
                "각 단계는 여러 번 실행해도 안전합니다. 이미 만들어진 에셋은 덮어쓰지 않습니다.",
                MessageType.Info);

            EditorGUILayout.Space(6f);

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.6f);
            if (GUILayout.Button("전체 실행 (0 → 5)", GUILayout.Height(38f))) RunAll();
            GUI.backgroundColor = previousColor;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("개별 실행", EditorStyles.boldLabel);

            Step("0. 한글 폰트 준비",
                "시스템 한글 폰트로 TMP 폰트 에셋을 만듭니다. 건너뛰면 UI의 한글이 네모로 보입니다.",
                KoreanFontSetup.Setup);

            Step("1. 데이터 에셋 생성",
                "재료 정의, 카탈로그, 레시피, 스테이지 에셋을 만듭니다.",
                HellsKitchenDataSetup.CreateAll);

            Step("2. 프리팹 생성",
                "재료, 접시, 플레이어, UI 프리팹을 만듭니다.",
                HellsKitchenPrefabBuilder.BuildAll);

            Step("3. 재료 아이콘 굽기",
                "재료 모델을 렌더해 아이콘을 만듭니다. 상자 이름표에도 쓰이므로 씬보다 먼저 실행해야 합니다.",
                IngredientIconBaker.BakeAll);

            Step("4. 주방 씬 생성",
                "설비를 세 구역으로 배치한 플레이 가능한 씬을 만듭니다. 새 씬으로 교체됩니다.",
                HellsKitchenSceneBuilder.BuildScene);

            Step("5. 캐릭터 애니메이션 설정",
                "KayKit 애니메이션의 아바타를 맞추고 애니메이터 컨트롤러를 만들어 플레이어에 연결합니다. " +
                "FBX 재임포트 때문에 시간이 조금 걸리고, 진행 상황은 콘솔에 단계별로 찍힙니다.",
                CharacterAnimationSetup.Setup);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("조작", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("이동: WASD / 방향키 / 게임패드 왼쪽 스틱");
            EditorGUILayout.LabelField("달리기: Shift / 게임패드 오른쪽 트리거");
            EditorGUILayout.LabelField("집기·놓기: E / 게임패드 A");
            EditorGUILayout.LabelField("썰기(누르고 있기): Space / 게임패드 X");
            EditorGUILayout.LabelField("내려놓기·던지기: Q / 게임패드 B", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("   짧게 누르면 발 앞에 내려놓고, 길게 누르면 게이지가 차서 던집니다.",
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("던지기 취소: F 또는 ESC / 게임패드 Y");

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("요리 순서", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("고기 → 도마에서 다지기 → 패티 → 화로에서 굽기", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("채소 → 도마에서 썰기", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("접시에 담아 주문 창구로", EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(10f);
            EditorGUILayout.HelpBox(
                "화면이 뿌옇게 보인다면 Game 뷰 상단 툴바를 확인해 주세요.\n" +
                "• 해상도를 Free Aspect 대신 1920x1080 같은 고정 값으로\n" +
                "• Scale 슬라이더를 1x 로\n" +
                "• 'Low Resolution Aspect Ratios' 체크 해제",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private static void RunAll()
        {
            // 순서가 중요하다. 아이콘은 씬보다 먼저 있어야 상자 이름표에 들어가고,
            // 프리팹은 씬보다 먼저 있어야 씬이 그것을 배치할 수 있다.
            KoreanFontSetup.Setup();
            HellsKitchenDataSetup.CreateAll();
            HellsKitchenPrefabBuilder.BuildAll();
            IngredientIconBaker.BakeAll();

            // 애니메이션을 씬보다 먼저 붙여 두면, 씬에 들어가는 플레이어가
            // 처음부터 완성된 프리팹이 된다.
            CharacterAnimationSetup.Setup();

            HellsKitchenSceneBuilder.BuildScene();
        }

        private static void Step(string title, string description, System.Action action)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("실행", GUILayout.Height(22f))) action?.Invoke();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3f);
        }
    }
}
