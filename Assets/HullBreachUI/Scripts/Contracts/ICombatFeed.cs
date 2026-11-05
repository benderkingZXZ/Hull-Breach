using System;
using HullBreach.Core;
using HullBreach.Data;
using UnityEngine;

namespace HullBreach.UI.Contracts
{
    public readonly struct RoomInfo
    {
        public readonly int Index;
        public readonly int Total;
        public readonly RoomRole Role;

        public RoomInfo(int index, int total, RoomRole role)
        {
            Index = index; Total = total; Role = role;
        }
    }

    public readonly struct DamageEvent
    {
        public readonly float Amount;
        public bool Absorbed { get { return Amount <= 0f; } }

        public DamageEvent(float amount)
        {
            Amount = amount;
        }
    }

    public interface ICombatFeed
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        float Shield { get; }
        bool IsAlive { get; }
        RobotType Robot { get; }

        float SkillCooldownNormalized { get; }
        float SkillCooldownRemaining { get; }

        bool IsRunning { get; }
        int CurrentRoomIndex { get; }
        int TotalRooms { get; }
        int EnemiesRemaining { get; }

        bool TryGetPlayerPosition(out Vector3 worldPosition);
        bool TryGetTargetPosition(out Vector3 worldPosition);

        event Action<float, float> HealthChanged;
        event Action<DamageEvent> PlayerDamaged;
        event Action SkillUsed;
        event Action<RobotType> RobotChanged;
        event Action<ShipDefinition, EntryPointType> ShipStarted;
        event Action<RoomInfo> RoomEntered;
        event Action<int> RoomCleared;
        event Action<int, Vector3> ResourcesEarned;
        event Action ShipFailed;
    }
}
