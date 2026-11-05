using System;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    public sealed class SettingsPanelUI : UIModal
    {
        private Slider volume;
        private Slider scale;
        private Slider opacity;
        private ButtonView vibration;
        private ButtonView damageNumbers;
        private ButtonView stickMode;
        private ButtonView resetButton;

        protected override void BuildContent(RectTransform content)
        {
            RectTransform box = UIFactory.Rect("Box", content);
            UIFactory.Anchor(box, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 1120f, 900f);
            UIFactory.Panel(box, "Panel", UIColors.WithAlpha(UIColors.Hull, 0.98f), UIColors.CyanDim);
            box.gameObject.AddComponent<AppearAnimation>();

            TMP_Text title = UIFactory.Label(box, "Title", "НАСТРОЙКИ", 60f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UIFactory.AnchorStretchX(title.rectTransform, 0.05f, 0.95f, 1f, 1f, -30f, 80f);

            RectTransform list = UIFactory.Rect("Rows", box);
            UIFactory.AnchorRange(list, 0.06f, 0.16f, 0.94f, 0.88f, 0f, 0f, 0f, 0f);
            UIFactory.VerticalGroup(list, 8f, TextAnchor.UpperCenter);

            volume = AddSliderRow(list, "Громкость", 0f, 1f, UISettings.SetMasterVolume);
            vibration = AddToggleRow(list, "Вибрация при уроне", () => UISettings.SetVibration(!UISettings.Vibration));
            damageNumbers = AddToggleRow(list, "Числа урона", () => UISettings.SetDamageNumbers(!UISettings.DamageNumbers));
            stickMode = AddToggleRow(list, "Стик", () => UISettings.SetFloatingJoystick(!UISettings.FloatingJoystick));
            scale = AddSliderRow(list, "Размер кнопок", 0.8f, 1.3f, UISettings.SetControlScale);
            opacity = AddSliderRow(list, "Прозрачность кнопок", 0.4f, 1f, UISettings.SetControlOpacity);

            resetButton = UIFactory.CreateButton(list, "ResetProgress", "СБРОСИТЬ ПРОГРЕСС", ButtonStyle.Danger, AskReset, 28f);
            UIFactory.AddLayout(resetButton.Rect, -1f, -1f, -1f, -1f, 84f, -1f);

            ButtonView close = UIFactory.CreateButton(box, "Close", "ГОТОВО", ButtonStyle.Primary, Close, 36f);
            UIFactory.Anchor(close.Rect, 0.5f, 0f, 0.5f, 0f, 0f, 30f, 460f, 100f);
        }

        private Slider AddSliderRow(RectTransform parent, string label, float min, float max, Action<float> setter)
        {
            RectTransform row = UIFactory.Rect("Row_" + label, parent);
            UIFactory.AddLayout(row, -1f, -1f, -1f, -1f, 92f, -1f);

            TMP_Text l = UIFactory.Label(row, "Label", label, 32f, UIColors.Text, FontStyles.Normal, TextAlignmentOptions.Left);
            UIFactory.AnchorRange(l.rectTransform, 0f, 0f, 0.42f, 1f, 0f, 0f, 0f, 0f);
            UIFactory.FitOneLine(l, 20f, 32f);

            Slider s = UIFactory.CreateSlider(row, "Slider", min, max, min, setter);
            UIFactory.AnchorRange((RectTransform)s.transform, 0.45f, 0.15f, 1f, 0.85f, 0f, 0f, 0f, 0f);
            return s;
        }

        private ButtonView AddToggleRow(RectTransform parent, string label, Action toggle)
        {
            RectTransform row = UIFactory.Rect("Row_" + label, parent);
            UIFactory.AddLayout(row, -1f, -1f, -1f, -1f, 92f, -1f);

            TMP_Text l = UIFactory.Label(row, "Label", label, 32f, UIColors.Text, FontStyles.Normal, TextAlignmentOptions.Left);
            UIFactory.AnchorRange(l.rectTransform, 0f, 0f, 0.42f, 1f, 0f, 0f, 0f, 0f);
            UIFactory.FitOneLine(l, 20f, 32f);

            ButtonView b = null;
            b = UIFactory.CreateButton(row, "Toggle", "", ButtonStyle.Secondary, () => { toggle(); Refresh(); }, 30f);
            UIFactory.AnchorRange(b.Rect, 0.45f, 0.08f, 1f, 0.92f, 0f, 0f, 0f, 0f);
            return b;
        }

        public void Present(bool allowProgressReset)
        {
            resetButton.SetVisible(allowProgressReset);
            Refresh();
            Open();
        }

        private void Refresh()
        {
            volume.SetValueWithoutNotify(UISettings.MasterVolume);
            scale.SetValueWithoutNotify(UISettings.ControlScale);
            opacity.SetValueWithoutNotify(UISettings.ControlOpacity);
            SetToggle(vibration, UISettings.Vibration, "ВКЛ", "ВЫКЛ");
            SetToggle(damageNumbers, UISettings.DamageNumbers, "ВКЛ", "ВЫКЛ");
            SetToggle(stickMode, UISettings.FloatingJoystick, "ПЛАВАЮЩИЙ", "ФИКСИРОВАННЫЙ");
        }

        private static void SetToggle(ButtonView b, bool on, string onText, string offText)
        {
            b.SetLabel(on ? onText : offText);
            b.EdgeColor = on ? UIColors.Green : UIColors.Steel;
            b.Edge.color = b.EdgeColor;
            b.Label.color = on ? UIColors.Green : UIColors.TextDim;
        }

        private void AskReset()
        {
            Manager.Confirm("Сбросить прогресс?",
                "Все ресурсы и улучшения робота будут удалены без возможности восстановления.",
                "СБРОСИТЬ", () =>
                {
                    Services.Progression.ResetAllProgress();
                    Manager.Toast("Прогресс сброшен", true);
                }, true);
        }

        protected override void OnClosed()
        {
            UISettings.Flush();
        }
    }
}
