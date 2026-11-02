using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    /// <summary>Главное меню: название, начало забега, прокачка, настройки, выход и выбор робота.</summary>
    public sealed class MainMenuUI : UIScreen
    {
        private sealed class RobotCardRefs
        {
            public RobotType Type;
            public CardView Card;
            public RectTransform Tag;
        }

        public override ScreenId Id { get { return ScreenId.MainMenu; } }

        private WalletView wallet;
        private TMP_Text stats;
        private readonly RobotCardRefs[] robots = new RobotCardRefs[2];

        protected override void Build()
        {
            RectTransform root = (RectTransform)transform;

            // ---- Название
            RectTransform titleBlock = UIFactory.Rect("TitleBlock", root);
            UIFactory.AnchorRange(titleBlock, 0f, 0.6f, 0.58f, 1f, 24f, 0f, 0f, 30f);
            titleBlock.gameObject.AddComponent<AppearAnimation>();

            TMP_Text glow = UIFactory.Label(titleBlock, "TitleGlow", UIStrings.GameTitle, 160f,
                UIColors.WithAlpha(UIColors.Cyan, 0.32f), FontStyles.Bold, TextAlignmentOptions.TopLeft, true);
            UIFactory.AnchorStretchX(glow.rectTransform, 0f, 1f, 1f, 1f, -6f, 190f);
            glow.rectTransform.offsetMin = new Vector2(7f, glow.rectTransform.offsetMin.y);
            glow.rectTransform.offsetMax = new Vector2(7f, glow.rectTransform.offsetMax.y);
            glow.characterSpacing = 6f;
            UIFactory.FitOneLine(glow, 70f, 160f);

            TMP_Text title = UIFactory.Label(titleBlock, "Title", UIStrings.GameTitle, 160f, UIColors.Text,
                FontStyles.Bold, TextAlignmentOptions.TopLeft, true);
            UIFactory.AnchorStretchX(title.rectTransform, 0f, 1f, 1f, 1f, 0f, 190f);
            title.characterSpacing = 6f;
            UIFactory.FitOneLine(title, 70f, 160f);

            TMP_Text sub = UIFactory.Label(titleBlock, "Subtitle", UIStrings.GameSubtitle, 40f, UIColors.Cyan,
                FontStyles.Bold, TextAlignmentOptions.TopLeft, true);
            UIFactory.AnchorStretchX(sub.rectTransform, 0f, 1f, 1f, 1f, -190f, 54f);
            sub.characterSpacing = 18f;
            UIFactory.FitOneLine(sub, 22f, 40f);

            TMP_Text tag = UIFactory.Label(titleBlock, "Tagline", UIStrings.Tagline, 28f, UIColors.TextDim,
                FontStyles.Normal, TextAlignmentOptions.TopLeft);
            UIFactory.AnchorStretchX(tag.rectTransform, 0f, 1f, 1f, 1f, -246f, 80f);

            // ---- Кнопки
            RectTransform column = UIFactory.Rect("Buttons", root);
            UIFactory.Anchor(column, 0f, 0f, 0f, 0f, 24f, 24f, 600f, 500f);
            UIFactory.VerticalGroup(column, 18f, TextAnchor.LowerCenter);

            ButtonView start = UIFactory.CreateButton(column, "Start", UIStrings.Start, ButtonStyle.Primary, OnStart, 46f, "play");
            UIFactory.AddLayout(start.Rect, -1f, -1f, -1f, -1f, 128f, -1f);
            ButtonView upgrades = UIFactory.CreateButton(column, "Upgrades", UIStrings.Upgrades, ButtonStyle.Secondary, OnUpgrades, 34f, "plus");
            UIFactory.AddLayout(upgrades.Rect, -1f, -1f, -1f, -1f, 98f, -1f);
            ButtonView settings = UIFactory.CreateButton(column, "Settings", UIStrings.Settings, ButtonStyle.Secondary,
                () => Manager.OpenSettings(true), 34f, "gear");
            UIFactory.AddLayout(settings.Rect, -1f, -1f, -1f, -1f, 98f, -1f);
            ButtonView quit = UIFactory.CreateButton(column, "Quit", UIStrings.Quit, ButtonStyle.Ghost, AskQuit, 30f, "close");
            UIFactory.AddLayout(quit.Rect, -1f, -1f, -1f, -1f, 80f, -1f);

            // ---- Выбор робота
            RectTransform robotPanel = UIFactory.Rect("RobotPanel", root);
            UIFactory.AnchorRange(robotPanel, 0.54f, 0.1f, 1f, 0.72f, 0f, 30f, 0f, 0f);

            TMP_Text robotTitle = UIFactory.Label(robotPanel, "RobotTitle", "БОЕВОЙ РОБОТ", 34f, UIColors.TextDim,
                FontStyles.Bold, TextAlignmentOptions.TopLeft);
            UIFactory.AnchorStretchX(robotTitle.rectTransform, 0f, 1f, 1f, 1f, 0f, 46f);
            robotTitle.characterSpacing = 8f;

            RectTransform row = UIFactory.Rect("RobotRow", robotPanel);
            UIFactory.AnchorRange(row, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 60f);
            UIFactory.HorizontalGroup(row, 28f, TextAnchor.MiddleLeft);

            BuildRobotCard(row, 0, RobotType.Samurai);
            BuildRobotCard(row, 1, RobotType.Techno);

            // ---- Кошелёк и статистика
            wallet = UIFactory.CreateWallet(root, "Wallet");
            UIFactory.Anchor(wallet.Rect, 1f, 1f, 1f, 1f, 0f, -10f, 320f, 60f);

            stats = UIFactory.Label(root, "Stats", "", 26f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.TopRight);
            UIFactory.Anchor(stats.rectTransform, 1f, 1f, 1f, 1f, 0f, -76f, 620f, 40f);
        }

        private void BuildRobotCard(RectTransform row, int index, RobotType type)
        {
            RobotDefinition def = RobotDefinitions.Get(type);
            Color accent = type == RobotType.Techno ? UIColors.Cyan : UIColors.Amber;

            CardView card = UIFactory.CreateCard(row, "Robot_" + type, accent);
            UIFactory.AddLayout(card.Rect, 240f, 380f, 0f, 470f, 470f, 0f);
            card.Visual.Restart(0.1f + index * 0.08f);
            card.Button.onClick.AddListener(() => OnRobotClicked(type));

            Image icon = UIFactory.Img(card.Content, "Icon", UIIcons.Get(UIIcons.ForRobot(type)), accent);
            icon.preserveAspect = true;
            UIFactory.Anchor(icon.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -22f, 170f, 170f);

            TMP_Text name = UIFactory.Label(card.Content, "Name", def.DisplayName, 42f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UIFactory.AnchorStretchX(name.rectTransform, 0.04f, 0.96f, 1f, 1f, -204f, 54f);
            UIFactory.FitOneLine(name, 24f, 42f);

            TMP_Text role = UIFactory.Label(card.Content, "Role", UIStrings.RobotRole(type), 24f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(role.rectTransform, 0.06f, 0.94f, 1f, 1f, -262f, 66f);

            TMP_Text numbers = UIFactory.Label(card.Content, "Numbers",
                "Здоровье " + Mathf.RoundToInt(def.BaseHealth) + "   Урон " + Mathf.RoundToInt(def.BaseDamage),
                26f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(numbers.rectTransform, 0.04f, 0.96f, 1f, 1f, -336f, 36f);
            UIFactory.FitOneLine(numbers, 16f, 26f);

            TMP_Text skill = UIFactory.Label(card.Content, "Skill", "Навык: " + def.SkillName, 26f, accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(skill.rectTransform, 0.04f, 0.96f, 1f, 1f, -376f, 36f);
            UIFactory.FitOneLine(skill, 16f, 26f);

            TMP_Text desc = UIFactory.Label(card.Content, "SkillDesc", UIStrings.RobotSkillText(type), 22f, UIColors.TextDim, FontStyles.Italic, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(desc.rectTransform, 0.06f, 0.94f, 1f, 1f, -412f, 52f);

            RectTransform selectedPill = CardParts.Pill(card.Content, "SelectedTag", "ВЫБРАН", accent, 26f);
            UIFactory.Anchor(selectedPill, 0.5f, 0f, 0.5f, 0f, 0f, 14f, 200f, 40f);

            RobotCardRefs refs = new RobotCardRefs();
            refs.Type = type;
            refs.Card = card;
            refs.Tag = selectedPill;
            robots[index] = refs;
        }

        protected override void OnShown()
        {
            Services.Progression.ResourcesChanged += HandleResources;
            Services.Progression.SelectedRobotChanged += HandleRobotChanged;
            RefreshAll(true);
        }

        protected override void OnHidden()
        {
            Services.Progression.ResourcesChanged -= HandleResources;
            Services.Progression.SelectedRobotChanged -= HandleRobotChanged;
        }

        private void HandleResources(int value) { wallet.SetValue(value, false); }
        private void HandleRobotChanged(RobotType type) { RefreshRobots(); }

        private void RefreshAll(bool instant)
        {
            wallet.SetValue(Services.Progression.Resources, instant);
            stats.text = "Побед: " + Services.Progression.Victories + "   Забегов: " + Services.Progression.TotalRuns +
                         "   Рекорд: " + Services.Progression.BestShipsCaptured + " из " + UIStrings.SectorShipCount;
            RefreshRobots();
        }

        private void RefreshRobots()
        {
            RobotType selected = Services.Progression.SelectedRobot;
            for (int i = 0; i < robots.Length; i++)
            {
                bool isSelected = robots[i].Type == selected;
                robots[i].Card.SetSelected(isSelected);
                robots[i].Tag.gameObject.SetActive(isSelected);
            }
        }

        private void OnRobotClicked(RobotType type)
        {
            if (Services.Flow.SelectRobot(type)) RefreshRobots();
        }

        private void OnStart()
        {
            if (!Services.Flow.StartSectorRun()) Manager.Toast("Не удалось начать забег", true);
        }

        private void OnUpgrades()
        {
            if (!Services.Flow.OpenUpgrades()) Manager.Toast("Прокачка сейчас недоступна", true);
        }

        private void AskQuit()
        {
            Manager.Confirm("Выйти из игры?", "Прогресс уже сохранён.", "ВЫЙТИ", UIManager.QuitGame, true);
        }

        public override bool HandleBack()
        {
            AskQuit();
            return true;
        }
    }
}
