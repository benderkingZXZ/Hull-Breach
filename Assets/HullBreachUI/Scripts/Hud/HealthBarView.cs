using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Hud
{
    public sealed class HealthBarView : MonoBehaviour
    {
        private RectTransform rect;
        private BarView ghost;
        private BarView main;
        private BarView shield;
        private TMP_Text text;
        private Image flash;
        private Image edge;

        private float current = 1f;
        private float max = 1f;
        private float shieldAmount;
        private float shownRatio = 1f;
        private float ghostRatio = 1f;
        private float ghostDelay;
        private float flashT;
        private float shakeT;
        private int lastCur = int.MinValue;
        private int lastMax = int.MinValue;
        private Vector2 basePosition;
        private bool basePositionKnown;

        public RectTransform Rect { get { return rect; } }

        public static HealthBarView Create(Transform parent, float width, float height)
        {
            RectTransform rt = UIFactory.Rect("HealthBar", parent);
            rt.sizeDelta = new Vector2(width, height);

            PanelView back = UIFactory.Panel(rt, "Back", UIColors.WithAlpha(UIColors.Hull, 0.85f), UIColors.WithAlpha(UIColors.Steel, 0.9f));

            RectTransform inner = UIFactory.Rect("Inner", rt);
            UIFactory.Stretch(inner, height + 4f, 8f, 10f, 8f);

            HealthBarView v = rt.gameObject.AddComponent<HealthBarView>();
            v.rect = rt;
            v.edge = back.Edge;

            v.ghost = UIFactory.CreateBar(inner, "Ghost", UIColors.WithAlpha(Color.white, 0.55f), UIColors.WithAlpha(Color.black, 0.5f));
            UIFactory.Stretch(v.ghost.Rect, 0f, 0f, 0f, 0f);
            v.main = UIFactory.CreateBar(inner, "Main", UIColors.Green, Color.clear);
            UIFactory.Stretch(v.main.Rect, 0f, 0f, 0f, 0f);
            v.shield = UIFactory.CreateBar(inner, "Shield", UIColors.Cyan, Color.clear);
            v.shield.Rect.anchorMin = new Vector2(0f, 0f);
            v.shield.Rect.anchorMax = new Vector2(1f, 0f);
            v.shield.Rect.pivot = new Vector2(0.5f, 0f);
            v.shield.Rect.anchoredPosition = Vector2.zero;
            v.shield.Rect.sizeDelta = new Vector2(0f, 8f);
            v.shield.Back.enabled = false;
            v.shield.Set(0f);

            Image icon = UIFactory.Img(rt, "Icon", UIIcons.Get("plus"), UIColors.Green);
            icon.preserveAspect = true;
            UIFactory.Anchor(icon.rectTransform, 0f, 0.5f, 0f, 0.5f, 14f, 0f, height - 22f, height - 22f);

            v.text = UIFactory.Label(inner, "Text", "", height * 0.5f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.Stretch(v.text.rectTransform);
            v.text.enableWordWrapping = false;

            v.flash = UIFactory.Img(rt, "Flash", ProceduralSprites.Panel, UIColors.WithAlpha(Color.white, 0f), Image.Type.Sliced);
            return v;
        }

        public void SetValues(float currentHealth, float maxHealth)
        {
            max = Mathf.Max(1f, maxHealth);
            current = Mathf.Clamp(currentHealth, 0f, max);
            int c = Mathf.CeilToInt(current);
            int m = Mathf.RoundToInt(max);
            if (c != lastCur || m != lastMax)
            {
                lastCur = c;
                lastMax = m;
                text.text = c + " / " + m;
            }
        }

        public void SetShield(float amount)
        {
            shieldAmount = Mathf.Max(0f, amount);
        }

        public void Snap()
        {
            shownRatio = current / max;
            ghostRatio = shownRatio;
            ghostDelay = 0f;
        }

        public void Pulse(bool damage)
        {
            flashT = 1f;
            flash.color = UIColors.WithAlpha(damage ? UIColors.Red : UIColors.Green, 0f);
            if (damage)
            {
                shakeT = 0.22f;
                ghostDelay = 0.45f;
            }
        }

        private void Update()
        {
            if (main == null) return;
            float dt = Time.unscaledDeltaTime;
            float target = current / max;

            shownRatio = Mathf.Lerp(shownRatio, target, 1f - Mathf.Exp(-dt * 16f));
            if (Mathf.Abs(shownRatio - target) < 0.001f) shownRatio = target;

            if (ghostRatio < shownRatio) ghostRatio = shownRatio;
            else if (ghostDelay > 0f) ghostDelay -= dt;
            else ghostRatio = Mathf.MoveTowards(ghostRatio, shownRatio, dt * 0.6f);

            main.Set(shownRatio);
            main.Fill.color = UIColors.HealthColor(shownRatio);
            ghost.Set(ghostRatio);
            shield.Set(Mathf.Clamp01(shieldAmount / max));
            shield.Rect.gameObject.SetActive(shieldAmount > 0.01f);

            if (flashT > 0f)
            {
                flashT = Mathf.MoveTowards(flashT, 0f, dt * 3.5f);
                Color c = flash.color;
                c.a = flashT * 0.5f;
                flash.color = c;
            }

            if (!basePositionKnown)
            {
                basePosition = rect.anchoredPosition;
                basePositionKnown = true;
            }
            if (shakeT > 0f)
            {
                shakeT = Mathf.MoveTowards(shakeT, 0f, dt);
                float k = shakeT / 0.22f;
                rect.anchoredPosition = basePosition + new Vector2(Mathf.Sin(shakeT * 90f) * 7f * k, 0f);
            }
            else if (basePositionKnown)
            {
                rect.anchoredPosition = basePosition;
            }

            bool critical = target < 0.25f && current > 0f;
            edge.color = critical
                ? Color.Lerp(UIColors.Steel, UIColors.Red, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f))
                : UIColors.WithAlpha(UIColors.Steel, 0.9f);
        }
    }
}
