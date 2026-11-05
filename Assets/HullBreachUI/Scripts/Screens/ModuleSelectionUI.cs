using System.Collections.Generic;
using HullBreach.Data;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    public sealed class ModuleSelectionUI : UIScreen
    {
        public override ScreenId Id { get { return ScreenId.ModuleSelection; } }

        private HeaderView header;
        private RectTransform cardsRow;
        private ButtonView confirm;
        private TMP_Text reward;
        private ModuleStripView owned;
        private readonly List<CardView> cards = new List<CardView>();
        private IReadOnlyList<ModuleInstance> offers;
        private int selectedIndex = -1;

        protected override void Build()
        {
            RectTransform root = (RectTransform)transform;
            header = UIFactory.CreateHeader(root, "КОРАБЛЬ ЗАХВАЧЕН", "", null, null, true);

            reward = UIFactory.Label(root, "Reward", "", 36f, UIColors.Amber, FontStyles.Bold, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(reward.rectTransform, 0.2f, 0.8f, 1f, 1f, -132f, 50f);

            cardsRow = UIFactory.Rect("Cards", root);
            UIFactory.AnchorRange(cardsRow, 0f, 0f, 1f, 1f, 0f, 160f, 0f, 190f);
            UIFactory.HorizontalGroup(cardsRow, 34f, TextAnchor.MiddleCenter);

            confirm = UIFactory.CreateButton(root, "Confirm", "ВЫБЕРИТЕ МОДУЛЬ", ButtonStyle.Primary, OnConfirm, 38f);
            UIFactory.Anchor(confirm.Rect, 0.5f, 0f, 0.5f, 0f, 0f, 0f, 820f, 118f);

            TMP_Text ownedTitle = UIFactory.Label(root, "OwnedTitle", "УЖЕ УСТАНОВЛЕНО", 22f, UIColors.TextDim, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
            UIFactory.Anchor(ownedTitle.rectTransform, 0f, 0f, 0f, 0f, 0f, 78f, 320f, 30f);
            ownedTitle.characterSpacing = 6f;
            owned = new ModuleStripView(root, "Owned", Services.Modules, 64f, false, "Это ваш первый модуль");
            UIFactory.Anchor(owned.Root, 0f, 0f, 0f, 0f, 0f, 4f, 440f, 70f);
        }

        protected override void OnShown()
        {
            Services.Progression.ResourcesChanged += HandleResources;
            header.Wallet.SetValue(Services.Progression.Resources, true);

            ShipRewardInfo info = Services.Flow.CurrentReward;
            offers = info != null ? info.ModuleOffers : null;
            selectedIndex = -1;

            if (info != null)
            {
                string place = info.Ship != null ? info.Ship.DisplayName : "Корабль";
                header.Subtitle.text = place + ". Выберите 1 из 3 модулей, остальные будут потеряны.";
                reward.text = "Добыча: +" + UIFormat.Number(info.ResourcesAwarded) + " ресурсов";
            }
            else
            {
                header.Subtitle.text = "Выберите модуль.";
                reward.text = "";
            }

            RebuildCards();
            owned.Refresh(null);
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
            if (offers == null) return;

            for (int i = 0; i < offers.Count; i++)
            {
                int index = i;
                ModuleInstance m = offers[i];
                Color rarity = UIColors.Rarity(m.Rarity);

                CardView card = UIFactory.CreateCard(cardsRow, "Module_" + i, rarity);
                UIFactory.AddLayout(card.Rect, 300f, 480f, 0f, 580f, 600f, 0f);
                card.Visual.Restart(i * 0.11f);
                card.Button.onClick.AddListener(() => OnCardClicked(index));
                cards.Add(card);

                RectTransform pill = CardParts.Pill(card.Content, "Rarity", UIStrings.Rarity(m.Rarity), rarity, 26f);
                UIFactory.Anchor(pill, 0.5f, 1f, 0.5f, 1f, 0f, -24f, 250f, 44f);

                Image ring = UIFactory.Img(card.Content, "IconRing", ProceduralSprites.Ring, UIColors.WithAlpha(rarity, 0.55f));
                UIFactory.Anchor(ring.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -86f, 170f, 170f);
                Image icon = UIFactory.Img(card.Content, "Icon", UIIcons.Get(UIIcons.ForModule(m.Id)), rarity);
                icon.preserveAspect = true;
                UIFactory.Anchor(icon.rectTransform, 0.5f, 1f, 0.5f, 1f, 0f, -116f, 110f, 110f);

                TMP_Text name = UIFactory.Label(card.Content, "Name", m.DisplayName, 40f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
                UIFactory.AnchorStretchX(name.rectTransform, 0.04f, 0.96f, 1f, 1f, -270f, 56f);
                UIFactory.FitOneLine(name, 22f, 40f);

                TMP_Text type = UIFactory.Label(card.Content, "Type",
                    m.IsActive ? "АКТИВНЫЙ" : "ПАССИВНЫЙ", 24f,
                    m.IsActive ? UIColors.Amber : UIColors.Cyan, FontStyles.Bold, TextAlignmentOptions.Center);
                UIFactory.AnchorStretchX(type.rectTransform, 0.04f, 0.96f, 1f, 1f, -330f, 32f);
                type.characterSpacing = 8f;

                TMP_Text how = UIFactory.Label(card.Content, "How",
                    m.IsActive ? "срабатывает по кнопке навыка" : "постоянный бонус", 21f,
                    UIColors.TextDim, FontStyles.Italic, TextAlignmentOptions.Center);
                UIFactory.AnchorStretchX(how.rectTransform, 0.04f, 0.96f, 1f, 1f, -362f, 30f);

                CardParts.Divider(card.Content, 402f);

                TMP_Text desc = UIFactory.Label(card.Content, "Description", m.Description, 30f, UIColors.Text, FontStyles.Normal, TextAlignmentOptions.Top);
                UIFactory.AnchorStretchX(desc.rectTransform, 0.07f, 0.93f, 1f, 1f, -418f, 170f);
            }
        }

        private void OnCardClicked(int index)
        {
            selectedIndex = index;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < cards.Count; i++) cards[i].SetSelected(i == selectedIndex);
            bool has = offers != null && selectedIndex >= 0 && selectedIndex < offers.Count;
            confirm.SetInteractable(has);
            confirm.SetLabel(has ? "УСТАНОВИТЬ: " + offers[selectedIndex].DisplayName.ToUpperInvariant() : "ВЫБЕРИТЕ МОДУЛЬ");
        }

        private void OnConfirm()
        {
            if (offers == null || selectedIndex < 0 || selectedIndex >= offers.Count) return;
            confirm.SetInteractable(false);
            if (!Services.Flow.ChooseModule(selectedIndex))
            {
                Manager.Toast("Не удалось установить модуль", true);
                RefreshSelection();
            }
        }

        public override bool HandleBack()
        {
            Manager.Toast("Сначала выберите модуль", true);
            return true;
        }
    }
}
