using System.Collections.Generic;
using UnityEngine;

namespace HullBreach.Combat
{
    public static class TargetRegistry
    {
        private static readonly List<Health> enemies = new List<Health>(64);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            enemies.Clear();
        }

        public static IReadOnlyList<Health> Enemies { get { return enemies; } }

        internal static void Register(Health h)
        {
            if (h != null && !enemies.Contains(h)) enemies.Add(h);
        }

        internal static void Unregister(Health h)
        {
            enemies.Remove(h);
        }

        public static Health FindNearestEnemy(Vector2 origin, float maxRange)
        {
            Health best = null;
            float bestSqr = maxRange * maxRange;
            for (int i = 0; i < enemies.Count; i++)
            {
                Health h = enemies[i];
                if (h == null || h.IsDead) continue;
                float sqr = ((Vector2)h.transform.position - origin).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = h;
                }
            }
            return best;
        }

        public static int CollectEnemiesInRadius(Vector2 origin, float radius, List<Health> results)
        {
            int added = 0;
            float sqrRadius = radius * radius;
            for (int i = 0; i < enemies.Count; i++)
            {
                Health h = enemies[i];
                if (h == null || h.IsDead) continue;
                if (((Vector2)h.transform.position - origin).sqrMagnitude <= sqrRadius)
                {
                    results.Add(h);
                    added++;
                }
            }
            return added;
        }
    }
}
