using UnityEditor;
using UnityEngine;

namespace HellsKitchen.EditorTools
{
    /// <summary>
    /// KayKit 에셋 팩의 경로를 한곳에 모아 둔다.
    /// 에셋 팩을 다른 폴더로 옮기거나 유료 버전으로 교체하면 여기만 고치면 된다.
    /// </summary>
    public static class KayKitPaths
    {
        public const string RestaurantFbx =
            "Assets/KayKit_Restaurant_Bits_1.0_FREE/KayKit_Restaurant_Bits_1.0_FREE/Assets/fbx/";

        public const string AdventurerCharacters =
            "Assets/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE/Characters/fbx/";

        public const string SkeletonCharacters =
            "Assets/KayKit_Skeletons_1.1_FREE/KayKit_Skeletons_1.1_FREE/characters/fbx/";

        public const string MediumRigAnimations =
            "Assets/KayKit_Character_Animations_1.1/KayKit_Character_Animations_1.1/Animations/fbx/Rig_Medium/";

        public const string DungeonFbx =
            "Assets/KayKit_Dungeon_Pack_1.1_FREE/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx/";

        /// <summary>레스토랑 팩에서 모델을 불러온다. 없으면 경고를 남기고 null.</summary>
        public static GameObject Restaurant(string fileName) => Load(RestaurantFbx + fileName + ".fbx");

        public static GameObject Adventurer(string fileName) => Load(AdventurerCharacters + fileName + ".fbx");

        public static GameObject Skeleton(string fileName) => Load(SkeletonCharacters + fileName + ".fbx");

        public static GameObject Dungeon(string fileName) => Load(DungeonFbx + fileName + ".fbx");

        public static GameObject Load(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[KayKitPaths] 모델을 찾을 수 없습니다: {assetPath}");
            }
            return asset;
        }
    }
}
