using System;
using UnityEngine;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.Enemies
{
    public enum EnemyAiState { Inactive, Chasing, Attacking, Stunned, Dead }

    /// <summary>
    /// ИИ врага: поиск/преследование игрока, атаки, оглушение, смерть и награда.
    /// Поведение зависит от EnemyType:
    ///  Drone — быстрый ближний бой; Gunner — стрелок, держит дистанцию; Heavy — медленный, удар с замахом;
    ///  Exploder — камикадзе с запалом и взрывом по площади; MiniBoss — ближний бой + круговой залп.
    /// Движение — прямая линия к игроку (без навигационной сетки).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        private const float BossBurstInterval = 5f;
        private const int BossBurstProjectiles = 12;

        [SerializeField] private EnemyType enemyType = EnemyType.Drone;
        [Tooltip("Префаб вражеского снаряда (Gunner, MiniBoss). Если пусто — невидимый снаряд-заглушка.")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private bool overrideStats;
        [SerializeField] private EnemyStats customStats;
        [Tooltip("Задержка перед уничтожением объекта после смерти (для анимации/VFX).")]
        [SerializeField] private float destroyDelay = 0.6f;

        private Rigidbody2D body;
        private Health health;
        private EnemyStats stats;
        private Transform target;
        private Health targetHealth;
        private bool activated;
        private float activateTime;
        private float nextAttackTime;
        private float stunUntil;
        private bool pendingStrike;
        private float strikeTime;
        private float fuseEnd = -1f;
        private float nextBurstTime;

        /// <summary>Срабатывает один раз при смерти (после отключения коллизий).</summary>
        public event Action<EnemyController> Died;

        public EnemyType Type { get { return enemyType; } }
        public bool IsBoss { get { return enemyType == EnemyType.MiniBoss; } }
        public EnemyAiState State { get; private set; } = EnemyAiState.Inactive;
        public Health Health { get { return health; } }
        /// <summary>Награда ресурсами за убийство (уже с учётом множителя точки входа). 0, если враг взорвался сам.</summary>
        public int ResourceReward { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.right;

        /// <summary>Программная настройка вместо инспектора. Вызывать до активации объекта (до Awake).</summary>
        public void Configure(EnemyType type, Projectile newProjectilePrefab)
        {
            enemyType = type;
            projectilePrefab = newProjectilePrefab;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            stats = overrideStats ? customStats : EnemyDefaults.Get(enemyType);
            ApplyScaledStats(stats, 1f, 1f);
            health.Died += HandleHealthDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= HandleHealthDied;
        }

        private void ApplyScaledStats(EnemyStats baseStats, float difficulty, float resourceMultiplier)
        {
            stats = baseStats;
            stats.MaxHealth = baseStats.MaxHealth * difficulty;
            stats.Damage = baseStats.Damage * (1f + (difficulty - 1f) * 0.5f);
            health.SetMaxHealth(stats.MaxHealth, false);
            health.ResetToFull();
            int min = Mathf.Min(baseStats.ResourceMin, baseStats.ResourceMax);
            int max = Mathf.Max(baseStats.ResourceMin, baseStats.ResourceMax);
            ResourceReward = Mathf.RoundToInt(UnityEngine.Random.Range(min, max + 1) * resourceMultiplier);
        }

        /// <summary>Настройка врага перед активацией. difficulty множит здоровье (и частично урон).</summary>
        public void Initialize(Transform playerTarget, float difficulty, float resourceMultiplier)
        {
            target = playerTarget;
            targetHealth = playerTarget != null ? playerTarget.GetComponentInParent<Health>() : null;
            EnemyStats baseStats = overrideStats ? customStats : EnemyDefaults.Get(enemyType);
            ApplyScaledStats(baseStats, Mathf.Max(0.1f, difficulty), Mathf.Max(0f, resourceMultiplier));
        }

        /// <summary>Враг начинает действовать через delay секунд (время «появления»).</summary>
        public void Activate(float delay)
        {
            activated = true;
            activateTime = Time.time + Mathf.Max(0f, delay);
            nextAttackTime = activateTime + 0.3f;
            nextBurstTime = activateTime + BossBurstInterval * 0.6f;
        }

        public void ApplyStun(float duration)
        {
            if (State == EnemyAiState.Dead || duration <= 0f) return;
            stunUntil = Mathf.Max(stunUntil, Time.time + duration);
            pendingStrike = false;
            fuseEnd = -1f;
        }

        private void FixedUpdate()
        {
            if (State == EnemyAiState.Dead || !activated || Time.time < activateTime) return;
            if (target == null || targetHealth == null || targetHealth.IsDead) return;

            if (Time.time < stunUntil)
            {
                State = EnemyAiState.Stunned;
                return;
            }
            if (State == EnemyAiState.Inactive || State == EnemyAiState.Stunned) State = EnemyAiState.Chasing;

            Vector2 toTarget = (Vector2)target.position - body.position;
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.001f ? toTarget / dist : Vector2.zero;
            if (dir != Vector2.zero) FacingDirection = dir;

            switch (enemyType)
            {
                case EnemyType.Gunner:
                    TickRanged(dir, dist);
                    break;
                case EnemyType.Exploder:
                    TickExploder(dir, dist);
                    break;
                default: // Drone, Heavy, MiniBoss
                    TickMelee(dir, dist);
                    break;
            }

            if (enemyType == EnemyType.MiniBoss && State != EnemyAiState.Dead && Time.time >= nextBurstTime)
            {
                FireRing();
                nextBurstTime = Time.time + BossBurstInterval;
            }
        }

        private void Move(Vector2 direction)
        {
            body.MovePosition(body.position + direction * stats.MoveSpeed * Time.fixedDeltaTime);
        }

        private void TickMelee(Vector2 dir, float dist)
        {
            if (pendingStrike)
            {
                if (Time.time >= strikeTime)
                {
                    pendingStrike = false;
                    if (dist <= stats.AttackRange * 1.3f)
                        targetHealth.TakeDamage(new DamageInfo(stats.Damage, gameObject, dir));
                    nextAttackTime = Time.time + stats.AttackCooldown;
                    State = EnemyAiState.Chasing;
                }
                return;
            }

            if (dist > stats.AttackRange)
            {
                Move(dir);
                return;
            }

            if (Time.time >= nextAttackTime)
            {
                pendingStrike = true;
                strikeTime = Time.time + stats.AttackWindup;
                State = EnemyAiState.Attacking;
            }
        }

        private void TickRanged(Vector2 dir, float dist)
        {
            if (dist > stats.PreferredDistance + 0.75f) Move(dir);
            else if (dist < stats.PreferredDistance - 1.5f) Move(-dir);

            if (dist <= stats.AttackRange && Time.time >= nextAttackTime)
            {
                Projectile.Spawn(projectilePrefab, body.position, dir, stats.ProjectileSpeed, stats.Damage,
                    Faction.Enemy, gameObject, 4f);
                nextAttackTime = Time.time + stats.AttackCooldown;
            }
        }

        private void TickExploder(Vector2 dir, float dist)
        {
            if (fuseEnd < 0f)
            {
                if (dist > stats.AttackRange)
                {
                    Move(dir);
                    return;
                }
                fuseEnd = Time.time + stats.FuseTime;
                State = EnemyAiState.Attacking;
                return;
            }

            if (Time.time >= fuseEnd) Explode();
        }

        private void Explode()
        {
            float dist = Vector2.Distance(body.position, target.position);
            if (dist <= stats.ExplosionRadius)
                targetHealth.TakeDamage(new DamageInfo(stats.Damage, gameObject, FacingDirection));
            ResourceReward = 0;
            health.Kill(gameObject);
        }

        private void FireRing()
        {
            float dmg = stats.Damage * 0.6f;
            for (int i = 0; i < BossBurstProjectiles; i++)
            {
                float angle = (360f / BossBurstProjectiles) * i * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Projectile.Spawn(projectilePrefab, body.position, d, stats.ProjectileSpeed, dmg, Faction.Enemy, gameObject, 4f);
            }
        }

        private void HandleHealthDied(Health h, DamageInfo info)
        {
            if (State == EnemyAiState.Dead) return;
            State = EnemyAiState.Dead;
            pendingStrike = false;
            body.simulated = false;   // отключает коллайдеры и физику
            Died?.Invoke(this);
            Destroy(gameObject, destroyDelay);
        }
    }
}
