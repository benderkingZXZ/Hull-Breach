using System;
using System.Collections.Generic;
using UnityEngine;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [Tooltip("Точка вылета снарядов/удара. Если пусто — центр объекта.")]
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileLifetime = 2.5f;
        [SerializeField, Range(30f, 360f)] private float meleeArcDegrees = 120f;
        [Tooltip("Space — атака, E — навык (только Legacy Input Manager).")]
        [SerializeField] private bool useKeyboardFallback = true;

        private PlayerController player;
        private readonly List<Health> buffer = new List<Health>(32);
        private bool attackHeld;
        private float nextAttackTime;
        private float skillReadyTime;
        private Health currentTarget;

        public event Action<Vector2, Vector2, RobotType> Attacked;
        public event Action<RobotType, Vector2> SkillUsed;
        public event Action<Health> TargetChanged;
        public event Action SkillButtonPressed;

        public bool IsAttackHeld { get { return attackHeld; } }
        public Health CurrentTarget { get { return currentTarget; } }
        public float SkillCooldownRemaining { get { return Mathf.Max(0f, skillReadyTime - Time.time); } }
        public float SkillCooldownNormalized
        {
            get
            {
                float cd = player != null ? player.Stats.SkillCooldown : 0f;
                return cd > 0f ? Mathf.Clamp01(SkillCooldownRemaining / cd) : 0f;
            }
        }

        private Vector2 Origin { get { return attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position; } }
        private bool CanAct { get { return player != null && player.InputEnabled && player.IsAlive; } }

        public void Configure(Projectile newProjectilePrefab, Transform newAttackOrigin)
        {
            projectilePrefab = newProjectilePrefab;
            attackOrigin = newAttackOrigin;
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void OnDisable()
        {
            attackHeld = false;
            SetTarget(null);
        }

        private void Update()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (!CanAct)
            {
                SetTarget(null);
                return;
            }

            bool held = attackHeld;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (useKeyboardFallback)
            {
                held |= Input.GetKey(KeyCode.Space);
                if (Input.GetKeyDown(KeyCode.E)) TryUseSkill();
            }
#endif
            Health target = TargetRegistry.FindNearestEnemy(Origin, player.Stats.AttackRange);
            SetTarget(target);
            if (target != null) player.SetFacing((Vector2)target.transform.position - Origin);

            if (held) TryAttack();
        }

        private void SetTarget(Health target)
        {
            if (currentTarget == target) return;
            currentTarget = target;
            TargetChanged?.Invoke(target);
        }

        public void SetAttackHeld(bool held)
        {
            attackHeld = held;
        }

        public bool TryAttack()
        {
            if (!CanAct || Time.time < nextAttackTime) return false;

            PlayerStats stats = player.Stats;
            Vector2 origin = Origin;
            Vector2 dir = player.FacingDirection;

            Health target = TargetRegistry.FindNearestEnemy(origin, stats.AttackRange);
            if (target != null)
            {
                Vector2 toTarget = (Vector2)target.transform.position - origin;
                if (toTarget.sqrMagnitude > 0.0001f) dir = toTarget.normalized;
                player.SetFacing(dir);
            }

            if (player.RobotType == RobotType.Samurai) PerformMeleeAttack(origin, dir, stats);
            else PerformRangedAttack(origin, dir, stats);

            nextAttackTime = Time.time + stats.AttackInterval;
            Attacked?.Invoke(origin, dir, player.RobotType);
            return true;
        }

        private void PerformMeleeAttack(Vector2 origin, Vector2 dir, PlayerStats stats)
        {
            buffer.Clear();
            TargetRegistry.CollectEnemiesInRadius(origin, stats.AttackRange, buffer);
            float halfArc = meleeArcDegrees * 0.5f;
            for (int i = 0; i < buffer.Count; i++)
            {
                Health h = buffer[i];
                if (h == null || h.IsDead) continue;
                Vector2 to = (Vector2)h.transform.position - origin;
                if (Vector2.Angle(dir, to) > halfArc) continue;
                h.TakeDamage(new DamageInfo(stats.Damage, gameObject, to.normalized));
            }
        }

        private void PerformRangedAttack(Vector2 origin, Vector2 dir, PlayerStats stats)
        {
            Projectile.Spawn(projectilePrefab, origin, dir, projectileSpeed, stats.Damage, Faction.Player, gameObject, projectileLifetime);
        }

        public bool TryUseSkill()
        {
            if (!CanAct) return false;

            SkillButtonPressed?.Invoke();

            if (Time.time < skillReadyTime) return false;

            PlayerStats stats = player.Stats;
            RobotDefinition def = RobotDefinitions.Get(player.RobotType);
            Vector2 origin = Origin;

            if (player.RobotType == RobotType.Samurai)
            {
                buffer.Clear();
                TargetRegistry.CollectEnemiesInRadius(origin, def.SkillRadius, buffer);
                float dmg = stats.Damage * def.SkillDamageMultiplier;
                for (int i = 0; i < buffer.Count; i++)
                {
                    Health h = buffer[i];
                    if (h == null || h.IsDead) continue;
                    Vector2 to = (Vector2)h.transform.position - origin;
                    h.TakeDamage(new DamageInfo(dmg, gameObject, to.normalized));
                }
            }
            else
            {
                int count = Mathf.Max(1, def.SkillProjectileCount);
                float dmg = stats.Damage * def.SkillDamageMultiplier;
                for (int i = 0; i < count; i++)
                {
                    float angle = (360f / count) * i * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Projectile.Spawn(projectilePrefab, origin, dir, projectileSpeed, dmg, Faction.Player, gameObject, projectileLifetime);
                }
            }

            skillReadyTime = Time.time + stats.SkillCooldown;
            SkillUsed?.Invoke(player.RobotType, origin);
            return true;
        }

        public void ResetCooldowns()
        {
            nextAttackTime = 0f;
            skillReadyTime = 0f;
        }
    }
}
