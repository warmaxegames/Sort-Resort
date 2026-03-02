using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace SortResort
{
    public class PowerUpBarUI : MonoBehaviour
    {
        private const float BAR_HEIGHT = 168f; // Bottom bar Y=1752 to 1920
        private const float BUTTON_SIZE = 150f;
        private const float BUTTON_SPACING = 20f;
        private const float BADGE_SIZE = 50f; // Matches the ~50px circle baked into sprite
        private const float PLUS_SIZE = 52f;
        // The sprite's built-in circle center offset from button center (measured from PNG)
        private const float BADGE_OFFSET_X = 43f;
        private const float BADGE_OFFSET_Y = 41f;

        private struct PowerUpButtonUI
        {
            public GameObject root;
            public Image buttonImage;
            public Button button;
            public CanvasGroup canvasGroup;
            public TextMeshProUGUI countText;
            public GameObject countBadge;
            public Image plusOverlay;
            public PowerUpType type;
        }

        private PowerUpButtonUI[] buttons = new PowerUpButtonUI[PowerUpData.POWER_UP_COUNT];
        private GameObject barContainer;
        private Sprite plusNormalSprite;
        private Sprite plusPressedSprite;

        public void CreateButtons(Transform canvasTransform)
        {
            // Load plus button sprites once
            plusNormalSprite = LoadSprite("Sprites/UI/PowerUps/plus_button");
            plusPressedSprite = LoadSprite("Sprites/UI/PowerUps/plus_button_pressed");

            // Create bar container anchored to bottom
            barContainer = new GameObject("PowerUp Bar");
            barContainer.transform.SetParent(canvasTransform, false);
            var barRect = barContainer.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(0, BAR_HEIGHT);

            // Calculate total width for centering 4 buttons
            float totalWidth = PowerUpData.POWER_UP_COUNT * BUTTON_SIZE + (PowerUpData.POWER_UP_COUNT - 1) * BUTTON_SPACING;
            float startX = -totalWidth / 2f + BUTTON_SIZE / 2f;

            for (int i = 0; i < PowerUpData.POWER_UP_COUNT; i++)
            {
                var type = (PowerUpType)i;
                var config = PowerUpData.GetConfig(type);
                float x = startX + i * (BUTTON_SIZE + BUTTON_SPACING);

                buttons[i] = CreateSingleButton(barContainer.transform, type, config, x);
            }

            barContainer.SetActive(false);
        }

        private PowerUpButtonUI CreateSingleButton(Transform parent, PowerUpType type, PowerUpData.PowerUpConfig config, float xPos)
        {
            var ui = new PowerUpButtonUI();
            ui.type = type;

            // Root container
            ui.root = new GameObject($"PowerUp_{config.spriteName}");
            ui.root.transform.SetParent(parent, false);
            var rootRect = ui.root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(xPos, 0);
            rootRect.sizeDelta = new Vector2(BUTTON_SIZE, BUTTON_SIZE);

            // Button image
            ui.buttonImage = ui.root.AddComponent<Image>();
            var normalSprite = LoadSprite($"Sprites/UI/PowerUps/{config.spriteName}");
            var pressedSprite = LoadSprite($"Sprites/UI/PowerUps/{config.pressedSpriteName}");
            if (normalSprite != null)
            {
                ui.buttonImage.sprite = normalSprite;
                ui.buttonImage.preserveAspect = true;
            }

            // Button component with sprite swap
            ui.button = ui.root.AddComponent<Button>();
            ui.button.targetGraphic = ui.buttonImage;
            if (pressedSprite != null)
            {
                var spriteState = new SpriteState();
                spriteState.pressedSprite = pressedSprite;
                ui.button.spriteState = spriteState;
                ui.button.transition = Selectable.Transition.SpriteSwap;
            }

            // CanvasGroup for gray-out
            ui.canvasGroup = ui.root.AddComponent<CanvasGroup>();

            // Click handler
            var capturedType = type;
            ui.button.onClick.AddListener(() => OnButtonClicked(capturedType));

            // Pointer down/up handlers for plus pressed sprite
            var trigger = ui.root.AddComponent<EventTrigger>();
            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener((_) => OnButtonPointerDown(capturedType));
            trigger.triggers.Add(downEntry);
            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener((_) => OnButtonPointerUp(capturedType));
            trigger.triggers.Add(upEntry);

            // Count badge - positioned over the white circle baked into the sprite
            // No programmatic circle needed; the sprite already has one
            ui.countBadge = new GameObject("CountBadge");
            ui.countBadge.transform.SetParent(ui.root.transform, false);
            var badgeRect = ui.countBadge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(BADGE_OFFSET_X, BADGE_OFFSET_Y);
            badgeRect.sizeDelta = new Vector2(BADGE_SIZE, BADGE_SIZE);

            // Count text - centered over the sprite's built-in circle
            var textGO = new GameObject("CountText");
            textGO.transform.SetParent(ui.countBadge.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            ui.countText = textGO.AddComponent<TextMeshProUGUI>();
            ui.countText.text = "0";
            ui.countText.fontSize = 24;
            ui.countText.font = FontManager.Bold;
            ui.countText.alignment = TextAlignmentOptions.Center;
            ui.countText.color = Color.black;
            ui.countText.raycastTarget = false;

            // Plus overlay (shown when count=0)
            var plusGO = new GameObject("PlusOverlay");
            plusGO.transform.SetParent(ui.countBadge.transform, false);
            var plusRect = plusGO.AddComponent<RectTransform>();
            plusRect.anchorMin = new Vector2(0.5f, 0.5f);
            plusRect.anchorMax = new Vector2(0.5f, 0.5f);
            plusRect.pivot = new Vector2(0.5f, 0.5f);
            plusRect.anchoredPosition = Vector2.zero;
            plusRect.sizeDelta = new Vector2(PLUS_SIZE, PLUS_SIZE);

            ui.plusOverlay = plusGO.AddComponent<Image>();
            if (plusNormalSprite != null)
            {
                ui.plusOverlay.sprite = plusNormalSprite;
                ui.plusOverlay.preserveAspect = true;
            }
            // Let clicks pass through to the main button underneath
            ui.plusOverlay.raycastTarget = false;

            plusGO.SetActive(false);

            return ui;
        }

        public void ConfigureForLevel(GameMode mode, LevelData levelData)
        {
            if (barContainer == null) return;

            bool anyVisible = false;

            for (int i = 0; i < PowerUpData.POWER_UP_COUNT; i++)
            {
                var type = (PowerUpType)i;
                var ui = buttons[i];
                if (ui.root == null) continue;

                bool unlocked = SaveManager.Instance?.IsPowerUpUnlocked(type) ?? false;
                bool availableInMode = PowerUpData.IsAvailableInMode(type, mode);
                bool availableForLevel = PowerUpData.IsAvailableForLevel(type, mode, levelData);

                if (!unlocked)
                {
                    ui.root.SetActive(false);
                    continue;
                }

                ui.root.SetActive(true);
                anyVisible = true;

                // Gray out if not available in this mode/level
                bool available = availableInMode && availableForLevel;
                ui.canvasGroup.alpha = available ? 1f : 0.4f;
                ui.canvasGroup.interactable = available;
                ui.button.interactable = available;

                // Update count
                int count = PowerUpManager.Instance?.GetCount(type) ?? 0;
                UpdateCountDisplay(i, count);
            }

            barContainer.SetActive(anyVisible);
        }

        public void UpdateCount(PowerUpType type, int newCount)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= buttons.Length) return;
            UpdateCountDisplay(idx, newCount);
        }

        public void Show()
        {
            if (barContainer != null)
                barContainer.SetActive(true);
        }

        public void Hide()
        {
            if (barContainer != null)
                barContainer.SetActive(false);
        }

        public GameObject GetButtonRoot(PowerUpType type)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= buttons.Length) return null;
            return buttons[idx].root;
        }

        public void SetButtonHighlight(PowerUpType type, bool highlighted)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= buttons.Length) return;
            var ui = buttons[idx];
            if (ui.buttonImage != null)
                ui.buttonImage.color = highlighted ? Color.yellow : Color.white;
        }

        public void ClearAllHighlights()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].buttonImage != null)
                    buttons[i].buttonImage.color = Color.white;
            }
        }

        private void UpdateCountDisplay(int idx, int count)
        {
            var ui = buttons[idx];
            if (ui.countText != null)
                ui.countText.text = count.ToString();

            // Show plus overlay when count is 0, hide badge text
            bool isEmpty = count <= 0;
            if (ui.plusOverlay != null)
                ui.plusOverlay.gameObject.SetActive(isEmpty);
            if (ui.countBadge != null)
            {
                // Hide the count text when showing plus
                if (ui.countText != null)
                    ui.countText.gameObject.SetActive(!isEmpty);
            }
        }

        private void OnButtonPointerDown(PowerUpType type)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= buttons.Length) return;

            int count = PowerUpManager.Instance?.GetCount(type) ?? 0;
            var ui = buttons[idx];
            if (count <= 0 && ui.plusOverlay != null && ui.plusOverlay.gameObject.activeSelf && plusPressedSprite != null)
            {
                ui.plusOverlay.sprite = plusPressedSprite;
            }
        }

        private void OnButtonPointerUp(PowerUpType type)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= buttons.Length) return;

            var ui = buttons[idx];
            if (ui.plusOverlay != null && plusNormalSprite != null)
            {
                ui.plusOverlay.sprite = plusNormalSprite;
            }
        }

        private void OnButtonClicked(PowerUpType type)
        {
            PowerUpManager.Instance?.ActivatePowerUp(type);
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Debug.LogWarning($"[PowerUpBarUI] Failed to load sprite: {resourcePath}");
            return null;
        }

    }
}
