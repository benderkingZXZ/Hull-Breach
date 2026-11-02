using System;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Contracts;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    /// <summary>Блок «Итоги забега»: корабли, отсеки, враги, время, добытые ресурсы и собранные модули.</summary>
    public sealed class RunResultView
    {
        public RectTransform Root { get; private set; }

        private readonly TMP_Text ships;
        private readonly TMP_Text rooms;
        private readonly TMP_Text enemies;
        private readonly TMP_Text time;
        private readonly NumberTicker resources;
        private readonly ModuleStripView modules;

        public RunResultView(Transform parent, Color accent, IModuleService moduleService)
        {
            PanelView panel = UIFactory.Panel(parent, "RunResult", UIColors.WithAlpha(UIColors.Hull, 0.92f), UIColors.WithAlpha(accent, 0.7f));
            Root = panel.Rect;

            TMP_Text title = UIFactory.Label(Root, "Title", "ИТОГИ ЗАБЕГА", 30f, UIColors.TextDim, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            UIFactory.AnchorStretchX(title.rectTransform, 0.05f, 0.95f, 1f, 1f, -24f, 40f);
            title.characterSpacing = 8f;

            ships = Row("Кораблей захвачено", 78f, UIColors.Text);
            rooms = Row("Отсеков зачищено", 142f, UIColors.Text);
            enemies = Row("Врагов уничтожено", 206f, UIColors.Text);
            time = Row("Время забега", 270f, UIColors.Text);
            TMP_Text resourceLabel = Row("Ресурсов добыто", 334f, UIColors.Amber);
            resources = resourceLabel.gameObject.AddComponent<NumberTicker>();
            resources.Label = resourceLabel;
            resources.Prefix = "+";

            TMP_Text modulesTitle = UIFactory.Label(Root, "ModulesTitle", "СОБРАННЫЕ МОДУЛИ", 24f, UIColors.TextDim, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            UIFactory.AnchorStretchX(modulesTitle.rectTransform, 0.05f, 0.95f, 1f, 1f, -420f, 32f);
            modulesTitle.characterSpacing = 6f;
            modules = new ModuleStripView(Root, "Modules", moduleService, 68f, false, "Модулей не собрано");
            UIFactory.AnchorStretchX(modules.Root, 0.05f, 0.95f, 1f, 1f, -458f, 72f);
        }

        private TMP_Text Row(string label, float yTop, Color valueColor)
        {
            RectTransform row = UIFactory.Rect("Row_" + label, Root);
            UIFactory.AnchorStretchX(row, 0.05f, 0.95f, 1f, 1f, -yTop, 56f);
            TMP_Text l = UIFactory.Label(row, "Label", label, 32f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            UIFactory.AnchorRange(l.rectTransform, 0f, 0f, 0.62f, 1f, 0f, 0f, 0f, 0f);
            UIFactory.FitOneLine(l, 18f, 32f);
            TMP_Text v = UIFactory.Label(row, "Value", "0", 40f, valueColor, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            UIFactory.AnchorRange(v.rectTransform, 0.6f, 0f, 1f, 1f, 0f, 0f, 0f, 0f);
            UIFactory.FitOneLine(v, 20f, 40f);
            return v;
        }

        public void Set(RunSummary s)
        {
            if (s == null)
            {
                ships.text = "0 из " + UIStrings.SectorShipCount;
                rooms.text = "0";
                enemies.text = "0";
                time.text = "00:00";
                resources.Set(0, true);
                modules.Refresh(null);
                return;
            }
            ships.text = s.ShipsCaptured + " из " + UIStrings.SectorShipCount;
            rooms.text = UIFormat.Number(s.RoomsCleared);
            enemies.text = UIFormat.Number(s.EnemiesKilled);
            time.text = UIFormat.Time(s.DurationSeconds);
            resources.Set(0, true);
            resources.Set(s.ResourcesEarned, false);
            modules.Refresh(s.Modules);
        }
    }

    /// <summary>Блок «Прогресс сохранён»: запас ресурсов, уровни прокачки, рекорды.</summary>
    public sealed class SavedProgressView
    {
        public RectTransform Root { get; private set; }

        private readonly WalletView wallet;
        private readonly TMP_Text health;
        private readonly TMP_Text damage;
        private readonly TMP_Text record;
        private readonly TMP_Text totals;

        public SavedProgressView(Transform parent)
        {
            PanelView panel = UIFactory.Panel(parent, "SavedProgress", UIColors.WithAlpha(UIColors.Hull, 0.92f), UIColors.WithAlpha(UIColors.Green, 0.7f));
            Root = panel.Rect;

            Image check = UIFactory.Img(Root, "Check", UIIcons.Get("check"), UIColors.Green);
            check.preserveAspect = true;
            UIFactory.Anchor(check.rectTransform, 0f, 1f, 0f, 1f, 30f, -22f, 46f, 46f);
            TMP_Text title = UIFactory.Label(Root, "Title", "ПРОГРЕСС СОХРАНЁН", 36f, UIColors.Green, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            UIFactory.AnchorStretchX(title.rectTransform, 0f, 1f, 1f, 1f, -22f, 46f);
            title.rectTransform.offsetMin = new Vector2(88f, title.rectTransform.offsetMin.y);
            title.rectTransform.offsetMax = new Vector2(-20f, title.rectTransform.offsetMax.y);
            UIFactory.FitOneLine(title, 20f, 36f);

            TMP_Text walletTitle = UIFactory.Label(Root, "WalletTitle", "Ресурсов в запасе", 28f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            UIFactory.AnchorStretchX(walletTitle.rectTransform, 0.07f, 0.93f, 1f, 1f, -98f, 36f);
            wallet = UIFactory.CreateWallet(Root, "Wallet");
            UIFactory.Anchor(wallet.Rect, 0f, 1f, 0f, 1f, 40f, -140f, 340f, 64f);
            wallet.Rect.localScale = new Vector3(1.3f, 1.3f, 1f);

            health = Line(250f);
            damage = Line(306f);
            record = Line(362f);
            totals = Line(418f);
        }

        private TMP_Text Line(float yTop)
        {
            TMP_Text t = UIFactory.Label(Root, "Line", "", 30f, UIColors.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            UIFactory.AnchorStretchX(t.rectTransform, 0.07f, 0.93f, 1f, 1f, -yTop, 48f);
            UIFactory.FitOneLine(t, 18f, 30f);
            return t;
        }

        public void Set(IProgressionService p)
        {
            int max = p.GetMaxUpgradeLevel();
            wallet.SetValue(p.Resources, true);
            health.text = "Здоровье: уровень " + p.GetUpgradeLevel(UpgradeType.Health) + " из " + max;
            damage.text = "Урон: уровень " + p.GetUpgradeLevel(UpgradeType.Damage) + " из " + max;
            record.text = "Лучший забег: " + p.BestShipsCaptured + " из " + UIStrings.SectorShipCount + " кораблей";
            totals.text = "Побед: " + p.Victories + ", забегов: " + p.TotalRuns;
        }
    }

    /// <summary>Общая основа экранов смерти и победы: заголовок, итоги, сохранённый прогресс и ряд кнопок.</summary>
    public abstract class RunEndScreenBase : UIScreen
    {
        private RunResultView result;
        private SavedProgressView saved;
        private RectTransform buttonBar;
        private TMP_Text subtitle;

        protected abstract string TitleText { get; }
        protected abstract string SubtitleText { get; }
        protected abstract Color AccentColor { get; }
        protected abstract void BuildButtons();
        protected virtual void BuildExtra(RectTransform root) { }

        protected override void Build()
        {
            RectTransform root = (RectTransform)transform;
            Color accent = AccentColor;

            RectTransform titleBlock = UIFactory.Rect("TitleBlock", root);
            UIFactory.AnchorRange(titleBlock, 0f, 0.78f, 1f, 1f, 24f, 0f, 0f, 16f);
            titleBlock.gameObject.AddComponent<AppearAnimation>();

            TMP_Text title = UIFactory.Label(titleBlock, "Title", TitleText, 96f, accent, FontStyles.Bold, TextAlignmentOptions.TopLeft, true);
            UIFactory.AnchorStretchX(title.rectTransform, 0f, 1f, 1f, 1f, 0f, 112f);
            title.characterSpacing = 4f;
            UIFactory.FitOneLine(title, 40f, 96f);

            subtitle = UIFactory.Label(titleBlock, "Subtitle", SubtitleText, 30f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            UIFactory.AnchorStretchX(subtitle.rectTransform, 0f, 1f, 1f, 1f, -118f, 44f);
            UIFactory.FitOneLine(subtitle, 18f, 30f);

            BuildExtra(root);

            result = new RunResultView(root, accent, Services.Modules);
            UIFactory.AnchorRange(result.Root, 0f, 0.2f, 0.53f, 0.78f, 24f, 0f, 12f, 0f);

            saved = new SavedProgressView(root);
            UIFactory.AnchorRange(saved.Root, 0.55f, 0.2f, 1f, 0.78f, 0f, 0f, 0f, 0f);

            buttonBar = UIFactory.Rect("Buttons", root);
            UIFactory.AnchorRange(buttonBar, 0f, 0f, 1f, 0.18f, 0f, 0f, 0f, 0f);
            UIFactory.HorizontalGroup(buttonBar, 28f, TextAnchor.MiddleCenter);
            BuildButtons();
        }

        protected ButtonView AddBarButton(string label, ButtonStyle style, Action onClick, float fontSize, string icon, float width, float height)
        {
            ButtonView b = UIFactory.CreateButton(buttonBar, "Btn_" + label, label, style, onClick, fontSize, icon);
            UIFactory.AddLayout(b.Rect, 240f, width, 0f, height, height, 0f);
            return b;
        }

        protected override void OnShown()
        {
            RunSummary summary = Services.Flow.LastRunSummary;
            result.Set(summary);
            saved.Set(Services.Progression);
        }

        protected bool Run(bool ok, string failMessage)
        {
            if (!ok) Manager.Toast(failMessage, true);
            return ok;
        }
    }
}
