using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    [CreateAssetMenu(fileName = "New World", menuName = "Sort Resort/World Data")]
    public class WorldData : ScriptableObject
    {
        [Header("Identity")]
        public string worldID = "W00";
        public string worldName = "Island";
        public int worldNumber = 0;

        [Header("Theme")]
        public WorldTheme theme = WorldTheme.Island;

        [Header("Folder Configuration")]
        public string worldFolderPath = "00_Island";

        [Header("World Description")]
        [TextArea(3, 6)]
        public string worldDescription = "Welcome to paradise!";

        [Header("Visuals")]
        public Sprite worldIcon;
        public Sprite worldIconLocked;
        public Sprite gameplayBackground;
        public Sprite menuBackground;
        public Color worldThemeColor = Color.white;

        [Header("Mascot")]
        public Sprite mascotIdle;
        public Sprite mascotHappy;
        public Sprite mascotSad;
        public Sprite mascotExcited;
        public RuntimeAnimatorController mascotAnimator;

        [Header("Audio")]
        public AudioClip worldThemeMusic;
        public AudioClip victoryMusic;
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;

        [Header("Content")]
        public List<ItemData> availableItems = new List<ItemData>();
        public List<ContainerData> availableContainers = new List<ContainerData>();

        [Header("Progression")]
        public bool isDefaultWorld = true;
        public bool isPurchased = false;
        public float purchasePrice = 2.99f;
        public int totalLevels = 100;

        [Header("Special Features")]
        public bool hasMovingContainers = false;
        public bool hasParallaxBackground = false;
        public bool hasAmbientSounds = false;
        public List<AudioClip> ambientSounds = new List<AudioClip>();

        public string GetLevelJSONPath(int levelNumber)
        {
            string levelNum = levelNumber.ToString("D3");
            return "Levels/" + worldFolderPath + "/JSON/" + worldID + "_Level_" + levelNum;
        }
    }

    public enum WorldTheme
    {
        Island,
        Farm,
        Supermarket,
        Medieval,
        Space
    }
}
