using System.Collections.Generic;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Enemies;
using HullBreach.Player;
using UnityEngine;

namespace HullBreach.Game
{
    /// <summary>
    /// Строит игровые объекты из кода: игрок, 5 типов врагов, снаряды. Это временная графика прототипа:
    /// когда появятся настоящие префабы, назначайте их вместо этих (см. docs/GAME_INTEGRATION.md).
    /// Шаблоны (враги, снаряды) лежат под НЕАКТИВНЫМ родителем: их компоненты не запускаются, а клоны оживают на сцене.
    /// </summary>
    public static class PrototypeFactory
    {
        /// <summary>Слои без имён (достаточно индексов): игрок и враги не сталкиваются физически.</summary>
        public const int PlayerLayer = 8;
        public const int EnemyLayer = 9;

        // ---------------------------------------------------------------- Снаряды

        public static Projectile CreateProjectile(Transform templatesRoot, string name, Color color, float size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(templatesRoot, false);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = size * 0.5f;
            col.isTrigger = true;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(size, size, 1f);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprites.Circle;
            sr.color = color;
            sr.sortingOrder = 14;

            return go.AddComponent<Projectile>();
        }

        // ---------------------------------------------------------------- Игрок

        /// <summary>Создаёт НЕАКТИВНЫЙ объект игрока. Вызывающий активирует его после настройки систем.</summary>
        public static PlayerController CreatePlayer(Projectile playerShot)
        {
            GameObject go = new GameObject("Player");
            go.SetActive(false);
            go.layer = PlayerLayer;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            Health health = go.AddComponent<Health>();
            health.Configure(Faction.Player, 100f, 0.3f);

            // Визуал (дочерний объект, чтобы не масштабировать корень с физикой)
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            SpriteRenderer body = visual.AddComponent<SpriteRenderer>();
            body.sprite = RuntimeSprites.Circle;
            body.sortingOrder = 8;

            GameObject ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(visual.transform, false);
            SpriteRenderer ring = ringGo.AddComponent<SpriteRenderer>();
            ring.sprite = RuntimeSprites.Ring;
            ring.color = new Color(1f, 1f, 1f, 0.9f);
            ring.sortingOrder = 9;

            GameObject nubGo = new GameObject("Nub");
            nubGo.transform.SetParent(go.transform, false);
            nubGo.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            SpriteRenderer nub = nubGo.AddComponent<SpriteRenderer>();
            nub.sprite = RuntimeSprites.Circle;
            nub.color = Color.white;
            nub.sortingOrder = 10;

            PlayerController controller = go.AddComponent<PlayerController>();
            PlayerCombat combat = go.AddComponent<PlayerCombat>();
            combat.Configure(playerShot, null);
            PlayerVisual.Attach(go, body, nubGo.transform);
            return controller;
        }

        // ---------------------------------------------------------------- Враги

        public static EnemyPrefabEntry[] CreateEnemyEntries(Transform templatesRoot, Projectile enemyShot, bool healthBars)
        {
            EnemyType[] types =
            {
                EnemyType.Drone, EnemyType.Gunner, EnemyType.Heavy, EnemyType.Exploder, EnemyType.MiniBoss
            };
            List<EnemyPrefabEntry> entries = new List<EnemyPrefabEntry>();
            for (int i = 0; i < types.Length; i++)
            {
                EnemyPrefabEntry entry = new EnemyPrefabEntry();
                entry.type = types[i];
                entry.prefab = CreateEnemy(templatesRoot, types[i], enemyShot, healthBars);
                entries.Add(entry);
            }
            return entries.ToArray();
        }

        private static EnemyController CreateEnemy(Transform templatesRoot, EnemyType type, Projectile enemyShot, bool healthBar)
        {
            float diameter;
            float mass;
            Color color;
            bool square = false;
            float rotation = 0f;
            switch (type)
            {
                case EnemyType.Drone:
                    diameter = 0.75f; mass = 1f; color = new Color(1f, 0.45f, 0.2f, 1f); square = true; rotation = 45f; break;
                case EnemyType.Gunner:
                    diameter = 0.9f; mass = 1.5f; color = new Color(0.78f, 0.36f, 1f, 1f); break;
                case EnemyType.Heavy:
                    diameter = 1.5f; mass = 6f; color = new Color(0.78f, 0.15f, 0.2f, 1f); square = true; break;
                case EnemyType.Exploder:
                    diameter = 0.8f; mass = 1f; color = new Color(1f, 0.9f, 0.2f, 1f); break;
                default: // MiniBoss
                    diameter = 2.6f; mass = 20f; color = new Color(1f, 0.2f, 0.7f, 1f); break;
            }

            GameObject go = new GameObject("Enemy_" + type);
            go.transform.SetParent(templatesRoot, false);
            go.layer = EnemyLayer;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = diameter * 0.5f * (square ? 0.9f : 1f);

            Health health = go.AddComponent<Health>();
            health.Configure(Faction.Enemy, HullBreach.Data.EnemyDefaults.Get(type).MaxHealth, 0f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(diameter, diameter, 1f);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = square ? RuntimeSprites.Square : RuntimeSprites.Circle;
            sr.color = color;
            sr.sortingOrder = 6;

            if (type == EnemyType.MiniBoss || type == EnemyType.Gunner)
            {
                GameObject ringGo = new GameObject("Ring");
                ringGo.transform.SetParent(go.transform, false);
                ringGo.transform.localScale = new Vector3(diameter * 1.15f, diameter * 1.15f, 1f);
                SpriteRenderer ring = ringGo.AddComponent<SpriteRenderer>();
                ring.sprite = RuntimeSprites.Ring;
                ring.color = new Color(color.r, color.g, color.b, 0.8f);
                ring.sortingOrder = 7;
            }

            EnemyController controller = go.AddComponent<EnemyController>();
            controller.Configure(type, enemyShot);

            if (healthBar) WorldHealthBar.Attach(go, Mathf.Max(0.9f, diameter), diameter * 0.5f + 0.35f);
            return controller;
        }
    }
}
