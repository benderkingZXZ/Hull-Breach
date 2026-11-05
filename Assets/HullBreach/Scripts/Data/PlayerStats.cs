using System;
using UnityEngine;
using HullBreach.Core;

namespace HullBreach.Data
{
    [Serializable]
    public struct StatModifiers
    {
        public float FlatHealth;
        public float DamageBonus;
        public float MoveSpeedBonus;
        public float AttackSpeedBonus;
    }

    public sealed class PlayerStats
    {
        public float MaxHealth { get; }
        public float Damage { get; }
        public float MoveSpeed { get; }
        public float AttackInterval { get; }
        public float AttackRange { get; }
        public float SkillCooldown { get; }

        public PlayerStats(float maxHealth, float damage, float moveSpeed, float attackInterval, float attackRange, float skillCooldown)
        {
            MaxHealth = maxHealth; Damage = damage; MoveSpeed = moveSpeed;
            AttackInterval = attackInterval; AttackRange = attackRange; SkillCooldown = skillCooldown;
        }
    }

    public static class PlayerStatsCalculator
    {
        public static PlayerStats Build(RobotType robot, float permanentBonusHealth, float permanentDamageMultiplier, StatModifiers modifiers)
        {
            RobotDefinition def = RobotDefinitions.Get(robot);
            float maxHealth = Mathf.Max(1f, def.BaseHealth + permanentBonusHealth + modifiers.FlatHealth);
            float damage = def.BaseDamage * Mathf.Max(0.01f, permanentDamageMultiplier) * (1f + modifiers.DamageBonus);
            float speed = def.MoveSpeed * (1f + modifiers.MoveSpeedBonus);
            float interval = Mathf.Max(0.1f, def.AttackInterval / (1f + modifiers.AttackSpeedBonus));
            return new PlayerStats(maxHealth, damage, speed, interval, def.AttackRange, def.SkillCooldown);
        }
    }
}
