using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Player;
using UnityEngine;

namespace HullBreach.Game
{
    public sealed class PlayerVisual : MonoBehaviour
    {
        private const float HurtDuration = 0.14f;
        private static readonly Color HurtTint = new Color(1f, 0.4f, 0.4f, 1f);

        private PlayerController player;
        private PlayerCombat combat;
        private Health health;
        private SpriteRenderer body;
        private SideSprites samuraiSprites;
        private SideSprites strelSprites;
        private SideSprites activeSprites;
        private ArtSide side = ArtSide.Front;
        private float hurtTimer;

        public static PlayerVisual Attach(GameObject playerObject, SpriteRenderer renderer, SideSprites samurai, SideSprites strel)
        {
            PlayerVisual visual = playerObject.AddComponent<PlayerVisual>();
            visual.body = renderer;
            visual.samuraiSprites = samurai;
            visual.strelSprites = strel;
            return visual;
        }

        private void OnEnable()
        {
            player = GetComponent<PlayerController>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<Health>();

            if (player != null)
            {
                player.RobotChanged += OnRobotChanged;
                OnRobotChanged(player.RobotType);
            }
            if (combat != null)
            {
                combat.Attacked += OnAttacked;
                combat.SkillUsed += OnSkillUsed;
            }
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (player != null) player.RobotChanged -= OnRobotChanged;
            if (combat != null)
            {
                combat.Attacked -= OnAttacked;
                combat.SkillUsed -= OnSkillUsed;
            }
            if (health != null) health.Damaged -= OnDamaged;
        }

        private void OnRobotChanged(RobotType robot)
        {
            activeSprites = robot == RobotType.Techno ? strelSprites : samuraiSprites;
            ApplySprite();
        }

        private void ApplySprite()
        {
            if (body == null || activeSprites == null) return;
            Sprite sprite = activeSprites.Get(side);
            if (sprite != null) body.sprite = sprite;
        }

        private void OnDamaged(DamageInfo info, float dealt)
        {
            if (dealt > 0f) hurtTimer = HurtDuration;
        }

        private void LateUpdate()
        {
            if (player == null) return;
            side = ArtCatalog.SideFromDirection(player.FacingDirection, side);
            ApplySprite();
            if (body == null) return;
            if (hurtTimer > 0f)
            {
                hurtTimer -= Time.deltaTime;
                body.color = HurtTint;
            }
            else
            {
                body.color = Color.white;
            }
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
            Color color = robot == RobotType.Samurai ? new Color(1f, 0.8f, 0.3f, 0.85f) : new Color(0.3f, 0.9f, 1f, 0.85f);
            OneShotFx.Spawn(RuntimeSprites.Ring, (Vector3)origin, 0f, color, new Vector2(0.8f, 0.8f), new Vector2(diameter, diameter), 0.35f);
        }
    }
}
