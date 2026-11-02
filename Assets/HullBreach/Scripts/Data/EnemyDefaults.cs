using System;
using HullBreach.Core;

namespace HullBreach.Data
{
    /// <summary>Характеристики врага. Можно переопределить на префабе (EnemyController.overrideStats).</summary>
    [Serializable]
    public struct EnemyStats
    {
        public float MaxHealth;
        public float Damage;
        public float MoveSpeed;
        /// <summary>Дальность атаки (для Exploder — расстояние запуска запала).</summary>
        public float AttackRange;
        public float AttackCooldown;
        /// <summary>Время замаха перед ударом ближнего боя.</summary>
        public float AttackWindup;
        /// <summary>Желаемая дистанция для стрелка.</summary>
        public float PreferredDistance;
        public float ProjectileSpeed;
        public float ExplosionRadius;
        public float FuseTime;
        public int ResourceMin;
        public int ResourceMax;
    }

    public static class EnemyDefaults
    {
        public static EnemyStats Get(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Drone:
                    // Быстрый хрупкий преследователь
                    return new EnemyStats { MaxHealth = 20f, Damage = 6f, MoveSpeed = 3.8f, AttackRange = 0.9f, AttackCooldown = 0.9f,
                        AttackWindup = 0.1f, ResourceMin = 2, ResourceMax = 4 };
                case EnemyType.Gunner:
                    // Стрелок: держит дистанцию
                    return new EnemyStats { MaxHealth = 28f, Damage = 8f, MoveSpeed = 2.2f, AttackRange = 7f, AttackCooldown = 1.6f,
                        PreferredDistance = 5f, ProjectileSpeed = 7f, ResourceMin = 4, ResourceMax = 7 };
                case EnemyType.Heavy:
                    // Медленный танк с замахом
                    return new EnemyStats { MaxHealth = 90f, Damage = 18f, MoveSpeed = 1.8f, AttackRange = 1.4f, AttackCooldown = 1.8f,
                        AttackWindup = 0.5f, ResourceMin = 8, ResourceMax = 12 };
                case EnemyType.Exploder:
                    // Камикадзе: подбегает, запал, взрыв по площади
                    return new EnemyStats { MaxHealth = 14f, Damage = 30f, MoveSpeed = 4.2f, AttackRange = 1.2f, AttackCooldown = 0f,
                        ExplosionRadius = 2.2f, FuseTime = 0.6f, ResourceMin = 3, ResourceMax = 5 };
                case EnemyType.MiniBoss:
                default:
                    // Мини-босс: ближний бой + круговой залп каждые ~5 с
                    return new EnemyStats { MaxHealth = 400f, Damage = 20f, MoveSpeed = 2.4f, AttackRange = 1.8f, AttackCooldown = 1.4f,
                        AttackWindup = 0.4f, ProjectileSpeed = 6f, ResourceMin = 60, ResourceMax = 80 };
            }
        }
    }
}
