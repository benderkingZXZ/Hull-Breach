using System;
using HullBreach.Core;
using HullBreach.Data;
using UnityEngine;

namespace HullBreach.UI.Contracts
{
    /// <summary>Комната, в которую вошёл игрок.</summary>
    public readonly struct RoomInfo
    {
        public readonly int Index;     // с нуля
        public readonly int Total;
        public readonly RoomRole Role;

        public RoomInfo(int index, int total, RoomRole role)
        {
            Index = index; Total = total; Role = role;
        }
    }

    /// <summary>Урон, полученный роботом. Amount — фактический (после щита); 0 = всё поглотил щит.</summary>
    public readonly struct DamageEvent
    {
        public readonly float Amount;
        public bool Absorbed { get { return Amount <= 0f; } }

        public DamageEvent(float amount)
        {
            Amount = amount;
        }
    }

    /// <summary>
    /// Данные боя для HUD (только чтение + события). Реализация: Bridge/CombatAdapter.
    /// Метод Poll-свойств (щит, перезарядка, враги) HUD опрашивает каждый кадр — событий для них в API v1.0 нет.
    /// </summary>
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

        event Action<float, float> HealthChanged;            // (текущее, максимум)
        event Action<DamageEvent> PlayerDamaged;
        event Action SkillUsed;
        event Action<RobotType> RobotChanged;
        event Action<ShipDefinition, EntryPointType> ShipStarted;
        event Action<RoomInfo> RoomEntered;
        event Action<int> RoomCleared;                       // индекс комнаты
        event Action<int, Vector3> ResourcesEarned;          // (сумма, мировая позиция)
        event Action ShipFailed;
    }
}
