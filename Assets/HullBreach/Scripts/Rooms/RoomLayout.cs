using System.Collections.Generic;
using UnityEngine;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.Rooms
{
    /// <summary>План одной комнаты корабля.</summary>
    public sealed class RoomPlan
    {
        public int Index;
        public RoomController Prefab;
        public RoomRole Role;
        public Vector3 Position;
        /// <summary>Индексы соседних комнат (двусторонние связи).</summary>
        public List<int> Connections = new List<int>();
    }

    /// <summary>План всего корабля: список комнат и связи между ними. Создаётся RoomGenerator без инстанцирования.</summary>
    public sealed class RoomLayout
    {
        public ShipDefinition Ship;
        public EntryPointType Entry;
        public int Seed;
        public List<RoomPlan> Rooms = new List<RoomPlan>();
    }
}
