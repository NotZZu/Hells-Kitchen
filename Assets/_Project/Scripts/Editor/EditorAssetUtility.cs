using System.IO;
using UnityEditor;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>에셋 생성 툴이 공통으로 쓰는 작은 도우미들.</summary>
    public static class EditorAssetUtility
    {
        /// <summary>경로의 모든 상위 폴더를 만든다. 이미 있으면 아무것도 하지 않는다.</summary>
        public static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>
        /// 이미 있으면 불러오고, 없으면 새로 만든다.
        /// 툴을 여러 번 돌려도 인스펙터에서 손댄 값이 날아가지 않게 하는 것이 목적이다.
        /// </summary>
        public static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null) return existing;

            var folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder)) EnsureFolder(folder);

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        /// <summary>
        /// 모델이 실제로 차지하는 크기를 잰다. 배치 높이와 간격을 자동으로 정하는 데 쓴다.
        ///
        /// 주의: 인스턴스의 회전을 절대 건드리지 말 것.
        /// KayKit FBX 는 Z-up 으로 만들어져 있고 Bake Axis Conversion 이 꺼져 있어서,
        /// 축을 바로잡는 -90도 회전이 프리팹 루트 트랜스폼에 들어 있다.
        /// 회전을 identity 로 덮어쓰면 모델이 눕혀진 채로 측정되어
        /// 캐릭터 키가 2.7m, 조리대 폭이 2m 로 나오는 식으로 전부 틀어진다.
        /// </summary>
        public static Bounds MeasureBounds(GameObject prefab)
        {
            if (prefab == null) return new Bounds(Vector3.zero, Vector3.zero);

            // Instantiate 는 프리팹이 가진 로컬 회전과 배율을 그대로 유지한다.
            // 위치만 원점으로 옮기면 월드 바운드가 곧 배치했을 때의 바운드가 된다.
            var temp = Object.Instantiate(prefab);
            temp.hideFlags = HideFlags.HideAndDontSave;
            temp.transform.position = Vector3.zero;

            var renderers = temp.GetComponentsInChildren<Renderer>();
            Bounds bounds;

            if (renderers.Length == 0)
            {
                bounds = new Bounds(new Vector3(0f, 0.45f, 0f), new Vector3(1f, 0.9f, 1f));
            }
            else
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }

            Object.DestroyImmediate(temp);
            return bounds;
        }

        /// <summary>
        /// 측정값이 상식적인지 확인한다. 에셋이 교체되거나 임포트 설정이 바뀌어
        /// 터무니없는 크기가 나왔을 때 배치가 조용히 망가지는 것을 막는다.
        /// </summary>
        public static bool LooksReasonable(Bounds bounds, float minSize = 0.05f, float maxSize = 12f)
        {
            var size = bounds.size;
            if (size.x < minSize || size.y < minSize || size.z < minSize) return false;
            if (size.x > maxSize || size.y > maxSize || size.z > maxSize) return false;
            return true;
        }
    }
}
