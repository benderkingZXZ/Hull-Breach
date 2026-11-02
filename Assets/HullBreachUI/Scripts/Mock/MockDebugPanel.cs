using HullBreach.Core;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using UnityEngine;

namespace HullBreach.UI.Mock
{
    /// <summary>
    /// Отладочные кнопки справа по центру: видны только в бою и только с заглушкой MockGameHost.
    /// Нужны, чтобы вручную вызвать урон, зачистку комнаты, смерть и проверить HUD и экраны.
    /// </summary>
    public sealed class MockDebugPanel : MonoBehaviour
    {
        private MockGameHost host;
        private GameObject panel;

        public static MockDebugPanel Create(UIRootRefs root, MockGameHost host)
        {
            // Корневой объект всегда активен (на нём Update); кнопки лежат в дочернем контейнере.
            RectTransform holder = UIFactory.Rect("MockDebug", root.OverlayLayer);
            UIFactory.Stretch(holder);
            MockDebugPanel p = holder.gameObject.AddComponent<MockDebugPanel>();
            p.host = host;

            RectTransform container = UIFactory.Rect("Buttons", holder);
            UIFactory.Anchor(container, 1f, 0.5f, 1f, 0.5f, -8f, 20f, 190f, 320f);
            UIFactory.VerticalGroup(container, 8f, TextAnchor.MiddleCenter);
            p.panel = container.gameObject;

            AddButton(container, "УРОН -15", ButtonStyle.Secondary, () => host.DebugDamage(15f));
            AddButton(container, "ЗАЧИСТИТЬ", ButtonStyle.Secondary, host.DebugKillEnemies);
            AddButton(container, "+100 РЕС.", ButtonStyle.Secondary, () => host.DebugAddResources(100));
            AddButton(container, "УБИТЬ РОБОТА", ButtonStyle.Danger, host.DebugKillRobot);
            container.gameObject.SetActive(false);
            return p;
        }

        private static void AddButton(RectTransform parent, string label, ButtonStyle style, System.Action action)
        {
            ButtonView b = UIFactory.CreateButton(parent, label, label, style, action, 24f);
            UIFactory.AddLayout(b.Rect, -1f, -1f, -1f, -1f, 66f, -1f);
        }

        private void Update()
        {
            if (host == null || panel == null) return;
            bool visible = host.CurrentState == GameState.Combat;
            if (panel.activeSelf != visible) panel.SetActive(visible);
        }
    }
}
