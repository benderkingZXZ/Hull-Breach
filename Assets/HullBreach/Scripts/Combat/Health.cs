using System;
using UnityEngine;
using HullBreach.Core;

namespace HullBreach.Combat
{
    /// <summary>
    /// Здоровье, щит, неуязвимость и смерть. Общий компонент для игрока и врагов.
    /// Расчёт урона: итог = Amount * (1 - damageReduction); сначала поглощается щитом.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.Enemy;
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Секунды неуязвимости после получения урона (для игрока ~0.3).")]
        [SerializeField] private float invulnerabilityAfterHit = 0f;

        private float current;
        private bool initialized;
        private float shieldAmount;
        private float shieldEndTime;
        private float invulnerableUntil;
        private float damageReduction;

        /// <summary>(текущее, максимум).</summary>
        public event Action<float, float> HealthChanged;
        /// <summary>(информация об уроне, фактически нанесённый урон после щита/снижения).</summary>
        public event Action<DamageInfo, float> Damaged;
        public event Action<Health, DamageInfo> Died;

        public Faction Faction { get { return faction; } }
        public float MaxHealth { get { return maxHealth; } }
        public float CurrentHealth { get { EnsureInitialized(); return current; } }
        public float Normalized { get { return maxHealth > 0f ? CurrentHealth / maxHealth : 0f; } }
        public bool IsDead { get; private set; }
        /// <summary>Полная неуязвимость (например, на время анимации).</summary>
        public bool IsInvulnerable { get; set; }
        public float ShieldAmount { get { return Time.time < shieldEndTime ? shieldAmount : 0f; } }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (faction == Faction.Enemy && !IsDead) TargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            TargetRegistry.Unregister(this);
        }

        /// <summary>
        /// Программная настройка вместо инспектора (процедурная сборка объектов, см. Assets/HullBreachGame).
        /// Вызывать до активации объекта.
        /// </summary>
        public void Configure(Faction newFaction, float newMaxHealth, float newInvulnerabilityAfterHit)
        {
            faction = newFaction;
            maxHealth = Mathf.Max(1f, newMaxHealth);
            invulnerabilityAfterHit = Mathf.Max(0f, newInvulnerabilityAfterHit);
            initialized = false;
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            current = maxHealth;
        }

        /// <summary>Применяет урон. Возвращает фактически нанесённый урон.</summary>
        public float TakeDamage(DamageInfo info)
        {
            EnsureInitialized();
            if (IsDead || info.Amount <= 0f) return 0f;
            if (!info.IgnoreInvulnerability && (IsInvulnerable || Time.time < invulnerableUntil)) return 0f;

            float amount = info.Amount * (1f - Mathf.Clamp01(damageReduction));

            if (shieldAmount > 0f && Time.time < shieldEndTime)
            {
                float absorbed = Mathf.Min(shieldAmount, amount);
                shieldAmount -= absorbed;
                amount -= absorbed;
            }

            if (amount <= 0f)
            {
                Damaged?.Invoke(info, 0f);
                return 0f;
            }

            current = Mathf.Max(0f, current - amount);
            if (invulnerabilityAfterHit > 0f) invulnerableUntil = Time.time + invulnerabilityAfterHit;

            Damaged?.Invoke(info, amount);
            HealthChanged?.Invoke(current, maxHealth);

            if (current <= 0f) Die(info);
            return amount;
        }

        public void Heal(float amount)
        {
            EnsureInitialized();
            if (IsDead || amount <= 0f) return;
            current = Mathf.Min(maxHealth, current + amount);
            HealthChanged?.Invoke(current, maxHealth);
        }

        /// <summary>Мгновенное убийство (самоуничтожение камикадзе и т.п.).</summary>
        public void Kill(GameObject source)
        {
            EnsureInitialized();
            if (IsDead) return;
            current = 0f;
            HealthChanged?.Invoke(current, maxHealth);
            Die(new DamageInfo(0f, source, Vector2.zero, true));
        }

        public void SetMaxHealth(float newMax, bool keepRatio)
        {
            EnsureInitialized();
            newMax = Mathf.Max(1f, newMax);
            float ratio = maxHealth > 0f ? current / maxHealth : 1f;
            maxHealth = newMax;
            current = keepRatio ? Mathf.Clamp(ratio * maxHealth, 0f, maxHealth) : Mathf.Min(current, maxHealth);
            HealthChanged?.Invoke(current, maxHealth);
        }

        /// <summary>Воскрешение / полное восстановление. Сбрасывает щит и неуязвимость.</summary>
        public void ResetToFull()
        {
            EnsureInitialized();
            IsDead = false;
            current = maxHealth;
            shieldAmount = 0f;
            shieldEndTime = 0f;
            invulnerableUntil = 0f;
            if (faction == Faction.Enemy && isActiveAndEnabled) TargetRegistry.Register(this);
            HealthChanged?.Invoke(current, maxHealth);
        }

        /// <summary>Поглощающий щит на ограниченное время (заменяет предыдущий).</summary>
        public void AddShield(float amount, float durationSeconds)
        {
            if (amount <= 0f || durationSeconds <= 0f) return;
            shieldAmount = amount;
            shieldEndTime = Time.time + durationSeconds;
        }

        /// <summary>Доля снижения урона 0..1.</summary>
        public void SetDamageReduction(float fraction)
        {
            damageReduction = Mathf.Clamp01(fraction);
        }

        private void Die(DamageInfo info)
        {
            IsDead = true;
            TargetRegistry.Unregister(this);
            Died?.Invoke(this, info);
        }
    }
}
