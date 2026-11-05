using System;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;

namespace HullBreach.UI.Screens
{
    public sealed class ConfirmDialogUI : UIModal
    {
        private TMP_Text title;
        private TMP_Text message;
        private ButtonView cancelButton;
        private ButtonView confirmButton;
        private ButtonView dangerButton;
        private Action onConfirm;

        protected override void BuildContent(RectTransform content)
        {
            RectTransform box = UIFactory.Rect("Box", content);
            UIFactory.Anchor(box, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 920f, 480f);
            UIFactory.Panel(box, "Panel", UIColors.WithAlpha(UIColors.Hull, 0.98f), UIColors.CyanDim);
            box.gameObject.AddComponent<AppearAnimation>();

            title = UIFactory.Label(box, "Title", "", 50f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UIFactory.AnchorStretchX(title.rectTransform, 0.05f, 0.95f, 1f, 1f, -34f, 70f);
            UIFactory.FitOneLine(title, 28f, 50f);

            message = UIFactory.Label(box, "Message", "", 32f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.Center);
            UIFactory.AnchorRange(message.rectTransform, 0.07f, 0.28f, 0.93f, 0.82f, 0f, 0f, 0f, 0f);

            cancelButton = UIFactory.CreateButton(box, "Cancel", UIStrings.Cancel, ButtonStyle.Secondary, Close, 32f);
            UIFactory.Anchor(cancelButton.Rect, 0f, 0f, 0f, 0f, 40f, 36f, 380f, 100f);

            confirmButton = UIFactory.CreateButton(box, "Confirm", UIStrings.Confirm, ButtonStyle.Primary, Accept, 32f);
            UIFactory.Anchor(confirmButton.Rect, 1f, 0f, 1f, 0f, -40f, 36f, 440f, 100f);

            dangerButton = UIFactory.CreateButton(box, "ConfirmDanger", UIStrings.Confirm, ButtonStyle.Danger, Accept, 32f);
            UIFactory.Anchor(dangerButton.Rect, 1f, 0f, 1f, 0f, -40f, 36f, 440f, 100f);
        }

        public void Present(string titleText, string messageText, string confirmLabel, string cancelLabel, Action confirmAction, bool danger)
        {
            title.text = titleText;
            message.text = messageText;
            cancelButton.SetLabel(cancelLabel);
            confirmButton.SetLabel(confirmLabel);
            dangerButton.SetLabel(confirmLabel);
            confirmButton.SetVisible(!danger);
            dangerButton.SetVisible(danger);
            onConfirm = confirmAction;
            Open();
        }

        private void Accept()
        {
            Action action = onConfirm;
            onConfirm = null;
            Close();
            if (action != null) action();
        }
    }
}
