using System;
using UnityEngine;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.Player
{
    /// <summary>
    /// Движение, смена робота и состояние игрока. Не содержит UI-логики.
    /// UI вызывает SetMoveInput(Vector2) из виртуального стика.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private RobotType robotType = RobotType.Samurai;
        [Tooltip("WASD/стрелки в Editor (работает только при включённом Legacy Input Manager).")]
        [SerializeField] private bool useKeyboardFallback = true;

        private Rigidbody2D body;
        private Health health;
        private PlayerStats stats;
        private Vector2 moveInput;
        private Vector2 keyboardInput;
        private bool inputEnabled = true;
        private bool healthHooked;

        public event Action<RobotType> RobotChanged;
        public event Action<PlayerStats> StatsApplied;
        /// <summary>Робот погиб (дублирует Health.Died для удобства).</summary>
        public event Action Died;

        public Health Health { get { EnsureComponents(); return health; } }
        public RobotType RobotType { get { return robotType; } }
        public PlayerStats Stats
        {
            get
            {
                if (stats == null) stats = PlayerStatsCalculator.Build(robotType, 0f, 1f, default(StatModifiers));
                return stats;
            }
        }
        public bool IsAlive { get { return !Health.IsDead; } }
        public bool InputEnabled { get { return inputEnabled; } }
        public Vector2 FacingDirection { get; private set; } = Vector2.right;

        private void Awake()
        {
            EnsureComponents();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            if (health.Faction != Faction.Player)
                Debug.LogWarning("[HullBreach] Health на игроке должен иметь Faction = Player.", this);
        }

        private void OnDestroy()
        {
            if (health != null && healthHooked) health.Died -= HandleHealthDied;
        }

        private void EnsureComponents()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (!healthHooked && health != null)
            {
                health.Died += HandleHealthDied;
                healthHooked = true;
            }
        }

        private void HandleHealthDied(Health h, DamageInfo info)
        {
            moveInput = Vector2.zero;
            Died?.Invoke();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (useKeyboardFallback)
                keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        private void FixedUpdate()
        {
            EnsureComponents();
            if (!inputEnabled || health.IsDead) return;

            Vector2 input = moveInput;
            if (keyboardInput.sqrMagnitude > 0.01f) input = keyboardInput;
            if (input.sqrMagnitude > 1f) input.Normalize();
            if (input.sqrMagnitude < 0.0004f) return;

            body.MovePosition(body.position + input * Stats.MoveSpeed * Time.fixedDeltaTime);
            FacingDirection = input.normalized;
        }

        /// <summary>Вектор движения от виртуального стика; длина > 1 обрезается.</summary>
        public void SetMoveInput(Vector2 direction)
        {
            moveInput = direction;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                moveInput = Vector2.zero;
                keyboardInput = Vector2.zero;
            }
        }

        /// <summary>Направление взгляда/атаки (используется автонаведением).</summary>
        public void SetFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f) FacingDirection = direction.normalized;
        }

        /// <summary>Применяет робота и рассчитанные характеристики.</summary>
        public void ApplyRobot(RobotType type, PlayerStats newStats, bool keepHealthRatio)
        {
            EnsureComponents();
            bool changed = robotType != type;
            robotType = type;
            stats = newStats;
            health.SetMaxHealth(newStats.MaxHealth, keepHealthRatio);
            if (changed) RobotChanged?.Invoke(type);
            StatsApplied?.Invoke(newStats);
        }

        public void TeleportTo(Vector3 position)
        {
            EnsureComponents();
            moveInput = Vector2.zero;
            body.position = position;
            transform.position = position;
        }

        /// <summary>Подготовка к новому кораблю: перенос в позицию, оживление мёртвого, опционально полное лечение.</summary>
        public void ResetForShip(Vector3 position, bool restoreFullHealth)
        {
            EnsureComponents();
            if (health.IsDead || restoreFullHealth) health.ResetToFull();
            TeleportTo(position);
        }
    }
}
