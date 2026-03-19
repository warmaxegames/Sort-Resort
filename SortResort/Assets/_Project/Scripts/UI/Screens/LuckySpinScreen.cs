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
        // 8 wheel sections clockwise from top, sampled from actual wheel art pixels.
        // The wheel image has a divider at 12 o'clock. Going clockwise:
        private static readonly Color[] SectionColors = new Color[]
        {
            new Color(0.91f, 0.39f, 0.09f),  // 0: Orange
            new Color(0.98f, 0.02f, 0.69f),  // 1: Hot Pink
            new Color(0.64f, 0.09f, 0.85f),  // 2: Purple
            new Color(0.72f, 0.69f, 0.80f),  // 3: Lavender
            new Color(0.72f, 0.91f, 0.05f),  // 4: Lime Green
            new Color(0.32f, 0.49f, 0.89f),  // 5: Blue
            new Color(0.51f, 0.78f, 0.65f),  // 6: Teal
            new Color(0.91f, 0.70f, 0.11f),  // 7: Gold
        };

        private static readonly string[] SectionNames = new string[]
        {
            "Orange", "Hot Pink", "Purple", "Lavender", "Lime Green", "Blue", "Teal", "Gold"
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
        private GameObject rewardPopup;
        private TextMeshProUGUI rewardText;
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
            rewardPopup.SetActive(false);
            spinButton.interactable = true;

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
            var wheelTex = Resources.Load<Texture2D>("Sprites/UI/LuckySpin/spin_wheel");
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
            rewardPopup = new GameObject("Reward Popup");
            rewardPopup.transform.SetParent(parent, false);
            var popRect = rewardPopup.AddComponent<RectTransform>();
            popRect.anchorMin = Vector2.zero;
            popRect.anchorMax = Vector2.one;
            popRect.offsetMin = Vector2.zero;
            popRect.offsetMax = Vector2.zero;

            // Dim overlay behind popup
            var dimGO = new GameObject("Dim");
            dimGO.transform.SetParent(rewardPopup.transform, false);
            var dimRect = dimGO.AddComponent<RectTransform>();
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = Vector2.zero;
            dimRect.offsetMax = Vector2.zero;
            var dimImg = dimGO.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.7f);

            // Popup card
            var cardGO = new GameObject("Card");
            cardGO.transform.SetParent(rewardPopup.transform, false);
            var cardRect = cardGO.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.1f, 0.3f);
            cardRect.anchorMax = new Vector2(0.9f, 0.7f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            var cardImg = cardGO.AddComponent<Image>();
            cardImg.color = new Color(0.95f, 0.92f, 0.82f);

            // Reward text
            var textGO = new GameObject("RewardText");
            textGO.transform.SetParent(cardGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.15f);
            textRect.anchorMax = new Vector2(0.95f, 0.85f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            rewardText = textGO.AddComponent<TextMeshProUGUI>();
            rewardText.text = "You won a reward!";
            rewardText.fontSize = 48;
            rewardText.alignment = TextAlignmentOptions.Center;
            rewardText.color = Color.black;
            FontManager.ApplyBold(rewardText);

            // Title text
            var titleTextGO = new GameObject("TitleText");
            titleTextGO.transform.SetParent(cardGO.transform, false);
            var titleTextRect = titleTextGO.AddComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0.05f, 0.7f);
            titleTextRect.anchorMax = new Vector2(0.85f, 0.95f);
            titleTextRect.offsetMin = Vector2.zero;
            titleTextRect.offsetMax = Vector2.zero;

            var titleTmp = titleTextGO.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CONGRATULATIONS!";
            titleTmp.fontSize = 42;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.6f, 0.4f, 0.1f);
            FontManager.ApplyBold(titleTmp);

            // X close button (upper right)
            var xGO = new GameObject("CloseButton");
            xGO.transform.SetParent(cardGO.transform, false);
            var xRect = xGO.AddComponent<RectTransform>();
            xRect.anchorMin = new Vector2(0.88f, 0.85f);
            xRect.anchorMax = new Vector2(1.0f, 1.0f);
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;

            var xImg = xGO.AddComponent<Image>();
            xImg.color = new Color(0.8f, 0.2f, 0.2f);

            var xTextGO = new GameObject("XText");
            xTextGO.transform.SetParent(xGO.transform, false);
            var xTextRect = xTextGO.AddComponent<RectTransform>();
            xTextRect.anchorMin = Vector2.zero;
            xTextRect.anchorMax = Vector2.one;
            xTextRect.offsetMin = Vector2.zero;
            xTextRect.offsetMax = Vector2.zero;
            var xTmp = xTextGO.AddComponent<TextMeshProUGUI>();
            xTmp.text = "X";
            xTmp.fontSize = 40;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;
            FontManager.ApplyBold(xTmp);

            var xBtn = xGO.AddComponent<Button>();
            xBtn.targetGraphic = xImg;
            xBtn.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButtonClick();
                Hide();
            });
        }

        // --- INPUT ---

        private void OnSpinClicked()
        {
            if (isSpinning) return;
            AudioManager.Instance?.PlayButtonClick();
            spinButton.interactable = false;

            // Determine reward immediately
            int rewardSection = UnityEngine.Random.Range(0, 8);
            Debug.Log($"[LuckySpin] Reward determined: Section {rewardSection} ({SectionNames[rewardSection]})");

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

            Debug.Log($"[LuckySpin] Wheel stopped at Z={desiredFinalZ:F1}. Reward: {SectionNames[targetSection]} (section {targetSection})");

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
            rewardText.text = $"You landed on\n<color=#{ColorUtility.ToHtmlStringRGB(SectionColors[section])}><size=64>{SectionNames[section]}</size></color>";
            rewardPopup.SetActive(true);
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