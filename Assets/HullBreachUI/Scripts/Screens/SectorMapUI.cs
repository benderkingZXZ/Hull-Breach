using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    /// <summary>
    /// Карта сектора: маршрут из 4 кораблей и босса, 2–3 доступные цели с риском и наградой.
    /// Выбор карточки не меняет состояние игры; SelectShip вызывается только кнопкой подтверждения.
    /// </summary>
    public sealed class SectorMapUI : UIScreen
    {
        private const int NodeCount = UIStrings.SectorShipCount + 1;

        public override ScreenId Id { get { return ScreenId.SectorMap; } }

        private HeaderView header;
        private RectTransform cardsRow;
        private ButtonView confirm;
        private ModuleStripView modules;
        private TMP_Text runGain;
        private readonly Image[] nodeBack = new Image[NodeCount];
        private readonly Image[] nodeIcon = new Image[NodeCount];
        private readonly Image[] connectors = new Image[NodeCount - 1];
        private readonly List<CardView> cards = new List<CardView>();
        private IReadOnlyList<ShipDefinition> options;
        private int selectedIndex = -1;

        protected override void Build()
        {
            RectTransform root = (RectTransform)transform;

            header = UIFactory.CreateHeader(root, "КАРТА СЕКТОРА", "", UIStrings.ToMenu, AskAbandon, true);

            // ---- Маршрут: 4 корабля и босс
            RectTransform track = UIFactory.Rect("Route", root);
            UIFactory.Anchor(track, 0.5f, 1f, 0.5f, 1f, 0f, -150f, 1100f, 120f);
            for (int i = 0; i < NodeCount; i++)
            {
                float x = (i - (NodeCount - 1) * 0.5f) * 230f;
                if (i < NodeCount - 1)
                {
                    RectTransform line = UIFactory.Rect("Link" + i, track);
                    UIFactory.Anchor(line, 0.5f, 0.5f, 0.5f, 0.5f, x + 115f, 8f, 140f, 6f);
                    connectors[i] = UIFactory.AddImage(line.gameObject, ProceduralSprites.White, UIColors.Steel);
                }
            }
            for (int i = 0; i < NodeCount; i++)
            {
                float x = (i - (NodeCount - 1) * 0.5f) * 230f;
                RectTransform node = UIFactory.Rect("Node" + i, track);
                UIFactory.Anchor(node, 0.5f, 0.5f, 0.5f, 0.5f, x, 8f, 88f, 88f);
                nodeBack[i] = UIFactory.AddImage(node.gameObject, ProceduralSprites.Circle, UIColors.Hull);
                UIFactory.Img(node, "Ring", ProceduralSprites.Ring, UIColors.Steel);
                nodeIcon[i] = UIFactory.Img(node, "Icon", UIIcons.Get("lock"), UIColors.TextDim);
                nodeIcon[i].preserveAspect = true;
                UIFactory.Stretch(nodeIcon[i].rectTransform, 22f, 22f, 22f, 22f);

                string caption = i == NodeCount - 1 ? "БОСС" : (i + 1).ToString();
                TMP_Text label = UIFactory.Label(node, "Caption", caption, 24f, UIColors.TextDim, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.AnchorStretchX(label.rectTransform, -0.2f, 1.2f, 0f, 1f, -6f, 30f);
            }

            // ---- Карточки кораблей
            cardsRow = UIFactory.Rect("Cards", root);
            UIFactory.AnchorRange(cardsRow, 0f, 0f, 1f, 1f, 0f, 150f, 0f, 290f);
            UIFactory.HorizontalGroup(cardsRow, 36f, TextAnchor.MiddleCenter);

            // ---- Низ экрана
            confirm = UIFactory.CreateButton(root, "Confirm", "К ВЫБОРУ ВХОДА", ButtonStyle.Primary, OnConfirm, 40f, "arrow_right");
            UIFactory.Anchor(confirm.Rect, 0.5f, 0f, 0.5f, 0f, 0f, 0f, 560f, 118f);

            TMP_Text modulesTitle = UIFactory.Label(root, "ModulesTitle", "МОДУЛИ ЗАБЕГА", 22f, UIColors.TextDim, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
            UIFactory.Anchor(modulesTitle.rectTransform, 0f, 0f, 0f, 0f, 0f, 78f, 320f, 30f);
            modulesTitle.characterSpacing = 6f;
            modules = new ModuleStripView(root, "Modules", Services.Modules, 64f, false, "Пока нет. Берите после захвата корабля");
            UIFactory.Anchor(modules.Root, 0f, 0f, 0f, 0f, 0f, 4f, 560f, 70f);

            runGain = UIFactory.Label(root, "RunGain", "", 28f, UIColors.Amber, FontStyles.Bold, TextAlignmentOptions.BottomRight);
            UIFactory.Anchor(runGain.rectTransform, 1f, 0f, 1f, 0f, 0f, 24f, 560f, 44f);
        }

        protected override void OnShown()
        {
            Services.Progression.ResourcesChanged += HandleResources;
            header.Wallet.SetValue(Services.Progression.Resources, true);

            options = Services.Flow.GetShipOptions();
            selectedIndex = options != null && options.Count == 1 ? 0 : -1;

            int tier = options != null && options.Count > 0
                ? options[0].Tier
                : Mathf.Min(Services.Flow.RunShipsCaptured + 1, NodeCount);
            header.Subtitle.text = tier >= NodeCount
                ? "Финал: флагман «Нексус-Прайм»"
                : "Мёртвый сектор. Корабль " + tier + " из " + UIStrings.SectorShipCount;
            UpdateRoute(tier);

            RebuildCards();
            modules.Refresh(null);
            runGain.text = Services.Flow.RunResourcesEarned > 0
                ? "Добыто за забег: +" + UIFormat.Number(Services.Flow.RunResourcesEarned)
                : "";
            RefreshSelection();
        }

        protected override void OnHidden()
        {
            Services.Progression.ResourcesChanged -= HandleResources;
        }

        private void HandleResources(int value) { header.Wallet.SetValue(value, false); }

        private void UpdateRoute(int tier)
        {
            for (int i = 0; i < NodeCount; i++)
            {
                int step = i + 1;
                bool bossNode = i == NodeCount - 1;
                if (step < tier)
                {
                    nodeBack[i].color = UIColors.WithAlpha(UIColors.Green, 0.25f);
                    nodeIcon[i].sprite = UIIcons.Get("check");
                    nodeIcon[i].color = UIColors.Green;
                }
                else if (step == tier)
                {
                    nodeBack[i].color = UIColors.WithAlpha(bossNode ? UIColors.RiskBoss : UIColors.Cyan, 0.3f);
                    nodeIcon[i].sprite = UIIcons.Get(bossNode ? "ship_boss" : "ship_b");
                    nodeIcon[i].color = bossNode ? UIColors.RiskBoss : UIColors.Cyan;
                }
                else
                {
                    nodeBack[i].color = UIColors.Hull;
                    nodeIcon[i].sprite = UIIcons.Get(bossNode ? "ship_boss" : "lock");
                    nodeIcon[i].color = UIColors.WithAlpha(UIColors.TextDim, 0.7f);
                }
                if (i < connectors.Length)
                    connectors[i].color = step < tier ? UIColors.Green : UIColors.WithAlpha(UIColors.Steel, 0.7f);
            }
        }

        private void RebuildCards()
        {
            for (int i = cardsRow.childCount - 1; i >= 0; i--)
            {
                GameObject child = cardsRow.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            cards.Clear();
            if (options == null) return;

            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                ShipDefinition ship = options[i];
                Color risk = UIColors.Risk(ship.Risk);

                CardView card = UIFactory.CreateCard(cardsRow, "Ship_" + ship.Id, risk);
                UIFactory.AddLayout(card.Rect, 300f, 480f, 0f, 540f, 600f, 0f);
                card.Visual.Restart(i * 0.09f);
                card.Button.onClick.AddListener(() => OnCardClicked(index));
                cards.Add(card);

                RectTransform tag = CardParts.Pill(card.Content, "Risk", UIStrings.Risk(ship.Risk), risk, 26f);
                UIFactory.Anchor(tag, 0.5f, 1f, 0.5f, 1f, 0f, -24f, 270f, 44f);

                Image icon = UIFactory.Img(card.Content, "Ship", UIIcons.Get(UIIcons.ForShip(ship.Risk)), risk);
                icon.preserveAspect = true;
                UIFactory.Anchor(icon.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -84f, 300f, 150f);

                TMP_Text name = UIFactory.Label(card.Content, "Name", ship.DisplayName, 40f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
                UIFactory.AnchorStretchX(name.rectTransform, 0.04f, 0.96f, 1f, 1f, -240f, 56f);
                UIFactory.FitOneLine(name, 22f, 40f);

                TMP_Text hint = UIFactory.Label(card.Content, "Hint", UIStrings.RiskHint(ship.Risk), 24f, UIColors.TextDim, FontStyles.Italic, TextAlignmentOptions.Top);
                UIFactory.AnchorStretchX(hint.rectTransform, 0.07f, 0.93f, 1f, 1f, -300f, 76f);

                CardParts.Divider(card.Content, 384f);

                RectTransform diffRow = CardParts.Row(card.Content, "Сложность", 402f, 44f);
                RectTransform pipsRect = UIFactory.Rect("Pips", diffRow);
                UIFactory.Anchor(pipsRect, 1f, 0.5f, 1f, 0.5f, 0f, 0f, 190f, 22f);
                UIFactory.HorizontalGroup(pipsRect, 6f, TextAnchor.MiddleRight);
                PipsView pips = new PipsView(pipsRect, 28f, 22f);
                pips.Set(DifficultyPips(ship), 5, risk, UIColors.WithAlpha(UIColors.Steel, 0.5f));

                RectTransform rewardRow = CardParts.Row(card.Content, "Награда", 454f, 52f);
                WalletView reward = UIFactory.CreateWallet(rewardRow, "Reward");
                UIFactory.Anchor(reward.Rect, 1f, 0.5f, 1f, 0.5f, 0f, 0f, 200f, 50f);
                reward.SetValue(ship.BaseResourceReward, true);

                TMP_Text note = UIFactory.Label(card.Content, "Note", "Итог зависит от точки входа", 21f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.Center);
                UIFactory.AnchorStretchX(note.rectTransform, 0.06f, 0.94f, 1f, 1f, -508f, 30f);
            }
        }

        /// <summary>1…5 делений: только для наглядности (множитель сложности корабля из API).</summary>
        private static int DifficultyPips(ShipDefinition ship)
        {
            if (ship.IsBossShip) return 5;
            return Mathf.Clamp(Mathf.CeilToInt((ship.DifficultyMultiplier - 0.6f) / 0.4f), 1, 5);
        }

        private void OnCardClicked(int index)
        {
            selectedIndex = index;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < cards.Count; i++) cards[i].SetSelected(i == selectedIndex);
            bool has = selectedIndex >= 0 && options != null && selectedIndex < options.Count;
            confirm.SetInteractable(has);
            confirm.SetLabel(has ? "К ВЫБОРУ ВХОДА" : "ВЫБЕРИТЕ КОРАБЛЬ");
        }

        private void OnConfirm()
        {
            if (options == null || selectedIndex < 0 || selectedIndex >= options.Count) return;
            if (!Services.Flow.SelectShip(options[selectedIndex].Id)) Manager.Toast("Не удалось выбрать корабль", true);
        }

        private void AskAbandon()
        {
            Manager.Confirm("Прервать забег?",
                "Ресурсы сохранятся. Захваченные корабли и собранные модули будут потеряны.",
                "ПРЕРВАТЬ", () => Services.Flow.AbandonRun(), true);
        }

        public override bool HandleBack()
        {
            AskAbandon();
            return true;
        }
    }
}
