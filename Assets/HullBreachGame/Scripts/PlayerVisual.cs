using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Player;
using UnityEngine;

namespace HullBreach.Game
{
    /// <summary>
    /// Временная графика игрока: цвет по роботу, «носик» по направлению, вспышки атаки/навыка/урона.
    /// Подписки живут, пока объект игрока активен (вне боя он скрыт; цвет обновляется при следующем включении).
    /// </summary>
    public sealed class PlayerVisual : MonoBehaviour
    {
        private static readonly Color SamuraiColor = new Color(1f, 0.73f, 0.22f, 1f);
        private static readonly Color TechnoColor = new Color(0.17f, 0.85f, 1f, 1f);

        private PlayerController player;
        private PlayerCombat combat;
        private Health health;
        private SpriteRenderer body;
        private Transform nub;
        private Color baseColor = Color.white;
        private float flashTimer;

        public static PlayerVisual Attach(GameObject playerObject, SpriteRenderer bodyRenderer, Transform nubTransform)
        {
            PlayerVisual v = playerObject.AddComponent<PlayerVisual>();
            v.body = bodyRenderer;
            v.nub = nubTransform;
            return v;
        }

        private void OnEnable()
        {
            player = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<Health>();

            ApplyRobot(player.RobotType);
            player.RobotChanged += ApplyRobot;
            if (combat != null)
            {
                combat.Attacked += OnAttacked;
                combat.SkillUsed += OnSkillUsed;
            }
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (player != null) player.RobotChanged -= ApplyRobot;
            if (combat != null)
            {
                combat.Attacked -= OnAttacked;
                combat.SkillUsed -= OnSkillUsed;
            }
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void ApplyRobot(RobotType robot)
        {
            baseColor = robot == RobotType.Techno ? TechnoColor : SamuraiColor;
            if (body != null) body.color = baseColor;
        }

        private void OnDamaged(DamageInfo info, float dealt)
        {
            flashTimer = 0.12f;
        }

        private void OnAttacked(Vector2 origin, Vector2 direction, RobotType robot)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (robot == RobotType.Samurai)
            {
                float reach = RobotDefinitions.Get(RobotType.Samurai).AttackRange;
                Vector3 pos = (Vector3)(origin + direction * (reach * 0.55f));
                OneShotFx.Spawn(RuntimeSprites.Square, pos, angle, new Color(1f, 0.95f, 0.7f, 0.9f),
                    new Vector2(reach * 0.5f, 0.25f), new Vector2(reach * 1.1f, 0.05f), 0.14f);
            }
            else
            {
                OneShotFx.Spawn(RuntimeSprites.Circle, (Vector3)(origin + direction * 0.6f), 0f, new Color(0.6f, 0.95f, 1f, 0.9f),
                    new Vector2(0.25f, 0.25f), new Vector2(0.6f, 0.6f), 0.1f);
            }
        }

        private void OnSkillUsed(RobotType robot, Vector2 origin)
        {
            float diameter = robot == RobotType.Samurai ? RobotDefinitions.Get(RobotType.Samurai).SkillRadius * 2f : 5f;
            Color c = robot == RobotType.Samurai ? new Color(1f, 0.8f, 0.3f, 0.85f) : new Color(0.3f, 0.9f, 1f, 0.85f);
            OneShotFx.Spawn(RuntimeSprites.Ring, (Vector3)origin, 0f, c, new Vector2(0.8f, 0.8f), new Vector2(diameter, diameter), 0.35f);
        }

        private void LateUpdate()
        {
            if (player != null && nub != null) nub.localPosition = (Vector3)(player.FacingDirection * 0.42f);

            if (body == null) return;
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                body.color = Color.white;
            }
            else
            {
                body.color = baseColor;
            }
        }
    }
}
