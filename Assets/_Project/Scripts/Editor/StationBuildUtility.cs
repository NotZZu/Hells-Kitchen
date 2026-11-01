using HellsKitchen.Runtime.Stations;
using HellsKitchen.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// 설비 오브젝트 하나를 조립한다.
    ///
    /// 높이 계산은 전적으로 모델의 실제 바운드에 맡긴다.
    /// KayKit 모델은 피벗이 바닥에 있는 것도 있고 중앙에 있는 것도 있어서,
    /// "크기"가 아니라 "가장 높은 점"과 "가장 낮은 점"을 따로 봐야
    /// 도마와 프라이팬이 상판에 정확히 얹힌다.
    /// </summary>
    public static class StationBuildUtility
    {
        private const string MaterialFolder = "Assets/_Project/Materials";
        private const string HighlightMaterialPath = MaterialFolder + "/StationHighlight.mat";

        public struct Options
        {
            /// <summary>상판에 얹을 소품 (도마, 프라이팬 등)</summary>
            public GameObject TopModel;

            /// <summary>진행바 프리팹</summary>
            public GameObject ProgressPrefab;

            /// <summary>설비 위에 띄울 라벨 프리팹</summary>
            public GameObject LabelPrefab;

            /// <summary>라벨에 표시할 이름. 비우면 라벨을 만들지 않는다.</summary>
            public string LabelText;

            /// <summary>라벨에 표시할 아이콘</summary>
            public Sprite LabelIcon;
        }

        public static T Create<T>(
            string name,
            Transform parent,
            Vector3 position,
            float yawDegrees,
            GameObject bodyModel,
            Options options = default) where T : Station
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            // 기본값 — 모델이 없을 때를 대비한 대략적인 조리대 크기.
            var bodyBounds = new Bounds(new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.9f, 1f));

            if (bodyModel != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(bodyModel);
                model.name = "Body";
                model.transform.SetParent(root.transform, false);

                var measured = EditorAssetUtility.MeasureBounds(bodyModel);
                if (EditorAssetUtility.LooksReasonable(measured))
                {
                    bodyBounds = measured;
                }
                else
                {
                    // 측정값이 터무니없으면 배치를 조용히 망가뜨리는 대신 알리고 기본값을 쓴다.
                    Debug.LogWarning(
                        $"[Hell's Kitchen] '{bodyModel.name}' 의 크기 측정값이 이상합니다({measured.size}). " +
                        "임포트 설정을 확인해 주세요. 일단 기본 크기로 배치합니다.");
                }
            }

            // 물건을 올려놓을 표면의 높이. 크기가 아니라 가장 높은 점이다.
            var surfaceY = bodyBounds.max.y;

            if (options.TopModel != null)
            {
                var top = (GameObject)PrefabUtility.InstantiatePrefab(options.TopModel);
                top.name = "Top";
                top.transform.SetParent(root.transform, false);

                var topBounds = EditorAssetUtility.MeasureBounds(options.TopModel);

                // 소품의 바닥(min.y)이 상판에 닿도록 내린다.
                // 피벗이 모델 중앙에 있으면 이 보정이 없을 때 절반이 떠 버린다.
                top.transform.localPosition = new Vector3(0f, surfaceY - topBounds.min.y, 0f);

                surfaceY += topBounds.size.y;
            }

            // 충돌과 상호작용 감지를 같은 콜라이더로 처리한다.
            var box = root.AddComponent<BoxCollider>();
            box.center = bodyBounds.center;
            box.size = new Vector3(
                Mathf.Max(0.2f, bodyBounds.size.x),
                Mathf.Max(0.2f, bodyBounds.size.y),
                Mathf.Max(0.2f, bodyBounds.size.z));

            var placePoint = new GameObject("PlacePoint");
            placePoint.transform.SetParent(root.transform, false);
            placePoint.transform.localPosition = new Vector3(0f, surfaceY + 0.005f, 0f);

            var highlight = CreateHighlight(root.transform, bodyBounds);

            var station = root.AddComponent<T>();
            station.EditorSetPlacePoint(placePoint.transform);
            station.EditorSetHighlight(highlight);

            if (options.ProgressPrefab != null)
            {
                var progress = (GameObject)PrefabUtility.InstantiatePrefab(options.ProgressPrefab);
                progress.transform.SetParent(root.transform, false);
                progress.transform.localPosition = new Vector3(0f, surfaceY + 0.45f, 0f);

                var view = progress.GetComponent<StationProgressView>();
                var fill = progress.transform.Find("Back/Fill")?.GetComponent<Image>();
                var group = progress.GetComponent<CanvasGroup>();
                if (view != null) view.EditorConfigure(station, fill, group);
            }

            if (options.LabelPrefab != null && !string.IsNullOrEmpty(options.LabelText))
            {
                var label = (GameObject)PrefabUtility.InstantiatePrefab(options.LabelPrefab);
                label.name = "Label";
                label.transform.SetParent(root.transform, false);

                // 라벨은 설비 회전과 무관하게 항상 카메라를 향해야 하므로
                // 부모 회전을 상쇄해 둔다(런타임에는 WorldLabel 이 매 프레임 맞춘다).
                label.transform.localRotation = Quaternion.Inverse(root.transform.rotation);
                label.transform.localPosition = new Vector3(0f, surfaceY + 0.75f, 0f);

                var view = label.GetComponent<WorldLabel>();
                if (view != null) view.EditorConfigure(options.LabelText, options.LabelIcon);
            }

            return station;
        }

        /// <summary>
        /// 바라보는 설비를 알리는 표시.
        ///
        /// 상판 위에 깔면 안 된다. 물건을 놓는 지점이 바로 그 높이라
        /// 올려놓은 재료가 이 판에 통째로 덮여 보이지 않게 된다.
        /// 그래서 설비 발치에 깔리는 얇은 링으로 만든다.
        /// </summary>
        private static GameObject CreateHighlight(Transform parent, Bounds bodyBounds)
        {
            var highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlight.name = "Highlight";
            highlight.transform.SetParent(parent, false);
            highlight.transform.localPosition = new Vector3(bodyBounds.center.x, 0.02f, bodyBounds.center.z);
            highlight.transform.localScale = new Vector3(bodyBounds.size.x * 1.12f, 0.04f, bodyBounds.size.z * 1.12f);

            // 하이라이트는 물리에 참여하면 안 된다.
            var collider = highlight.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            var renderer = highlight.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetHighlightMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            highlight.SetActive(false);
            return highlight;
        }

        private static Material GetHighlightMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath);
            if (existing != null) return existing;

            EditorAssetUtility.EnsureFolder(MaterialFolder);

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var material = new Material(shader) { name = "StationHighlight" };

            // URP Unlit 을 반투명으로 세팅한다.
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f); // Transparent
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);     // Alpha
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(1f, 0.82f, 0.2f, 0.75f));
            material.color = new Color(1f, 0.82f, 0.2f, 0.75f);
            material.renderQueue = 3000;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            AssetDatabase.CreateAsset(material, HighlightMaterialPath);
            return material;
        }
    }
}
