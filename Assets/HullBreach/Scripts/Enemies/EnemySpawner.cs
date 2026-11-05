using System;
using System.Collections.Generic;
using UnityEngine;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Rooms;

namespace HullBreach.Enemies
{
    [Serializable]
    public class EnemyPrefabEntry
    {
        public EnemyType type;
        public bool finalShipBoss;
        public EnemyController prefab;
    }

    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyPrefabEntry[] enemyPrefabs;
        [SerializeField] private float spawnActivationDelay = 0.8f;
        [SerializeField] private float spawnJitter = 0.5f;

        private static readonly EnemyType[] RegularTypes =
            { EnemyType.Drone, EnemyType.Gunner, EnemyType.Heavy, EnemyType.Exploder };

        public void Configure(EnemyPrefabEntry[] entries, float activationDelay)
        {
            enemyPrefabs = entries;
            spawnActivationDelay = Mathf.Max(0f, activationDelay);
        }

        public bool HasPrefabFor(EnemyType type)
        {
            return FindPrefab(type, false) != null;
        }

        private EnemyController FindPrefab(EnemyType type, bool finalShipBoss)
        {
            if (enemyPrefabs == null) return null;
            EnemyController fallback = null;
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                EnemyPrefabEntry entry = enemyPrefabs[i];
                if (entry == null || entry.type != type || entry.prefab == null) continue;
                if (entry.finalShipBoss == finalShipBoss) return entry.prefab;
                if (!entry.finalShipBoss) fallback = entry.prefab;
            }
            return fallback;
        }

        public int SpawnForRoom(RoomController room, RoomSpawnContext ctx, Transform target, System.Random rng)
        {
            if (room == null || ctx == null || ctx.Ship == null || ctx.Profile == null) return 0;

            bool finalBoss = ctx.Ship.IsBossShip;
            float difficulty = ctx.Ship.DifficultyMultiplier * ctx.Profile.EnemyStrengthMultiplier * (1f + 0.05f * ctx.RoomIndex);
            float resourceMultiplier = ctx.Profile.ResourceMultiplier;

            int baseCount = 2 + ctx.Ship.Tier + ctx.RoomIndex / 2;
            int count = Mathf.Max(1, Mathf.RoundToInt(baseCount * ctx.Profile.EnemyCountMultiplier));
            int spawned = 0;

            if (ctx.IsReactorRoom)
            {
                if (SpawnOne(room, EnemyType.MiniBoss, room.BossSpawnPosition, target, difficulty, resourceMultiplier, finalBoss)) spawned++;
                count = Mathf.Max(2, count / 2);
            }

            for (int i = 0; i < count; i++)
            {
                EnemyType type = RollType(ctx.Profile, rng);
                Vector3 pos = GetSpawnPosition(room, i, rng);
                if (SpawnOne(room, type, pos, target, difficulty, resourceMultiplier, finalBoss)) spawned++;
            }
            return spawned;
        }

        private bool SpawnOne(RoomController room, EnemyType type, Vector3 position, Transform target,
            float difficulty, float resourceMultiplier, bool finalBoss)
        {
            EnemyController prefab = FindPrefab(type, finalBoss);
            if (prefab == null)
            {
                Debug.LogError("[HullBreach] EnemySpawner: не назначен префаб для " + type, this);
                return false;
            }
            EnemyController enemy = Instantiate(prefab, position, Quaternion.identity, room.transform);
            enemy.Initialize(target, difficulty, resourceMultiplier);
            room.RegisterEnemy(enemy);
            enemy.Activate(spawnActivationDelay);
            return true;
        }

        private EnemyType RollType(EntryPointProfile profile, System.Random rng)
        {
            float total = 0f;
            for (int i = 0; i < RegularTypes.Length; i++) total += profile.GetEnemyWeight(RegularTypes[i]);
            if (total <= 0f) return EnemyType.Drone;

            float roll = (float)(rng.NextDouble() * total);
            for (int i = 0; i < RegularTypes.Length; i++)
            {
                roll -= profile.GetEnemyWeight(RegularTypes[i]);
                if (roll <= 0f) return RegularTypes[i];
            }
            return RegularTypes[RegularTypes.Length - 1];
        }

        private Vector3 GetSpawnPosition(RoomController room, int index, System.Random rng)
        {
            Vector3 jitter = new Vector3(
                (float)(rng.NextDouble() * 2.0 - 1.0) * spawnJitter,
                (float)(rng.NextDouble() * 2.0 - 1.0) * spawnJitter, 0f);

            IReadOnlyList<Transform> points = room.EnemySpawnPoints;
            if (points == null || points.Count == 0) return room.transform.position + jitter;

            Transform point = points[index % points.Count];
            Vector3 basePos = point != null ? point.position : room.transform.position;
            return basePos + jitter;
        }
    }
}
