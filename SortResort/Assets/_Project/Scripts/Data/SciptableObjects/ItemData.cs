using UnityEngine;

namespace SortResort
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Sort Resort/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemID = "coconut";
        public string itemName = "Coconut";

        [Header("World Reference")]
        [Tooltip("Which world this item belongs to")]
        public WorldData parentWorld;

        [Header("Visuals")]
        [Tooltip("The main sprite for this item")]
        public Sprite itemSprite;

        [Tooltip("Optional shadow sprite")]
        public Sprite itemShadow;

        [Tooltip("Color tint applied to the sprite")]
        public Color tintColor = Color.white;

        [Header("Audio")]
        [Tooltip("Sound when picking up this item")]
        public AudioClip pickupSound;

        [Tooltip("Sound when dropping this item")]
        public AudioClip dropSound;

        [Tooltip("Sound when matching 3 of this item")]
        public AudioClip matchSound;

        [Header("Animation")]
        public AnimationClip idleAnimation;
        public AnimationClip matchAnimation;
        public AnimationClip disappearAnimation;

        [Header("Prefab (Optional)")]
        [Tooltip("Custom prefab for this item. If null, uses default item prefab.")]
        public GameObject customPrefab;
    }
}
