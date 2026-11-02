using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    /// <summary>Экран победы: уничтожение «Нексус-Прайм», итоги забега, возврат в меню.</summary>
    public sealed class VictoryScreenUI : RunEndScreenBase
    {
        public override ScreenId Id { get { return ScreenId.Victory; } }

        protected override string TitleText { get { return "НЕКСУС-ПРАЙМ УНИЧТОЖЕН"; } }
        protected override string SubtitleText { get { return "Мёртвый сектор очищен. Флагман мятежного ИИ разбит."; } }
        protected override Color AccentColor { get { return UIColors.Cyan; } }

        protected override void BuildExtra(RectTransform root)
        {
            // Силуэт флагмана, перечёркнутый крестом: наглядный итог победы над боссом.
            RectTransform badge = UIFactory.Rect("BossDestroyed", root);
            UIFactory.Anchor(badge, 1f, 1f, 1f, 1f, 0f, -10f, 300f, 190f);
            badge.gameObject.AddComponent<AppearAnimation>();
            Image boss = UIFactory.Img(badge, "Boss", UIIcons.Get("ship_boss"), UIColors.WithAlpha(UIColors.RiskBoss, 0.55f));
            boss.preserveAspect = true;
            Image cross = UIFactory.Img(badge, "Cross", UIIcons.Get("close"), UIColors.WithAlpha(UIColors.Red, 0.9f));
            cross.preserveAspect = true;
            UIFactory.Stretch(cross.rectTransform, 55f, 15f, 55f, 15f);
        }

        protected override void BuildButtons()
        {
            AddBarButton("НОВЫЙ ЗАБЕГ", ButtonStyle.Secondary, () => Run(Services.Flow.StartSectorRun(), "Не удалось начать забег"), 32f, "play", 400f, 104f);
            AddBarButton("ПРОКАЧКА", ButtonStyle.Secondary, () => Run(Services.Flow.OpenUpgrades(), "Прокачка недоступна"), 32f, "plus", 360f, 104f);
            AddBarButton("В ГЛАВНОЕ МЕНЮ", ButtonStyle.Primary, () => Run(Services.Flow.GoToMainMenu(), "Не удалось вернуться в меню"), 38f, null, 520f, 124f);
        }

        public override bool HandleBack()
        {
            Services.Flow.GoToMainMenu();
            return true;
        }
    }
}
