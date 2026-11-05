using System.Collections.Generic;
using HullBreach.Core;

namespace HullBreach.Data
{
    public sealed class ShipCompletionResult
    {
        public ShipDefinition Ship { get; }
        public EntryPointType Entry { get; }
        public int ResourcesAwarded { get; }
        public int RoomsCleared { get; }
        public int EnemiesKilled { get; }
        public float DurationSeconds { get; }

        public ShipCompletionResult(ShipDefinition ship, EntryPointType entry, int resourcesAwarded, int roomsCleared, int enemiesKilled, float durationSeconds)
        {
            Ship = ship; Entry = entry; ResourcesAwarded = resourcesAwarded;
            RoomsCleared = roomsCleared; EnemiesKilled = enemiesKilled; DurationSeconds = durationSeconds;
        }
    }

    public sealed class ShipFailureInfo
    {
        public ShipDefinition Ship { get; }
        public EntryPointType Entry { get; }
        public int RoomIndex { get; }
        public int RoomsCleared { get; }
        public int EnemiesKilled { get; }

        public ShipFailureInfo(ShipDefinition ship, EntryPointType entry, int roomIndex, int roomsCleared, int enemiesKilled)
        {
            Ship = ship; Entry = entry; RoomIndex = roomIndex; RoomsCleared = roomsCleared; EnemiesKilled = enemiesKilled;
        }
    }

    public sealed class ShipRewardInfo
    {
        public ShipDefinition Ship { get; }
        public EntryPointType Entry { get; }
        public int ResourcesAwarded { get; }
        public IReadOnlyList<ModuleInstance> ModuleOffers { get; }

        public ShipRewardInfo(ShipDefinition ship, EntryPointType entry, int resourcesAwarded, IReadOnlyList<ModuleInstance> moduleOffers)
        {
            Ship = ship; Entry = entry; ResourcesAwarded = resourcesAwarded; ModuleOffers = moduleOffers;
        }
    }

    public sealed class RunSummary
    {
        public bool Won { get; }
        public RobotType Robot { get; }
        public int ShipsCaptured { get; }
        public int RoomsCleared { get; }
        public int EnemiesKilled { get; }
        public int ResourcesEarned { get; }
        public float DurationSeconds { get; }
        public IReadOnlyList<ModuleInstance> Modules { get; }

        public RunSummary(bool won, RobotType robot, int shipsCaptured, int roomsCleared, int enemiesKilled,
            int resourcesEarned, float durationSeconds, IReadOnlyList<ModuleInstance> modules)
        {
            Won = won; Robot = robot; ShipsCaptured = shipsCaptured; RoomsCleared = roomsCleared;
            EnemiesKilled = enemiesKilled; ResourcesEarned = resourcesEarned; DurationSeconds = durationSeconds; Modules = modules;
        }
    }

    public sealed class RoomSpawnContext
    {
        public ShipDefinition Ship { get; }
        public EntryPointProfile Profile { get; }
        public int RoomIndex { get; }
        public int TotalRooms { get; }
        public bool IsReactorRoom { get; }

        public RoomSpawnContext(ShipDefinition ship, EntryPointProfile profile, int roomIndex, int totalRooms, bool isReactorRoom)
        {
            Ship = ship; Profile = profile; RoomIndex = roomIndex; TotalRooms = totalRooms; IsReactorRoom = isReactorRoom;
        }
    }
}
