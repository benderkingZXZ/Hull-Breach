using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace HullBreach.Game
{
    /// <summary>
    /// Создаёт TMP-шрифты с кириллицей во время запуска из TTF в Resources/Fonts (Liberation Sans, SIL OFL)
    /// и упаковывает их в UITheme для интерфейса.
    /// </summary>
    public static class FontFactory
    {
        public static UITheme CreateTheme()
        {
            UITheme theme = ScriptableObject.CreateInstance<UITheme>();
            TMP_FontAsset body = CreateFont("Fonts/HB_Body");
            TMP_FontAsset title = CreateFont("Fonts/HB_Title");

            if (body == null) body = title;
            if (title == null) title = body;
            theme.BodyFont = body;
            theme.TitleFont = title;
            theme.SpriteOverrides = new UITheme.SpriteOverride[0];

            RegisterFallback(body);
            return theme;
        }

        private static TMP_FontAsset CreateFont(string resourcePath)
        {
            Font ttf = Resources.Load<Font>(resourcePath);
            if (ttf == null)
            {
                Debug.LogWarning("[HullBreach] Шрифт Resources/" + resourcePath + " не найден: кириллица может не отображаться.");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
            {
                Debug.LogWarning("[HullBreach] Не удалось создать TMP-шрифт из " + resourcePath);
                return null;
            }
            asset.name = ttf.name + " SDF (runtime)";
            return asset;
        }

        /// <summary>Добавляет динамический шрифт в запасные шрифты TMP: тексты без явного шрифта тоже получат кириллицу.</summary>
        private static void RegisterFallback(TMP_FontAsset font)
        {
            if (font == null) return;
            if (TMP_Settings.instance == null || TMP_Settings.fallbackFontAssets == null) return;
            if (!TMP_Settings.fallbackFontAssets.Contains(font)) TMP_Settings.fallbackFontAssets.Add(font);
        }
    }
}
