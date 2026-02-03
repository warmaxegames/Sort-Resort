using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SortResort
{
    /// <summary>
    /// Handles the animated level complete background with rays and curtains
    /// </summary>
    public class AnimatedLevelComplete : MonoBehaviour
    {
        private Image raysImage;
        private Image curtainsImage;

        private Sprite[] raysFrames;
        private Sprite[] curtainsFrames;

        private int currentRaysFrame = 0;
        private int currentCurtainsFrame = 0;

        private bool isPlaying = false;
        private bool raysComplete = false;
        private bool curtainsComplete = false;

        private float raysFrameRate = 30f; // FPS for rays animation
        private float curtainsFrameRate = 24f; // FPS for curtains animation

        private float raysTimer = 0f;
        private float curtainsTimer = 0f;

        private static Sprite[] cachedRaysFrames;
        private static Sprite[] cachedCurtainsFrames;

        public void Initialize(Image raysImg, Image curtainsImg)
        {
            raysImage = raysImg;
            curtainsImage = curtainsImg;

            LoadFrames();

            // Set initial frames
            if (raysFrames != null && raysFrames.Length > 0)
            {
                raysImage.sprite = raysFrames[0];
            }

            if (curtainsFrames != null && curtainsFrames.Length > 0)
            {
                curtainsImage.sprite = curtainsFrames[0];
            }
        }

        private void LoadFrames()
        {
            // Use cached frames if available
            if (cachedRaysFrames == null)
            {
                // Load rays frames
                var rayTextures = Resources.LoadAll<Texture2D>("Sprites/UI/LevelComplete/Rays");
                cachedRaysFrames = new Sprite[rayTextures.Length];

                // Sort by name to ensure correct order
                System.Array.Sort(rayTextures, (a, b) => a.name.CompareTo(b.name));

                for (int i = 0; i < rayTextures.Length; i++)
                {
                    var tex = rayTextures[i];
                    cachedRaysFrames[i] = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }

                Debug.Log($"[AnimatedLevelComplete] Loaded {cachedRaysFrames.Length} rays frames");
            }

            if (cachedCurtainsFrames == null)
            {
                // Load curtains frames
                var curtainTextures = Resources.LoadAll<Texture2D>("Sprites/UI/LevelComplete/Curtains");
                cachedCurtainsFrames = new Sprite[curtainTextures.Length];

                // Sort by name to ensure correct order
                System.Array.Sort(curtainTextures, (a, b) => a.name.CompareTo(b.name));

                for (int i = 0; i < curtainTextures.Length; i++)
                {
                    var tex = curtainTextures[i];
                    cachedCurtainsFrames[i] = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                }

                Debug.Log($"[AnimatedLevelComplete] Loaded {cachedCurtainsFrames.Length} curtains frames");
            }

            raysFrames = cachedRaysFrames;
            curtainsFrames = cachedCurtainsFrames;
        }

        public void Play()
        {
            isPlaying = true;
            raysComplete = false;
            curtainsComplete = false;
            currentRaysFrame = 0;
            currentCurtainsFrame = 0;
            raysTimer = 0f;
            curtainsTimer = 0f;

            // Show both images
            if (raysImage != null) raysImage.gameObject.SetActive(true);
            if (curtainsImage != null) curtainsImage.gameObject.SetActive(true);
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
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Update rays animation (plays once)
            if (!raysComplete && raysFrames != null && raysFrames.Length > 0)
            {
                raysTimer += Time.unscaledDeltaTime;
                float raysFrameTime = 1f / raysFrameRate;

                if (raysTimer >= raysFrameTime)
                {
                    raysTimer -= raysFrameTime;
                    currentRaysFrame++;

                    if (currentRaysFrame >= raysFrames.Length)
                    {
                        // Stay on last frame
                        currentRaysFrame = raysFrames.Length - 1;
                        raysComplete = true;
                    }

                    raysImage.sprite = raysFrames[currentRaysFrame];
                }
            }

            // Update curtains animation (plays once)
            if (!curtainsComplete && curtainsFrames != null && curtainsFrames.Length > 0)
            {
                curtainsTimer += Time.unscaledDeltaTime;
                float curtainsFrameTime = 1f / curtainsFrameRate;

                if (curtainsTimer >= curtainsFrameTime)
                {
                    curtainsTimer -= curtainsFrameTime;
                    currentCurtainsFrame++;

                    if (currentCurtainsFrame >= curtainsFrames.Length)
                    {
                        // Stay on last frame
                        currentCurtainsFrame = curtainsFrames.Length - 1;
                        curtainsComplete = true;
                    }

                    curtainsImage.sprite = curtainsFrames[currentCurtainsFrame];
                }
            }
        }
    }
}
