using System.Collections.Generic;
using System.IO;
using HellsKitchen.Data;
using UnityEditor;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// 재료 모델을 정면에서 찍어 주문 티켓용 아이콘 스프라이트를 만든다.
    ///
    /// 아이콘을 따로 그리지 않고 3D 모델을 그대로 쓰는 이유는,
    /// 티켓에 보이는 그림과 주방에 놓인 물건이 정확히 같아야
    /// 플레이어가 티켓을 보고 즉시 재료를 찾을 수 있기 때문이다.
    /// </summary>
    public static class IngredientIconBaker
    {
        private const string IconFolder = "Assets/_Project/Data/Icons";
        private const int IconSize = 256;
        private const int RenderLayer = 31;

        [MenuItem("Hell's Kitchen/3. 재료 아이콘 굽기", priority = 30)]
        public static void BakeAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<IngredientCatalog>(HellsKitchenDataSetup.CatalogPath);
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("먼저 데이터를 만들어 주세요",
                    "메뉴에서 '1. 데이터 에셋 생성' 을 먼저 실행해 주세요.", "확인");
                return;
            }

            EditorAssetUtility.EnsureFolder(IconFolder);

            var created = new List<string>();

            try
            {
                for (int i = 0; i < catalog.Definitions.Count; i++)
                {
                    var definition = catalog.Definitions[i];
                    if (definition == null) continue;

                    // 티켓에 나오는 형태를 우선으로 찍는다.
                    // 다만 손질하면 다른 재료가 되는 것(고기 -> 패티)은 손질 전 모습이 맞다.
                    // 그 아이콘은 재료 상자 이름표에 붙기 때문이다.
                    GameObject model;
                    if (definition.ChopResult != null)
                    {
                        model = definition.RawModel;
                    }
                    else
                    {
                        model = definition.ChoppedModel != null ? definition.ChoppedModel
                            : (definition.CookedModel != null ? definition.CookedModel : definition.RawModel);
                    }

                    if (model == null) continue;

                    EditorUtility.DisplayProgressBar(
                        "아이콘 굽는 중", definition.ResolvedName, (float)i / catalog.Definitions.Count);

                    var path = $"{IconFolder}/Icon_{definition.Type}.png";
                    if (!Render(model, path)) continue;

                    created.Add(path);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();

            // 임포트가 끝난 뒤에 스프라이트를 연결한다.
            for (int i = 0; i < catalog.Definitions.Count; i++)
            {
                var definition = catalog.Definitions[i];
                if (definition == null) continue;

                var path = $"{IconFolder}/Icon_{definition.Type}.png";
                ConfigureAsSprite(path);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;

                definition.Icon = sprite;
                EditorUtility.SetDirty(definition);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Hell's Kitchen] 아이콘 {created.Count}개를 구웠습니다.");
        }

        private static bool Render(GameObject model, string outputPath)
        {
            var stageRoot = new GameObject("IconStage") { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture renderTexture = null;
            Texture2D readback = null;

            try
            {
                var instance = Object.Instantiate(model, stageRoot.transform);
                instance.transform.localPosition = Vector3.zero;

                // 프리팹이 가진 축 보정 회전을 지우면 안 된다.
                // 보여 주고 싶은 각도는 그 위에 얹는다.
                instance.transform.rotation = Quaternion.Euler(0f, 35f, 0f) * instance.transform.rotation;

                SetLayerRecursive(instance, RenderLayer);

                var bounds = CalculateBounds(instance);
                var radius = Mathf.Max(0.05f, bounds.extents.magnitude);

                var cameraGo = new GameObject("IconCamera") { hideFlags = HideFlags.HideAndDontSave };
                cameraGo.transform.SetParent(stageRoot.transform, false);

                var camera = cameraGo.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = radius * 1.02f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.cullingMask = 1 << RenderLayer;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = radius * 12f + 5f;

                var direction = Quaternion.Euler(22f, -30f, 0f) * Vector3.forward;
                cameraGo.transform.position = bounds.center - direction * (radius * 4f);
                cameraGo.transform.rotation = Quaternion.LookRotation(direction);

                var lightGo = new GameObject("IconLight") { hideFlags = HideFlags.HideAndDontSave };
                lightGo.transform.SetParent(stageRoot.transform, false);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.3f;
                light.cullingMask = 1 << RenderLayer;
                lightGo.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

                renderTexture = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4
                };

                camera.targetTexture = renderTexture;
                camera.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = renderTexture;

                readback = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
                readback.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
                readback.Apply();

                RenderTexture.active = previous;
                camera.targetTexture = null;

                File.WriteAllBytes(outputPath, readback.EncodeToPNG());
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Hell's Kitchen] 아이콘 렌더에 실패했습니다: {outputPath}\n{e.Message}");
                return false;
            }
            finally
            {
                if (readback != null) Object.DestroyImmediate(readback);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }
                Object.DestroyImmediate(stageRoot);
            }
        }

        private static void ConfigureAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Bounds CalculateBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.one * 0.3f);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void SetLayerRecursive(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform) SetLayerRecursive(child.gameObject, layer);
        }
    }
}
