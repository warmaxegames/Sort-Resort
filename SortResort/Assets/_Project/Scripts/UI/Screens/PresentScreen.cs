using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    public class PresentScreen
    {
        // Present box types - extensible for future box types
        private struct PresentBoxType
        {
            public string id;
            public string displayName;
            public string spritePath;
            public System.Func<int> getCount;
            public System.Action useOne;
        }

        private static readonly string[] BoxTypeIds = { "normal" };

        private GameObject panel;
        private MonoBehaviour coroutineHost;
        private Image presentImage;
        private RectTransform presentRect;
        private Button presentButton;
        private Image rewardImage;
        private RectTransform rewardRect;
        private GameObject dismissOverlay;
        private TextMeshProUGUI countText;
        private Image leftArrowImage;
        private Image rightArrowImage;
        private Button leftArrowButton;
        private Button rightArrowButton;
        private Sprite[] openFrames;
        private Sprite[] smokeFrames;
        private Image smokeImage;
        private bool isAnimating;
        private Coroutine animCoroutine;
        private Coroutine shakeCoroutine;
        private Image presentGlowImage;

        // Box type cycling
        private List<PresentBoxType> boxTypes = new List<PresentBoxType>();
        private int currentBoxIndex;
        private Dictionary<string, Sprite> boxSprites = new Dictionary<string, Sprite>();

        // Grey color for disabled arrows
        private static readonly Color ArrowGreyColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);

        public GameObject Panel => panel;
        public bool IsVisible => panel != null && panel.activeSelf;

        public event Action OnClosed;

        public void Create(Transform parent, MonoBehaviour host)
        {
            coroutineHost = host;
            InitBoxTypes();

            panel = new GameObject("Present Screen Panel");
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = panel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5200;
            panel.AddComponent<GraphicRaycaster>();

            // Layer 0: Background (same as daily spin)
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(panel.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            var bgTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/spin_screen_background");
            if (bgTex != null)
            {
                bgImg.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f), 100f);
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = new Color(0.1f, 0.05f, 0.2f);
            }

            // Layer 1: Dark overlay (95% opacity)
            var dimGO = new GameObject("DarkOverlay");
            dimGO.transform.SetParent(panel.transform, false);
            var dimRect = dimGO.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImg = dimGO.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.95f);

            // Layer 2: Reward item (hidden until reveal)
            var rewardGO = new GameObject("RewardItem");
            rewardGO.transform.SetParent(panel.transform, false);
            rewardRect = rewardGO.AddComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(0.5f, 0.5f);
            rewardRect.anchorMax = new Vector2(0.5f, 0.5f);
            rewardRect.pivot = new Vector2(0.5f, 0.5f);
            rewardRect.sizeDelta = new Vector2(300, 300);
            rewardImage = rewardGO.AddComponent<Image>();
            rewardImage.preserveAspect = true;
            rewardImage.raycastTarget = false;
            rewardGO.SetActive(false);

            // Layer 2b: Fullscreen dismiss overlay (tap anywhere to dismiss reward)
            dismissOverlay = new GameObject("DismissOverlay");
            dismissOverlay.transform.SetParent(panel.transform, false);
            var dismissRect = dismissOverlay.AddComponent<RectTransform>();
            dismissRect.anchorMin = Vector2.zero;
            dismissRect.anchorMax = Vector2.one;
            dismissRect.offsetMin = Vector2.zero;
            dismissRect.offsetMax = Vector2.zero;
            var dismissImg = dismissOverlay.AddComponent<Image>();
            dismissImg.color = new Color(0, 0, 0, 0); // Invisible
            dismissImg.raycastTarget = true;
            var dismissBtn = dismissOverlay.AddComponent<Button>();
            dismissBtn.transition = Selectable.Transition.None;
            dismissBtn.onClick.AddListener(OnRewardClicked);
            dismissOverlay.SetActive(false);

            // Layer 3a: Glow behind present box
            var presentGlowGO = new GameObject("PresentGlow");
            presentGlowGO.transform.SetParent(panel.transform, false);
            var presentGlowRect = presentGlowGO.AddComponent<RectTransform>();
            presentGlowRect.anchorMin = new Vector2(0.5f, 0.5f);
            presentGlowRect.anchorMax = new Vector2(0.5f, 0.5f);
            presentGlowRect.pivot = new Vector2(0.5f, 0.5f);
            presentGlowRect.sizeDelta = new Vector2(550, 550);
            presentGlowImage = presentGlowGO.AddComponent<Image>();
            presentGlowImage.sprite = CreateGlowSprite(128);
            presentGlowImage.color = new Color(1f, 0.8f, 0.2f, 0.3f);
            presentGlowImage.raycastTarget = false;

            // Layer 3b: Present box (clickable)
            var presentGO = new GameObject("PresentBox");
            presentGO.transform.SetParent(panel.transform, false);
            presentRect = presentGO.AddComponent<RectTransform>();
            presentRect.anchorMin = new Vector2(0.5f, 0.5f);
            presentRect.anchorMax = new Vector2(0.5f, 0.5f);
            presentRect.pivot = new Vector2(0.5f, 0.5f);
            presentRect.sizeDelta = new Vector2(400, 400);
            presentImage = presentGO.AddComponent<Image>();
            presentImage.preserveAspect = true;
            presentImage.raycastTarget = true;

            presentButton = presentGO.AddComponent<Button>();
            presentButton.transition = Selectable.Transition.None;
            presentButton.onClick.AddListener(OnPresentClicked);

            // Layer 3c: Smoke effect (plays on top of present during open animation)
            var smokeGO = new GameObject("SmokeEffect");
            smokeGO.transform.SetParent(panel.transform, false);
            var smokeRect = smokeGO.AddComponent<RectTransform>();
            smokeRect.anchorMin = new Vector2(0.5f, 0.5f);
            smokeRect.anchorMax = new Vector2(0.5f, 0.5f);
            smokeRect.pivot = new Vector2(0.5f, 0.5f);
            smokeRect.sizeDelta = new Vector2(500, 500);
            smokeImage = smokeGO.AddComponent<Image>();
            smokeImage.preserveAspect = true;
            smokeImage.raycastTarget = false;
            smokeGO.SetActive(false);

            // Layer 4: Count text below present (e.g. "x3") - gold, matching spin reward style
            var countGO = new GameObject("CountText");
            countGO.transform.SetParent(panel.transform, false);
            var countRect = countGO.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0.5f);
            countRect.anchorMax = new Vector2(0.5f, 0.5f);
            countRect.pivot = new Vector2(0.5f, 1f);
            countRect.anchoredPosition = new Vector2(0, -260);
            countRect.sizeDelta = new Vector2(300, 100);
            countText = countGO.AddComponent<TextMeshProUGUI>();
            countText.text = "";
            countText.fontSize = 84;
            countText.fontStyle = FontStyles.Bold;
            countText.alignment = TextAlignmentOptions.Center;
            countText.color = new Color(1f, 0.84f, 0f); // Gold
            countText.raycastTarget = false;
            countText.outlineWidth = 0.25f;
            countText.outlineColor = Color.black;
            if (FontManager.Bold != null)
                countText.font = FontManager.Bold;

            // Layer 5: Navigation arrows (same as world select arrows)
            CreateNavigationArrows(panel.transform);

            // Layer 6: Close button (top-right X)
            CreateCloseButton(panel.transform);

            // Pre-load animation frames
            LoadAnimationFrames();

            panel.SetActive(false);
        }

        private void InitBoxTypes()
        {
            boxTypes.Clear();
            boxSprites.Clear();

            // Normal present box
            var normalTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/normal_present_box");
            Sprite normalSprite = null;
            if (normalTex != null)
                normalSprite = Sprite.Create(normalTex, new Rect(0, 0, normalTex.width, normalTex.height), new Vector2(0.5f, 0.5f), 100f);

            boxTypes.Add(new PresentBoxType
            {
                id = "normal",
                displayName = "Present Box",
                spritePath = "Sprites/UI/LuckySpin/normal_present_box",
                getCount = () => SaveManager.Instance?.GetNormalPresentCount() ?? 0,
                useOne = () => { SaveManager.Instance?.UseNormalPresent(); AchievementManager.Instance?.RecordPresentBoxOpened(); }
            });

            if (normalSprite != null)
                boxSprites["normal"] = normalSprite;

            // Future box types can be added here:
            // boxTypes.Add(new PresentBoxType { id = "gold", ... });

            currentBoxIndex = 0;
        }

        private void CreateNavigationArrows(Transform parent)
        {
            // Left arrow
            var leftGO = new GameObject("LeftArrow");
            leftGO.transform.SetParent(parent, false);
            var leftRect = leftGO.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0.5f);
            leftRect.anchorMax = new Vector2(0, 0.5f);
            leftRect.pivot = new Vector2(0, 0.5f);
            leftRect.anchoredPosition = new Vector2(25, 0);
            leftRect.sizeDelta = new Vector2(153, 176);

            leftArrowImage = leftGO.AddComponent<Image>();
            var leftSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left");
            var leftPressedSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left_pressed");
            if (leftSprite != null)
            {
                leftArrowImage.sprite = leftSprite;
                leftArrowImage.preserveAspect = true;
            }

            leftArrowButton = leftGO.AddComponent<Button>();
            leftArrowButton.targetGraphic = leftArrowImage;
            if (leftSprite != null && leftPressedSprite != null)
            {
                leftArrowButton.transition = Selectable.Transition.SpriteSwap;
                leftArrowButton.spriteState = new SpriteState { pressedSprite = leftPressedSprite };
            }
            leftArrowButton.onClick.AddListener(OnLeftArrowClicked);

            // Right arrow
            var rightGO = new GameObject("RightArrow");
            rightGO.transform.SetParent(parent, false);
            var rightRect = rightGO.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(1, 0.5f);
            rightRect.anchorMax = new Vector2(1, 0.5f);
            rightRect.pivot = new Vector2(1, 0.5f);
            rightRect.anchoredPosition = new Vector2(-25, 0);
            rightRect.sizeDelta = new Vector2(153, 176);

            rightArrowImage = rightGO.AddComponent<Image>();
            var rightSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right");
            var rightPressedSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right_pressed");
            if (rightSprite != null)
            {
                rightArrowImage.sprite = rightSprite;
                rightArrowImage.preserveAspect = true;
            }

            rightArrowButton = rightGO.AddComponent<Button>();
            rightArrowButton.targetGraphic = rightArrowImage;
            if (rightSprite != null && rightPressedSprite != null)
            {
                rightArrowButton.transition = Selectable.Transition.SpriteSwap;
                rightArrowButton.spriteState = new SpriteState { pressedSprite = rightPressedSprite };
            }
            rightArrowButton.onClick.AddListener(OnRightArrowClicked);
        }

        private void CreateCloseButton(Transform parent)
        {
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(parent, false);
            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-60, -60);
            closeRect.sizeDelta = new Vector2(100, 100);

            var closeBtnNormalSprite = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_2");
            var closeBtnPressedSprite = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_pressed");
            var closeImg = closeGO.AddComponent<Image>();
            closeImg.sprite = closeBtnNormalSprite;
            closeImg.raycastTarget = true;

            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(OnCloseClicked);

            var pressHandler = closeGO.AddComponent<EventTrigger>();
            var pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { closeImg.sprite = closeBtnPressedSprite ?? closeBtnNormalSprite; });
            pressHandler.triggers.Add(pointerDown);
            var pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { closeImg.sprite = closeBtnNormalSprite; });
            pressHandler.triggers.Add(pointerUp);
        }

        private void LoadAnimationFrames()
        {
            var textures = Resources.LoadAll<Texture2D>("Sprites/UI/PresentOpen");

            // Sort textures by name first, then create sprites
            var openTextures = new List<Texture2D>();
            var smokeTextures = new List<Texture2D>();
            foreach (var tex in textures)
            {
                if (tex.name.StartsWith("present_open_"))
                    openTextures.Add(tex);
                else if (tex.name.StartsWith("smoke_"))
                    smokeTextures.Add(tex);
            }

            openTextures.Sort((a, b) => a.name.CompareTo(b.name));
            smokeTextures.Sort((a, b) => a.name.CompareTo(b.name));

            openFrames = new Sprite[openTextures.Count];
            for (int i = 0; i < openTextures.Count; i++)
            {
                var tex = openTextures[i];
                openFrames[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            smokeFrames = new Sprite[smokeTextures.Count];
            for (int i = 0; i < smokeTextures.Count; i++)
            {
                var tex = smokeTextures[i];
                smokeFrames[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            Debug.Log($"[PresentScreen] Loaded {openFrames.Length} present open frames, {smokeFrames.Length} smoke frames");
        }

        public void Show()
        {
            if (panel == null) return;

            isAnimating = false;
            // Find the first box type that has presents
            currentBoxIndex = 0;
            for (int i = 0; i < boxTypes.Count; i++)
            {
                if (boxTypes[i].getCount() > 0)
                {
                    currentBoxIndex = i;
                    break;
                }
            }
            ResetToPresent();
            panel.SetActive(true);
        }

        public void Hide()
        {
            if (shakeCoroutine != null)
            {
                coroutineHost.StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }
            if (animCoroutine != null)
            {
                coroutineHost.StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
            isAnimating = false;
            if (panel != null) panel.SetActive(false);
            OnClosed?.Invoke();
        }

        private void ResetToPresent()
        {
            var boxType = boxTypes[currentBoxIndex];

            // Show present box
            if (boxSprites.TryGetValue(boxType.id, out var sprite))
                presentImage.sprite = sprite;
            presentImage.color = Color.white;
            presentRect.localScale = Vector3.one;
            presentRect.localEulerAngles = Vector3.zero;
            presentRect.anchoredPosition = Vector2.zero;
            presentImage.gameObject.SetActive(true);
            if (presentGlowImage != null)
            {
                presentGlowImage.gameObject.SetActive(true);
                presentGlowImage.color = new Color(1f, 0.8f, 0.2f, 0.3f);
            }

            // Hide reward, smoke, and dismiss overlay
            rewardImage.gameObject.SetActive(false);
            dismissOverlay.SetActive(false);
            smokeImage.gameObject.SetActive(false);
            rewardRect.anchoredPosition = Vector2.zero;
            rewardRect.localScale = Vector3.one;
            rewardImage.color = Color.white;

            // Update count and arrows
            UpdateCountText();
            UpdateArrows();

            // Start idle shake animation
            if (shakeCoroutine != null) coroutineHost.StopCoroutine(shakeCoroutine);
            shakeCoroutine = coroutineHost.StartCoroutine(IdleShakeAnimation());
        }

        private void UpdateCountText()
        {
            if (countText == null) return;
            var boxType = boxTypes[currentBoxIndex];
            int count = boxType.getCount();
            countText.text = $"x{count}";
            countText.gameObject.SetActive(true);
        }

        private void UpdateArrows()
        {
            bool multipleTypes = boxTypes.Count > 1;

            if (leftArrowImage != null)
                leftArrowImage.color = multipleTypes ? Color.white : ArrowGreyColor;
            if (rightArrowImage != null)
                rightArrowImage.color = multipleTypes ? Color.white : ArrowGreyColor;
            if (leftArrowButton != null)
                leftArrowButton.interactable = multipleTypes;
            if (rightArrowButton != null)
                rightArrowButton.interactable = multipleTypes;
        }

        private void OnLeftArrowClicked()
        {
            if (isAnimating || boxTypes.Count <= 1) return;
            AudioManager.Instance?.PlayButtonClick();
            currentBoxIndex = (currentBoxIndex - 1 + boxTypes.Count) % boxTypes.Count;
            ResetToPresent();
        }

        private void OnRightArrowClicked()
        {
            if (isAnimating || boxTypes.Count <= 1) return;
            AudioManager.Instance?.PlayButtonClick();
            currentBoxIndex = (currentBoxIndex + 1) % boxTypes.Count;
            ResetToPresent();
        }

        private void OnCloseClicked()
        {
            if (isAnimating) return;
            AudioManager.Instance?.PlayButtonClick();
            Hide();
        }

        private void OnPresentClicked()
        {
            if (isAnimating) return;
            var boxType = boxTypes[currentBoxIndex];
            if (boxType.getCount() <= 0) return;

            // Consume the present
            boxType.useOne();

            // Determine reward (for now always middlefinger)
            string rewardSpritePath = "Sprites/UI/PresentOpen/middlefinger";

            animCoroutine = coroutineHost.StartCoroutine(PlayOpenAnimation(rewardSpritePath));
        }

        private void OnRewardClicked()
        {
            if (isAnimating) return;
            animCoroutine = coroutineHost.StartCoroutine(DismissRewardAnimation());
        }

        private IEnumerator PlayOpenAnimation(string rewardSpritePath)
        {
            isAnimating = true;

            // Stop idle shake
            if (shakeCoroutine != null)
            {
                coroutineHost.StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }
            presentRect.localEulerAngles = Vector3.zero;
            presentRect.anchoredPosition = Vector2.zero;
            presentRect.localScale = Vector3.one;

            // Hide count text and arrows during animation
            if (countText != null) countText.gameObject.SetActive(false);
            if (leftArrowImage != null) leftArrowImage.gameObject.SetActive(false);
            if (rightArrowImage != null) rightArrowImage.gameObject.SetActive(false);

            // Phase 1: Play present open + smoke animations simultaneously
            float frameDuration = 1f / 24f;
            int maxFrames = Mathf.Max(
                openFrames != null ? openFrames.Length : 0,
                smokeFrames != null ? smokeFrames.Length : 0);

            // Play open present sound at start of animation
            var openPresentClip = Resources.Load<AudioClip>("Audio/SFX/open_present");
            if (openPresentClip != null)
                AudioManager.Instance?.PlaySFX(openPresentClip);

            // Show smoke and ramp up glow strongly during open
            if (smokeFrames != null && smokeFrames.Length > 0)
                smokeImage.gameObject.SetActive(true);

            // Strong glow during opening
            if (presentGlowImage != null)
                presentGlowImage.gameObject.SetActive(true);

            for (int i = 0; i < maxFrames; i++)
            {
                if (openFrames != null && i < openFrames.Length)
                    presentImage.sprite = openFrames[i];
                if (smokeFrames != null && i < smokeFrames.Length)
                    smokeImage.sprite = smokeFrames[i];

                // Glow intensifies through the open animation
                if (presentGlowImage != null)
                {
                    float glowT = (float)i / maxFrames;
                    float glowAlpha = Mathf.Lerp(0.4f, 1f, glowT);
                    float glowScale = Mathf.Lerp(1f, 1.5f, glowT);
                    presentGlowImage.color = new Color(1f, 0.9f, 0.4f, glowAlpha);
                    presentGlowImage.rectTransform.localScale = new Vector3(glowScale, glowScale, 1f);
                }

                yield return new WaitForSeconds(frameDuration);
            }

            // Hide smoke after animation
            smokeImage.gameObject.SetActive(false);

            // Phase 2: Fade out the present box + glow

            float fadeTime = 0.3f;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeTime);
                presentImage.color = new Color(1f, 1f, 1f, alpha);
                // Fade glow out too
                if (presentGlowImage != null)
                    presentGlowImage.color = new Color(1f, 0.9f, 0.4f, alpha);
                yield return null;
            }
            presentImage.gameObject.SetActive(false);
            if (presentGlowImage != null)
            {
                presentGlowImage.gameObject.SetActive(false);
                presentGlowImage.rectTransform.localScale = Vector3.one;
            }

            // Phase 3: Show reward item - grow from small to full size
            var rewardTex = Resources.Load<Texture2D>(rewardSpritePath);
            if (rewardTex != null)
                rewardImage.sprite = Sprite.Create(rewardTex, new Rect(0, 0, rewardTex.width, rewardTex.height), new Vector2(0.5f, 0.5f), 100f);

            rewardImage.gameObject.SetActive(true);
            dismissOverlay.SetActive(false); // Enable after grow animation
            rewardImage.color = Color.white;
            rewardRect.anchoredPosition = Vector2.zero;
            rewardRect.localScale = Vector3.zero;

            float growDuration = 0.5f;
            elapsed = 0f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / growDuration;
                float scale = EaseOutBack(t);
                rewardRect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rewardRect.localScale = Vector3.one;

            // Now wait for player to tap anywhere to dismiss
            dismissOverlay.SetActive(true);
            isAnimating = false;
        }

        private IEnumerator DismissRewardAnimation()
        {
            isAnimating = true;
            dismissOverlay.SetActive(false);

            float duration = 0.4f;
            float elapsed = 0f;
            Vector2 startPos = rewardRect.anchoredPosition;
            Vector2 endPos = new Vector2(800, 200);
            Vector3 startScale = Vector3.one;
            Vector3 endScale = new Vector3(0.3f, 0.3f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = EaseInBack(t);

                rewardRect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                rewardRect.localScale = Vector3.Lerp(startScale, endScale, t);
                rewardRect.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(0, -30f, t));
                yield return null;
            }

            rewardImage.gameObject.SetActive(false);
            rewardRect.localEulerAngles = Vector3.zero;

            // Show arrows again
            if (leftArrowImage != null) leftArrowImage.gameObject.SetActive(true);
            if (rightArrowImage != null) rightArrowImage.gameObject.SetActive(true);

            // Check if there are any presents remaining (any type)
            bool anyRemaining = false;
            for (int i = 0; i < boxTypes.Count; i++)
            {
                if (boxTypes[i].getCount() > 0)
                {
                    anyRemaining = true;
                    // If current type is empty, switch to one that has presents
                    if (boxTypes[currentBoxIndex].getCount() <= 0)
                        currentBoxIndex = i;
                    break;
                }
            }

            if (anyRemaining)
            {
                if (countText != null) countText.gameObject.SetActive(true);
                ResetToPresent();
                isAnimating = false;
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
                isAnimating = false;
                Hide();
            }
        }

        private IEnumerator IdleShakeAnimation()
        {
            // Intermittent shake bursts: short rapid wobble, then pause, repeat
            while (true)
            {
                // Wait 1.5-3s between shake bursts
                yield return new WaitForSeconds(UnityEngine.Random.Range(1.5f, 3f));

                // Bulge up at start of burst
                float bulgeUpTime = 0.08f;
                float bulgeElapsed = 0f;
                while (bulgeElapsed < bulgeUpTime)
                {
                    bulgeElapsed += Time.deltaTime;
                    float t = bulgeElapsed / bulgeUpTime;
                    float s = 1f + 0.08f * Mathf.Sin(t * Mathf.PI * 0.5f);
                    presentRect.localScale = new Vector3(s, s, 1f);
                    // Pulse glow brighter during burst
                    if (presentGlowImage != null)
                        presentGlowImage.color = new Color(1f, 0.8f, 0.2f, Mathf.Lerp(0.3f, 0.85f, t));
                    yield return null;
                }

                // Do a shake burst (3-5 quick shakes)
                int shakeCount = UnityEngine.Random.Range(3, 6);
                for (int i = 0; i < shakeCount; i++)
                {
                    float burstT = (float)i / shakeCount;

                    // Shake gets more intense toward the middle of the burst
                    float intensity = Mathf.Sin(burstT * Mathf.PI);
                    float maxAngle = 8f * intensity + 2f;
                    float maxOffset = 4f * intensity + 1f;

                    // Bulge pulses with each shake
                    float bulgeScale = 1f + 0.06f * intensity;

                    // Quick tilt one direction
                    float angle = maxAngle * (i % 2 == 0 ? 1f : -1f);
                    float offsetX = maxOffset * (i % 2 == 0 ? 1f : -1f);
                    float offsetY = UnityEngine.Random.Range(-2f, 2f) * intensity;

                    float shakeDuration = 0.07f;
                    float elapsed = 0f;

                    while (elapsed < shakeDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / shakeDuration;
                        float smooth = Mathf.SmoothStep(0f, 1f, t);
                        presentRect.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(0, angle, smooth));
                        presentRect.anchoredPosition = new Vector2(
                            Mathf.Lerp(0, offsetX, smooth),
                            Mathf.Lerp(0, offsetY, smooth));
                        // Squash and stretch: slightly wider when tilting
                        float squash = 1f + 0.03f * Mathf.Abs(Mathf.Sin(smooth * Mathf.PI));
                        presentRect.localScale = new Vector3(
                            bulgeScale * squash,
                            bulgeScale / squash,
                            1f);
                        yield return null;
                    }
                }

                // Settle back with a slight bounce
                float settleTime = 0.2f;
                float settleElapsed2 = 0f;
                float startAngle = presentRect.localEulerAngles.z;
                if (startAngle > 180f) startAngle -= 360f;
                Vector2 settleStartPos = presentRect.anchoredPosition;
                Vector3 settleStartScale = presentRect.localScale;

                while (settleElapsed2 < settleTime)
                {
                    settleElapsed2 += Time.deltaTime;
                    float t = settleElapsed2 / settleTime;
                    float decay = 1f - t;
                    float bounce = Mathf.Sin(t * Mathf.PI * 2f) * decay * 0.3f;
                    presentRect.localEulerAngles = new Vector3(0, 0, startAngle * (1f - t + bounce));
                    presentRect.anchoredPosition = Vector2.Lerp(settleStartPos, Vector2.zero, t);
                    // Scale settles back to 1
                    float scaleT = Mathf.SmoothStep(0f, 1f, t);
                    presentRect.localScale = Vector3.Lerp(settleStartScale, Vector3.one, scaleT);
                    // Glow fades back
                    if (presentGlowImage != null)
                        presentGlowImage.color = new Color(1f, 0.8f, 0.2f, Mathf.Lerp(0.85f, 0.3f, t));
                    yield return null;
                }

                presentRect.localEulerAngles = Vector3.zero;
                presentRect.anchoredPosition = Vector2.zero;
                presentRect.localScale = Vector3.one;
                if (presentGlowImage != null)
                    presentGlowImage.color = new Color(1f, 0.8f, 0.2f, 0.3f);
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }

        private static float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        private static Sprite CreateGlowSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center + 0.5f) / center;
                    float dy = (y - center + 0.5f) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha; // Quadratic falloff for soft glow
                    byte a = (byte)(alpha * 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
