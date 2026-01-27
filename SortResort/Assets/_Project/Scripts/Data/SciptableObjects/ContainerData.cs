using UnityEngine;

[CreateAssetMenu(fileName = "New Container", menuName = "Sort Resort/Container Data")]
public class ContainerData : ScriptableObject
{
    [Header("Identity")]
    public string containerID = "beach_basket";
    public string containerName = "Beach Basket";

    [Header("World Reference")]
    [Tooltip("Which world this container belongs to")]
    public WorldData parentWorld;

    [Header("Configuration")]
    [UnityEngine.Range(1, 3)]
    [Tooltip("How many columns/slots this container has")]
    public int numberOfSlots = 3;

    [Header("Visuals")]
    [Tooltip("The main container sprite")]
    public Sprite containerSprite;

    [Tooltip("Overlay shown when container is locked")]
    public Sprite lockedOverlaySprite;

    [Tooltip("Lock icon shown on locked containers")]
    public Sprite lockIconSprite;

    [Header("Audio")]
    [Tooltip("Sound played when container unlocks")]
    public AudioClip unlockSound;

    [Header("Special Behavior")]
    [Tooltip("Can this container move across the screen?")]
    public bool canMove = false;

    [Tooltip("Can this container rotate?")]
    public bool canRotate = false;
}