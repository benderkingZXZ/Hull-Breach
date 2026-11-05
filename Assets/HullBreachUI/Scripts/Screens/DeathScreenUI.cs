using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using UnityEngine;

namespace HullBreach.UI.Screens
{
    public sealed class DeathScreenUI : RunEndScreenBase
    {
        public override ScreenId Id { get { return ScreenId.Death; } }

        protected override string TitleText { get { return "РОБОТ УНИЧТОЖЕН"; } }
        protected override string SubtitleText { get { return "Забег завершён. Корабли и модули потеряны, ресурсы и прокачка остаются."; } }
        protected override Color AccentColor { get { return UIColors.Red; } }

        protected override void BuildButtons()
        {
            AddBarButton(UIStrings.ToMenu, ButtonStyle.Ghost, () => Run(Services.Flow.GoToMainMenu(), "Не удалось вернуться в меню"), 30f, null, 300f, 100f);
            AddBarButton("К ПРОКАЧКЕ", ButtonStyle.Secondary, () => Run(Services.Flow.OpenUpgrades(), "Прокачка недоступна"), 34f, "plus", 430f, 110f);
            AddBarButton("НАЧАТЬ ЗАНОВО", ButtonStyle.Primary, () => Run(Services.Flow.StartSectorRun(), "Не удалось начать забег"), 38f, "play", 520f, 124f);
        }

        public override bool HandleBack()
        {
            Services.Flow.GoToMainMenu();
            return true;
        }
    }
}
