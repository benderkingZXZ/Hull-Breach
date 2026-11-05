using System;
using System.Collections.Generic;
using UnityEngine;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.Rooms
{
    [Serializable]
    public class RoomTemplateSet
    {
        public EntryPointType entryPoint;
        [Tooltip("Шаблоны обычных комнат (RoomController с Role = Normal).")]
        public RoomController[] normalRooms;
        [Tooltip("Шаблоны финальной комнаты (RoomController с Role = Reactor).")]
        public RoomController[] reactorRooms;
    }

    public class RoomGenerator : MonoBehaviour
    {
        [SerializeField] private RoomTemplateSet[] templateSets;
        [SerializeField] private Transform roomsRoot;
        [Tooltip("Расстояние между центрами комнат по X (должно быть больше размера самой большой комнаты + запас).")]
        [SerializeField] private float roomSpacing = 60f;

        private readonly List<RoomController> spawnedRooms = new List<RoomController>();

        public IReadOnlyList<RoomController> SpawnedRooms { get { return spawnedRooms; } }

        public void Configure(RoomTemplateSet[] sets, Transform root, float spacing)
        {
            templateSets = sets;
            roomsRoot = root;
            roomSpacing = Mathf.Max(1f, spacing);
        }

        private RoomTemplateSet FindSet(EntryPointType entry)
        {
            if (templateSets == null) return null;
            for (int i = 0; i < templateSets.Length; i++)
                if (templateSets[i] != null && templateSets[i].entryPoint == entry) return templateSets[i];
            return null;
        }

        public RoomLayout GenerateLayout(ShipDefinition ship, EntryPointType entry, int seed)
        {
            RoomTemplateSet set = FindSet(entry);
            if (set == null)
            {
                Debug.LogError("[HullBreach] RoomGenerator: нет набора шаблонов для точки входа " + entry, this);
                return null;
            }

            EntryPointProfile profile = EntryPointCatalog.Get(entry);
            System.Random rng = new System.Random(seed);
            Vector3 origin = roomsRoot != null ? roomsRoot.position : transform.position;

            RoomLayout layout = new RoomLayout { Ship = ship, Entry = entry, Seed = seed };
            List<RoomController> used = new List<RoomController>();
            RoomController previous = null;

            for (int i = 0; i < profile.RoomCount; i++)
            {
                bool isLast = i == profile.RoomCount - 1;
                RoomController prefab = isLast
                    ? PickRandom(set.reactorRooms, rng)
                    : PickNormal(set.normalRooms, used, previous, rng);

                RoomPlan plan = new RoomPlan
                {
                    Index = i,
                    Prefab = prefab,
                    Role = isLast ? RoomRole.Reactor : RoomRole.Normal,
                    Position = origin + new Vector3(i * roomSpacing, 0f, 0f)
                };
                if (i > 0) plan.Connections.Add(i - 1);
                if (!isLast) plan.Connections.Add(i + 1);
                layout.Rooms.Add(plan);

                if (!isLast && prefab != null) { used.Add(prefab); previous = prefab; }
            }
            return layout;
        }

        private static RoomController PickRandom(RoomController[] pool, System.Random rng)
        {
            if (pool == null) return null;
            List<RoomController> candidates = new List<RoomController>();
            for (int i = 0; i < pool.Length; i++) if (pool[i] != null) candidates.Add(pool[i]);
            return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
        }

        private static RoomController PickNormal(RoomController[] pool, List<RoomController> used, RoomController previous, System.Random rng)
        {
            if (pool == null) return null;
            List<RoomController> candidates = new List<RoomController>();
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && !used.Contains(pool[i])) candidates.Add(pool[i]);
            if (candidates.Count == 0)
                for (int i = 0; i < pool.Length; i++)
                    if (pool[i] != null && pool[i] != previous) candidates.Add(pool[i]);
            if (candidates.Count == 0)
                for (int i = 0; i < pool.Length; i++)
                    if (pool[i] != null) candidates.Add(pool[i]);
            return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
        }

        public bool ValidateLayout(RoomLayout layout, out string error)
        {
            if (layout == null || layout.Rooms == null || layout.Rooms.Count == 0) { error = "План корабля пуст."; return false; }

            int expected = EntryPointCatalog.Get(layout.Entry).RoomCount;
            int n = layout.Rooms.Count;
            if (n != expected) { error = "Ожидалось комнат: " + expected + ", в плане: " + n; return false; }

            for (int i = 0; i < n; i++)
            {
                RoomPlan plan = layout.Rooms[i];
                if (plan.Index != i) { error = "Нарушен порядок индексов у комнаты " + i; return false; }
                if (plan.Prefab == null) { error = "Для комнаты " + i + " не найден шаблон (проверьте RoomTemplateSet)."; return false; }

                bool shouldBeReactor = i == n - 1;
                if ((plan.Role == RoomRole.Reactor) != shouldBeReactor) { error = "Роль комнаты " + i + " неверна."; return false; }
                if (plan.Prefab.Role != plan.Role) { error = "Шаблон " + plan.Prefab.name + " имеет роль " + plan.Prefab.Role + ", а нужна " + plan.Role; return false; }

                string roomError;
                if (!plan.Prefab.Validate(out roomError)) { error = roomError; return false; }

                if (!shouldBeReactor && !plan.Connections.Contains(i + 1)) { error = "Комната " + i + " не ведёт в следующую (тупик)."; return false; }
                for (int c = 0; c < plan.Connections.Count; c++)
                    if (plan.Connections[c] < 0 || plan.Connections[c] >= n) { error = "Связь за пределами плана у комнаты " + i; return false; }
            }

            bool[] visited = new bool[n];
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(0);
            visited[0] = true;
            int reached = 1;
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                List<int> links = layout.Rooms[cur].Connections;
                for (int k = 0; k < links.Count; k++)
                {
                    int next = links[k];
                    if (!visited[next]) { visited[next] = true; reached++; queue.Enqueue(next); }
                }
            }
            if (reached != n) { error = "Недостижимые комнаты: достигнуто " + reached + " из " + n; return false; }

            error = null;
            return true;
        }

        public List<RoomController> BuildRooms(RoomLayout layout)
        {
            ClearRooms();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                RoomPlan plan = layout.Rooms[i];
                RoomController room = Instantiate(plan.Prefab, plan.Position, Quaternion.identity, roomsRoot);
                room.name = "Room_" + i + "_" + plan.Prefab.name;
                room.Initialize(i);
                spawnedRooms.Add(room);
                if (i > 0) room.gameObject.SetActive(false);
            }
            return new List<RoomController>(spawnedRooms);
        }

        public void ClearRooms()
        {
            for (int i = 0; i < spawnedRooms.Count; i++)
                if (spawnedRooms[i] != null) Destroy(spawnedRooms[i].gameObject);
            spawnedRooms.Clear();
        }
    }
}
