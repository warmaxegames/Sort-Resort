using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SortResort
{
    /// <summary>
    /// Handles the animated level complete background with rays, curtains, level board, and star ribbon
    /// </summary>
    public class AnimatedLevelComplete : MonoBehaviour
    {
        private Image raysImage;
        private Image curtainsImage;
        private Image levelBoardImage;
        private Image starRibbonImage;

        private Sprite[] raysFrames;
        private Sprite[] curtainsFrames;
        private Sprite[] levelBoardFrames;
        private Sprite[] starRibbonFrames;

        private int currentRaysFrame = 0;
        private int currentCurtainsFrame = 0;
        private int currentLevelBoardFrame = 0;
        private int currentStarRibbonFrame = 0;

        private bool isPlaying = false;
        private bool raysComplete = false;
        private bool curtainsComplete = false;
        private bool levelBoardComplete = false;
        private bool starRibbonComplete = false;

        private float raysFrameRate = 30f;
        private float curtainsFrameRate = 24f;
        private float levelBoardFrameRate = 24f;
        private float starRibbonFrameRate = 24f;

        private float raysTimer = 0f;
        private float curtainsTimer = 0f;
        private float levelBoardTimer = 0f;
        private float starRibbonTimer = 0f;

        private static Sprite[] cachedRaysFrames;
        private static Sprite[] cachedCurtainsFrames;
        private static Sprite[] cachedLevelBoardFrames;
        private static Sprite[] cachedStarRibbonFrames;

        public void Initialize(Image raysImg, Image curtainsImg)
        {
            raysImage = raysImg;
            curtainsImage = curtainsImg;

            LoadFrames();

            // Set initial frames
            if (raysFrames != null && raysFrames.Length > 0)
                raysImage.sprite = raysFrames[0];

            if (curtainsFrames != null && curtainsFrames.Length > 0)
                curtainsImage.sprite = curtainsFrames[0];
        }

        public void SetLevelBoardImage(Image img)
        {
            levelBoardImage = img;
            if (levelBoardFrames != null && levelBoardFrames.Length > 0)
                levelBoardImage.sprite = levelBoardFrames[0];
        }

        public void SetStarRibbonImage(Image img)
        {
            starRibbonImage = img;
            if (starRibbonFrames != null && starRibbonFrames.Length > 0)
                starRibbonImage.sprite = starRibbonFrames[0];
        }

        private void LoadFrames()
        {
            if (cachedRaysFrames == null)
                cachedRaysFrames = LoadFramesFromFolder("Sprites/UI/LevelComplete/Rays", "rays");

            if (cachedCurtainsFrames == null)
                cachedCurtainsFrames = LoadFramesFromFolder("Sprites/UI/LevelComplete/Curtains", "curtains");

            if (cachedLevelBoardFrames == null)
                cachedLevelBoardFrames = LoadFramesFromFolder("Sprites/UI/LevelComplete/LevelBoard", "level board");

            if (cachedStarRibbonFrames == null)
                cachedStarRibbonFrames = LoadFramesFromFolder("Sprites/UI/LevelComplete/StarRibbon", "star ribbon");

            raysFrames = cachedRaysFrames;
            curtainsFrames = cachedCurtainsFrames;
            levelBoardFrames = cachedLevelBoardFrames;
            starRibbonFrames = cachedStarRibbonFrames;
        }

        private static Sprite[] LoadFramesFromFolder(string resourcePath, string label)
        {
            var textures = Resources.LoadAll<Texture2D>(resourcePath);
            if (textures.Length == 0)
            {
                Debug.LogWarning($"[AnimatedLevelComplete] No frames found at: {resourcePath}");
                return new Sprite[0];
            }

            System.Array.Sort(textures, (a, b) => a.name.CompareTo(b.name));

            var sprites = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                var tex = textures[i];
                sprites[i] = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
            }

            Debug.Log($"[AnimatedLevelComplete] Loaded {sprites.Length} {label} frames");
            return sprites;
        }

        public void Play()
        {
            isPlaying = true;
            raysComplete = false;
            curtainsComplete = false;
            levelBoardComplete = false;
            starRibbonComplete = false;
            currentRaysFrame = 0;
            currentCurtainsFrame = 0;
            currentLevelBoardFrame = 0;
            currentStarRibbonFrame = 0;
            raysTimer = 0f;
            curtainsTimer = 0f;
            levelBoardTimer = 0f;
            starRibbonTimer = 0f;

            if (raysImage != null) raysImage.gameObject.SetActive(true);
            if (curtainsImage != null) curtainsImage.gameObject.SetActive(true);
            if (levelBoardImage != null) levelBoardImage.gameObject.SetActive(true);
            if (starRibbonImage != null) starRibbonImage.gameObject.SetActive(true);
        }

        public void Stop()
        {
            isPlaying = false;
        }

        public void Hide()
        {
            isPlaying = false;
            if (raysImage != null) raysImage.gameObject.SetActive(false);
            if (curtainsImage != null) curtainsImage.gameObject.SetActive(false);
            if (levelBoardImage != null) levelBoardImage.gameObject.SetActive(false);
            if (starRibbonImage != null) starRibbonImage.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isPlaying) return;

            UpdateLayer(ref currentRaysFrame, ref raysTimer, ref raysComplete, raysFrames, raysImage, raysFrameRate);
            UpdateLayer(ref currentCurtainsFrame, ref curtainsTimer, ref curtainsComplete, curtainsFrames, curtainsImage, curtainsFrameRate);
            UpdateLayer(ref currentLevelBoardFrame, ref levelBoardTimer, ref levelBoardComplete, levelBoardFrames, levelBoardImage, levelBoardFrameRate);
            UpdateLayer(ref currentStarRibbonFrame, ref starRibbonTimer, ref starRibbonComplete, starRibbonFrames, starRibbonImage, starRibbonFrameRate);
        }

        private void UpdateLayer(ref int currentFrame, ref float timer, ref bool complete, Sprite[] frames, Image image, float frameRate)
        {
            if (complete || frames == null || frames.Length == 0 || image == null) return;

            timer += Time.unscaledDeltaTime;
            float frameTime = 1f / frameRate;

            if (timer >= frameTime)
            {
                timer -= frameTime;
                currentFrame++;

                if (currentFrame >= frames.Length)
                {
                    currentFrame = frames.Length - 1;
                    complete = true;
                }

                image.sprite = frames[currentFrame];
            }
        }
    }
}
