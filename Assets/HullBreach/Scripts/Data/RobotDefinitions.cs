using HullBreach.Core;

namespace HullBreach.Data
{
    public sealed class RobotDefinition
    {
        public RobotType Type { get; }
        public string DisplayName { get; }
        public float BaseHealth { get; }
        public float BaseDamage { get; }
        public float MoveSpeed { get; }
        public float AttackInterval { get; }
        public float AttackRange { get; }
        public bool IsRanged { get; }
        public string SkillName { get; }
        public float SkillCooldown { get; }
        public float SkillDamageMultiplier { get; }
        public float SkillRadius { get; }
        public int SkillProjectileCount { get; }

        public RobotDefinition(RobotType type, string displayName, float baseHealth, float baseDamage, float moveSpeed,
            float attackInterval, float attackRange, bool isRanged, string skillName, float skillCooldown,
            float skillDamageMultiplier, float skillRadius, int skillProjectileCount)
        {
            Type = type; DisplayName = displayName; BaseHealth = baseHealth; BaseDamage = baseDamage;
            MoveSpeed = moveSpeed; AttackInterval = attackInterval; AttackRange = attackRange; IsRanged = isRanged;
            SkillName = skillName; SkillCooldown = skillCooldown; SkillDamageMultiplier = skillDamageMultiplier;
            SkillRadius = skillRadius; SkillProjectileCount = skillProjectileCount;
        }
    }

    public static class RobotDefinitions
    {
        private static readonly RobotDefinition samurai = new RobotDefinition(
            RobotType.Samurai, "Сэм", 120f, 14f, 5.5f, 0.45f, 2.4f, false,
            "Буря клинков", 6f, 2.5f, 3.2f, 0);

        private static readonly RobotDefinition techno = new RobotDefinition(
            RobotType.Techno, "Стрел", 90f, 9f, 5.0f, 0.35f, 7.5f, true,
            "Перегруз-залп", 8f, 1.0f, 0f, 12);

        public static RobotDefinition Get(RobotType type)
        {
            return type == RobotType.Techno ? techno : samurai;
        }
    }
}
