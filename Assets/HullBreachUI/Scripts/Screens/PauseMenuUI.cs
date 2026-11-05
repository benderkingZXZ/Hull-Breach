using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;

namespace HullBreach.UI.Screens
{
    public sealed class PauseMenuUI : UIModal
    {
        protected override void BuildContent(RectTransform content)
        {
            RectTransform box = UIFactory.Rect("Box", content);
            UIFactory.Anchor(box, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 760f, 640f);
            UIFactory.Panel(box, "Panel", UIColors.WithAlpha(UIColors.Hull, 0.98f), UIColors.CyanDim);
            box.gameObject.AddComponent<AppearAnimation>();

            TMP_Text title = UIFactory.Label(box, "Title", "ПАУЗА", 64f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UIFactory.AnchorStretchX(title.rectTransform, 0.05f, 0.95f, 1f, 1f, -36f, 84f);

            RectTransform column = UIFactory.Rect("Buttons", box);
            UIFactory.AnchorRange(column, 0.08f, 0.06f, 0.92f, 0.74f, 0f, 0f, 0f, 0f);
            UIFactory.VerticalGroup(column, 22f, TextAnchor.MiddleCenter);

            ButtonView resume = UIFactory.CreateButton(column, "Resume", UIStrings.Resume, ButtonStyle.Primary, Close, 38f, "play");
            UIFactory.AddLayout(resume.Rect, -1f, -1f, -1f, -1f, 120f, -1f);

            ButtonView settings = UIFactory.CreateButton(column, "Settings", UIStrings.Settings, ButtonStyle.Secondary,
                () => Manager.OpenSettings(false), 34f, "gear");
            UIFactory.AddLayout(settings.Rect, -1f, -1f, -1f, -1f, 100f, -1f);

            ButtonView abandon = UIFactory.CreateButton(column, "Abandon", UIStrings.AbandonRun, ButtonStyle.Danger, AskAbandon, 34f);
            UIFactory.AddLayout(abandon.Rect, -1f, -1f, -1f, -1f, 100f, -1f);
        }

        private void AskAbandon()
        {
            Manager.Confirm("Покинуть забег?",
                "Ресурсы сохранятся. Захваченные корабли и собранные модули будут потеряны.",
                "ПОКИНУТЬ", () => Services.Flow.AbandonRun(), true);
        }

        protected override void OnClosed()
        {
            Manager.SetPaused(false);
        }
    }
}
