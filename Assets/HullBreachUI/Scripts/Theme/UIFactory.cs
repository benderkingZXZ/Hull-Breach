using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Theme
{
    public enum ButtonStyle { Primary, Secondary, Danger, Ghost }

    public sealed class PanelView
    {
        public RectTransform Rect;
        public Image Fill;
        public Image Edge;
    }

    public sealed class ButtonView
    {
        public RectTransform Rect;
        public Button Button;
        public TMP_Text Label;
        public Image Fill;
        public Image Edge;
        public Image Icon;
        public Color EdgeColor;

        public void SetLabel(string text) { Label.text = text; }

        public void SetInteractable(bool value)
        {
            Button.interactable = value;
            Label.alpha = value ? 1f : 0.5f;
            if (Edge != null) Edge.color = UIColors.WithAlpha(EdgeColor, value ? EdgeColor.a : 0.3f);
            if (Icon != null) Icon.color = UIColors.WithAlpha(Icon.color, value ? 1f : 0.5f);
        }

        public void SetVisible(bool value) { Rect.gameObject.SetActive(value); }
    }

    public sealed class BarView
    {
        public RectTransform Rect;
        public Image Back;
        public Image Fill;
        public RectTransform FillRect;

        public void Set(float value)
        {
            value = Mathf.Clamp01(value);
            FillRect.anchorMax = new Vector2(value, 1f);
            Fill.enabled = value > 0.001f;
        }
    }

    public sealed class CardView
    {
        public RectTransform Rect;
        public Button Button;
        public Image Fill;
        public Image Edge;
        public Image Glow;
        public RectTransform Content;
        public CardVisual Visual;

        public void SetSelected(bool value) { Visual.SetSelected(value); }
    }

    /// <summary>Ряд маленьких плашек: уровень прокачки, сложность, прогресс по комнатам.</summary>
    public sealed class PipsView
    {
        public RectTransform Rect;
        private readonly List<Image> pips = new List<Image>();
        private readonly float pipWidth;
        private readonly float pipHeight;

        public PipsView(RectTransform rect, float pipWidth, float pipHeight)
        {
            Rect = rect;
            this.pipWidth = pipWidth;
            this.pipHeight = pipHeight;
        }

        public void Set(int filled, int total, Color on, Color off)
        {
            while (pips.Count < total) pips.Add(CreatePip());
            for (int i = 0; i < pips.Count; i++)
            {
                bool active = i < total;
                pips[i].gameObject.SetActive(active);
                if (active) pips[i].color = i < filled ? on : off;
            }
        }

        private Image CreatePip()
        {
            RectTransform r = UIFactory.Rect("Pip", Rect);
            r.sizeDelta = new Vector2(pipWidth, pipHeight);
            UIFactory.AddLayout(r, pipWidth, pipWidth, 0f, pipHeight, pipHeight, 0f);
            return UIFactory.AddImage(r.gameObject, ProceduralSprites.Bar, Color.white, Image.Type.Sliced);
        }

        /// <summary>Перекрасить одну плашку (например, текущую комнату).</summary>
        public void Highlight(int index, Color color)
        {
            if (index >= 0 && index < pips.Count) pips[index].color = color;
        }
    }

    public sealed class WalletView
    {
        public RectTransform Rect;
        public NumberTicker Ticker;
        public void SetValue(int value, bool instant) { Ticker.Set(value, instant); }
    }

    public sealed class HeaderView
    {
        public RectTransform Rect;
        public TMP_Text Title;
        public TMP_Text Subtitle;
        public ButtonView Back;
        public WalletView Wallet;
    }

    /// <summary>Фабрика элементов Unity UI, собираемых кодом (без префабов и сцен).</summary>
    public static class UIFactory
    {
        // ------------------------------------------------------------------ RectTransform

        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Якорь в одной точке + пивот + позиция + размер.</summary>
        public static RectTransform Anchor(RectTransform rt, float ax, float ay, float px, float py, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(px, py);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Якоря-диапазон (доли родителя) + отступы в единицах холста.</summary>
        public static RectTransform AnchorRange(RectTransform rt, float minX, float minY, float maxX, float maxY,
            float left, float bottom, float right, float top)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Растянут по ширине (доли родителя), по высоте — одна точка якоря.</summary>
        public static RectTransform AnchorStretchX(RectTransform rt, float minX, float maxX, float ay, float py, float y, float h)
        {
            rt.anchorMin = new Vector2(minX, ay);
            rt.anchorMax = new Vector2(maxX, ay);
            rt.pivot = new Vector2(0.5f, py);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, h);
            return rt;
        }

        public static LayoutElement AddLayout(RectTransform rt, float minW = -1f, float prefW = -1f, float flexW = -1f,
            float minH = -1f, float prefH = -1f, float flexH = -1f)
        {
            LayoutElement e = rt.gameObject.GetComponent<LayoutElement>();
            if (e == null) e = rt.gameObject.AddComponent<LayoutElement>();
            e.minWidth = minW; e.preferredWidth = prefW; e.flexibleWidth = flexW;
            e.minHeight = minH; e.preferredHeight = prefH; e.flexibleHeight = flexH;
            return e;
        }

        public static HorizontalLayoutGroup HorizontalGroup(RectTransform rt, float spacing, TextAnchor align)
        {
            HorizontalLayoutGroup g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = false;
            return g;
        }

        public static VerticalLayoutGroup VerticalGroup(RectTransform rt, float spacing, TextAnchor align)
        {
            VerticalLayoutGroup g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        // ------------------------------------------------------------------ Изображения

        public static Image AddImage(GameObject go, Sprite sprite, Color color, Image.Type type = Image.Type.Simple, bool raycast = false)
        {
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.raycastTarget = raycast;
            return img;
        }

        /// <summary>Дочерний объект на всю область родителя с Image.</summary>
        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
        {
            RectTransform rt = Rect(name, parent);
            Stretch(rt);
            return AddImage(rt.gameObject, sprite, color, type);
        }

        public static PanelView Panel(Transform parent, string name, Color fill, Color edge, bool withEdge = true)
        {
            PanelView p = new PanelView();
            p.Rect = Rect(name, parent);
            Stretch(p.Rect);
            p.Fill = AddImage(p.Rect.gameObject, ProceduralSprites.Panel, fill, Image.Type.Sliced);
            if (withEdge) p.Edge = Img(p.Rect, "Edge", ProceduralSprites.PanelEdge, edge, Image.Type.Sliced);
            return p;
        }

        // ------------------------------------------------------------------ Текст

        public static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center, bool title = false)
        {
            RectTransform rt = Rect(name, parent);
            Stretch(rt);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = title ? UIAssets.TitleFont : UIAssets.BodyFont;
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        /// <summary>Текст сжимается, а не переносится, если не помещается.</summary>
        public static TMP_Text FitOneLine(TMP_Text t, float minSize, float maxSize)
        {
            t.enableWordWrapping = false;
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = maxSize;
            return t;
        }

        // ------------------------------------------------------------------ Кнопки

        public static ButtonView CreateButton(Transform parent, string name, string label, ButtonStyle style,
            Action onClick, float fontSize = 34f, string iconKey = null)
        {
            RectTransform rt = Rect(name, parent);
            Color fill, edge, text;
            switch (style)
            {
                case ButtonStyle.Primary:
                    fill = UIColors.Cyan; edge = UIColors.WithAlpha(Color.white, 0.55f); text = UIColors.TextOnAccent; break;
                case ButtonStyle.Danger:
                    fill = UIColors.RedDark; edge = UIColors.Red; text = UIColors.Text; break;
                case ButtonStyle.Ghost:
                    fill = UIColors.WithAlpha(UIColors.Hull, 0.35f); edge = UIColors.WithAlpha(UIColors.Steel, 0.9f); text = UIColors.TextDim; break;
                default:
                    fill = UIColors.WithAlpha(UIColors.HullLight, 0.96f); edge = UIColors.CyanDim; text = UIColors.Text; break;
            }

            ButtonView v = new ButtonView();
            v.Rect = rt;
            v.EdgeColor = edge;
            v.Fill = AddImage(rt.gameObject, ProceduralSprites.Panel, fill, Image.Type.Sliced, true);
            v.Edge = Img(rt, "Edge", ProceduralSprites.PanelEdge, edge, Image.Type.Sliced);

            float labelLeft = 18f;
            if (!string.IsNullOrEmpty(iconKey))
            {
                v.Icon = Img(rt, "Icon", UIIcons.Get(iconKey), text, Image.Type.Simple);
                v.Icon.preserveAspect = true;
                Anchor(v.Icon.rectTransform, 0f, 0.5f, 0f, 0.5f, 22f, 0f, fontSize * 1.1f, fontSize * 1.1f);
                labelLeft = 22f + fontSize * 1.1f + 8f;
            }

            v.Label = Label(rt, "Label", label, fontSize, text, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(v.Label.rectTransform, labelLeft, 6f, 18f, 6f);
            FitOneLine(v.Label, fontSize * 0.5f, fontSize);

            v.Button = rt.gameObject.AddComponent<Button>();
            v.Button.targetGraphic = v.Fill;
            v.Button.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = v.Button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            v.Button.colors = cb;
            Navigation nav = v.Button.navigation;
            nav.mode = Navigation.Mode.None;
            v.Button.navigation = nav;
            if (onClick != null) v.Button.onClick.AddListener(() => onClick());

            rt.gameObject.AddComponent<PressScale>();
            return v;
        }

        // ------------------------------------------------------------------ Полосы, слайдеры, карточки

        public static BarView CreateBar(Transform parent, string name, Color fill, Color back)
        {
            BarView b = new BarView();
            b.Rect = Rect(name, parent);
            b.Back = AddImage(b.Rect.gameObject, ProceduralSprites.Bar, back, Image.Type.Sliced);
            b.FillRect = Rect("Fill", b.Rect);
            b.FillRect.anchorMin = Vector2.zero;
            b.FillRect.anchorMax = Vector2.one;
            b.FillRect.offsetMin = Vector2.zero;
            b.FillRect.offsetMax = Vector2.zero;
            b.Fill = AddImage(b.FillRect.gameObject, ProceduralSprites.Bar, fill, Image.Type.Sliced);
            return b;
        }

        /// <summary>Строка «название — полоса — значение» для карточек точки входа.</summary>
        public static BarView CreateStatRow(Transform parent, float yTop, string label, float fill, Color color, string valueText)
        {
            RectTransform row = Rect("Stat_" + label, parent);
            AnchorStretchX(row, 0.06f, 0.94f, 1f, 1f, -yTop, 34f);

            TMP_Text l = Label(row, "Label", label, 24f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.Left);
            AnchorRange(l.rectTransform, 0f, 0f, 0.42f, 1f, 0f, 0f, 0f, 0f);
            FitOneLine(l, 16f, 24f);

            BarView bar = CreateBar(row, "Bar", color, UIColors.WithAlpha(Color.black, 0.5f));
            bar.Rect.anchorMin = new Vector2(0.44f, 0.5f);
            bar.Rect.anchorMax = new Vector2(0.80f, 0.5f);
            bar.Rect.offsetMin = new Vector2(0f, -7f);
            bar.Rect.offsetMax = new Vector2(0f, 7f);
            bar.Set(fill);

            TMP_Text v = Label(row, "Value", valueText, 26f, color, FontStyles.Bold, TextAlignmentOptions.Right);
            AnchorRange(v.rectTransform, 0.82f, 0f, 1f, 1f, 0f, 0f, 0f, 0f);
            FitOneLine(v, 16f, 26f);
            return bar;
        }

        public static Slider CreateSlider(Transform parent, string name, float min, float max, float value, Action<float> onChanged)
        {
            RectTransform root = Rect(name, parent);
            Slider slider = root.gameObject.AddComponent<Slider>();

            RectTransform bg = Rect("Background", root);
            bg.anchorMin = new Vector2(0f, 0.5f);
            bg.anchorMax = new Vector2(1f, 0.5f);
            bg.offsetMin = new Vector2(0f, -8f);
            bg.offsetMax = new Vector2(0f, 8f);
            AddImage(bg.gameObject, ProceduralSprites.Bar, UIColors.WithAlpha(Color.black, 0.55f), Image.Type.Sliced, true);

            RectTransform fillArea = Rect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(22f, -8f);
            fillArea.offsetMax = new Vector2(-22f, 8f);
            RectTransform fill = Rect("Fill", fillArea);
            Stretch(fill);
            AddImage(fill.gameObject, ProceduralSprites.Bar, UIColors.Cyan, Image.Type.Sliced);

            RectTransform handleArea = Rect("Handle Slide Area", root);
            Stretch(handleArea, 22f, 0f, 22f, 0f);
            RectTransform handle = Rect("Handle", handleArea);
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(0f, 0.5f);
            handle.sizeDelta = new Vector2(44f, 44f);
            Image handleImg = AddImage(handle.gameObject, ProceduralSprites.Circle, UIColors.Text, Image.Type.Simple, true);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.value = value;
            slider.onValueChanged.AddListener(v => { if (onChanged != null) onChanged(v); });
            return slider;
        }

        public static CardView CreateCard(Transform parent, string name, Color accent)
        {
            CardView c = new CardView();
            c.Rect = Rect(name, parent);

            c.Glow = Img(c.Rect, "Glow", ProceduralSprites.Panel, UIColors.WithAlpha(accent, 0f), Image.Type.Sliced);
            Stretch(c.Glow.rectTransform, -14f, -14f, -14f, -14f);

            c.Fill = Img(c.Rect, "Fill", ProceduralSprites.Panel, UIColors.WithAlpha(UIColors.Hull, 0.96f), Image.Type.Sliced);
            c.Fill.raycastTarget = true;
            c.Edge = Img(c.Rect, "Edge", ProceduralSprites.PanelEdge, UIColors.WithAlpha(accent, 0.55f), Image.Type.Sliced);

            c.Content = Rect("Content", c.Rect);
            Stretch(c.Content);

            c.Button = c.Rect.gameObject.AddComponent<Button>();
            c.Button.targetGraphic = c.Fill;
            c.Button.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = c.Button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.05f;
            c.Button.colors = cb;
            Navigation nav = c.Button.navigation;
            nav.mode = Navigation.Mode.None;
            c.Button.navigation = nav;

            c.Visual = c.Rect.gameObject.AddComponent<CardVisual>();
            c.Visual.Edge = c.Edge;
            c.Visual.Glow = c.Glow;
            c.Visual.Accent = accent;
            return c;
        }

        // ------------------------------------------------------------------ Кошелёк и шапка экрана

        public static WalletView CreateWallet(Transform parent, string name)
        {
            WalletView w = new WalletView();
            w.Rect = Rect(name, parent);

            Image icon = Img(w.Rect, "Icon", UIIcons.Get("hex"), UIColors.Amber);
            icon.preserveAspect = true;
            Anchor(icon.rectTransform, 0f, 0.5f, 0f, 0.5f, 0f, 0f, 46f, 46f);

            TMP_Text label = Label(w.Rect, "Value", "0", 44f, UIColors.Amber, FontStyles.Bold, TextAlignmentOptions.Left);
            AnchorRange(label.rectTransform, 0f, 0f, 1f, 1f, 58f, 0f, 0f, 0f);
            FitOneLine(label, 24f, 44f);

            w.Ticker = w.Rect.gameObject.AddComponent<NumberTicker>();
            w.Ticker.Label = label;
            return w;
        }

        public static HeaderView CreateHeader(Transform parent, string title, string subtitle,
            string backLabel, Action onBack, bool withWallet)
        {
            HeaderView h = new HeaderView();
            h.Rect = Rect("Header", parent);
            AnchorRange(h.Rect, 0f, 1f, 1f, 1f, 0f, -130f, 0f, 0f);

            float left = 0f;
            if (!string.IsNullOrEmpty(backLabel))
            {
                h.Back = CreateButton(h.Rect, "BackButton", backLabel, ButtonStyle.Secondary, onBack, 30f, "back");
                Anchor(h.Back.Rect, 0f, 1f, 0f, 1f, 0f, -6f, 250f, 78f);
                left = 280f;
            }

            float right = withWallet ? 330f : 0f;
            h.Title = Label(h.Rect, "Title", title, 54f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.TopLeft, true);
            AnchorStretchX(h.Title.rectTransform, 0f, 1f, 1f, 1f, 0f, 66f);
            h.Title.rectTransform.offsetMin = new Vector2(left, h.Title.rectTransform.offsetMin.y);
            h.Title.rectTransform.offsetMax = new Vector2(-right, h.Title.rectTransform.offsetMax.y);
            FitOneLine(h.Title, 30f, 54f);

            h.Subtitle = Label(h.Rect, "Subtitle", subtitle, 28f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            AnchorStretchX(h.Subtitle.rectTransform, 0f, 1f, 1f, 1f, -66f, 40f);
            h.Subtitle.rectTransform.offsetMin = new Vector2(left, h.Subtitle.rectTransform.offsetMin.y);
            h.Subtitle.rectTransform.offsetMax = new Vector2(-right, h.Subtitle.rectTransform.offsetMax.y);
            FitOneLine(h.Subtitle, 18f, 28f);

            if (withWallet)
            {
                h.Wallet = CreateWallet(h.Rect, "Wallet");
                Anchor(h.Wallet.Rect, 1f, 1f, 1f, 1f, 0f, -10f, 300f, 60f);
            }
            return h;
        }
    }
}
