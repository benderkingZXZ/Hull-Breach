using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Framework
{
    public sealed class BannerView : MonoBehaviour
    {
        private CanvasGroup group;
        private TMP_Text label;
        private Image edge;
        private float timer;
        private float duration;

        public RectTransform Rect { get; private set; }

        public static BannerView Create(Transform parent, string name, float width, float height, float fontSize)
        {
            RectTransform rt = UIFactory.Rect(name, parent);
            rt.sizeDelta = new Vector2(width, height);

            PanelView panel = UIFactory.Panel(rt, "Panel", UIColors.WithAlpha(UIColors.Hull, 0.9f), UIColors.WithAlpha(UIColors.Cyan, 0.8f));
            TMP_Text text = UIFactory.Label(rt, "Text", "", fontSize, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(text.rectTransform, 24f, 6f, 24f, 6f);
            UIFactory.FitOneLine(text, fontSize * 0.55f, fontSize);

            BannerView b = rt.gameObject.AddComponent<BannerView>();
            b.Rect = rt;
            b.label = text;
            b.edge = panel.Edge;
            b.group = rt.gameObject.AddComponent<CanvasGroup>();
            b.group.alpha = 0f;
            b.group.interactable = false;
            b.group.blocksRaycasts = false;
            b.timer = 999f;
            b.duration = 1f;
            return b;
        }

        public void Show(string text, Color accent, float seconds = 2f)
        {
            if (label == null) return;
            label.text = text;
            label.color = Color.Lerp(UIColors.Text, accent, 0.65f);
            edge.color = accent;
            duration = Mathf.Max(0.6f, seconds);
            timer = 0f;
        }

        public void HideNow()
        {
            timer = duration + 1f;
            if (group != null) group.alpha = 0f;
        }

        private void Update()
        {
            if (group == null || timer > duration) return;
            timer += Time.unscaledDeltaTime;
            const float fadeIn = 0.15f;
            const float fadeOut = 0.4f;
            float a = 1f;
            if (timer < fadeIn) a = timer / fadeIn;
            else if (timer > duration - fadeOut) a = Mathf.Clamp01((duration - timer) / fadeOut);
            group.alpha = a;
        }
    }
}
