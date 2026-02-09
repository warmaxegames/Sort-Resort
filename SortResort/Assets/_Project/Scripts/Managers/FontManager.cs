using UnityEngine;
using TMPro;

namespace SortResort
{
    /// <summary>
    /// Static utility for accessing Benzin TMP font assets at runtime.
    /// Fonts are lazy-loaded from Resources/Fonts/ and cached.
    /// Primary font: Benzin-Bold (also set as TMP default).
    /// </summary>
    public static class FontManager
    {
        private static TMP_FontAsset _bold;
        private static TMP_FontAsset _semiBold;
        private static TMP_FontAsset _medium;
        private static TMP_FontAsset _extraBold;

        public static TMP_FontAsset Bold =>
            _bold ?? (_bold = LoadFont("Fonts/BENZIN-BOLD SDF"));

        public static TMP_FontAsset SemiBold =>
            _semiBold ?? (_semiBold = LoadFont("Fonts/BENZIN-SEMIBOLD SDF"));

        public static TMP_FontAsset Medium =>
            _medium ?? (_medium = LoadFont("Fonts/BENZIN-MEDIUM SDF"));

        public static TMP_FontAsset ExtraBold =>
            _extraBold ?? (_extraBold = LoadFont("Fonts/BENZIN-EXTRABOLD SDF"));

        private static TMP_FontAsset LoadFont(string resourcePath)
        {
            var font = Resources.Load<TMP_FontAsset>(resourcePath);
            if (font == null)
            {
                Debug.LogWarning($"[FontManager] Font not found at Resources/{resourcePath}. " +
                    "Run 'Tools > Sort Resort > Fonts > Generate Benzin SDF Fonts' in the Editor.");
            }
            return font;
        }

        /// <summary>
        /// Applies Benzin-Bold to a TMP_Text component (works for both UGUI and world-space).
        /// </summary>
        public static void ApplyBold(TMP_Text text)
        {
            if (text != null && Bold != null)
                text.font = Bold;
        }
    }
}
