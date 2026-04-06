using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace SortResort.UI
{
    public class MyRoomScreen
    {
        private GameObject panel;
        private MonoBehaviour coroutineHost;
        private Transform roomContainer;

        // Slot display
        private Dictionary<RoomSlot, Image> slotImages = new Dictionary<RoomSlot, Image>();
        private Dictionary<RoomSlot, RectTransform> slotRects = new Dictionary<RoomSlot, RectTransform>();
        private Dictionary<RoomSlot, string> currentEquipped = new Dictionary<RoomSlot, string>();

        // Picker state
        private GameObject pickerPanel;
        private GameObject pickerContent;
        private TextMeshProUGUI pickerTitle;
        private RectTransform pickerRect;
        private RoomSlot? activePickerSlot;
        private string previewItemId; // Currently previewed (not yet saved)
        private List<GameObject> pickerCards = new List<GameObject>();
        private GameObject pickerHighlight; // Highlight border on selected card
        private Coroutine slideCoroutine;
        private bool pickerVisible;

        // Picker dismiss overlay (catches taps outside picker when open)
        private GameObject pickerDismissOverlay;

        // Greyscale cache
        private Dictionary<string, Sprite> greyscaleCache = new Dictionary<string, Sprite>();

        // Music state
        private bool musicPickerMode; // true when picker is showing music instead of items
        private string currentMusicId;

        private const float PICKER_HEIGHT = 260f; // Height of the bottom bar
        private const float SLIDE_DURATION = 0.2f;

        // Music tracks available in the room
        private struct MusicTrack
        {
            public string id, displayName, resourcePath;
            public MusicTrack(string id, string displayName, string resourcePath)
            { this.id = id; this.displayName = displayName; this.resourcePath = resourcePath; }
        }

        private static readonly MusicTrack[] MusicTracks = new MusicTrack[]
        {
            new MusicTrack("island_background", "Island\nChill", "Audio/Music/island_background"),
            new MusicTrack("island_gameplay", "Island\nPlay", "Audio/Music/island_gameplay_music"),
            new MusicTrack("supermarket_background", "Market\nChill", "Audio/Music/supermarket_background"),
            new MusicTrack("supermarket_gameplay", "Market\nPlay", "Audio/Music/supermarket_gameplay_music"),
            new MusicTrack("farm_background", "Farm\nChill", "Audio/Music/farm_background_music"),
            new MusicTrack("farm_gameplay", "Farm\nPlay", "Audio/Music/farm_gameplay_music"),
            new MusicTrack("space_background", "Space\nChill", "Audio/Music/space_background"),
            new MusicTrack("space_gameplay", "Space\nPlay", "Audio/Music/space_gameplay_music"),
            new MusicTrack("tavern_background", "Tavern\nChill", "Audio/Music/tavern_background"),
            new MusicTrack("tavern_gameplay", "Tavern\nPlay", "Audio/Music/tavern_gameplay_music"),
            new MusicTrack("worldmap", "World\nMap", "Audio/Music/worldmap_music"),
        };

        public void Create(Transform parent, MonoBehaviour host)
        {
            coroutineHost = host;

            // Main panel
            panel = new GameObject("MyRoomPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var canvas = panel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5200;
            panel.AddComponent<GraphicRaycaster>();

            // Room container (holds all layered items)
            var roomGO = new GameObject("RoomContainer");
            roomGO.transform.SetParent(panel.transform, false);
            var roomRect = roomGO.AddComponent<RectTransform>();
            roomRect.anchorMin = Vector2.zero;
            roomRect.anchorMax = Vector2.one;
            roomRect.offsetMin = Vector2.zero;
            roomRect.offsetMax = Vector2.zero;
            roomContainer = roomGO.transform;

            // Create all slot layers (each wrapped in its own try-catch)
            foreach (var slot in RoomData.SlotLayerOrder)
            {
                try
                {
                    CreateSlotLayer(slot);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[MyRoomScreen] Error creating slot {slot}: {e}");
                }
            }

            // Picker dismiss overlay
            pickerDismissOverlay = new GameObject("PickerDismissOverlay");
            pickerDismissOverlay.transform.SetParent(panel.transform, false);
            var dismissRect = pickerDismissOverlay.AddComponent<RectTransform>();
            dismissRect.anchorMin = Vector2.zero;
            dismissRect.anchorMax = Vector2.one;
            dismissRect.offsetMin = Vector2.zero;
            dismissRect.offsetMax = Vector2.zero;
            var dismissImg = pickerDismissOverlay.AddComponent<Image>();
            dismissImg.color = new Color(0, 0, 0, 0);
            var dismissBtn = pickerDismissOverlay.AddComponent<Button>();
            dismissBtn.transition = Selectable.Transition.None;
            dismissBtn.onClick.AddListener(ClosePicker);
            pickerDismissOverlay.SetActive(false);

            try { CreatePickerPanel(); }
            catch (System.Exception e) { Debug.LogError($"[MyRoomScreen] Error creating picker: {e}"); }

            // Close button - ALWAYS created
            try { CreateCloseButton(); }
            catch (System.Exception e) { Debug.LogError($"[MyRoomScreen] Error creating close button: {e}"); }

            // Music button
            try { CreateMusicButton(); }
            catch (System.Exception e) { Debug.LogError($"[MyRoomScreen] Error creating music button: {e}"); }

            Debug.Log($"[MyRoomScreen] Create complete. Slots created: {slotImages.Count}, panel children: {panel.transform.childCount}");

            panel.SetActive(false);
        }

        private void CreateSlotLayer(RoomSlot slot)
        {
            string equippedId = null;
            if (SaveManager.Instance != null)
                equippedId = SaveManager.Instance.GetRoomEquippedItem(slot.ToString());

            if (string.IsNullOrEmpty(equippedId))
                equippedId = RoomData.GetDefaultItemId(slot);

            // Validate saved item still exists, fall back to default if not
            var itemData = RoomData.GetItemById(equippedId);
            if (itemData == null)
            {
                equippedId = RoomData.GetDefaultItemId(slot);
                itemData = RoomData.GetItemById(equippedId);
            }

            Debug.Log($"[MyRoomScreen] CreateSlotLayer: slot={slot}, equippedId={equippedId ?? "NULL"}");

            currentEquipped[slot] = equippedId;

            if (itemData == null)
            {
                Debug.LogWarning($"[MyRoomScreen] No item data found for slot={slot}, id={equippedId}");
                return;
            }

            var go = new GameObject($"Slot_{slot}");
            go.transform.SetParent(roomContainer, false);
            var rt = go.AddComponent<RectTransform>();

            // Position using crop anchors
            itemData.ComputeAnchors(out var anchorMin, out var anchorMax);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.raycastTarget = true;

            // Load sprite
            LoadSlotSprite(img, itemData);

            // Alpha hit test - clicks pass through transparent pixels to items behind
            try { img.alphaHitTestMinimumThreshold = 0.1f; }
            catch (System.Exception) { } // Fails if texture not readable, non-fatal

            // Button for click detection
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            var capturedSlot = slot;
            btn.onClick.AddListener(() => OnSlotClicked(capturedSlot));

            slotImages[slot] = img;
            slotRects[slot] = rt;
        }

        private void LoadSlotSprite(Image img, RoomItemData itemData)
        {
            // Handle "none" items - visually invisible but keep raycast target
            // so users can still tap the area to reopen the picker
            if (RoomData.IsNoneItem(itemData.id))
            {
                img.sprite = null;
                img.color = new Color(0, 0, 0, 0.01f); // Nearly invisible but still catches raycasts
                return;
            }

            var tex = Resources.Load<Texture2D>(itemData.resourcePath);
            if (tex != null)
            {
                img.sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                img.color = Color.white;
                img.preserveAspect = false; // Anchors handle sizing
            }
            else
            {
                img.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Fallback
                Debug.LogWarning($"[MyRoomScreen] Missing sprite: {itemData.resourcePath}");
            }
        }

        private void SwapSlotItem(RoomSlot slot, string newItemId)
        {
            var itemData = RoomData.GetItemById(newItemId);
            if (itemData == null || !slotImages.ContainsKey(slot)) return;

            // Update anchors for new item (may be different size/position)
            itemData.ComputeAnchors(out var anchorMin, out var anchorMax);
            slotRects[slot].anchorMin = anchorMin;
            slotRects[slot].anchorMax = anchorMax;
            slotRects[slot].offsetMin = Vector2.zero;
            slotRects[slot].offsetMax = Vector2.zero;

            // Load new sprite
            LoadSlotSprite(slotImages[slot], itemData);
            currentEquipped[slot] = newItemId;
        }

        // --- Picker ---

        private void CreatePickerPanel()
        {
            // Bottom bar that slides up from below the screen
            pickerPanel = new GameObject("BottomPickerPanel");
            pickerPanel.transform.SetParent(panel.transform, false);
            pickerRect = pickerPanel.AddComponent<RectTransform>();
            pickerRect.anchorMin = new Vector2(0, 0);
            pickerRect.anchorMax = new Vector2(1, 0);
            pickerRect.pivot = new Vector2(0.5f, 0);
            pickerRect.sizeDelta = new Vector2(0, PICKER_HEIGHT);
            pickerRect.anchoredPosition = new Vector2(0, -PICKER_HEIGHT); // Start hidden below

            // Background - the wooden scroll menu bar sprite
            var bgImg = pickerPanel.AddComponent<Image>();
            var barTex = Resources.Load<Texture2D>("Sprites/UI/MyRoom/ScrollBar/scroll_menu_bottom_bar");
            if (barTex != null)
            {
                bgImg.sprite = Sprite.Create(barTex, new Rect(0, 0, barTex.width, barTex.height), new Vector2(0.5f, 0.5f), 100f);
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0.3f, 0.2f, 0.1f, 0.95f);
            }

            // Title label (top of bar)
            var titleGO = new GameObject("PickerTitle");
            titleGO.transform.SetParent(pickerPanel.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -15);
            titleRect.sizeDelta = new Vector2(0, 40);
            pickerTitle = titleGO.AddComponent<TextMeshProUGUI>();
            pickerTitle.text = "SLOT";
            pickerTitle.fontSize = 28;
            pickerTitle.fontStyle = FontStyles.Bold;
            pickerTitle.alignment = TextAlignmentOptions.Center;
            pickerTitle.color = new Color(1f, 0.95f, 0.85f, 1f);

            // Left arrow button (cycle to previous slot) - purple arrow sprite
            var leftArrowGO = new GameObject("LeftArrow");
            leftArrowGO.transform.SetParent(pickerPanel.transform, false);
            var leftArrowRect = leftArrowGO.AddComponent<RectTransform>();
            leftArrowRect.anchorMin = new Vector2(0, 0.5f);
            leftArrowRect.anchorMax = new Vector2(0, 0.5f);
            leftArrowRect.pivot = new Vector2(0.5f, 0.5f);
            leftArrowRect.anchoredPosition = new Vector2(30, -5);
            leftArrowRect.sizeDelta = new Vector2(55, 65);
            var leftArrowImg = leftArrowGO.AddComponent<Image>();
            var leftSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left");
            var leftPressedSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_left_pressed");
            if (leftSprite != null)
            {
                leftArrowImg.sprite = leftSprite;
                leftArrowImg.preserveAspect = true;
                leftArrowImg.color = Color.white;
            }
            else
            {
                leftArrowImg.color = new Color(0.15f, 0.1f, 0.05f, 0.6f);
            }
            var leftArrowBtn = leftArrowGO.AddComponent<Button>();
            leftArrowBtn.targetGraphic = leftArrowImg;
            if (leftSprite != null && leftPressedSprite != null)
            {
                leftArrowBtn.transition = Selectable.Transition.SpriteSwap;
                leftArrowBtn.spriteState = new SpriteState { pressedSprite = leftPressedSprite };
            }
            leftArrowBtn.onClick.AddListener(OnPickerPrevSlot);

            // Right arrow button (cycle to next slot) - purple arrow sprite
            var rightArrowGO = new GameObject("RightArrow");
            rightArrowGO.transform.SetParent(pickerPanel.transform, false);
            var rightArrowRect = rightArrowGO.AddComponent<RectTransform>();
            rightArrowRect.anchorMin = new Vector2(1, 0.5f);
            rightArrowRect.anchorMax = new Vector2(1, 0.5f);
            rightArrowRect.pivot = new Vector2(0.5f, 0.5f);
            rightArrowRect.anchoredPosition = new Vector2(-30, -5);
            rightArrowRect.sizeDelta = new Vector2(55, 65);
            var rightArrowImg = rightArrowGO.AddComponent<Image>();
            var rightSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right");
            var rightPressedSprite = Resources.Load<Sprite>("Sprites/UI/Buttons/button_right_pressed");
            if (rightSprite != null)
            {
                rightArrowImg.sprite = rightSprite;
                rightArrowImg.preserveAspect = true;
                rightArrowImg.color = Color.white;
            }
            else
            {
                rightArrowImg.color = new Color(0.15f, 0.1f, 0.05f, 0.6f);
            }
            var rightArrowBtn = rightArrowGO.AddComponent<Button>();
            rightArrowBtn.targetGraphic = rightArrowImg;
            if (rightSprite != null && rightPressedSprite != null)
            {
                rightArrowBtn.transition = Selectable.Transition.SpriteSwap;
                rightArrowBtn.spriteState = new SpriteState { pressedSprite = rightPressedSprite };
            }
            rightArrowBtn.onClick.AddListener(OnPickerNextSlot);

            // Horizontal scroll view for items (narrowed to fit between arrows)
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(pickerPanel.transform, false);
            var scrollRectTransform = scrollGO.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.offsetMin = new Vector2(60, 15);
            scrollRectTransform.offsetMax = new Vector2(-60, -45);

            var scrollView = scrollGO.AddComponent<ScrollRect>();
            scrollView.horizontal = true;
            scrollView.vertical = false;
            scrollView.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = Color.clear;

            scrollView.viewport = viewportRect;

            // Content (horizontal layout)
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            var layout = contentGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.content = contentRect;
            pickerContent = contentGO;

            pickerPanel.SetActive(false);
        }

        private void CreateCloseButton()
        {
            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(panel.transform, false);
            var closeBtnRect = closeBtnGO.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 1);
            closeBtnRect.anchorMax = new Vector2(1, 1);
            closeBtnRect.pivot = new Vector2(1, 1);
            closeBtnRect.anchoredPosition = new Vector2(-20, -20);
            closeBtnRect.sizeDelta = new Vector2(120, 120);

            var closeBtnImg = closeBtnGO.AddComponent<Image>();
            var normalSprite = UIManager.LoadFullRectSprite("Sprites/UI/Achievements/closebutton_2");
            var pressedSprite = UIManager.LoadFullRectSprite("Sprites/UI/Achievements/closebutton_pressed");

            if (normalSprite != null)
            {
                closeBtnImg.sprite = normalSprite;
                closeBtnImg.color = Color.white;
            }
            else
            {
                // Bright red fallback - unmissable
                closeBtnImg.color = new Color(0.9f, 0.15f, 0.15f, 1f);
            }

            closeBtnImg.raycastTarget = true;

            var closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(CloseScreen);

            // Always add an X text label so it's visible even without sprite
            var xTextGO = new GameObject("XLabel");
            xTextGO.transform.SetParent(closeBtnGO.transform, false);
            var xTextRect = xTextGO.AddComponent<RectTransform>();
            xTextRect.anchorMin = Vector2.zero;
            xTextRect.anchorMax = Vector2.one;
            xTextRect.offsetMin = Vector2.zero;
            xTextRect.offsetMax = Vector2.zero;
            var xText = xTextGO.AddComponent<TextMeshProUGUI>();
            xText.text = "X";
            xText.fontSize = 48;
            xText.fontStyle = FontStyles.Bold;
            xText.alignment = TextAlignmentOptions.Center;
            xText.color = normalSprite != null ? Color.clear : Color.white; // Only show text if no sprite
            xText.raycastTarget = false;

            Debug.Log($"[MyRoomScreen] Close button created. Sprite loaded: {normalSprite != null}");

            // Press visual
            if (pressedSprite != null)
            {
                var trigger = closeBtnGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var down = new UnityEngine.EventSystems.EventTrigger.Entry();
                down.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
                down.callback.AddListener((_) => closeBtnImg.sprite = pressedSprite);
                trigger.triggers.Add(down);
                var up = new UnityEngine.EventSystems.EventTrigger.Entry();
                up.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
                up.callback.AddListener((_) => closeBtnImg.sprite = normalSprite);
                trigger.triggers.Add(up);
            }
        }

        private void CreateMusicButton()
        {
            var musicBtnGO = new GameObject("MusicButton");
            musicBtnGO.transform.SetParent(panel.transform, false);
            var musicBtnRect = musicBtnGO.AddComponent<RectTransform>();
            musicBtnRect.anchorMin = new Vector2(0, 1);
            musicBtnRect.anchorMax = new Vector2(0, 1);
            musicBtnRect.pivot = new Vector2(0.5f, 0.5f);
            musicBtnRect.anchoredPosition = new Vector2(50, -50);
            musicBtnRect.sizeDelta = new Vector2(80, 80);

            var musicBtnImg = musicBtnGO.AddComponent<Image>();
            var musicNoteSprite = UIManager.LoadFullRectSprite("Sprites/UI/Settings/musicnote_circle");
            if (musicNoteSprite != null)
            {
                musicBtnImg.sprite = musicNoteSprite;
                musicBtnImg.preserveAspect = true;
                musicBtnImg.color = Color.white;
            }
            else
            {
                musicBtnImg.color = new Color(0.3f, 0.25f, 0.5f, 0.85f);
            }
            musicBtnImg.raycastTarget = true;

            var musicBtn = musicBtnGO.AddComponent<Button>();
            musicBtn.targetGraphic = musicBtnImg;
            musicBtn.transition = Selectable.Transition.ColorTint;
            musicBtn.onClick.AddListener(OnMusicButtonClicked);
        }

        private void OnMusicButtonClicked()
        {
            AudioManager.Instance?.PlayButtonClick();

            if (pickerVisible && musicPickerMode)
            {
                ClosePicker();
                return;
            }

            OpenMusicPicker();
        }

        private void OpenMusicPicker()
        {
            // Close any item picker first
            if (pickerVisible && !musicPickerMode)
            {
                // Save current item selection before switching
                if (activePickerSlot.HasValue && previewItemId != null)
                    SaveManager.Instance?.SetRoomEquippedItem(activePickerSlot.Value.ToString(), previewItemId);
            }

            musicPickerMode = true;
            activePickerSlot = null;

            // Load current music selection
            if (string.IsNullOrEmpty(currentMusicId))
            {
                currentMusicId = SaveManager.Instance?.GetRoomEquippedItem("Music");
                if (string.IsNullOrEmpty(currentMusicId))
                    currentMusicId = "island_background"; // Default
            }

            pickerTitle.text = "MUSIC";

            // Clear old cards
            foreach (var card in pickerCards)
                Object.Destroy(card);
            pickerCards.Clear();

            // Populate with music tracks
            foreach (var track in MusicTracks)
            {
                CreateMusicCard(track.id, track.displayName, track.resourcePath);
            }

            pickerPanel.SetActive(true);
            pickerDismissOverlay.SetActive(true);

            if (slideCoroutine != null)
                coroutineHost.StopCoroutine(slideCoroutine);
            slideCoroutine = coroutineHost.StartCoroutine(SlidePicker(true));
            pickerVisible = true;
        }

        private void CreateMusicCard(string trackId, string displayName, string resourcePath)
        {
            bool isSelected = (currentMusicId == trackId);

            var cardGO = new GameObject($"MusicCard_{trackId}");
            cardGO.transform.SetParent(pickerContent.transform, false);

            var cardLE = cardGO.AddComponent<LayoutElement>();
            cardLE.preferredWidth = 150;
            cardLE.minWidth = 150;

            var cardImg = cardGO.AddComponent<Image>();
            cardImg.color = isSelected
                ? new Color(1f, 0.85f, 0.4f, 0.3f)
                : new Color(0.15f, 0.12f, 0.08f, 0.6f);

            if (isSelected)
            {
                var outline = cardGO.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                outline.effectDistance = new Vector2(4, 4);
            }

            // Track name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(cardGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(8, 0);
            nameRect.offsetMax = new Vector2(-8, 0);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = displayName;
            nameTMP.fontSize = 18;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = new Color(1f, 0.95f, 0.85f, 1f);
            nameTMP.raycastTarget = false;

            var btn = cardGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = cardImg;
            var capturedId = trackId;
            var capturedPath = resourcePath;
            btn.onClick.AddListener(() => OnMusicTrackSelected(capturedId, capturedPath));

            pickerCards.Add(cardGO);
        }

        private void OnMusicTrackSelected(string trackId, string resourcePath)
        {
            if (currentMusicId == trackId) return;

            AudioManager.Instance?.PlayButtonClick();
            currentMusicId = trackId;

            // Play the selected music immediately as preview
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null)
                AudioManager.Instance?.CrossfadeToMusic(clip);

            // Refresh highlight
            RefreshMusicPickerHighlights();
        }

        private void RefreshMusicPickerHighlights()
        {
            foreach (var card in pickerCards)
                Object.Destroy(card);
            pickerCards.Clear();

            foreach (var track in MusicTracks)
                CreateMusicCard(track.id, track.displayName, track.resourcePath);
        }

        // --- Slot Click ---

        private void OnSlotClicked(RoomSlot slot)
        {
            Debug.Log($"[MyRoomScreen] Slot clicked: {slot}");
            AudioManager.Instance?.PlayButtonClick();

            if (pickerVisible && activePickerSlot == slot)
            {
                ClosePicker();
                return;
            }

            OpenPicker(slot);
        }

        // Slots that can be cycled through (excludes Music which is separate)
        private static readonly RoomSlot[] CycleableSlots = RoomData.SlotLayerOrder;

        private void OnPickerPrevSlot()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (musicPickerMode)
            {
                // Switch from music to last item slot
                SaveMusicSelection();
                OpenPicker(CycleableSlots[CycleableSlots.Length - 1]);
                return;
            }
            if (!activePickerSlot.HasValue) return;

            // Save current selection before switching
            SaveCurrentSlotSelection();

            int idx = System.Array.IndexOf(CycleableSlots, activePickerSlot.Value);
            int prevIdx = (idx - 1 + CycleableSlots.Length) % CycleableSlots.Length;
            OpenPickerImmediate(CycleableSlots[prevIdx]);
        }

        private void OnPickerNextSlot()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (musicPickerMode)
            {
                // Switch from music to first item slot
                SaveMusicSelection();
                OpenPicker(CycleableSlots[0]);
                return;
            }
            if (!activePickerSlot.HasValue) return;

            // Save current selection before switching
            SaveCurrentSlotSelection();

            int idx = System.Array.IndexOf(CycleableSlots, activePickerSlot.Value);
            int nextIdx = (idx + 1) % CycleableSlots.Length;
            OpenPickerImmediate(CycleableSlots[nextIdx]);
        }

        private void SaveCurrentSlotSelection()
        {
            if (activePickerSlot.HasValue && previewItemId != null)
                SaveManager.Instance?.SetRoomEquippedItem(activePickerSlot.Value.ToString(), previewItemId);
        }

        private void SaveMusicSelection()
        {
            if (!string.IsNullOrEmpty(currentMusicId))
                SaveManager.Instance?.SetRoomEquippedItem("Music", currentMusicId);
        }

        // Opens picker for a slot without slide animation (for cycling between slots)
        private void OpenPickerImmediate(RoomSlot slot)
        {
            musicPickerMode = false;
            activePickerSlot = slot;
            previewItemId = currentEquipped.ContainsKey(slot) ? currentEquipped[slot] : null;

            pickerTitle.text = RoomData.GetSlotDisplayName(slot).ToUpper();

            foreach (var card in pickerCards)
                Object.Destroy(card);
            pickerCards.Clear();

            var items = RoomData.GetItemsForSlot(slot);
            foreach (var item in items)
                CreatePickerCard(item);
        }

        private void OpenPicker(RoomSlot slot)
        {
            musicPickerMode = false;
            activePickerSlot = slot;
            previewItemId = currentEquipped.ContainsKey(slot) ? currentEquipped[slot] : null;

            // Set title
            pickerTitle.text = RoomData.GetSlotDisplayName(slot).ToUpper();

            // Clear old cards
            foreach (var card in pickerCards)
                Object.Destroy(card);
            pickerCards.Clear();

            // Populate with items for this slot
            var items = RoomData.GetItemsForSlot(slot);
            foreach (var item in items)
            {
                CreatePickerCard(item);
            }

            // Show picker
            pickerPanel.SetActive(true);
            pickerDismissOverlay.SetActive(true);

            if (slideCoroutine != null)
                coroutineHost.StopCoroutine(slideCoroutine);
            slideCoroutine = coroutineHost.StartCoroutine(SlidePicker(true));
            pickerVisible = true;
        }

        private void ClosePicker()
        {
            if (!pickerVisible) return;

            // Save the selection
            if (musicPickerMode)
            {
                // Save music selection
                if (!string.IsNullOrEmpty(currentMusicId))
                    SaveManager.Instance?.SetRoomEquippedItem("Music", currentMusicId);
            }
            else if (activePickerSlot.HasValue && previewItemId != null)
            {
                SaveManager.Instance?.SetRoomEquippedItem(activePickerSlot.Value.ToString(), previewItemId);
            }

            pickerDismissOverlay.SetActive(false);

            if (slideCoroutine != null)
                coroutineHost.StopCoroutine(slideCoroutine);
            slideCoroutine = coroutineHost.StartCoroutine(SlidePicker(false));
            pickerVisible = false;
            activePickerSlot = null;
            musicPickerMode = false;
        }

        private void CreatePickerCard(RoomItemData item)
        {
            bool isUnlocked = RoomData.IsRoomItemUnlocked(item.id);
            bool isSelected = (previewItemId == item.id);

            // Square tile for horizontal scrolling
            var cardGO = new GameObject($"Card_{item.id}");
            cardGO.transform.SetParent(pickerContent.transform, false);

            var cardLE = cardGO.AddComponent<LayoutElement>();
            cardLE.preferredWidth = 150;
            cardLE.minWidth = 150;

            // Transparent card background (no visible bg, just the item image)
            var cardImg = cardGO.AddComponent<Image>();
            cardImg.color = Color.clear;

            // Thumbnail image (fills most of card)
            var thumbGO = new GameObject("Thumbnail");
            thumbGO.transform.SetParent(cardGO.transform, false);
            var thumbRect = thumbGO.AddComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(0.05f, 0.15f);
            thumbRect.anchorMax = new Vector2(0.95f, 0.95f);
            thumbRect.offsetMin = Vector2.zero;
            thumbRect.offsetMax = Vector2.zero;

            var thumbImg = thumbGO.AddComponent<Image>();
            thumbImg.preserveAspect = true;
            thumbImg.raycastTarget = false;

            if (RoomData.IsNoneItem(item.id))
            {
                // Red X for "none" option
                thumbImg.color = Color.clear;
                var xGO = new GameObject("RedX");
                xGO.transform.SetParent(thumbGO.transform, false);
                var xRect = xGO.AddComponent<RectTransform>();
                xRect.anchorMin = Vector2.zero;
                xRect.anchorMax = Vector2.one;
                xRect.offsetMin = Vector2.zero;
                xRect.offsetMax = Vector2.zero;
                var xText = xGO.AddComponent<TextMeshProUGUI>();
                xText.text = "X";
                xText.fontSize = 60;
                xText.fontStyle = FontStyles.Bold;
                xText.alignment = TextAlignmentOptions.Center;
                xText.color = new Color(0.9f, 0.2f, 0.2f, 1f);
                xText.raycastTarget = false;
            }
            else
            {
                var sprite = UIManager.LoadFullRectSprite(item.resourcePath);
                if (sprite != null)
                {
                    thumbImg.sprite = sprite;
                    if (!isUnlocked)
                        thumbImg.color = Color.black; // Black silhouette for locked items
                }
            }

            // Outline highlight on the thumbnail image for selected item
            if (isSelected)
            {
                var outline = thumbGO.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                outline.effectDistance = new Vector2(4, 4);

                // Second outline for thicker border
                var outline2 = thumbGO.AddComponent<Outline>();
                outline2.effectColor = new Color(1f, 0.85f, 0.2f, 0.7f);
                outline2.effectDistance = new Vector2(2, 2);
            }

            // Item name (small, below thumbnail)
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(cardGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 0.15f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = item.displayName;
            nameTMP.fontSize = 14;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = isUnlocked ? new Color(1f, 0.95f, 0.85f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            nameTMP.raycastTarget = false;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Locked items already shown as black silhouette via thumbImg.color

            // Click handler
            if (isUnlocked)
            {
                var btn = cardGO.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = cardImg;
                var capturedItemId = item.id;
                var capturedSlot = item.slot;
                btn.onClick.AddListener(() => OnPickerItemSelected(capturedSlot, capturedItemId));
            }

            pickerCards.Add(cardGO);
        }

        private void OnPickerItemSelected(RoomSlot slot, string itemId)
        {
            if (previewItemId == itemId) return; // Already selected

            AudioManager.Instance?.PlayButtonClick();

            // Live preview: swap the item in the room immediately
            previewItemId = itemId;
            SwapSlotItem(slot, itemId);

            // Rebuild picker cards to update highlight
            RefreshPickerHighlights();
        }

        private void RefreshPickerHighlights()
        {
            if (!activePickerSlot.HasValue) return;

            // Rebuild cards to reflect new selection
            foreach (var card in pickerCards)
                Object.Destroy(card);
            pickerCards.Clear();

            var items = RoomData.GetItemsForSlot(activePickerSlot.Value);
            foreach (var item in items)
            {
                CreatePickerCard(item);
            }
        }

        private IEnumerator SlidePicker(bool open)
        {
            float startY = open ? -PICKER_HEIGHT : 0;
            float endY = open ? 0 : -PICKER_HEIGHT;
            float elapsed = 0;

            while (elapsed < SLIDE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / SLIDE_DURATION);
                // Ease out
                t = 1f - (1f - t) * (1f - t);
                float y = Mathf.Lerp(startY, endY, t);
                pickerRect.anchoredPosition = new Vector2(0, y);
                yield return null;
            }

            pickerRect.anchoredPosition = new Vector2(0, endY);

            if (!open)
                pickerPanel.SetActive(false);

            slideCoroutine = null;
        }

        // --- Show/Hide ---

        public void Show()
        {
            if (panel == null) return;

            // Refresh equipped items from save data
            foreach (var slot in RoomData.SlotLayerOrder)
            {
                string savedId = null;
                if (SaveManager.Instance != null)
                    savedId = SaveManager.Instance.GetRoomEquippedItem(slot.ToString());

                if (string.IsNullOrEmpty(savedId))
                    savedId = RoomData.GetDefaultItemId(slot);

                // Validate saved item still exists
                if (RoomData.GetItemById(savedId) == null)
                    savedId = RoomData.GetDefaultItemId(slot);

                string currentId = currentEquipped.ContainsKey(slot) ? currentEquipped[slot] : null;
                if (savedId != null && savedId != currentId)
                    SwapSlotItem(slot, savedId);
            }

            // Load and play saved room music
            currentMusicId = SaveManager.Instance?.GetRoomEquippedItem("Music");
            if (string.IsNullOrEmpty(currentMusicId))
                currentMusicId = "island_background";

            // Play room music
            foreach (var track in MusicTracks)
            {
                if (track.id == currentMusicId)
                {
                    var clip = Resources.Load<AudioClip>(track.resourcePath);
                    if (clip != null)
                        AudioManager.Instance?.CrossfadeToMusic(clip);
                    break;
                }
            }

            panel.SetActive(true);
            Debug.Log("[MyRoomScreen] Shown");
        }

        public void Hide()
        {
            if (panel == null) return;

            // Close picker if open (saves selection)
            if (pickerVisible)
                ClosePicker();

            // Restore worldmap music
            var worldmapClip = Resources.Load<AudioClip>("Audio/Music/worldmap_music");
            if (worldmapClip != null)
                AudioManager.Instance?.CrossfadeToMusic(worldmapClip);

            panel.SetActive(false);
            Debug.Log("[MyRoomScreen] Hidden");
        }

        private void CloseScreen()
        {
            AudioManager.Instance?.PlayButtonClick();
            Hide();
        }

        public bool IsVisible => panel != null && panel.activeSelf;
    }
}
