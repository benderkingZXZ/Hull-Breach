using System.Collections.Generic;
using UnityEngine;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.Rooms
{
    public sealed class RoomPlan
    {
        public int Index;
        public RoomController Prefab;
        public RoomRole Role;
        public Vector3 Position;
        public List<int> Connections = new List<int>();
    }

    public sealed class RoomLayout
    {
        public ShipDefinition Ship;
        public EntryPointType Entry;
        public int Seed;
        public List<RoomPlan> Rooms = new List<RoomPlan>();
    }
}
