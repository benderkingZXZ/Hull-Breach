using System;
using TMPro;
using UnityEngine;

namespace HullBreach.UI.Theme
{
    [CreateAssetMenu(menuName = "Hull Breach/UI Theme", fileName = "HullBreachUITheme")]
    public sealed class UITheme : ScriptableObject
    {
        [Serializable]
        public struct SpriteOverride
        {
            [Tooltip("Ключ, см. README: panel, panel_edge, bar, bar_edge, circle, ring, vignette, icon.shield, icon.ship_a ...")]
            public string Key;
            public Sprite Sprite;
        }

        [Header("Шрифты (TMP). Нужен шрифт с кириллицей!")]
        public TMP_FontAsset BodyFont;
        public TMP_FontAsset TitleFont;

        [Header("Подмена спрайтов по ключам")]
        public SpriteOverride[] SpriteOverrides;
    }
}
