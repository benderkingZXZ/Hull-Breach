using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    /// <summary>Мелкие повторяющиеся детали карточек: плашки-теги и разделители.</summary>
    public static class CardParts
    {
        /// <summary>Закрашенная плашка с текстом (риск, редкость, «ВЫБРАН»). Позицию задаёт вызывающий код.</summary>
        public static RectTransform Pill(Transform parent, string name, string text, Color color, float fontSize)
        {
            RectTransform rt = UIFactory.Rect(name, parent);
            UIFactory.AddImage(rt.gameObject, ProceduralSprites.Bar, color, Image.Type.Sliced);
            TMP_Text label = UIFactory.Label(rt, "Text", text, fontSize, UIColors.TextOnAccent, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(label.rectTransform, 10f, 2f, 10f, 2f);
            UIFactory.FitOneLine(label, fontSize * 0.5f, fontSize);
            label.characterSpacing = 4f;
            return rt;
        }

        public static void SetPillText(RectTransform pill, string text)
        {
            TMP_Text label = pill.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
        }

        public static void Divider(Transform parent, float yTop)
        {
            RectTransform rt = UIFactory.Rect("Divider", parent);
            UIFactory.AnchorStretchX(rt, 0.06f, 0.94f, 1f, 1f, -yTop, 2f);
            UIFactory.AddImage(rt.gameObject, ProceduralSprites.White, UIColors.WithAlpha(UIColors.Steel, 0.6f));
        }

        /// <summary>Подпись слева и значение справа в одной строке карточки.</summary>
        public static RectTransform Row(Transform parent, string label, float yTop, float height)
        {
            RectTransform row = UIFactory.Rect("Row_" + label, parent);
            UIFactory.AnchorStretchX(row, 0.06f, 0.94f, 1f, 1f, -yTop, height);
            TMP_Text l = UIFactory.Label(row, "Label", label, 26f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.Left);
            UIFactory.AnchorRange(l.rectTransform, 0f, 0f, 0.5f, 1f, 0f, 0f, 0f, 0f);
            UIFactory.FitOneLine(l, 16f, 26f);
            return row;
        }
    }
}
