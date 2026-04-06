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
            public string animFolder; // Resources path to animation frames folder
            public string framePrefix; // e.g. "present_open_", "farm_open_"
            public System.Func<int> getCount;
            public System.Action useOne;
            public Sprite[] openFrames; // Per-type animation frames
            public float displayScale; // Scale correction vs standard box (1.0 = no change)
            public Vector2 displayOffset; // Position offset to center content
        }

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
        private Sprite[] smokeFrames;
        private Image smokeImage;
        private bool isAnimating;
        private Coroutine animCoroutine;
        private Coroutine shakeCoroutine;
        private Image presentGlowImage;

        // Box type cycling
        private List<PresentBoxType> boxTypes = new List<PresentBoxType>();
        private int currentBoxIndex;

        // Grey color for disabled arrows
        private static readonly Color ArrowGreyColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        // Present box resting position (centered in glow)
        private static readonly Vector2 PresentRestPos = new Vector2(0, 40);

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
            presentRect.anchoredPosition = PresentRestPos;
            presentRect.sizeDelta = new Vector2(480, 480);
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

            // Info button (shows loot table contents)
            CreateInfoButton(panel.transform);

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

            // Normal present box (default/Island style) - 628x800, content fills canvas
            boxTypes.Add(new PresentBoxType
            {
                id = "normal",
                displayName = "Present Box",
                animFolder = "Sprites/UI/PresentOpen",
                framePrefix = "present_open_",
                getCount = () => SaveManager.Instance?.GetPresentCount("normal") ?? 0,
                useOne = () => { SaveManager.Instance?.UsePresent("normal"); AchievementManager.Instance?.RecordPresentBoxOpened(); },
                displayScale = 1f,
                displayOffset = Vector2.zero
            });

            // Island box - 800x800 (scaled from 250x250), content bbox ~(50,140)-(561,740)
            boxTypes.Add(new PresentBoxType
            {
                id = "island",
                displayName = "Island Box",
                animFolder = "Sprites/UI/PresentOpen/Island",
                framePrefix = "island_open_",
                getCount = () => SaveManager.Instance?.GetPresentCount("island") ?? 0,
                useOne = () => { SaveManager.Instance?.UsePresent("island"); AchievementManager.Instance?.RecordPresentBoxOpened(); },
                displayScale = 1.15f,
                displayOffset = new Vector2(68f, -14f)
            });

            // Supermarket box - 800x800 (scaled from 250x250), content bbox ~(76,245)-(548,737)
            boxTypes.Add(new PresentBoxType
            {
                id = "supermarket",
                displayName = "Supermarket Box",
                animFolder = "Sprites/UI/PresentOpen/Supermarket",
                framePrefix = "supermarket_open_",
                getCount = () => SaveManager.Instance?.GetPresentCount("supermarket") ?? 0,
                useOne = () => { SaveManager.Instance?.UsePresent("supermarket"); AchievementManager.Instance?.RecordPresentBoxOpened(); },
                displayScale = 1.28f,
                displayOffset = new Vector2(68f, 19f)
            });

            // Farm box - 800x800, content bbox ~(6,198)-(673,800), hay at bottom takes extra space
            boxTypes.Add(new PresentBoxType
            {
                id = "farm",
                displayName = "Farm Box",
                animFolder = "Sprites/UI/PresentOpen/Farm",
                framePrefix = "farm_open_",
                getCount = () => SaveManager.Instance?.GetPresentCount("farm") ?? 0,
                useOne = () => { SaveManager.Instance?.UsePresent("farm"); AchievementManager.Instance?.RecordPresentBoxOpened(); },
                displayScale = 1.15f,
                displayOffset = new Vector2(56f, 2f)
            });

            // Space box - 800x800, content bbox ~(57,220)-(558,746), smaller & left-shifted
            boxTypes.Add(new PresentBoxType
            {
                id = "space",
                displayName = "Space Box",
                animFolder = "Sprites/UI/PresentOpen/Space",
                framePrefix = "space_open_",
                getCount = () => SaveManager.Instance?.GetPresentCount("space") ?? 0,
                useOne = () => { SaveManager.Instance?.UsePresent("space"); AchievementManager.Instance?.RecordPresentBoxOpened(); },
                displayScale = 1.20f,
                displayOffset = new Vector2(66f, 8f)
            });

            // Tavern box - 800x800, content bbox ~(57,249)-(567,740), smaller & left-shifted
            boxTypes.Add(new PresentBoxType
            {
                id = "tavern",
                displayName = "Tavern Box",
                animFolder = "Sprites/UI/PresentOpen/Tavern",
                framePrefix = "tavern_open_",
                getCount = () => SaveManager.Instance?.GetPresentCount("tavern") ?? 0,
                useOne = () => { SaveManager.Instance?.UsePresent("tavern"); AchievementManager.Instance?.RecordPresentBoxOpened(); },
                displayScale = 1.23f,
                displayOffset = new Vector2(65f, 11f)
            });

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

        // --- Info Panel ---

        private GameObject infoOverlay;

        private void CreateInfoButton(Transform parent)
        {
            var infoBtnGO = new GameObject("InfoButton");
            infoBtnGO.transform.SetParent(parent, false);
            var infoRect = infoBtnGO.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.5f, 0.5f);
            infoRect.anchorMax = new Vector2(0.5f, 0.5f);
            infoRect.anchoredPosition = new Vector2(0, -370);
            infoRect.sizeDelta = new Vector2(200, 50);

            var infoImg = infoBtnGO.AddComponent<Image>();
            infoImg.color = new Color(0.2f, 0.15f, 0.1f, 0.8f);

            var infoBtn = infoBtnGO.AddComponent<Button>();
            infoBtn.targetGraphic = infoImg;
            infoBtn.transition = Selectable.Transition.ColorTint;
            infoBtn.onClick.AddListener(OnInfoClicked);

            var infoTextGO = new GameObject("Text");
            infoTextGO.transform.SetParent(infoBtnGO.transform, false);
            var infoTextRect = infoTextGO.AddComponent<RectTransform>();
            infoTextRect.anchorMin = Vector2.zero;
            infoTextRect.anchorMax = Vector2.one;
            infoTextRect.offsetMin = Vector2.zero;
            infoTextRect.offsetMax = Vector2.zero;
            var infoTMP = infoTextGO.AddComponent<TextMeshProUGUI>();
            infoTMP.text = "View Items";
            infoTMP.fontSize = 24;
            infoTMP.fontStyle = FontStyles.Bold;
            infoTMP.alignment = TextAlignmentOptions.Center;
            infoTMP.color = Color.white;
            infoTMP.raycastTarget = false;
        }

        private void OnInfoClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            ShowInfoOverlay();
        }

        private void ShowInfoOverlay()
        {
            if (infoOverlay != null)
                UnityEngine.Object.Destroy(infoOverlay);

            var boxType = boxTypes[currentBoxIndex];
            var pool = LootTable.GetLootPool(boxType.id);
            var ownedItems = SaveManager.Instance?.GetOwnedRoomItems() ?? new List<string>();

            int ownedCount = 0;
            foreach (var itemId in pool)
            {
                if (ownedItems.Contains(itemId)) ownedCount++;
            }

            // Fullscreen overlay
            infoOverlay = new GameObject("InfoOverlay");
            infoOverlay.transform.SetParent(panel.transform, false);
            var overlayRect = infoOverlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Dark bg
            var dimImg = infoOverlay.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.92f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(infoOverlay.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -40);
            titleRect.sizeDelta = new Vector2(600, 50);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = $"{boxType.displayName} Items";
            titleTMP.fontSize = 36;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = new Color(1f, 0.85f, 0.5f, 1f);

            // Subtitle - collection progress
            var subGO = new GameObject("Subtitle");
            subGO.transform.SetParent(infoOverlay.transform, false);
            var subRect = subGO.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1);
            subRect.anchorMax = new Vector2(0.5f, 1);
            subRect.pivot = new Vector2(0.5f, 1);
            subRect.anchoredPosition = new Vector2(0, -95);
            subRect.sizeDelta = new Vector2(600, 35);
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = $"Collected: {ownedCount} / {pool.Count}";
            subTMP.fontSize = 26;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.color = Color.white;

            // Scroll view with grid
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(infoOverlay.transform, false);
            var scrollTransform = scrollGO.AddComponent<RectTransform>();
            scrollTransform.anchorMin = new Vector2(0, 0);
            scrollTransform.anchorMax = new Vector2(1, 1);
            scrollTransform.offsetMin = new Vector2(30, 80);
            scrollTransform.offsetMax = new Vector2(-30, -140);

            var scrollView = scrollGO.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Elastic;

            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();
            var vpImg = viewportGO.AddComponent<Image>();
            vpImg.color = Color.clear;
            scrollView.viewport = viewportRect;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;

            var grid = contentGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180, 200);
            grid.spacing = new Vector2(15, 15);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.content = contentRect;

            // Populate grid
            foreach (var itemId in pool)
            {
                bool owned = ownedItems.Contains(itemId);
                CreateInfoCell(contentGO.transform, itemId, owned);
            }

            // Close button (same size as main close button: 100x100)
            var closeGO = new GameObject("CloseInfoBtn");
            closeGO.transform.SetParent(infoOverlay.transform, false);
            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-60, -60);
            closeRect.sizeDelta = new Vector2(100, 100);

            var closeImg = closeGO.AddComponent<Image>();
            var closeSpr = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_2");
            var closeSprPressed = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_pressed");
            if (closeSpr != null) closeImg.sprite = closeSpr;
            else closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayButtonClick();
                UnityEngine.Object.Destroy(infoOverlay);
                infoOverlay = null;
            });

            if (closeSpr != null && closeSprPressed != null)
            {
                var pressTrigger = closeGO.AddComponent<EventTrigger>();
                var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                down.callback.AddListener((_) => closeImg.sprite = closeSprPressed);
                pressTrigger.triggers.Add(down);
                var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                up.callback.AddListener((_) => closeImg.sprite = closeSpr);
                pressTrigger.triggers.Add(up);
            }

            // Navigation arrows to cycle through box types while viewing items
            var leftNavGO = new GameObject("InfoLeftArrow");
            leftNavGO.transform.SetParent(infoOverlay.transform, false);
            var leftNavRect = leftNavGO.AddComponent<RectTransform>();
            leftNavRect.anchorMin = new Vector2(0, 0.5f);
            leftNavRect.anchorMax = new Vector2(0, 0.5f);
            leftNavRect.pivot = new Vector2(0, 0.5f);
            leftNavRect.anchoredPosition = new Vector2(25, 0);
            leftNavRect.sizeDelta = new Vector2(153, 176);
            var leftNavImg = leftNavGO.AddComponent<Image>();
            var leftNavSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left");
            var leftNavPressed = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left_pressed");
            if (leftNavSprite != null)
            {
                leftNavImg.sprite = leftNavSprite;
                leftNavImg.preserveAspect = true;
            }
            var leftNavBtn = leftNavGO.AddComponent<Button>();
            leftNavBtn.targetGraphic = leftNavImg;
            if (leftNavSprite != null && leftNavPressed != null)
            {
                leftNavBtn.transition = Selectable.Transition.SpriteSwap;
                leftNavBtn.spriteState = new SpriteState { pressedSprite = leftNavPressed };
            }
            leftNavBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayButtonClick();
                OnLeftArrowClicked();
                ShowInfoOverlay(); // Rebuild for new box type
            });

            var rightNavGO = new GameObject("InfoRightArrow");
            rightNavGO.transform.SetParent(infoOverlay.transform, false);
            var rightNavRect = rightNavGO.AddComponent<RectTransform>();
            rightNavRect.anchorMin = new Vector2(1, 0.5f);
            rightNavRect.anchorMax = new Vector2(1, 0.5f);
            rightNavRect.pivot = new Vector2(1, 0.5f);
            rightNavRect.anchoredPosition = new Vector2(-25, 0);
            rightNavRect.sizeDelta = new Vector2(153, 176);
            var rightNavImg = rightNavGO.AddComponent<Image>();
            var rightNavSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right");
            var rightNavPressed = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right_pressed");
            if (rightNavSprite != null)
            {
                rightNavImg.sprite = rightNavSprite;
                rightNavImg.preserveAspect = true;
            }
            var rightNavBtn = rightNavGO.AddComponent<Button>();
            rightNavBtn.targetGraphic = rightNavImg;
            if (rightNavSprite != null && rightNavPressed != null)
            {
                rightNavBtn.transition = Selectable.Transition.SpriteSwap;
                rightNavBtn.spriteState = new SpriteState { pressedSprite = rightNavPressed };
            }
            rightNavBtn.onClick.AddListener(() => {
                AudioManager.Instance?.PlayButtonClick();
                OnRightArrowClicked();
                ShowInfoOverlay(); // Rebuild for new box type
            });
        }

        private void CreateInfoCell(Transform parent, string itemId, bool owned)
        {
            var cellGO = new GameObject($"Cell_{itemId}");
            cellGO.transform.SetParent(parent, false);

            var cellImg = cellGO.AddComponent<Image>();
            cellImg.color = new Color(0.15f, 0.12f, 0.1f, 0.6f);

            // Thumbnail
            var thumbGO = new GameObject("Thumb");
            thumbGO.transform.SetParent(cellGO.transform, false);
            var thumbRect = thumbGO.AddComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(0.1f, 0.25f);
            thumbRect.anchorMax = new Vector2(0.9f, 0.95f);
            thumbRect.offsetMin = Vector2.zero;
            thumbRect.offsetMax = Vector2.zero;

            var thumbImg = thumbGO.AddComponent<Image>();
            thumbImg.preserveAspect = true;
            thumbImg.raycastTarget = false;

            var itemData = RoomData.GetItemById(itemId);
            if (itemData != null)
            {
                var sprite = UIManager.LoadFullRectSprite(itemData.resourcePath);
                if (sprite != null)
                {
                    thumbImg.sprite = sprite;
                    if (!owned)
                        thumbImg.color = Color.black; // Black silhouette for unowned
                }
            }

            // Name or "?"
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(cellGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 0.25f);
            nameRect.offsetMin = new Vector2(4, 0);
            nameRect.offsetMax = new Vector2(-4, 0);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = owned ? (itemData?.displayName ?? itemId) : "???";
            nameTMP.fontSize = 14;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = owned ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
            nameTMP.raycastTarget = false;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Checkmark for owned items
            if (owned)
            {
                var checkGO = new GameObject("Check");
                checkGO.transform.SetParent(cellGO.transform, false);
                var checkRect = checkGO.AddComponent<RectTransform>();
                checkRect.anchorMin = new Vector2(1, 1);
                checkRect.anchorMax = new Vector2(1, 1);
                checkRect.pivot = new Vector2(1, 1);
                checkRect.anchoredPosition = new Vector2(-5, -5);
                checkRect.sizeDelta = new Vector2(30, 30);
                var checkTMP = checkGO.AddComponent<TextMeshProUGUI>();
                checkTMP.text = "OK";
                checkTMP.fontSize = 16;
                checkTMP.fontStyle = FontStyles.Bold;
                checkTMP.alignment = TextAlignmentOptions.Center;
                checkTMP.color = new Color(0.2f, 0.9f, 0.3f, 1f);
                checkTMP.raycastTarget = false;
            }
        }

        private void LoadAnimationFrames()
        {
            // Load smoke frames (shared across all box types)
            var smokeTextures = new List<Texture2D>();
            foreach (var tex in Resources.LoadAll<Texture2D>("Sprites/UI/PresentOpen"))
            {
                if (tex.name.StartsWith("smoke_"))
                    smokeTextures.Add(tex);
            }
            smokeTextures.Sort((a, b) => a.name.CompareTo(b.name));
            smokeFrames = new Sprite[smokeTextures.Count];
            for (int i = 0; i < smokeTextures.Count; i++)
            {
                var tex = smokeTextures[i];
                smokeFrames[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }

            // Load per-type open animation frames
            for (int b = 0; b < boxTypes.Count; b++)
            {
                var bt = boxTypes[b];
                var openTextures = new List<Texture2D>();
                foreach (var tex in Resources.LoadAll<Texture2D>(bt.animFolder))
                {
                    if (tex.name.StartsWith(bt.framePrefix))
                        openTextures.Add(tex);
                }
                openTextures.Sort((a, b2) => a.name.CompareTo(b2.name));
                bt.openFrames = new Sprite[openTextures.Count];
                for (int i = 0; i < openTextures.Count; i++)
                {
                    var tex = openTextures[i];
                    bt.openFrames[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
                boxTypes[b] = bt;
                Debug.Log($"[PresentScreen] Loaded {bt.openFrames.Length} frames for {bt.id}");
            }

            Debug.Log($"[PresentScreen] Loaded {smokeFrames.Length} smoke frames, {boxTypes.Count} box types");
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

        private Vector3 CurrentBaseScale => Vector3.one * boxTypes[currentBoxIndex].displayScale;
        private Vector2 CurrentRestPos => PresentRestPos + boxTypes[currentBoxIndex].displayOffset;

        private void ResetToPresent()
        {
            var boxType = boxTypes[currentBoxIndex];

            // Show present box using first animation frame for seamless transition
            if (boxType.openFrames != null && boxType.openFrames.Length > 0)
                presentImage.sprite = boxType.openFrames[0];
            presentImage.color = Color.white;
            presentRect.localScale = CurrentBaseScale;
            presentRect.localEulerAngles = Vector3.zero;
            presentRect.anchoredPosition = CurrentRestPos;
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

            // Start idle shake animation only if there are presents to open
            if (shakeCoroutine != null) coroutineHost.StopCoroutine(shakeCoroutine);
            if (boxType.getCount() > 0)
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

        private string lastRewardItemId;
        private bool lastRewardWasDuplicate;

        private void OnPresentClicked()
        {
            if (isAnimating) return;
            var boxType = boxTypes[currentBoxIndex];
            if (boxType.getCount() <= 0) return;

            // Consume the present
            boxType.useOne();

            // Roll from loot table
            string itemId = LootTable.GetRandomReward(boxType.id);
            string rewardSpritePath;

            if (itemId != null)
            {
                lastRewardItemId = itemId;
                lastRewardWasDuplicate = SaveManager.Instance != null && SaveManager.Instance.OwnsRoomItem(itemId);

                if (!lastRewardWasDuplicate && SaveManager.Instance != null)
                    SaveManager.Instance.UnlockRoomItem(itemId);

                var itemData = RoomData.GetItemById(itemId);
                rewardSpritePath = itemData?.resourcePath ?? "Sprites/UI/PresentOpen/middlefinger";
            }
            else
            {
                // Fallback if pool is empty
                rewardSpritePath = "Sprites/UI/PresentOpen/middlefinger";
                lastRewardItemId = null;
                lastRewardWasDuplicate = false;
            }

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
            presentRect.anchoredPosition = CurrentRestPos;
            presentRect.localScale = CurrentBaseScale;

            // Hide count text and arrows during animation
            if (countText != null) countText.gameObject.SetActive(false);
            if (leftArrowImage != null) leftArrowImage.gameObject.SetActive(false);
            if (rightArrowImage != null) rightArrowImage.gameObject.SetActive(false);

            // Phase 1: Play present open + smoke animations simultaneously
            float frameDuration = 1f / 24f;
            var currentOpenFrames = boxTypes[currentBoxIndex].openFrames;
            int maxFrames = Mathf.Max(
                currentOpenFrames != null ? currentOpenFrames.Length : 0,
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
                if (currentOpenFrames != null && i < currentOpenFrames.Length)
                    presentImage.sprite = currentOpenFrames[i];
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

            if (lastRewardWasDuplicate)
            {
                // Duplicate sequence: stamp → spin → coins
                yield return coroutineHost.StartCoroutine(PlayDuplicateRecycleAnimation());
            }
            else
            {
                // Normal: wait for player to tap anywhere to dismiss
                dismissOverlay.SetActive(true);
                isAnimating = false;
            }
        }

        private IEnumerator PlayDuplicateRecycleAnimation()
        {
            // 1. Show "DUPLICATE" stamp
            var stampGO = new GameObject("DuplicateStamp");
            stampGO.transform.SetParent(rewardImage.transform.parent, false);
            var stampRect = stampGO.AddComponent<RectTransform>();
            stampRect.anchorMin = new Vector2(0.5f, 0.5f);
            stampRect.anchorMax = new Vector2(0.5f, 0.5f);
            stampRect.sizeDelta = new Vector2(400, 80);
            stampRect.localRotation = Quaternion.Euler(0, 0, -15);

            var stampImg = stampGO.AddComponent<Image>();
            stampImg.color = new Color(0.8f, 0.15f, 0.1f, 0.85f);

            var stampTextGO = new GameObject("Text");
            stampTextGO.transform.SetParent(stampGO.transform, false);
            var stampTextRect = stampTextGO.AddComponent<RectTransform>();
            stampTextRect.anchorMin = Vector2.zero;
            stampTextRect.anchorMax = Vector2.one;
            stampTextRect.offsetMin = Vector2.zero;
            stampTextRect.offsetMax = Vector2.zero;
            var stampTMP = stampTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            stampTMP.text = "DUPLICATE";
            stampTMP.fontSize = 48;
            stampTMP.fontStyle = TMPro.FontStyles.Bold;
            stampTMP.alignment = TMPro.TextAlignmentOptions.Center;
            stampTMP.color = Color.white;

            // Stamp scales in
            stampRect.localScale = new Vector3(2f, 2f, 1f);
            float stampDuration = 0.3f;
            float elapsed = 0f;
            while (elapsed < stampDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stampDuration);
                float scale = Mathf.Lerp(2f, 1f, t * t);
                stampRect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            // 2. Pause
            yield return new WaitForSeconds(0.5f);

            // 3. Shrink and spin both reward + stamp together
            float spinDuration = 0.6f;
            elapsed = 0f;
            Vector3 rewardStartScale = rewardRect.localScale;
            while (elapsed < spinDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / spinDuration);
                float scale = Mathf.Lerp(1f, 0f, t * t);
                float rotation = t * 720f;
                rewardRect.localScale = new Vector3(scale, scale, 1f);
                rewardRect.localRotation = Quaternion.Euler(0, 0, rotation);
                stampRect.localScale = new Vector3(scale, scale, 1f);
                stampRect.localRotation = Quaternion.Euler(0, 0, -15 + rotation);
                yield return null;
            }

            rewardImage.gameObject.SetActive(false);
            rewardRect.localRotation = Quaternion.identity;
            rewardRect.localScale = Vector3.one;
            UnityEngine.Object.Destroy(stampGO);

            // 4. Show "+5" coins text
            var coinsGO = new GameObject("CoinsReward");
            coinsGO.transform.SetParent(rewardImage.transform.parent, false);
            var coinsRect = coinsGO.AddComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(0.5f, 0.5f);
            coinsRect.anchorMax = new Vector2(0.5f, 0.5f);
            coinsRect.sizeDelta = new Vector2(300, 80);
            var coinsTMP = coinsGO.AddComponent<TMPro.TextMeshProUGUI>();
            coinsTMP.text = "+5 COINS";
            coinsTMP.fontSize = 56;
            coinsTMP.fontStyle = TMPro.FontStyles.Bold;
            coinsTMP.alignment = TMPro.TextAlignmentOptions.Center;
            coinsTMP.color = new Color(1f, 0.85f, 0.2f, 1f);

            // Bounce up animation
            float bounceDuration = 0.4f;
            elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);
                float y = EaseOutBack(t) * 30f;
                coinsRect.anchoredPosition = new Vector2(0, y);
                coinsRect.localScale = Vector3.one * EaseOutBack(t);
                yield return null;
            }

            // Award coins
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.AddCoins(5);

            // Wait then auto-dismiss
            yield return new WaitForSeconds(0.8f);

            UnityEngine.Object.Destroy(coinsGO);

            // Continue to next box or close
            isAnimating = false;
            coroutineHost.StartCoroutine(AfterDuplicateDismiss());
        }

        private IEnumerator AfterDuplicateDismiss()
        {
            yield return null;
            // Check if there are more presents
            var boxType = boxTypes[currentBoxIndex];
            if (boxType.getCount() > 0)
            {
                ResetToPresent();
            }
            else
            {
                // Find next box type with presents
                bool found = false;
                for (int i = 0; i < boxTypes.Count; i++)
                {
                    if (boxTypes[i].getCount() > 0)
                    {
                        currentBoxIndex = i;
                        ResetToPresent();
                        found = true;
                        break;
                    }
                }
                if (!found) Hide();
            }
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
                float baseScale = boxTypes[currentBoxIndex].displayScale;
                Vector2 restPos = CurrentRestPos;

                // Wait 1.5-3s between shake bursts
                yield return new WaitForSeconds(UnityEngine.Random.Range(1.5f, 3f));

                // Bulge up at start of burst
                float bulgeUpTime = 0.08f;
                float bulgeElapsed = 0f;
                while (bulgeElapsed < bulgeUpTime)
                {
                    bulgeElapsed += Time.deltaTime;
                    float t = bulgeElapsed / bulgeUpTime;
                    float s = baseScale * (1f + 0.08f * Mathf.Sin(t * Mathf.PI * 0.5f));
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
                    float bulgeScale = baseScale * (1f + 0.06f * intensity);

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
                            restPos.x + Mathf.Lerp(0, offsetX, smooth),
                            restPos.y + Mathf.Lerp(0, offsetY, smooth));
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
                    presentRect.anchoredPosition = Vector2.Lerp(settleStartPos, restPos, t);
                    // Scale settles back to base
                    float scaleT = Mathf.SmoothStep(0f, 1f, t);
                    presentRect.localScale = Vector3.Lerp(settleStartScale, CurrentBaseScale, scaleT);
                    // Glow fades back
                    if (presentGlowImage != null)
                        presentGlowImage.color = new Color(1f, 0.8f, 0.2f, Mathf.Lerp(0.85f, 0.3f, t));
                    yield return null;
                }

                presentRect.localEulerAngles = Vector3.zero;
                presentRect.anchoredPosition = restPos;
                presentRect.localScale = CurrentBaseScale;
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
