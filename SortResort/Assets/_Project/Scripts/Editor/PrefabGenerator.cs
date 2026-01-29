using UnityEngine;
using UnityEditor;
using System.IO;

namespace SortResort.Editor
{
    /// <summary>
    /// Editor utility to generate prefabs for Items, Slots, and Containers.
    /// Run from menu: Tools > Sort Resort > Generate Prefabs
    /// </summary>
    public class PrefabGenerator : MonoBehaviour
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string ItemPrefabPath = PrefabFolder + "/Item.prefab";
        private const string SlotPrefabPath = PrefabFolder + "/Slot.prefab";
        private const string ContainerPrefabPath = PrefabFolder + "/Container.prefab";
        private const string SingleSlotContainerPrefabPath = PrefabFolder + "/SingleSlotContainer.prefab";

        [MenuItem("Tools/Sort Resort/Generate Prefabs")]
        public static void GenerateAllPrefabs()
        {
            // Ensure prefab folder exists
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }

            GenerateItemPrefab();
            GenerateSlotPrefab();
            GenerateContainerPrefab();
            GenerateSingleSlotContainerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PrefabGenerator] All prefabs generated successfully!");
            EditorUtility.DisplayDialog("Prefab Generator",
                "Prefabs generated successfully!\n\n" +
                "- Item.prefab\n" +
                "- Slot.prefab\n" +
                "- Container.prefab\n" +
                "- SingleSlotContainer.prefab\n\n" +
                "Location: Assets/_Project/Prefabs/", "OK");
        }

        [MenuItem("Tools/Sort Resort/Generate Item Prefab")]
        public static void GenerateItemPrefab()
        {
            // Create the item GameObject
            var itemGO = new GameObject("Item");

            // Add SpriteRenderer
            var sr = itemGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            // Add BoxCollider2D
            var col = itemGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f); // Will be resized when sprite is set

            // Add Item component
            itemGO.AddComponent<Item>();

            // Set layer (Items = 6)
            itemGO.layer = 6;

            // Save as prefab
            SavePrefab(itemGO, ItemPrefabPath);

            // Clean up
            DestroyImmediate(itemGO);

            Debug.Log("[PrefabGenerator] Item prefab generated: " + ItemPrefabPath);
        }

        [MenuItem("Tools/Sort Resort/Generate Slot Prefab")]
        public static void GenerateSlotPrefab()
        {
            // Create the slot GameObject
            var slotGO = new GameObject("Slot");

            // Add SpriteRenderer for visual feedback (optional highlight)
            var sr = slotGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 0;
            sr.color = new Color(1f, 1f, 1f, 0.1f); // Very subtle

            // Add BoxCollider2D
            var col = slotGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.28f, 2.56f); // Default slot size

            // Add Slot component
            slotGO.AddComponent<Slot>();

            // Set layer (Slots = 7)
            slotGO.layer = 7;

            // Save as prefab
            SavePrefab(slotGO, SlotPrefabPath);

            // Clean up
            DestroyImmediate(slotGO);

            Debug.Log("[PrefabGenerator] Slot prefab generated: " + SlotPrefabPath);
        }

        [MenuItem("Tools/Sort Resort/Generate Container Prefab")]
        public static void GenerateContainerPrefab()
        {
            // Create the container GameObject
            var containerGO = new GameObject("Container");

            // Create shelf sprite child (for visual)
            var shelfGO = new GameObject("ShelfSprite");
            shelfGO.transform.SetParent(containerGO.transform);
            shelfGO.transform.localPosition = Vector3.zero;

            var shelfSR = shelfGO.AddComponent<SpriteRenderer>();
            shelfSR.sortingOrder = -1;
            // Sprite will be assigned when container is configured

            // Create Slots parent (slots are created dynamically by ItemContainer.Initialize)
            var slotsParent = new GameObject("Slots");
            slotsParent.transform.SetParent(containerGO.transform);
            slotsParent.transform.localPosition = Vector3.zero;

            // Add ItemContainer component and wire up references
            var itemContainer = containerGO.AddComponent<ItemContainer>();

            // Use SerializedObject to set private serialized fields
            var so = new SerializedObject(itemContainer);
            so.FindProperty("containerSprite").objectReferenceValue = shelfSR;
            so.FindProperty("slotsParent").objectReferenceValue = slotsParent.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            SavePrefab(containerGO, ContainerPrefabPath);

            // Clean up
            DestroyImmediate(containerGO);

            Debug.Log("[PrefabGenerator] Container prefab generated: " + ContainerPrefabPath);
        }

        [MenuItem("Tools/Sort Resort/Generate Single Slot Container Prefab")]
        public static void GenerateSingleSlotContainerPrefab()
        {
            // Create the container GameObject
            var containerGO = new GameObject("SingleSlotContainer");

            // Create shelf sprite child (for visual)
            var shelfGO = new GameObject("ShelfSprite");
            shelfGO.transform.SetParent(containerGO.transform);
            shelfGO.transform.localPosition = Vector3.zero;

            var shelfSR = shelfGO.AddComponent<SpriteRenderer>();
            shelfSR.sortingOrder = -1;

            // Create Slots parent
            var slotsParent = new GameObject("Slots");
            slotsParent.transform.SetParent(containerGO.transform);
            slotsParent.transform.localPosition = Vector3.zero;

            // Add ItemContainer component and wire up references
            var itemContainer = containerGO.AddComponent<ItemContainer>();

            // Use SerializedObject to set private serialized fields
            var so = new SerializedObject(itemContainer);
            so.FindProperty("containerSprite").objectReferenceValue = shelfSR;
            so.FindProperty("slotsParent").objectReferenceValue = slotsParent.transform;
            so.FindProperty("slotCount").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            SavePrefab(containerGO, SingleSlotContainerPrefabPath);

            // Clean up
            DestroyImmediate(containerGO);

            Debug.Log("[PrefabGenerator] SingleSlotContainer prefab generated: " + SingleSlotContainerPrefabPath);
        }

        private static void SavePrefab(GameObject go, string path)
        {
            // Check if prefab already exists
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existingPrefab != null)
            {
                // Update existing prefab
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            else
            {
                // Create new prefab
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
        }
    }
}
