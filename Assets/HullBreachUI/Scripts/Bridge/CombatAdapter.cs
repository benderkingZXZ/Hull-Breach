using System;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Player;
using HullBreach.Rooms;
using HullBreach.RunFlow;
using HullBreach.UI.Contracts;
using UnityEngine;

namespace HullBreach.UI.Bridge
{
    /// <summary>
    /// ICombatFeed + ICombatInputSink поверх Health / PlayerController / PlayerCombat / ShipRunManager.
    /// Подписывается на события бэкенда в конструкторе и отписывается в Dispose.
    /// Игрок может быть неактивен вне боя — это не мешает подпискам и опросу.
    /// </summary>
    public sealed class CombatAdapter : ICombatFeed, ICombatInputSink, IDisposable
    {
        private readonly ShipRunManager run;
        private readonly PlayerController player;
        private readonly Health health;
        private readonly PlayerCombat combat;
        private Health currentTarget;
        private bool disposed;

        public CombatAdapter(ShipRunManager run, PlayerController player, PlayerCombat combat)
        {
            this.run = run;
            this.player = player;
            this.combat = combat;
            health = player.Health;

            health.HealthChanged += OnHealthChanged;
            health.Damaged += OnDamaged;
            combat.SkillUsed += OnSkillUsed;
            combat.TargetChanged += OnTargetChanged;
            player.RobotChanged += OnRobotChanged;
            run.ShipStarted += OnShipStarted;
            run.RoomEntered += OnRoomEntered;
            run.RoomCleared += OnRoomCleared;
            run.ResourcesEarned += OnResourcesEarned;
            run.ShipFailed += OnShipFailed;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
                health.Damaged -= OnDamaged;
            }
            if (combat != null)
            {
                combat.SkillUsed -= OnSkillUsed;
                combat.TargetChanged -= OnTargetChanged;
            }
            if (player != null) player.RobotChanged -= OnRobotChanged;
            if (run != null)
            {
                run.ShipStarted -= OnShipStarted;
                run.RoomEntered -= OnRoomEntered;
                run.RoomCleared -= OnRoomCleared;
                run.ResourcesEarned -= OnResourcesEarned;
                run.ShipFailed -= OnShipFailed;
            }
        }

        // ------------------------------------------------------------------ ICombatFeed: состояние

        public float CurrentHealth { get { return health.CurrentHealth; } }
        public float MaxHealth { get { return health.MaxHealth; } }
        public float Shield { get { return health.ShieldAmount; } }
        public bool IsAlive { get { return !health.IsDead; } }
        public RobotType Robot { get { return player.RobotType; } }

        public float SkillCooldownNormalized { get { return combat.SkillCooldownNormalized; } }
        public float SkillCooldownRemaining { get { return combat.SkillCooldownRemaining; } }

        public bool IsRunning { get { return run.IsRunning; } }
        public int CurrentRoomIndex { get { return run.CurrentRoomIndex; } }
        public int TotalRooms { get { return run.TotalRooms; } }
        public int EnemiesRemaining { get { return run.EnemiesRemaining; } }

        public bool TryGetPlayerPosition(out Vector3 worldPosition)
        {
            if (player != null)
            {
                worldPosition = player.transform.position;
                return true;
            }
            worldPosition = Vector3.zero;
            return false;
        }

        public bool TryGetTargetPosition(out Vector3 worldPosition)
        {
            if (currentTarget != null && !currentTarget.IsDead)
            {
                worldPosition = currentTarget.transform.position;
                return true;
            }
            worldPosition = Vector3.zero;
            return false;
        }

        // ------------------------------------------------------------------ ICombatFeed: события

        public event Action<float, float> HealthChanged;
        public event Action<DamageEvent> PlayerDamaged;
        public event Action SkillUsed;
        public event Action<RobotType> RobotChanged;
        public event Action<ShipDefinition, EntryPointType> ShipStarted;
        public event Action<RoomInfo> RoomEntered;
        public event Action<int> RoomCleared;
        public event Action<int, Vector3> ResourcesEarned;
        public event Action ShipFailed;

        private void OnHealthChanged(float current, float max)
        {
            Action<float, float> h = HealthChanged;
            if (h != null) h(current, max);
        }

        private void OnDamaged(DamageInfo info, float dealt)
        {
            Action<DamageEvent> h = PlayerDamaged;
            if (h != null) h(new DamageEvent(dealt));
        }

        private void OnSkillUsed(RobotType robot, Vector2 direction)
        {
            Action h = SkillUsed;
            if (h != null) h();
        }

        private void OnTargetChanged(Health target)
        {
            currentTarget = target;
        }

        private void OnRobotChanged(RobotType robot)
        {
            Action<RobotType> h = RobotChanged;
            if (h != null) h(robot);
        }

        private void OnShipStarted(ShipDefinition ship, EntryPointType entry)
        {
            Action<ShipDefinition, EntryPointType> h = ShipStarted;
            if (h != null) h(ship, entry);
        }

        private void OnRoomEntered(int index, int total, RoomController room)
        {
            RoomRole role = room != null ? room.Role : RoomRole.Normal;
            Action<RoomInfo> h = RoomEntered;
            if (h != null) h(new RoomInfo(index, total, role));
        }

        private void OnRoomCleared(int index)
        {
            Action<int> h = RoomCleared;
            if (h != null) h(index);
        }

        private void OnResourcesEarned(int amount, Vector3 position)
        {
            Action<int, Vector3> h = ResourcesEarned;
            if (h != null) h(amount, position);
        }

        private void OnShipFailed(ShipFailureInfo info)
        {
            Action h = ShipFailed;
            if (h != null) h();
        }

        // ------------------------------------------------------------------ ICombatInputSink

        public void SetMoveInput(Vector2 direction) { if (player != null) player.SetMoveInput(direction); }
        public void SetAttackHeld(bool held) { if (combat != null) combat.SetAttackHeld(held); }
        public bool TryAttack() { return combat != null && combat.TryAttack(); }
        public bool TryUseSkill() { return combat != null && combat.TryUseSkill(); }
    }
}
