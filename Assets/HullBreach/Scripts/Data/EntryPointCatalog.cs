using System.Collections.Generic;
using HullBreach.Core;

namespace HullBreach.Data
{
    /// <summary>Параметры одной из трёх точек входа (риск — награда).</summary>
    public sealed class EntryPointProfile
    {
        private readonly float[] enemyWeights;

        public EntryPointType Entry { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int RoomCount { get; }
        public float EnemyCountMultiplier { get; }
        public float EnemyStrengthMultiplier { get; }
        public float ResourceMultiplier { get; }
        /// <summary>Сдвиг шансов редкости модуля (0..1): доля «веса» обычных, переходящая в редкие/эпические.</summary>
        public float RareModuleBonus { get; }

        public EntryPointProfile(EntryPointType entry, string displayName, string description, int roomCount,
            float enemyCountMultiplier, float enemyStrengthMultiplier, float resourceMultiplier, float rareModuleBonus,
            float weightDrone, float weightGunner, float weightHeavy, float weightExploder)
        {
            Entry = entry;
            DisplayName = displayName;
            Description = description;
            RoomCount = roomCount;
            EnemyCountMultiplier = enemyCountMultiplier;
            EnemyStrengthMultiplier = enemyStrengthMultiplier;
            ResourceMultiplier = resourceMultiplier;
            RareModuleBonus = rareModuleBonus;
            enemyWeights = new[] { weightDrone, weightGunner, weightHeavy, weightExploder };
        }

        /// <summary>Вес типа врага в таблице спавна (MiniBoss всегда 0 — он ставится отдельно).</summary>
        public float GetEnemyWeight(EnemyType type)
        {
            int i = (int)type;
            return (i >= 0 && i < enemyWeights.Length) ? enemyWeights[i] : 0f;
        }
    }

    public static class EntryPointCatalog
    {
        private static readonly EntryPointProfile[] all =
        {
            new EntryPointProfile(EntryPointType.EngineRoom, "Машинное отделение",
                "Короткий маршрут: 3 комнаты, но очень много врагов.",
                3, 1.6f, 1.0f, 1.0f, 0f, 4f, 2f, 1f, 3f),
            new EntryPointProfile(EntryPointType.CargoHold, "Грузовой трюм",
                "Длинный маршрут: 5 комнат, больше ресурсов.",
                5, 1.0f, 0.9f, 1.6f, 0f, 3f, 2f, 2f, 1f),
            new EntryPointProfile(EntryPointType.Bridge, "Мостик",
                "4 комнаты, сильные враги, есть шанс на редкий модуль.",
                4, 0.9f, 1.35f, 1.0f, 0.25f, 1f, 4f, 3f, 1f),
        };

        public static IReadOnlyList<EntryPointProfile> All => all;

        public static EntryPointProfile Get(EntryPointType type)
        {
            for (int i = 0; i < all.Length; i++)
                if (all[i].Entry == type) return all[i];
            return all[0];
        }
    }
}
