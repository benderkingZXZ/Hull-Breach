using System.Collections.Generic;
using System.Globalization;
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
    /// Выбор точки входа: три карточки с явным разделением «РИСК» и «НАГРАДА».
    /// Полосы нормированы по самому большому значению среди трёх входов — видно, чем вход отличается от соседей.
    /// </summary>
    public sealed class EntrySelectionUI : UIScreen
    {
        private const int MaxRoomsForPips = 5;

        public override ScreenId Id { get { return ScreenId.EntrySelection; } }

        private HeaderView header;
        private RectTransform cardsRow;
        private ButtonView confirm;
        private readonly List<CardView> cards = new List<CardView>();
        private IReadOnlyList<EntryPointProfile> profiles;
        private int selectedIndex = -1;

        protected override void Build()
        {
            RectTransform root = (RectTransform)transform;
            header = UIFactory.CreateHeader(root, "ТОЧКА ВХОДА", "", UIStrings.Back, OnBack, true);

            cardsRow = UIFactory.Rect("Cards", root);
            UIFactory.AnchorRange(cardsRow, 0f, 0f, 1f, 1f, 0f, 150f, 0f, 150f);
            UIFactory.HorizontalGroup(cardsRow, 32f, TextAnchor.MiddleCenter);

            confirm = UIFactory.CreateButton(root, "Confirm", "ВЫБЕРИТЕ ТОЧКУ ВХОДА", ButtonStyle.Primary, OnConfirm, 40f);
            UIFactory.Anchor(confirm.Rect, 0.5f, 0f, 0.5f, 0f, 0f, 0f, 760f, 118f);
        }

        protected override void OnShown()
        {
            Services.Progression.ResourcesChanged += HandleResources;
            header.Wallet.SetValue(Services.Progression.Resources, true);

            ShipDefinition ship = Services.Flow.SelectedShip;
            header.Subtitle.text = ship != null
                ? ship.DisplayName + ", " + UIStrings.Risk(ship.Risk).ToLowerInvariant() + ". Вход определяет весь забег."
                : "Вход определяет весь забег.";

            profiles = Services.Flow.GetEntryPoints();
            selectedIndex = -1;
            RebuildCards();
            RefreshSelection();
        }

        protected override void OnHidden()
        {
            Services.Progression.ResourcesChanged -= HandleResources;
        }

        private void HandleResources(int value) { header.Wallet.SetValue(value, false); }

        private void RebuildCards()
        {
            for (int i = cardsRow.childCount - 1; i >= 0; i--)
            {
                GameObject child = cardsRow.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            cards.Clear();
            if (profiles == null) return;

            float maxCount = 0.01f, maxStrength = 0.01f, maxResources = 0.01f, maxRare = 0.01f;
            for (int i = 0; i < profiles.Count; i++)
            {
                maxCount = Mathf.Max(maxCount, profiles[i].EnemyCountMultiplier);
                maxStrength = Mathf.Max(maxStrength, profiles[i].EnemyStrengthMultiplier);
                maxResources = Mathf.Max(maxResources, profiles[i].ResourceMultiplier);
                maxRare = Mathf.Max(maxRare, profiles[i].RareModuleBonus);
            }

            for (int i = 0; i < profiles.Count; i++)
            {
                int index = i;
                EntryPointProfile p = profiles[i];
                Color accent = UIColors.Entry(p.Entry);

                CardView card = UIFactory.CreateCard(cardsRow, "Entry_" + p.Entry, accent);
                UIFactory.AddLayout(card.Rect, 300f, 520f, 0f, 640f, 700f, 0f);
                card.Visual.Restart(i * 0.09f);
                card.Button.onClick.AddListener(() => OnCardClicked(index));
                cards.Add(card);

                Image icon = UIFactory.Img(card.Content, "Icon", UIIcons.Get(UIIcons.ForEntry(p.Entry)), accent);
                icon.preserveAspect = true;
                UIFactory.Anchor(icon.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -24f, 110f, 110f);

                TMP_Text name = UIFactory.Label(card.Content, "Name", p.DisplayName, 40f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
                UIFactory.AnchorStretchX(name.rectTransform, 0.04f, 0.96f, 1f, 1f, -144f, 54f);
                UIFactory.FitOneLine(name, 22f, 40f);

                TMP_Text desc = UIFactory.Label(card.Content, "Desc", p.Description, 24f, UIColors.TextDim, FontStyles.Italic, TextAlignmentOptions.Top);
                UIFactory.AnchorStretchX(desc.rectTransform, 0.07f, 0.93f, 1f, 1f, -200f, 90f);

                RectTransform roomsRow = CardParts.Row(card.Content, p.RoomCount + " " + UIFormat.Plural(p.RoomCount, "отсек", "отсека", "отсеков"), 300f, 40f);
                RectTransform pipsRect = UIFactory.Rect("Pips", roomsRow);
                UIFactory.Anchor(pipsRect, 1f, 0.5f, 1f, 0.5f, 0f, 0f, 200f, 22f);
                UIFactory.HorizontalGroup(pipsRect, 6f, TextAnchor.MiddleRight);
                PipsView pips = new PipsView(pipsRect, 30f, 22f);
                pips.Set(p.RoomCount, MaxRoomsForPips, accent, UIColors.WithAlpha(UIColors.Steel, 0.45f));

                CardParts.Divider(card.Content, 352f);

                TMP_Text riskTitle = UIFactory.Label(card.Content, "RiskTitle", "РИСК", 26f, UIColors.Red, FontStyles.Bold, TextAlignmentOptions.Left);
                UIFactory.AnchorStretchX(riskTitle.rectTransform, 0.06f, 0.94f, 1f, 1f, -362f, 32f);
                riskTitle.characterSpacing = 10f;
                UIFactory.CreateStatRow(card.Content, 400f, "Врагов", p.EnemyCountMultiplier / maxCount, UIColors.Red, Multiplier(p.EnemyCountMultiplier));
                UIFactory.CreateStatRow(card.Content, 442f, "Сила врагов", p.EnemyStrengthMultiplier / maxStrength, UIColors.Red, Multiplier(p.EnemyStrengthMultiplier));

                TMP_Text rewardTitle = UIFactory.Label(card.Content, "RewardTitle", "НАГРАДА", 26f, UIColors.Amber, FontStyles.Bold, TextAlignmentOptions.Left);
                UIFactory.AnchorStretchX(rewardTitle.rectTransform, 0.06f, 0.94f, 1f, 1f, -506f, 32f);
                rewardTitle.characterSpacing = 10f;
                UIFactory.CreateStatRow(card.Content, 544f, "Ресурсы", p.ResourceMultiplier / maxResources, UIColors.Amber, Multiplier(p.ResourceMultiplier));
                float rare = p.RareModuleBonus > 0f ? p.RareModuleBonus / maxRare : 0f;
                string rareText = p.RareModuleBonus > 0f ? "+" + Mathf.RoundToInt(p.RareModuleBonus * 100f) + "%" : "нет";
                UIFactory.CreateStatRow(card.Content, 586f, "Редкие модули", rare, UIColors.RarityEpic, rareText);
            }
        }

        private static string Multiplier(float value)
        {
            return "×" + value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void OnCardClicked(int index)
        {
            selectedIndex = index;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < cards.Count; i++) cards[i].SetSelected(i == selectedIndex);
            bool has = profiles != null && selectedIndex >= 0 && selectedIndex < profiles.Count;
            confirm.SetInteractable(has);
            confirm.SetLabel(has ? "НАЧАТЬ АБОРДАЖ: " + profiles[selectedIndex].DisplayName.ToUpperInvariant() : "ВЫБЕРИТЕ ТОЧКУ ВХОДА");
        }

        private void OnConfirm()
        {
            if (profiles == null || selectedIndex < 0 || selectedIndex >= profiles.Count) return;
            confirm.SetInteractable(false);   // защита от двойного нажатия: дальше состояние сменится само
            if (!Services.Flow.ConfirmEntryPoint(profiles[selectedIndex].Entry))
            {
                Manager.Toast("Не удалось начать абордаж", true);
                RefreshSelection();
            }
        }

        private void OnBack()
        {
            Services.Flow.CancelShipSelection();
        }

        public override bool HandleBack()
        {
            OnBack();
            return true;
        }
    }
}
