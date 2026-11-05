using System.Collections.Generic;
using System.Globalization;
using HullBreach.Core;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    public sealed class UpgradeScreenUI : UIScreen
    {
        private sealed class UpgradeCardRefs
        {
            public UpgradeType Type;
            public CardView Card;
            public TMP_Text Level;
            public PipsView Pips;
            public TMP_Text Current;
            public TMP_Text Next;
            public ButtonView Buy;
        }

        public override ScreenId Id { get { return ScreenId.Upgrades; } }

        private HeaderView header;
        private readonly List<UpgradeCardRefs> upgradeCards = new List<UpgradeCardRefs>();

        protected override void Build()
        {
            RectTransform root = (RectTransform)transform;
            header = UIFactory.CreateHeader(root, UIStrings.Upgrades, "Улучшения действуют со следующего корабля.", UIStrings.Back, OnBack, true);

            RectTransform row = UIFactory.Rect("Cards", root);
            UIFactory.AnchorRange(row, 0f, 0f, 1f, 1f, 0f, 150f, 0f, 150f);
            UIFactory.HorizontalGroup(row, 48f, TextAnchor.MiddleCenter);

            BuildCard(row, UpgradeType.Health, "plus", UIColors.Green, 0);
            BuildCard(row, UpgradeType.Damage, "core", UIColors.Amber, 1);

            ButtonView fight = UIFactory.CreateButton(root, "ToBattle", "В БОЙ", ButtonStyle.Primary, OnFight, 42f, "play");
            UIFactory.Anchor(fight.Rect, 1f, 0f, 1f, 0f, 0f, 0f, 480f, 118f);
        }

        private void BuildCard(RectTransform row, UpgradeType type, string iconKey, Color accent, int order)
        {
            CardView card = UIFactory.CreateCard(row, "Upgrade_" + type, accent);
            UIFactory.AddLayout(card.Rect, 380f, 640f, 0f, 600f, 640f, 0f);
            card.Button.transition = Selectable.Transition.None;
            card.Visual.Restart(order * 0.1f);

            Image icon = UIFactory.Img(card.Content, "Icon", UIIcons.Get(iconKey), accent);
            icon.preserveAspect = true;
            UIFactory.Anchor(icon.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -28f, 120f, 120f);

            TMP_Text name = UIFactory.Label(card.Content, "Name", UIStrings.UpgradeName(type).ToUpperInvariant(), 52f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UIFactory.AnchorStretchX(name.rectTransform, 0.04f, 0.96f, 1f, 1f, -156f, 64f);
            UIFactory.FitOneLine(name, 28f, 52f);

            TMP_Text level = UIFactory.Label(card.Content, "Level", "", 34f, accent, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(level.rectTransform, 0.04f, 0.96f, 1f, 1f, -226f, 44f);

            RectTransform pipsRect = UIFactory.Rect("Pips", card.Content);
            UIFactory.Anchor(pipsRect, 0.5f, 1f, 0.5f, 1f, 0f, -282f, 560f, 24f);
            UIFactory.HorizontalGroup(pipsRect, 6f, TextAnchor.MiddleCenter);
            PipsView pips = new PipsView(pipsRect, 40f, 22f);

            CardParts.Divider(card.Content, 326f);

            TMP_Text current = UIFactory.Label(card.Content, "Current", "", 32f, UIColors.Text, FontStyles.Normal, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(current.rectTransform, 0.05f, 0.95f, 1f, 1f, -342f, 44f);
            UIFactory.FitOneLine(current, 18f, 32f);

            TMP_Text next = UIFactory.Label(card.Content, "Next", "", 28f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(next.rectTransform, 0.05f, 0.95f, 1f, 1f, -390f, 40f);
            UIFactory.FitOneLine(next, 16f, 28f);

            ButtonView buy = UIFactory.CreateButton(card.Content, "Buy", "", ButtonStyle.Primary, () => AskBuy(type), 36f);
            UIFactory.AnchorStretchX(buy.Rect, 0.08f, 0.92f, 0f, 0f, 36f, 112f);

            UpgradeCardRefs refs = new UpgradeCardRefs();
            refs.Type = type;
            refs.Card = card;
            refs.Level = level;
            refs.Pips = pips;
            refs.Current = current;
            refs.Next = next;
            refs.Buy = buy;
            upgradeCards.Add(refs);
        }

        protected override void OnShown()
        {
            Services.Progression.ResourcesChanged += HandleResources;
            Services.Progression.UpgradeLevelChanged += HandleLevel;
            header.Wallet.SetValue(Services.Progression.Resources, true);
            RefreshCards();
        }

        protected override void OnHidden()
        {
            Services.Progression.ResourcesChanged -= HandleResources;
            Services.Progression.UpgradeLevelChanged -= HandleLevel;
        }

        private void HandleResources(int value)
        {
            header.Wallet.SetValue(value, false);
            RefreshCards();
        }

        private void HandleLevel(UpgradeType type, int level) { RefreshCards(); }

        private void RefreshCards()
        {
            for (int i = 0; i < upgradeCards.Count; i++) RefreshCard(upgradeCards[i]);
        }

        private void RefreshCard(UpgradeCardRefs c)
        {
            int level = Services.Progression.GetUpgradeLevel(c.Type);
            int max = Services.Progression.GetMaxUpgradeLevel();
            int cost = Services.Progression.GetUpgradeCost(c.Type);
            int wallet = Services.Progression.Resources;

            c.Level.text = "Уровень " + level + " из " + max;
            c.Pips.Set(level, max, c.Type == UpgradeType.Health ? UIColors.Green : UIColors.Amber, UIColors.WithAlpha(UIColors.Steel, 0.45f));

            if (c.Type == UpgradeType.Health)
            {
                float bonus = Services.Progression.GetBonusHealth();
                c.Current.text = level > 0 ? "Сейчас: +" + Mathf.RoundToInt(bonus) + " к здоровью" : "Сейчас: без бонуса";
                c.Next.text = level > 0 && level < max
                    ? "Дальше: +" + Mathf.RoundToInt(bonus / level * (level + 1))
                    : (level >= max ? "Достигнут максимум" : "");
            }
            else
            {
                float mult = Services.Progression.GetDamageMultiplier();
                c.Current.text = level > 0 ? "Сейчас: ×" + mult.ToString("0.##", CultureInfo.InvariantCulture) + " к урону" : "Сейчас: без бонуса";
                float perLevel = level > 0 ? (mult - 1f) / level : 0f;
                c.Next.text = level > 0 && level < max
                    ? "Дальше: ×" + (1f + perLevel * (level + 1)).ToString("0.##", CultureInfo.InvariantCulture)
                    : (level >= max ? "Достигнут максимум" : "");
            }

            if (cost < 0)
            {
                c.Buy.SetLabel("МАКСИМУМ");
                c.Buy.SetInteractable(false);
            }
            else if (wallet < cost)
            {
                c.Buy.SetLabel("НУЖНО " + UIFormat.Number(cost));
                c.Buy.SetInteractable(false);
            }
            else
            {
                c.Buy.SetLabel("УЛУЧШИТЬ ЗА " + UIFormat.Number(cost));
                c.Buy.SetInteractable(true);
            }
        }

        private void AskBuy(UpgradeType type)
        {
            int level = Services.Progression.GetUpgradeLevel(type);
            int cost = Services.Progression.GetUpgradeCost(type);
            if (cost < 0) return;
            int left = Services.Progression.Resources - cost;

            Manager.Confirm("Улучшить " + UIStrings.UpgradeName(type).ToLowerInvariant() + "?",
                "С уровня " + level + " на " + (level + 1) + ".\nСтоимость: " + UIFormat.Number(cost) +
                ". Останется: " + UIFormat.Number(Mathf.Max(0, left)) + ".",
                "КУПИТЬ", () => Buy(type));
        }

        private void Buy(UpgradeType type)
        {
            if (Services.Progression.TryPurchaseUpgrade(type))
                Manager.Toast(UIStrings.UpgradeName(type) + ": уровень " + Services.Progression.GetUpgradeLevel(type));
            else
                Manager.Toast("Не хватает ресурсов", true);
            RefreshCards();
        }

        private void OnBack()
        {
            Services.Flow.CloseUpgrades();
        }

        private void OnFight()
        {
            if (!Services.Flow.StartSectorRun()) Manager.Toast("Не удалось начать забег", true);
        }

        public override bool HandleBack()
        {
            OnBack();
            return true;
        }
    }
}
