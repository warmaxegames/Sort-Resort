using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    /// <summary>
    /// Daily login lucky spin wheel screen.
    /// 8 colored sections, spin animation with click sounds, reward popup.
    /// </summary>
    public class LuckySpinScreen
    {
        // Reward types for spin wheel sections
        private enum SpinRewardType { Coins, PowerUp, PresentBox }

        private struct SpinReward
        {
            public SpinRewardType type;
            public int coinAmount;
            public PowerUpType powerUpType;
            public string iconPath;
            public string displayName;
        }

        // 8 wheel sections clockwise from top (pointer position)
        private static readonly SpinReward[] SectionRewards = new SpinReward[]
        {
            new SpinReward { type = SpinRewardType.Coins, coinAmount = 5, iconPath = "Sprites/UI/LuckySpin/coins_icon", displayName = "5 Coins" },
            new SpinReward { type = SpinRewardType.PowerUp, powerUpType = PowerUpType.TimeFreeze, iconPath = "Sprites/UI/PowerUps/time_freeze_intro", displayName = "Time Freeze" },
            new SpinReward { type = SpinRewardType.Coins, coinAmount = 10, iconPath = "Sprites/UI/LuckySpin/coins_icon", displayName = "10 Coins" },
            new SpinReward { type = SpinRewardType.PowerUp, powerUpType = PowerUpType.MoveFreeze, iconPath = "Sprites/UI/PowerUps/moves_freeze_intro", displayName = "Move Freeze" },
            new SpinReward { type = SpinRewardType.Coins, coinAmount = 20, iconPath = "Sprites/UI/LuckySpin/coins_icon", displayName = "20 Coins" },
            new SpinReward { type = SpinRewardType.PresentBox, iconPath = "Sprites/UI/LuckySpin/normal_present_box", displayName = "Present Box" },
            new SpinReward { type = SpinRewardType.PowerUp, powerUpType = PowerUpType.DestroyLocker, iconPath = "Sprites/UI/PowerUps/destroy_locker_intro", displayName = "Destroy Lock" },
            new SpinReward { type = SpinRewardType.PowerUp, powerUpType = PowerUpType.SwapItems, iconPath = "Sprites/UI/PowerUps/swap_items_intro", displayName = "Swap Items" },
        };

        private GameObject panel;
        private RectTransform wheelRect;
        private RectTransform raysSpinnerRect;
        private Image raysSpinnerImage;
        private Image titleImage;
        private Button spinButton;
        private Image spinButtonImage;
        private Sprite spinButtonNormal;
        private Sprite spinButtonPressed;
        private AudioClip wheelClickClip;
        private GameObject rewardOverlay;
        private Image rewardIconImage;
        private Image rewardGlowImage;
        private TextMeshProUGUI rewardAmountText;
        private Image congratsImage;
        private Image acceptButtonImage;
        private Sprite acceptNormal;
        private Sprite acceptPressed;
        private Coroutine congratsPulseCoroutine;
        private Coroutine glowPulseCoroutine;
        private int pendingRewardSection;
        private bool isSpinning;
        private MonoBehaviour coroutineHost;
        private Coroutine pulseCoroutine;
        private Coroutine spinCoroutine;
        private Coroutine raysSpinCoroutine;
        private Coroutine wheelEffectCoroutine;
        private GameObject wheelEffectGO;
        private Image wheelEffectImage;
        private Sprite[] wheelEffectSprites;
        private AudioClip electricSoundClip;

        // Rays spinner spin speed (degrees per second) � matched to wheel peak speed feel
        private const float RaysSpinSpeed = 120f;

        public GameObject Panel => panel;
        public bool IsVisible => panel != null && panel.activeSelf;

        /// <summary>
        /// Event fired when the screen is closed (X button or after reward claimed).
        /// </summary>
        public event Action OnClosed;

        public void Create(Transform parent, MonoBehaviour host)
        {
            coroutineHost = host;

            panel = new GameObject("Lucky Spin Panel");
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Override sorting to render above everything
            var canvas = panel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5300;
            panel.AddComponent<GraphicRaycaster>();
            panel.AddComponent<CanvasGroup>();

            // Load assets
            wheelClickClip = Resources.Load<AudioClip>("Audio/SFX/wheel_click");
            electricSoundClip = Resources.Load<AudioClip>("Audio/SFX/electric_sound");
            // Preload wheel effect frames as Texture2D and create full-rect sprites
            // to avoid Unity's alpha-trim making the effect appear zoomed in.
            var allLoadedTex = Resources.LoadAll<Texture2D>("Sprites/UI/LuckySpin/WheelEffect");
            Debug.Log("[LuckySpinScreen] LoadAll<Texture2D> found: " + allLoadedTex.Length);
            wheelEffectSprites = new Sprite[42];
            System.Array.Sort(allLoadedTex, (a, b) => a.name.CompareTo(b.name));
            for (int i = 0; i < allLoadedTex.Length && i < 42; i++)
            {
                var tex = allLoadedTex[i];
                wheelEffectSprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            Debug.Log("[LuckySpinScreen] Assigned " + allLoadedTex.Length + " frames to array");

            // Layer 0: Full screen background (new spin_screen_background)
            CreateBackground(panel.transform);

            // Layer 1: Rays spinner (behind wheel and title, above background)
            CreateRaysSpinner(panel.transform);

            // Layer 2: Title (top)
            CreateTitle(panel.transform);

            // Layer 3: Wheel (center)
            CreateWheel(panel.transform);

            // Layer 3.5: Wheel effect animation (ON TOP of wheel, below pointer)
            CreateWheelEffect(panel.transform);

            // Layer 4: Pointer (overlapping top of wheel)
            CreatePointer(panel.transform);

            // Layer 5: Spin button (bottom)
            CreateSpinButton(panel.transform);

            // Layer 6: Reward popup (hidden)
            CreateRewardPopup(panel.transform);

            panel.SetActive(false);
        }

        public void Show()
        {
            if (panel == null) return;
            isSpinning = false;
            rewardOverlay.SetActive(false);
            spinButton.interactable = true;
            spinButtonImage.gameObject.SetActive(true);

            // Randomize starting angle to one of 8 section-aligned positions (divider at pointer)
            int startSection = UnityEngine.Random.Range(0, 8);
            float startAngle = startSection * 45f;
            wheelRect.localEulerAngles = new Vector3(0, 0, startAngle);

            // Reset rays spinner rotation and stop any leftover spin coroutine
            if (raysSpinCoroutine != null)
            {
                coroutineHost.StopCoroutine(raysSpinCoroutine);
                raysSpinCoroutine = null;
            }
            if (wheelEffectCoroutine != null)
            {
                coroutineHost.StopCoroutine(wheelEffectCoroutine);
                wheelEffectCoroutine = null;
            }
            if (wheelEffectGO != null) wheelEffectGO.SetActive(false);
            if (raysSpinnerRect != null)
                raysSpinnerRect.localEulerAngles = Vector3.zero;
            if (wheelEffectCoroutine != null)
            {
                coroutineHost.StopCoroutine(wheelEffectCoroutine);
                wheelEffectCoroutine = null;
            }
            if (wheelEffectGO != null) wheelEffectGO.SetActive(false);

            panel.SetActive(true);

            // Start title pulse
            if (pulseCoroutine != null) coroutineHost.StopCoroutine(pulseCoroutine);
            pulseCoroutine = coroutineHost.StartCoroutine(PulseTitle());
        }

        public void Hide()
        {
            if (panel == null) return;
            if (pulseCoroutine != null)
            {
                coroutineHost.StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            if (spinCoroutine != null)
            {
                coroutineHost.StopCoroutine(spinCoroutine);
                spinCoroutine = null;
            }
            if (raysSpinCoroutine != null)
            {
                coroutineHost.StopCoroutine(raysSpinCoroutine);
                raysSpinCoroutine = null;
            }
            if (wheelEffectCoroutine != null)
            {
                coroutineHost.StopCoroutine(wheelEffectCoroutine);
                wheelEffectCoroutine = null;
            }
            if (wheelEffectGO != null) wheelEffectGO.SetActive(false);
            if (congratsPulseCoroutine != null)
            {
                coroutineHost.StopCoroutine(congratsPulseCoroutine);
                congratsPulseCoroutine = null;
            }
            if (glowPulseCoroutine != null)
            {
                coroutineHost.StopCoroutine(glowPulseCoroutine);
                glowPulseCoroutine = null;
            }
            panel.SetActive(false);
            OnClosed?.Invoke();
        }

        /// <summary>
        /// Check if the player has already spun today.
        /// </summary>
        public static bool HasSpunToday()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save == null) return true; // No save = don't show
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            return save.lastLuckySpinDate == today;
        }

        /// <summary>
        /// Mark today's spin as used and save.
        /// </summary>
        private static void MarkSpunToday()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save == null) return;
            save.lastLuckySpinDate = DateTime.Now.ToString("yyyy-MM-dd");
            SaveManager.Instance.SaveGame();
        }

        // --- UI CREATION ---

        private void CreateBackground(Transform parent)
        {
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(parent, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImg = bgGO.AddComponent<Image>();
            // Use new spin_screen_background; fall back to old background if missing
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
        }

        /// <summary>
        /// Creates the rays spinner image: reuses rays_spinner.png, tinted white at 85% transparency.
        /// Sits above the background but behind the wheel and title.
        /// </summary>
        private void CreateRaysSpinner(Transform parent)
        {
            var raysGO = new GameObject("Rays Spinner");
            raysGO.transform.SetParent(parent, false);
            raysSpinnerRect = raysGO.AddComponent<RectTransform>();

            // Centered, fixed 2500x2500 � mirrors the level complete screen sizing
            raysSpinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            raysSpinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            raysSpinnerRect.pivot = new Vector2(0.5f, 0.5f);
            raysSpinnerRect.anchoredPosition = Vector2.zero;
            raysSpinnerRect.sizeDelta = new Vector2(2500f, 2500f);

            raysSpinnerImage = raysGO.AddComponent<Image>();
            raysSpinnerImage.preserveAspect = true;
            raysSpinnerImage.raycastTarget = false;

            // rays_spinner is a multiple-sprite sheet; must load as Texture2D and create a full-rect sprite
            // (same pattern as UIManager.LoadFullRectSprite) so that Image.color tinting works correctly
            var raysTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/rays_spinner");
            if (raysTex != null)
            {
                raysSpinnerImage.sprite = Sprite.Create(raysTex, new Rect(0, 0, raysTex.width, raysTex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            else
            {
                Debug.LogWarning("[LuckySpinScreen] rays_spinner texture not found.");
            }

            // Tint white, 85% transparent (alpha 0.15 = 15% opaque = 85% transparent)
            raysSpinnerImage.color = new Color(1f, 1f, 1f, 0.15f); // white (asset recolored) at 85% transparency
        }

        private void CreateTitle(Transform parent)
        {
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(parent, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            // Top of screen, centered - 15% larger (was 0.70 wide x 0.13 tall, now 0.805 x 0.1495)
            titleRect.anchorMin = new Vector2(0.0975f, 0.810f);
            titleRect.anchorMax = new Vector2(0.9025f, 0.960f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            titleImage = titleGO.AddComponent<Image>();
            var titleTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/lucky_spin_title");
            if (titleTex != null)
            {
                titleImage.sprite = Sprite.Create(titleTex, new Rect(0, 0, titleTex.width, titleTex.height), new Vector2(0.5f, 0.5f), 100f);
                titleImage.preserveAspect = true;
            }
            titleImage.raycastTarget = false;
        }

        private void CreateWheel(Transform parent)
        {
            var wheelGO = new GameObject("Spin Wheel");
            wheelGO.transform.SetParent(parent, false);
            wheelRect = wheelGO.AddComponent<RectTransform>();
            // Center of screen - moved down 100px, then 10% smaller
            wheelRect.anchorMin = new Vector2(0.113f, 0.273f);
            wheelRect.anchorMax = new Vector2(0.887f, 0.723f);
            wheelRect.offsetMin = Vector2.zero;
            wheelRect.offsetMax = Vector2.zero;

            var wheelImg = wheelGO.AddComponent<Image>();
            var wheelTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/daily_spin_wheel");
            if (wheelTex != null)
            {
                wheelImg.sprite = Sprite.Create(wheelTex, new Rect(0, 0, wheelTex.width, wheelTex.height), new Vector2(0.5f, 0.5f), 100f);
                wheelImg.preserveAspect = true;
            }
            wheelImg.raycastTarget = false;
        }

        private void CreatePointer(Transform parent)
        {
            var pointerGO = new GameObject("Pointer");
            pointerGO.transform.SetParent(parent, false);
            var pointerRect = pointerGO.AddComponent<RectTransform>();
            // Overlapping top of wheel - moved down 130+30=160px (160/1920 = 0.083)
            pointerRect.anchorMin = new Vector2(0.42f, 0.686f);
            pointerRect.anchorMax = new Vector2(0.58f, 0.746f);
            pointerRect.offsetMin = Vector2.zero;
            pointerRect.offsetMax = Vector2.zero;

            var pointerImg = pointerGO.AddComponent<Image>();
            var pointerTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/spin_wheel_pointer");
            if (pointerTex != null)
            {
                pointerImg.sprite = Sprite.Create(pointerTex, new Rect(0, 0, pointerTex.width, pointerTex.height), new Vector2(0.5f, 0.5f), 100f);
                pointerImg.preserveAspect = true;
            }
            pointerImg.raycastTarget = false;
        }

        private void CreateSpinButton(Transform parent)
        {
            var btnGO = new GameObject("Spin Button");
            btnGO.transform.SetParent(parent, false);
            var btnRect = btnGO.AddComponent<RectTransform>();
            // 15% larger than previous + moved up 50px
            btnRect.anchorMin = new Vector2(0.184f, 0.095f);
            btnRect.anchorMax = new Vector2(0.816f, 0.197f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            spinButtonImage = btnGO.AddComponent<Image>();
            spinButtonNormal = LoadSprite("Sprites/UI/LuckySpin/spin_button");
            spinButtonPressed = LoadSprite("Sprites/UI/LuckySpin/spin_button_pressed");
            if (spinButtonNormal != null)
                spinButtonImage.sprite = spinButtonNormal;
            spinButtonImage.preserveAspect = true;

            spinButton = btnGO.AddComponent<Button>();
            spinButton.targetGraphic = spinButtonImage;
            spinButton.transition = Selectable.Transition.None;
            spinButton.onClick.AddListener(OnSpinClicked);

            // Manual press visual via EventTrigger
            var trigger = btnGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((_) => { if (spinButtonPressed != null) spinButtonImage.sprite = spinButtonPressed; });
            trigger.triggers.Add(pointerDown);
            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((_) => { if (spinButtonNormal != null) spinButtonImage.sprite = spinButtonNormal; });
            trigger.triggers.Add(pointerUp);
        }

        private void CreateRewardPopup(Transform parent)
        {
            rewardOverlay = new GameObject("Reward Overlay");
            rewardOverlay.transform.SetParent(parent, false);
            var popRect = rewardOverlay.AddComponent<RectTransform>();
            popRect.anchorMin = Vector2.zero;
            popRect.anchorMax = Vector2.one;
            popRect.offsetMin = Vector2.zero;
            popRect.offsetMax = Vector2.zero;

            // Layer 1: Dark overlay (95% opacity)
            var dimGO = new GameObject("DarkOverlay");
            dimGO.transform.SetParent(rewardOverlay.transform, false);
            var dimRect = dimGO.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImg = dimGO.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.95f);

            // Layer 2: Glow behind reward icon (procedural white circle)
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(rewardOverlay.transform, false);
            var glowRect = glowGO.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(500, 500);
            rewardGlowImage = glowGO.AddComponent<Image>();
            rewardGlowImage.sprite = CreateGlowSprite();
            rewardGlowImage.color = new Color(1f, 0.9f, 0.5f, 0.5f);
            rewardGlowImage.raycastTarget = false;

            // Layer 3: Reward icon (centered, sized for visibility)
            var iconGO = new GameObject("RewardIcon");
            iconGO.transform.SetParent(rewardOverlay.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(300, 300);
            rewardIconImage = iconGO.AddComponent<Image>();
            rewardIconImage.preserveAspect = true;
            rewardIconImage.raycastTarget = false;

            // Layer 4: Coin amount text (below icon, only visible for coin rewards)
            var amountGO = new GameObject("CoinAmount");
            amountGO.transform.SetParent(rewardOverlay.transform, false);
            var amountRect = amountGO.AddComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0.5f, 0.5f);
            amountRect.anchorMax = new Vector2(0.5f, 0.5f);
            amountRect.pivot = new Vector2(0.5f, 1f);
            amountRect.anchoredPosition = new Vector2(0, -185);
            amountRect.sizeDelta = new Vector2(300, 100);
            rewardAmountText = amountGO.AddComponent<TextMeshProUGUI>();
            rewardAmountText.text = "";
            rewardAmountText.fontSize = 84;
            rewardAmountText.fontStyle = FontStyles.Bold;
            rewardAmountText.alignment = TextAlignmentOptions.Center;
            rewardAmountText.color = new Color(1f, 0.84f, 0f);
            rewardAmountText.raycastTarget = false;
            rewardAmountText.outlineWidth = 0.25f;
            rewardAmountText.outlineColor = Color.black;
            if (FontManager.Bold != null)
                rewardAmountText.font = FontManager.Bold;

            // Layer 5: Congrats text (positioned via crop metadata)
            var congratsGO = new GameObject("CongratsText");
            congratsGO.transform.SetParent(rewardOverlay.transform, false);
            var congratsRect = congratsGO.AddComponent<RectTransform>();
            congratsImage = congratsGO.AddComponent<Image>();
            var congratsTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/congrats_text");
            if (congratsTex != null)
            {
                congratsImage.sprite = Sprite.Create(congratsTex, new Rect(0, 0, congratsTex.width, congratsTex.height), new Vector2(0.5f, 0.5f), 100f);
                congratsImage.preserveAspect = true;
            }
            congratsImage.raycastTarget = false;
            CropMetadata.ApplyCropAnchors(congratsRect, "Sprites/UI/LuckySpin/congrats_text");

            // Layer 6: Accept button (positioned via crop metadata)
            var acceptGO = new GameObject("AcceptButton");
            acceptGO.transform.SetParent(rewardOverlay.transform, false);
            var acceptRect = acceptGO.AddComponent<RectTransform>();

            acceptButtonImage = acceptGO.AddComponent<Image>();
            acceptNormal = LoadSprite("Sprites/UI/LuckySpin/accept_button");
            acceptPressed = LoadSprite("Sprites/UI/LuckySpin/accept_button_pressed");
            if (acceptNormal != null)
                acceptButtonImage.sprite = acceptNormal;
            acceptButtonImage.preserveAspect = true;
            CropMetadata.ApplyCropAnchors(acceptRect, "Sprites/UI/LuckySpin/accept_button");
            // Source image is ~9.5px left of center; nudge right to center on screen
            acceptRect.offsetMin = new Vector2(10, acceptRect.offsetMin.y);
            acceptRect.offsetMax = new Vector2(10, acceptRect.offsetMax.y);

            var acceptBtn = acceptGO.AddComponent<Button>();
            acceptBtn.targetGraphic = acceptButtonImage;
            acceptBtn.transition = Selectable.Transition.None;
            acceptBtn.onClick.AddListener(OnAcceptClicked);

            // Manual press visual
            var acceptTrigger = acceptGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var acceptDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            acceptDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            acceptDown.callback.AddListener((_) => { if (acceptPressed != null) acceptButtonImage.sprite = acceptPressed; });
            acceptTrigger.triggers.Add(acceptDown);
            var acceptUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            acceptUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            acceptUp.callback.AddListener((_) => { if (acceptNormal != null) acceptButtonImage.sprite = acceptNormal; });
            acceptTrigger.triggers.Add(acceptUp);
        }

        private void OnAcceptClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            GrantReward(pendingRewardSection);
            Hide();
        }

        private void GrantReward(int section)
        {
            var reward = SectionRewards[section];
            switch (reward.type)
            {
                case SpinRewardType.Coins:
                    AchievementManager.Instance?.AddCoins(reward.coinAmount);
                    Debug.Log($"[LuckySpin] Granted {reward.coinAmount} coins");
                    break;
                case SpinRewardType.PowerUp:
                    if (SaveManager.Instance != null)
                    {
                        int current = SaveManager.Instance.GetPowerUpCount(reward.powerUpType);
                        SaveManager.Instance.SetPowerUpCount(reward.powerUpType, current + 1);
                        Debug.Log($"[LuckySpin] Granted power-up: {reward.displayName}");
                    }
                    break;
                case SpinRewardType.PresentBox:
                    SaveManager.Instance?.AddNormalPresent();
                    Debug.Log($"[LuckySpin] Granted present box");
                    break;
            }
        }

        public static Sprite CreateGlowSpritePublic() => CreateGlowSprite();

        private static Sprite CreateGlowSprite()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = size / 2f;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist / radius);
                    alpha = alpha * alpha; // quadratic falloff
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // --- INPUT ---

        private void OnSpinClicked()
        {
            if (isSpinning) return;
            AudioManager.Instance?.PlayButtonClick();
            spinButton.interactable = false;

            // Determine reward immediately (skip locked powerups)
            var eligibleSections = new System.Collections.Generic.List<int>();
            for (int i = 0; i < SectionRewards.Length; i++)
            {
                var r = SectionRewards[i];
                if (r.type == SpinRewardType.PowerUp &&
                    SaveManager.Instance != null &&
                    !SaveManager.Instance.IsPowerUpUnlocked(r.powerUpType))
                    continue;
                eligibleSections.Add(i);
            }
            int rewardSection = eligibleSections[UnityEngine.Random.Range(0, eligibleSections.Count)];
            Debug.Log($"[LuckySpin] Reward determined: Section {rewardSection} ({SectionRewards[rewardSection].displayName})");

            // Mark spin used
            MarkSpunToday();

            spinCoroutine = coroutineHost.StartCoroutine(SpinWheelAnimation(rewardSection));
        }

        // --- ANIMATIONS ---

        /// <summary>
        /// Smooth 3-phase clockwise spin using analytical sine-based speed curves.
        /// Speed transitions are C1-continuous (no jerks at phase boundaries).
        /// Position is computed analytically from time, so no frame-rate dependent drift.
        /// After the wheel stops, the rays spinner animates for 2 seconds.
        /// </summary>
        private IEnumerator SpinWheelAnimation(int targetSection)
        {
            isSpinning = true;

            float startZ = wheelRect.localEulerAngles.z;

            // LANDING MATH:
            // When the wheel is at Z rotation R, the section at the pointer is
            // floor(R / 45). Section N center = N*45 + 22.5 degrees.
            float desiredFinalZ = targetSection * 45f + 22.5f + UnityEngine.Random.Range(-15f, 15f);
            desiredFinalZ = ((desiredFinalZ % 360f) + 360f) % 360f;

            // Total clockwise rotation = startZ - desiredFinalZ (mod 360) + N full rotations.
            // Clockwise on screen = subtracting from Z.
            float rawDiff = startZ - desiredFinalZ;
            rawDiff = ((rawDiff % 360f) + 360f) % 360f; // Normalize to [0, 360)
            float totalRotation = rawDiff + 5400f; // At least 15 full rotations (~250 RPM peak)

            // 3-phase timing: fast max speed, long dramatic slowdown
            float accelTime = 1.0f;
            float constTime = 0.0f;
            float decelTime = 4.5f;
            float totalTime = accelTime + constTime + decelTime;

            // Speed curves:
            //   Accel:  speed = maxSpeed * sin(t/accelTime * PI/2)     � smooth ramp 0 to max
            //   Const:  speed = maxSpeed                               � holds at max
            //   Decel:  speed = maxSpeed * (1 - tau)^(n-1)             � fast brake, long crawl
            //
            // Decel uses power curve (n=3): drops to 25% speed at halfway,
            // then crawls � last 30% of time covers only 2.7% of distance.
            //
            // Integrated distances:
            //   Accel dist = maxSpeed * accelTime * 2/PI
            //   Const dist = maxSpeed * constTime
            //   Decel dist = maxSpeed * decelTime / n
            const float decelPower = 3f;
            float distFactor = accelTime * 2f / Mathf.PI + constTime + decelTime / decelPower;
            float maxSpeed = totalRotation / distFactor;

            float accelDist = maxSpeed * accelTime * 2f / Mathf.PI;
            float constDist = maxSpeed * constTime;

            float elapsed = 0f;
            float prevRotation = 0f;
            float clickAccum = 0f;
            float clickInterval = 45f; // Click once per section boundary

            while (elapsed < totalTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Min(elapsed, totalTime);

                // Calculate cumulative rotation analytically from elapsed time
                float rotation;
                if (t <= accelTime)
                {
                    rotation = maxSpeed * accelTime * 2f / Mathf.PI
                        * (1f - Mathf.Cos(t / accelTime * Mathf.PI / 2f));
                }
                else if (t <= accelTime + constTime)
                {
                    float constElapsed = t - accelTime;
                    rotation = accelDist + maxSpeed * constElapsed;
                }
                else
                {
                    // Power curve decel: position = totalDecelDist * (1 - (1-tau)^n)
                    // where tau = decelElapsed / decelTime, n = decelPower
                    float decelElapsed = t - accelTime - constTime;
                    float tau = decelElapsed / decelTime;
                    float totalDecelDist = maxSpeed * decelTime / decelPower;
                    rotation = accelDist + constDist + totalDecelDist * (1f - Mathf.Pow(1f - tau, decelPower));
                }

                // Click sound at each section boundary
                float delta = rotation - prevRotation;
                clickAccum += delta;
                while (clickAccum >= clickInterval)
                {
                    clickAccum -= clickInterval;
                    if (wheelClickClip != null)
                        AudioManager.Instance?.PlaySFX(wheelClickClip);
                }
                prevRotation = rotation;

                // Apply rotation (clockwise = subtract Z)
                wheelRect.localEulerAngles = new Vector3(0, 0, startZ - rotation);
                yield return null;
            }

            // Snap to exact final angle
            wheelRect.localEulerAngles = new Vector3(0, 0, desiredFinalZ);

            isSpinning = false;

            Debug.Log($"[LuckySpin] Wheel stopped at Z={desiredFinalZ:F1}. Reward: {SectionRewards[targetSection].displayName} (section {targetSection})");

            // Animate rays spinner and wheel effect simultaneously after wheel stops
            if (raysSpinCoroutine != null) coroutineHost.StopCoroutine(raysSpinCoroutine);
            raysSpinCoroutine = coroutineHost.StartCoroutine(SpinRaysAnimation(2f));
            if (wheelEffectCoroutine != null) coroutineHost.StopCoroutine(wheelEffectCoroutine);

            // Wait for the wheel effect animation to fully complete before showing reward
            yield return coroutineHost.StartCoroutine(WheelEffectAnimation());
            ShowReward(targetSection);
        }

        /// <summary>
        /// Spins the rays spinner image clockwise at RaysSpinSpeed degrees/sec for the given duration,
        /// then fades it back to its resting alpha.
        /// </summary>
        private IEnumerator SpinRaysAnimation(float duration)
        {
            if (raysSpinnerRect == null || raysSpinnerImage == null) yield break;

            float elapsed = 0f;
            float currentZ = raysSpinnerRect.localEulerAngles.z;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                currentZ -= RaysSpinSpeed * Time.unscaledDeltaTime; // clockwise
                raysSpinnerRect.localEulerAngles = new Vector3(0, 0, currentZ);
                yield return null;
            }

            raysSpinCoroutine = null;
        }


        /// <summary>
        /// Creates a hidden Image GameObject centered on the wheel for the frame animation.
        /// Sized to match the 900x900 effect sprite dimensions centered on the wheel.
        /// </summary>
        private void CreateWheelEffect(Transform parent)
        {
            wheelEffectGO = new GameObject("Wheel Effect");
            wheelEffectGO.transform.SetParent(parent, false);
            var rt = wheelEffectGO.AddComponent<RectTransform>();

            // 900x900 sprites centered on the wheel (center anchor ~0.5, 0.498).
            // Width: 900/1080 = 0.8333, Height: 900/1920 = 0.46875
            rt.anchorMin = new Vector2(0.0833f, 0.2636f);
            rt.anchorMax = new Vector2(0.9167f, 0.7324f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            wheelEffectImage = wheelEffectGO.AddComponent<Image>();
            wheelEffectImage.raycastTarget = false;
            wheelEffectImage.preserveAspect = true;
            wheelEffectGO.SetActive(false);
        }

        /// <summary>
        /// Plays the 42-frame wheel effect animation at 24fps, centered on the wheel.
        /// Plays electric_sound simultaneously. Hides itself when complete.
        /// </summary>
        private IEnumerator WheelEffectAnimation()
        {
            if (wheelEffectGO == null || wheelEffectImage == null || wheelEffectSprites == null)
            {
                Debug.LogWarning($"[LuckySpinScreen] WheelEffect early exit: GO={wheelEffectGO != null}, Image={wheelEffectImage != null}, Sprites={wheelEffectSprites != null}");
                yield break;
            }

            const int frameCount = 42;
            const float fps = 33f;
            float frameDuration = 1f / fps;

            // Assign first frame BEFORE activating to prevent white-box flash
            // (Image with no sprite renders as solid white rectangle)
            if (wheelEffectSprites[0] != null)
            {
                wheelEffectImage.sprite = wheelEffectSprites[0];
                Debug.Log($"[LuckySpinScreen] WheelEffect starting, sprite[0] size: {wheelEffectSprites[0].texture.width}x{wheelEffectSprites[0].texture.height}");
            }
            else
            {
                Debug.LogWarning("[LuckySpinScreen] WheelEffect sprite[0] is null, skipping animation");
                yield break;
            }

            // Play electric sound on a dedicated AudioSource at full volume
            // (the SFX system double-scales volume, making this clip too quiet)
            if (electricSoundClip != null)
            {
                var tempGO = new GameObject("ElectricSound");
                var src = tempGO.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
                src.volume = 0.35f;
                src.clip = electricSoundClip;
                src.Play();
                UnityEngine.Object.Destroy(tempGO, electricSoundClip.length + 0.1f);
            }

            wheelEffectGO.SetActive(true);

            for (int i = 0; i < frameCount; i++)
            {
                if (wheelEffectSprites[i] != null)
                    wheelEffectImage.sprite = wheelEffectSprites[i];

                yield return new WaitForSecondsRealtime(frameDuration);
            }

            wheelEffectGO.SetActive(false);
            wheelEffectCoroutine = null;
        }
        private void ShowReward(int section)
        {
            pendingRewardSection = section;
            var reward = SectionRewards[section];

            // Set reward icon
            if (reward.type == SpinRewardType.Coins || reward.type == SpinRewardType.PresentBox)
            {
                // Use cropped sprites for coins/present — load as Texture2D for full-rect
                var tex = Resources.Load<Texture2D>(reward.iconPath);
                if (tex != null)
                    rewardIconImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            else
            {
                // Power-up icons are already proper sprites
                var sprite = Resources.Load<Sprite>(reward.iconPath);
                if (sprite != null)
                    rewardIconImage.sprite = sprite;
            }

            // Show coin amount text only for coin rewards
            if (reward.type == SpinRewardType.Coins)
            {
                rewardAmountText.gameObject.SetActive(true);
                rewardAmountText.text = reward.coinAmount.ToString();
            }
            else
            {
                rewardAmountText.gameObject.SetActive(false);
            }

            // Stop title pulse and hide spin button so they don't distract from reward
            if (pulseCoroutine != null)
            {
                coroutineHost.StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            if (titleImage != null)
                titleImage.GetComponent<RectTransform>().localScale = Vector3.one;
            if (spinButtonImage != null)
                spinButtonImage.gameObject.SetActive(false);

            // Start pulsing animations
            if (congratsPulseCoroutine != null) coroutineHost.StopCoroutine(congratsPulseCoroutine);
            congratsPulseCoroutine = coroutineHost.StartCoroutine(PulseCongrats());
            if (glowPulseCoroutine != null) coroutineHost.StopCoroutine(glowPulseCoroutine);
            glowPulseCoroutine = coroutineHost.StartCoroutine(PulseGlow());

            rewardOverlay.SetActive(true);

            // Play the powerup unlock sound
            AudioManager.Instance?.PlaySpecialItemUnlockSound();
        }

        private IEnumerator PulseCongrats()
        {
            if (congratsImage == null) yield break;
            var rt = congratsImage.GetComponent<RectTransform>();
            if (rt == null) yield break;

            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                float scale = 1f + 0.085f * Mathf.Sin(time * 5f);
                rt.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        private IEnumerator PulseGlow()
        {
            if (rewardGlowImage == null) yield break;

            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                float alpha = 0.4f + 0.3f * Mathf.Sin(time * 3f);
                rewardGlowImage.color = new Color(1f, 0.9f, 0.5f, alpha);
                yield return null;
            }
        }

        private IEnumerator PulseTitle()
        {
            if (titleImage == null) yield break;
            var rt = titleImage.GetComponent<RectTransform>();
            if (rt == null) yield break;

            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                float scale = 1f + 0.085f * Mathf.Sin(time * 5f);
                rt.localScale = Vector3.one * scale;
                yield return null;
            }
        }

        // --- UTILITY ---

        private static Sprite LoadSprite(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Debug.LogWarning($"[LuckySpin] Failed to load sprite: {resourcePath}");
            return null;
        }
    }
}