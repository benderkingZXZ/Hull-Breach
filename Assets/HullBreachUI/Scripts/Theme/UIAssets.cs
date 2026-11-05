using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HullBreach.UI.Theme
{
    public static class UIAssets
    {
        private static UITheme theme;
        private static readonly Dictionary<string, Sprite> overrides = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            theme = null;
            overrides.Clear();
        }

        public static TMP_FontAsset BodyFont { get { return theme != null ? theme.BodyFont : null; } }

        public static TMP_FontAsset TitleFont
        {
            get { return (theme != null && theme.TitleFont != null) ? theme.TitleFont : BodyFont; }
        }

        public static void Initialize(UITheme newTheme)
        {
            theme = newTheme;
            overrides.Clear();
            if (theme == null || theme.SpriteOverrides == null) return;
            for (int i = 0; i < theme.SpriteOverrides.Length; i++)
            {
                UITheme.SpriteOverride o = theme.SpriteOverrides[i];
                if (!string.IsNullOrEmpty(o.Key) && o.Sprite != null) overrides[o.Key] = o.Sprite;
            }
        }

        public static Sprite GetOverride(string key)
        {
            Sprite s;
            return overrides.TryGetValue(key, out s) ? s : null;
        }
    }
}
