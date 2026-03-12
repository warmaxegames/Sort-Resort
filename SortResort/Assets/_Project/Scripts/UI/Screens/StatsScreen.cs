using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SortResort.UI
{
    /// <summary>
    /// Player stats screen with tabbed categories, mirroring the achievements panel style.
    /// Reuses the same board/tab/close button sprites from the achievements UI.
    /// </summary>
    public class StatsScreen
    {
        // Tab IDs
        public const string TAB_GENERAL = "general";
        public const string TAB_MATCHING = "matching";
        public const string TAB_STARS = "stars";
        public const string TAB_MODES = "modes";
        public const string TAB_POWERUPS = "powerups";
        public const string TAB_WORLDS = "worlds";

        private static readonly string[] TabIds = { TAB_GENERAL, TAB_MATCHING, TAB_STARS, TAB_MODES, TAB_POWERUPS, TAB_WORLDS };
        private static readonly string[] TabLabels = { "GENERAL", "MATCHING", "STARS", "MODES", "POWER\nUPS", "WORLDS" };

        private GameObject panel;
        private Transform listContent;
        private string currentTab = TAB_GENERAL;
        private Dictionary<string, Button> tabButtons = new Dictionary<string, Button>();
        private Dictionary<string, Image> tabImages = new Dictionary<string, Image>();
        private Sprite tabSprite;
        private Sprite tabPressedSprite;
        private TextMeshProUGUI titleBarText;
        private TextMeshProUGUI titleBarShadow;

        public GameObject Panel => panel;

        /// <summary>
        /// Creates the stats panel hierarchy, mirroring the achievements panel layout.
        /// </summary>
        public void Create(Transform parent)
        {
            panel = new GameObject("Stats Panel");
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Override sorting to render above everything
            var canvas = panel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5200;
            panel.AddComponent<GraphicRaycaster>();
            panel.AddComponent<CanvasGroup>();

            // Cache tab sprites (reuse achievements tab sprites)
            tabSprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_tab");
            tabPressedSprite = LoadFullRectSprite("Sprites/UI/Achievements/achv_tab_pressed");

            // Layer 0: Dark background
            CreateFullscreenImage(panel.transform, "DimBg", null, new Color(0, 0, 0, 0.95f), true);

            // Layer 1: Board
            CreateCroppedLayer(panel.transform, "Board", "Sprites/UI/Achievements/achv_board");

            // Layer 2: Yellow background
            CreateCroppedLayer(panel.transform, "BgYellow", "Sprites/UI/Achievements/achv_bg_yellow");

            // Layer 3: Scroll Area (same bounds as achievements)
            var scrollAreaGO = new GameObject("ScrollArea");
            scrollAreaGO.transform.SetParent(panel.transform, false);
            var scrollAreaRect = scrollAreaGO.AddComponent<RectTransform>();
            scrollAreaRect.anchorMin = new Vector2(0.215f, 0.061f);
            scrollAreaRect.anchorMax = new Vector2(0.822f, 0.729f);
            scrollAreaRect.offsetMin = Vector2.zero;
            scrollAreaRect.offsetMax = Vector2.zero;

            var scrollRect = scrollAreaGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 50f;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollAreaGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();
            scrollRect.viewport = viewportRect;

            // List content
            var listContentGO = new GameObject("ListContent");
            listContentGO.transform.SetParent(viewportGO.transform, false);
            var listContentRect = listContentGO.AddComponent<RectTransform>();
            listContentRect.anchorMin = new Vector2(0, 1);
            listContentRect.anchorMax = new Vector2(1, 1);
            listContentRect.pivot = new Vector2(0.5f, 1);
            listContentRect.anchoredPosition = Vector2.zero;
            listContentRect.sizeDelta = new Vector2(0, 0);
            listContent = listContentGO.transform;

            var csf = listContentGO.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var vLayout = listContentGO.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8;
            vLayout.padding = new RectOffset(10, 10, 20, 20);
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            scrollRect.content = listContentRect;

            // Layer 4: Tab Container (same position as achievements)
            var tabContainerGO = new GameObject("TabContainer");
            tabContainerGO.transform.SetParent(panel.transform, false);
            var tabContainerRect = tabContainerGO.AddComponent<RectTransform>();
            tabContainerRect.anchorMin = new Vector2(0f, 0.318f);
            tabContainerRect.anchorMax = new Vector2(0.202f, 0.704f);
            tabContainerRect.offsetMin = Vector2.zero;
            tabContainerRect.offsetMax = Vector2.zero;

            var tabLayout = tabContainerGO.AddComponent<VerticalLayoutGroup>();
            tabLayout.spacing = 0;
            tabLayout.padding = new RectOffset(0, 0, 0, 0);
            tabLayout.childAlignment = TextAnchor.UpperLeft;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = false;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = false;

            // Create tabs
            for (int i = 0; i < TabIds.Length; i++)
                CreateTab(tabContainerGO.transform, TabIds[i], TabLabels[i]);

            // Layer 5: Title Bar
            var titleBarGO = CreateCroppedLayer(panel.transform, "TitleBar", "Sprites/UI/Achievements/achv_title_bar");

            // Title bar shadow text
            var shadowGO = new GameObject("TitleBarShadow");
            shadowGO.transform.SetParent(titleBarGO.transform, false);
            var shadowRect = shadowGO.AddComponent<RectTransform>();
            var shadowMin = CropMetadata.ConvertAnchorToCropSpace(new Vector2(0.158f, 0.743f), "Sprites/UI/Achievements/achv_title_bar");
            var shadowMax = CropMetadata.ConvertAnchorToCropSpace(new Vector2(0.831f, 0.843f), "Sprites/UI/Achievements/achv_title_bar");
            shadowRect.anchorMin = shadowMin;
            shadowRect.anchorMax = shadowMax;
            shadowRect.offsetMin = new Vector2(3, -3);
            shadowRect.offsetMax = new Vector2(3, -3);
            titleBarShadow = shadowGO.AddComponent<TextMeshProUGUI>();
            titleBarShadow.text = "PLAYER STATS";
            titleBarShadow.fontSize = 48;
            titleBarShadow.fontStyle = FontStyles.Bold;
            titleBarShadow.alignment = TextAlignmentOptions.Center;
            titleBarShadow.color = new Color(0, 0, 0, 0.85f);
            titleBarShadow.enableAutoSizing = true;
            titleBarShadow.fontSizeMin = 28;
            titleBarShadow.fontSizeMax = 48;
            if (FontManager.ExtraBold != null)
                titleBarShadow.font = FontManager.ExtraBold;

            // Title bar main text
            var textGO = new GameObject("TitleBarText");
            textGO.transform.SetParent(titleBarGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            var textMin = CropMetadata.ConvertAnchorToCropSpace(new Vector2(0.158f, 0.743f), "Sprites/UI/Achievements/achv_title_bar");
            var textMax = CropMetadata.ConvertAnchorToCropSpace(new Vector2(0.831f, 0.843f), "Sprites/UI/Achievements/achv_title_bar");
            textRect.anchorMin = textMin;
            textRect.anchorMax = textMax;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            titleBarText = textGO.AddComponent<TextMeshProUGUI>();
            titleBarText.text = "PLAYER STATS";
            titleBarText.fontSize = 48;
            titleBarText.fontStyle = FontStyles.Bold;
            titleBarText.alignment = TextAlignmentOptions.Center;
            titleBarText.color = Color.white;
            titleBarText.enableAutoSizing = true;
            titleBarText.fontSizeMin = 28;
            titleBarText.fontSizeMax = 48;
            if (FontManager.ExtraBold != null)
                titleBarText.font = FontManager.ExtraBold;

            // Layer 6: Title Banner ("STATS" reuses achv_title or we show text)
            CreateCroppedLayer(panel.transform, "TitleBanner", "Sprites/UI/Achievements/achv_title");

            // Layer 7: Close Button
            CreateCloseButton(panel.transform);

            panel.SetActive(false);
            Debug.Log("[StatsScreen] Stats panel created");
        }

        public void Show()
        {
            if (panel == null) return;
            currentTab = TAB_GENERAL;
            UpdateTabVisuals();
            UpdateTitleBar();
            RefreshContent();
            panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel == null) return;
            AudioManager.Instance?.PlayButtonClick();
            panel.SetActive(false);
        }

        private void OnTabClicked(string tabId)
        {
            AudioManager.Instance?.PlayButtonClick();
            currentTab = tabId;
            UpdateTabVisuals();
            UpdateTitleBar();
            RefreshContent();
        }

        private void UpdateTabVisuals()
        {
            foreach (var kvp in tabImages)
            {
                bool selected = kvp.Key == currentTab;
                kvp.Value.sprite = selected ? tabPressedSprite : tabSprite;
            }
        }

        private void UpdateTitleBar()
        {
            string title = GetTabTitle(currentTab);
            if (titleBarText != null) titleBarText.text = title;
            if (titleBarShadow != null) titleBarShadow.text = title;
        }

        private static string GetTabTitle(string tab)
        {
            switch (tab)
            {
                case TAB_GENERAL: return "General";
                case TAB_MATCHING: return "Matching";
                case TAB_STARS: return "Stars";
                case TAB_MODES: return "Modes";
                case TAB_POWERUPS: return "Power-Ups";
                case TAB_WORLDS: return "Worlds";
                default: return "Stats";
            }
        }

        private void RefreshContent()
        {
            if (listContent == null) return;

            // Clear existing
            foreach (Transform child in listContent)
                UnityEngine.Object.Destroy(child.gameObject);

            switch (currentTab)
            {
                case TAB_GENERAL: PopulateGeneral(); break;
                case TAB_MATCHING: PopulateMatching(); break;
                case TAB_STARS: PopulateStars(); break;
                case TAB_MODES: PopulateModes(); break;
                case TAB_POWERUPS: PopulatePowerUps(); break;
                case TAB_WORLDS: PopulateWorlds(); break;
            }
        }

        #region Tab Content Builders

        private void PopulateGeneral()
        {
            var am = AchievementManager.Instance;
            if (am == null) return;

            AddStatRow("Daily Logins", FormatNumber(am.DailyLoginCount));
            AddStatRow("Total Playtime", FormatPlaytime(am.TotalPlaytimeSeconds));
            AddStatRow("Total Moves", FormatNumber(am.TotalMovesMade));

            // Derive total levels completed across all modes
            int totalCompleted = GetTotalLevelsCompleted();
            AddStatRow("Levels Completed", FormatNumber(totalCompleted));
            AddStatRow("Levels Failed", FormatNumber(am.TotalLevelsFailed));
            AddStatRow("Levels Restarted", FormatNumber(am.TotalLevelsRestarted));
        }

        private void PopulateMatching()
        {
            var am = AchievementManager.Instance;
            if (am == null) return;

            AddStatRow("Triples Made", FormatNumber(am.TotalMatchesMade));
            AddStatRow("Total Combos", FormatNumber(am.TotalCombos));
            AddStatRow("Best Combo Streak", FormatNumber(am.BestComboStreak));
            AddSeparator();
            AddStatRow("Good! (2x)", FormatNumber(am.TotalGoodCombos));
            AddStatRow("Amazing! (3x)", FormatNumber(am.TotalAmazingCombos));
            AddStatRow("Perfect! (4x+)", FormatNumber(am.TotalPerfectCombos));
        }

        private void PopulateStars()
        {
            var save = SaveManager.Instance;
            if (save == null) return;

            int totalStars = save.CurrentSave.totalStars;
            int threeStarLevels = GetThreeStarLevelCount();
            var am = AchievementManager.Instance;
            int bestStreak = am?.ThreeStarStreak ?? 0;

            AddStatRow("Total Stars", FormatNumber(totalStars));
            AddStatRow("3-Star Levels", FormatNumber(threeStarLevels));
            AddStatRow("Best 3-Star Streak", FormatNumber(bestStreak));
        }

        private void PopulateModes()
        {
            var save = SaveManager.Instance;
            var am = AchievementManager.Instance;
            if (save == null) return;

            // Per-mode level counts
            AddStatRow("FreePlay Levels Beaten", FormatNumber(GetModeLevelsCompleted(GameMode.FreePlay)));
            AddStatRow("Star Mode Levels Beaten", FormatNumber(GetModeLevelsCompleted(GameMode.StarMode)));
            AddStatRow("Timer Mode Levels Beaten", FormatNumber(GetModeLevelsCompleted(GameMode.TimerMode)));
            AddStatRow("Hard Mode Levels Beaten", FormatNumber(GetModeLevelsCompleted(GameMode.HardMode)));

            if (am != null)
            {
                AddSeparator();
                AddStatRow("Best Hard Mode Streak", FormatNumber(am.HardModeStreak));
                AddStatRow("Photo Finishes", FormatNumber(am.TotalPhotoFinishes));
                AddStatRow("Negative Time Clears", FormatNumber(am.TotalNegativeTimeCompletions));
            }
        }

        private void PopulatePowerUps()
        {
            var am = AchievementManager.Instance;
            if (am == null) return;

            AddStatRow("Total Used", FormatNumber(am.TotalPowerUpsUsed));
            AddSeparator();
            AddStatRow("Swaps Used", FormatNumber(am.PowerUpsUsedSwap));
            AddStatRow("Locks Destroyed", FormatNumber(am.PowerUpsUsedDestroyLocker));
            AddStatRow("Move Freezes", FormatNumber(am.PowerUpsUsedMoveFreeze));
            AddStatRow("Time Freezes", FormatNumber(am.PowerUpsUsedTimeFreeze));
        }

        private void PopulateWorlds()
        {
            var save = SaveManager.Instance;
            if (save == null) return;

            int unlockedCount = save.CurrentSave.unlockedWorlds?.Count ?? 1;
            int totalWorlds = AchievementManager.Worlds.Count;
            AddStatRow("Worlds Unlocked", $"{unlockedCount} / {totalWorlds}");

            AddSeparator();

            string[] worldNames = { "St. Games Island", "Superstore", "Wilty Acres", "Space Station", "The Oink & Anchor" };
            string[] worldIds = { "island", "supermarket", "farm", "space", "tavern" };

            for (int i = 0; i < worldIds.Length; i++)
            {
                int completed = GetWorldLevelsCompletedAllModes(worldIds[i]);
                int stars = GetWorldTotalStarsAllModes(worldIds[i]);
                AddStatRow(worldNames[i], $"{completed}/100  ({stars} stars)");
            }
        }

        #endregion

        #region Data Helpers

        private int GetTotalLevelsCompleted()
        {
            var save = SaveManager.Instance;
            if (save?.CurrentSave?.modeProgress == null) return 0;

            int total = 0;
            foreach (var mp in save.CurrentSave.modeProgress)
            {
                if (mp.worldProgress == null) continue;
                foreach (var wp in mp.worldProgress)
                {
                    if (wp.levelProgress == null) continue;
                    foreach (var lp in wp.levelProgress)
                    {
                        if (lp.isCompleted) total++;
                    }
                }
            }
            return total;
        }

        private int GetModeLevelsCompleted(GameMode mode)
        {
            var save = SaveManager.Instance;
            if (save?.CurrentSave?.modeProgress == null) return 0;

            foreach (var mp in save.CurrentSave.modeProgress)
            {
                if (mp.mode != mode || mp.worldProgress == null) continue;
                int count = 0;
                foreach (var wp in mp.worldProgress)
                {
                    if (wp.levelProgress == null) continue;
                    foreach (var lp in wp.levelProgress)
                    {
                        if (lp.isCompleted) count++;
                    }
                }
                return count;
            }
            return 0;
        }

        private int GetThreeStarLevelCount()
        {
            var save = SaveManager.Instance;
            if (save?.CurrentSave?.modeProgress == null) return 0;

            // Count unique levels that have 3 stars in any mode
            var threeStarred = new HashSet<string>();
            foreach (var mp in save.CurrentSave.modeProgress)
            {
                if (mp.worldProgress == null) continue;
                foreach (var wp in mp.worldProgress)
                {
                    if (wp.levelProgress == null) continue;
                    foreach (var lp in wp.levelProgress)
                    {
                        if (lp.starsEarned >= 3)
                            threeStarred.Add(lp.levelKey);
                    }
                }
            }
            return threeStarred.Count;
        }

        private int GetWorldTotalStarsAllModes(string worldId)
        {
            var save = SaveManager.Instance;
            if (save?.CurrentSave?.modeProgress == null) return 0;

            // Best stars per level across all modes
            var bestStars = new Dictionary<int, int>();
            foreach (var mp in save.CurrentSave.modeProgress)
            {
                if (mp.worldProgress == null) continue;
                foreach (var wp in mp.worldProgress)
                {
                    if (wp.worldId != worldId || wp.levelProgress == null) continue;
                    foreach (var lp in wp.levelProgress)
                    {
                        if (!bestStars.ContainsKey(lp.levelNumber) || lp.starsEarned > bestStars[lp.levelNumber])
                            bestStars[lp.levelNumber] = lp.starsEarned;
                    }
                }
            }
            int total = 0;
            foreach (var v in bestStars.Values) total += v;
            return total;
        }

        private int GetWorldLevelsCompletedAllModes(string worldId)
        {
            var save = SaveManager.Instance;
            if (save?.CurrentSave?.modeProgress == null) return 0;

            // Count unique levels completed across all modes for this world
            var completed = new HashSet<int>();
            foreach (var mp in save.CurrentSave.modeProgress)
            {
                if (mp.worldProgress == null) continue;
                foreach (var wp in mp.worldProgress)
                {
                    if (wp.worldId != worldId || wp.levelProgress == null) continue;
                    foreach (var lp in wp.levelProgress)
                    {
                        if (lp.isCompleted)
                            completed.Add(lp.levelNumber);
                    }
                }
            }
            return completed.Count;
        }

        #endregion

        #region UI Row Builders

        private void AddStatRow(string label, string value)
        {
            var rowGO = new GameObject("StatRow");
            rowGO.transform.SetParent(listContent, false);

            var layoutElem = rowGO.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 60;
            layoutElem.minHeight = 60;

            // Background
            var rowImg = rowGO.AddComponent<Image>();
            rowImg.color = new Color(0.95f, 0.88f, 0.7f, 0.6f);
            rowImg.raycastTarget = false;

            // Horizontal layout
            var hLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(20, 20, 5, 5);
            hLayout.spacing = 10;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;
            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 28;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.color = new Color(0.25f, 0.15f, 0.05f, 1f);
            labelTMP.enableAutoSizing = true;
            labelTMP.fontSizeMin = 18;
            labelTMP.fontSizeMax = 28;
            if (FontManager.Bold != null)
                labelTMP.font = FontManager.Bold;

            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(rowGO.transform, false);
            var valueLayout = valueGO.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 220;
            var valueTMP = valueGO.AddComponent<TextMeshProUGUI>();
            valueTMP.text = value;
            valueTMP.fontSize = 28;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.alignment = TextAlignmentOptions.MidlineRight;
            valueTMP.color = new Color(0.15f, 0.1f, 0.0f, 1f);
            valueTMP.enableAutoSizing = true;
            valueTMP.fontSizeMin = 18;
            valueTMP.fontSizeMax = 28;
            if (FontManager.ExtraBold != null)
                valueTMP.font = FontManager.ExtraBold;
        }

        private void AddSeparator()
        {
            // Outer spacer with fixed height to hold the thin line
            var spacerGO = new GameObject("Separator");
            spacerGO.transform.SetParent(listContent, false);
            var layoutElem = spacerGO.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 20;
            layoutElem.minHeight = 20;
            layoutElem.flexibleHeight = 0;

            // Thin line centered inside the spacer
            var lineGO = new GameObject("Line");
            lineGO.transform.SetParent(spacerGO.transform, false);
            var lineRect = lineGO.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.05f, 0.5f);
            lineRect.anchorMax = new Vector2(0.95f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(0, 3);
            var lineImg = lineGO.AddComponent<Image>();
            lineImg.color = new Color(0.5f, 0.35f, 0.15f, 0.5f);
            lineImg.raycastTarget = false;
        }

        #endregion

        #region Format Helpers

        private static string FormatNumber(int num)
        {
            return num.ToString("N0"); // adds commas: 1,234
        }

        private static string FormatPlaytime(float seconds)
        {
            if (seconds < 60) return $"{(int)seconds}s";
            if (seconds < 3600)
            {
                int m = (int)(seconds / 60);
                int s = (int)(seconds % 60);
                return $"{m}m {s}s";
            }
            int h = (int)(seconds / 3600);
            int min = (int)((seconds % 3600) / 60);
            return $"{h}h {min}m";
        }

        #endregion

        #region UI Creation Helpers

        private void CreateTab(Transform parent, string tabId, string label)
        {
            var tabGO = new GameObject($"Tab_{tabId}");
            tabGO.transform.SetParent(parent, false);

            var layoutElem = tabGO.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = 109;
            layoutElem.minHeight = 109;
            layoutElem.preferredWidth = 218;

            var tabImg = tabGO.AddComponent<Image>();
            tabImg.sprite = tabSprite;
            tabImg.preserveAspect = false;

            var tabBtn = tabGO.AddComponent<Button>();
            tabBtn.targetGraphic = tabImg;
            tabBtn.transition = Selectable.Transition.None;

            string captured = tabId;
            tabBtn.onClick.AddListener(() => OnTabClicked(captured));

            // Label text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(tabGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);
            textRect.anchoredPosition = new Vector2(-10, 0);

            var tabText = textGO.AddComponent<TextMeshProUGUI>();
            tabText.text = label;
            tabText.fontSize = 18;
            tabText.fontStyle = FontStyles.Bold;
            tabText.alignment = TextAlignmentOptions.Center;
            tabText.color = Color.white;
            tabText.enableAutoSizing = true;
            tabText.fontSizeMin = 9;
            tabText.fontSizeMax = 18;
            tabText.textWrappingMode = TextWrappingModes.Normal;
            tabText.overflowMode = TextOverflowModes.Truncate;
            if (FontManager.ExtraBold != null)
                tabText.font = FontManager.ExtraBold;

            tabButtons[tabId] = tabBtn;
            tabImages[tabId] = tabImg;
        }

        private static Image CreateFullscreenImage(Transform parent, string name, Sprite sprite, Color color, bool raycast)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        private static GameObject CreateCroppedLayer(Transform parent, string name, string resourcePath)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = LoadFullRectSprite(resourcePath);
            img.preserveAspect = true;
            img.raycastTarget = false;
            CropMetadata.ApplyCropAnchors(r, resourcePath);
            return go;
        }

        private void CreateCloseButton(Transform parent)
        {
            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(parent, false);
            var closeBtnRect = closeBtnGO.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.9019f, 0.8880f);
            closeBtnRect.anchorMax = new Vector2(0.9019f, 0.8880f);
            closeBtnRect.pivot = new Vector2(0.5f, 0.5f);
            closeBtnRect.sizeDelta = new Vector2(118, 118);

            var normalSprite = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_2");
            var pressedSprite = Resources.Load<Sprite>("Sprites/UI/Achievements/closebutton_pressed");
            var closeBtnImg = closeBtnGO.AddComponent<Image>();
            closeBtnImg.sprite = normalSprite;
            closeBtnImg.raycastTarget = true;

            var closeBtn = closeBtnGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnImg;
            closeBtn.transition = Selectable.Transition.None;
            closeBtn.onClick.AddListener(Hide);

            // Swap sprite on press
            var trigger = closeBtnGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { closeBtnImg.sprite = pressedSprite ?? normalSprite; });
            trigger.triggers.Add(pointerDown);
            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { closeBtnImg.sprite = normalSprite; });
            trigger.triggers.Add(pointerUp);
        }

        /// <summary>
        /// Load a sprite using full rect to bypass Unity's alpha-trim.
        /// </summary>
        private static Sprite LoadFullRectSprite(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        #endregion
    }
}
