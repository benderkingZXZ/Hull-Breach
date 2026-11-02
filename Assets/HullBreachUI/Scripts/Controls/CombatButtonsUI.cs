using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Contracts;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Controls
{
    /// <summary>
    /// Кнопки справа: атака (удерживать — стрелять/бить с автонаведением) и навык с радиальным индикатором
    /// перезарядки. Над атакой — значки активных модулей с перезарядкой (кнопка навыка запускает и их).
    /// </summary>
    public sealed class CombatButtonsUI : MonoBehaviour
    {
        private sealed class Badge
        {
            public ModuleId Id;
            public Image Overlay;
            public Image Ring;
        }

        private ICombatInputSink input;
        private ICombatFeed feed;
        private IModuleService modules;

        private CanvasGroup group;
        private RectTransform attackRect;
        private RectTransform skillRect;
        private RectTransform badgeColumn;
        private Image attackRing;
        private Image skillRing;
        private Image skillOverlay;
        private TMP_Text skillTimer;
        private TMP_Text skillName;
        private TouchButton attackButton;
        private TouchButton skillButton;
        private readonly List<Badge> badges = new List<Badge>();

        private int lastTimerSeconds = -1;
        private float shake;
        private float attackPulse;

        public static CombatButtonsUI Create(RectTransform parent, ICombatInputSink input, ICombatFeed feed, IModuleService modules)
        {
            RectTransform container = UIFactory.Rect("CombatButtons", parent);
            UIFactory.Stretch(container);
            CombatButtonsUI ui = container.gameObject.AddComponent<CombatButtonsUI>();
            ui.input = input;
            ui.feed = feed;
            ui.modules = modules;
            ui.Build(container);
            return ui;
        }

        private void Build(RectTransform container)
        {
            group = container.gameObject.AddComponent<CanvasGroup>();

            // ---- Атака (большая, в углу под большим пальцем)
            attackRect = UIFactory.Rect("Attack", container);
            UIFactory.Anchor(attackRect, 1f, 0f, 0.5f, 0.5f, -150f, 150f, 214f, 214f);
            UIFactory.AddImage(attackRect.gameObject, ProceduralSprites.Circle, UIColors.WithAlpha(UIColors.Hull, 0.78f), Image.Type.Simple, true);
            attackRing = UIFactory.Img(attackRect, "Ring", ProceduralSprites.Ring, UIColors.Cyan);
            Image attackIcon = UIFactory.Img(attackRect, "Icon", UIIcons.Get("crosshair"), UIColors.Text);
            attackIcon.preserveAspect = true;
            UIFactory.Stretch(attackIcon.rectTransform, 62f, 62f, 62f, 62f);
            attackButton = attackRect.gameObject.AddComponent<TouchButton>();
            attackButton.Pressed += OnAttackPressed;
            attackButton.Released += OnAttackReleased;

            // ---- Навык (выше и левее: дуга большого пальца)
            skillRect = UIFactory.Rect("Skill", container);
            UIFactory.Anchor(skillRect, 1f, 0f, 0.5f, 0.5f, -372f, 262f, 156f, 156f);
            UIFactory.AddImage(skillRect.gameObject, ProceduralSprites.Circle, UIColors.WithAlpha(UIColors.Hull, 0.78f), Image.Type.Simple, true);
            skillRing = UIFactory.Img(skillRect, "Ring", ProceduralSprites.Ring, UIColors.Amber);
            Image skillIcon = UIFactory.Img(skillRect, "Icon", UIIcons.Get("bolt"), UIColors.Amber);
            skillIcon.preserveAspect = true;
            UIFactory.Stretch(skillIcon.rectTransform, 44f, 44f, 44f, 44f);

            skillOverlay = UIFactory.Img(skillRect, "Cooldown", ProceduralSprites.Circle, UIColors.WithAlpha(Color.black, 0.68f));
            skillOverlay.type = Image.Type.Filled;
            skillOverlay.fillMethod = Image.FillMethod.Radial360;
            skillOverlay.fillOrigin = (int)Image.Origin360.Top;
            skillOverlay.fillClockwise = false;
            skillOverlay.fillAmount = 0f;

            skillTimer = UIFactory.Label(skillRect, "Timer", "", 54f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center);
            skillTimer.gameObject.SetActive(false);

            skillName = UIFactory.Label(skillRect, "Name", "", 22f, UIColors.TextDim, FontStyles.Bold, TextAlignmentOptions.Top);
            UIFactory.Anchor(skillName.rectTransform, 0.5f, 0f, 0.5f, 1f, 0f, -2f, 300f, 30f);

            skillButton = skillRect.gameObject.AddComponent<TouchButton>();
            skillButton.Pressed += OnSkillPressed;

            // ---- Значки активных модулей над кнопкой атаки
            badgeColumn = UIFactory.Rect("ModuleBadges", container);
            UIFactory.Anchor(badgeColumn, 1f, 0f, 0.5f, 0f, -150f, 290f, 76f, 280f);
            UIFactory.VerticalGroup(badgeColumn, 8f, TextAnchor.LowerCenter);

            ApplySettings();
        }

        private void OnEnable()
        {
            UISettings.Changed += ApplySettings;
            if (modules != null) modules.OwnedModulesChanged += RebuildBadges;
            ApplySettings();
            RebuildBadges();
            RefreshSkillName();
            if (feed != null) feed.RobotChanged += HandleRobotChanged;
        }

        private void OnDisable()
        {
            UISettings.Changed -= ApplySettings;
            if (modules != null) modules.OwnedModulesChanged -= RebuildBadges;
            if (feed != null) feed.RobotChanged -= HandleRobotChanged;
            // кнопки сами отпустятся в TouchButton.OnDisable; на всякий случай гасим автоатаку
            if (input != null) input.SetAttackHeld(false);
        }

        private void HandleRobotChanged(RobotType robot) { RefreshSkillName(); }

        private void RefreshSkillName()
        {
            if (skillName == null || feed == null) return;
            skillName.text = RobotDefinitions.Get(feed.Robot).SkillName;
        }

        private void ApplySettings()
        {
            if (group == null) return;
            group.alpha = UISettings.ControlOpacity;
            float s = UISettings.ControlScale;
            attackRect.localScale = new Vector3(s, s, 1f);
            skillRect.localScale = new Vector3(s, s, 1f);
        }

        private void OnAttackPressed()
        {
            attackPulse = 1f;
            if (input != null) input.SetAttackHeld(true);
        }

        private void OnAttackReleased()
        {
            if (input != null) input.SetAttackHeld(false);
        }

        private void OnSkillPressed()
        {
            if (input != null && !input.TryUseSkill()) shake = 0.25f;
        }

        private void RebuildBadges()
        {
            if (badgeColumn == null) return;
            for (int i = badgeColumn.childCount - 1; i >= 0; i--)
            {
                GameObject child = badgeColumn.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            badges.Clear();
            if (modules == null) return;

            IReadOnlyList<ModuleInstance> owned = modules.OwnedModules;
            for (int i = 0; i < owned.Count; i++)
            {
                ModuleInstance m = owned[i];
                if (!m.IsActive) continue;
                Color rarity = UIColors.Rarity(m.Rarity);

                RectTransform b = UIFactory.Rect("Badge_" + m.Id, badgeColumn);
                UIFactory.AddLayout(b, 70f, 70f, 0f, 70f, 70f, 0f);
                UIFactory.AddImage(b.gameObject, ProceduralSprites.Circle, UIColors.WithAlpha(UIColors.Hull, 0.85f));
                Image icon = UIFactory.Img(b, "Icon", UIIcons.Get(UIIcons.ForModule(m.Id)), rarity);
                icon.preserveAspect = true;
                UIFactory.Stretch(icon.rectTransform, 16f, 16f, 16f, 16f);

                Image overlay = UIFactory.Img(b, "Cooldown", ProceduralSprites.Circle, UIColors.WithAlpha(Color.black, 0.68f));
                overlay.type = Image.Type.Filled;
                overlay.fillMethod = Image.FillMethod.Radial360;
                overlay.fillOrigin = (int)Image.Origin360.Top;
                overlay.fillClockwise = false;
                overlay.fillAmount = 0f;
                Image ring = UIFactory.Img(b, "Ring", ProceduralSprites.Ring, rarity);

                Badge badge = new Badge();
                badge.Id = m.Id;
                badge.Overlay = overlay;
                badge.Ring = ring;
                badges.Add(badge);
            }
        }

        private void Update()
        {
            if (feed == null || input == null) return;
            float dt = Time.unscaledDeltaTime;

            // Автоатака: бэкенд сбрасывает флаг при деактивации игрока, поэтому подтверждаем каждый кадр.
            bool held = attackButton.IsHeld;
            if (held) input.SetAttackHeld(true);

            Vector3 targetPos;
            bool locked = feed.TryGetTargetPosition(out targetPos);
            Color ringTarget = held ? Color.white : (locked ? UIColors.Red : UIColors.Cyan);
            attackRing.color = Color.Lerp(attackRing.color, ringTarget, dt * 14f);
            attackPulse = Mathf.MoveTowards(attackPulse, held ? 0.6f : 0f, dt * 4f);
            float attackScale = UISettings.ControlScale * (1f - 0.06f * attackPulse);
            attackRect.localScale = new Vector3(attackScale, attackScale, 1f);

            // Навык: радиальная перезарядка и секунды
            float remaining = feed.SkillCooldownRemaining;
            skillOverlay.fillAmount = feed.SkillCooldownNormalized;
            bool onCooldown = remaining > 0.02f;
            skillRing.color = Color.Lerp(skillRing.color, onCooldown ? UIColors.WithAlpha(UIColors.Steel, 0.9f) : UIColors.Amber, dt * 12f);
            if (onCooldown)
            {
                int seconds = Mathf.CeilToInt(remaining);
                if (seconds != lastTimerSeconds)
                {
                    lastTimerSeconds = seconds;
                    skillTimer.text = seconds.ToString();
                }
                if (!skillTimer.gameObject.activeSelf) skillTimer.gameObject.SetActive(true);
            }
            else
            {
                lastTimerSeconds = -1;
                if (skillTimer.gameObject.activeSelf) skillTimer.gameObject.SetActive(false);
            }

            shake = Mathf.MoveTowards(shake, 0f, dt);
            float offset = shake > 0f ? Mathf.Sin(shake * 70f) * 8f * (shake / 0.25f) : 0f;
            skillRect.anchoredPosition = new Vector2(-372f + offset, 262f);
            float skillScale = UISettings.ControlScale * (skillButton.IsHeld ? 0.94f : 1f);
            skillRect.localScale = new Vector3(skillScale, skillScale, 1f);

            // Активные модули
            for (int i = 0; i < badges.Count; i++)
            {
                float n = modules.GetCooldownNormalized(badges[i].Id);
                badges[i].Overlay.fillAmount = n;
                Color rc = badges[i].Ring.color;
                rc.a = n > 0.01f ? 0.35f : 1f;
                badges[i].Ring.color = rc;
            }
        }
    }
}
