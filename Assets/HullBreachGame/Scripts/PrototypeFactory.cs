using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Enemies;
using HullBreach.Player;
using UnityEngine;

namespace HullBreach.Game
{
    public static class PrototypeFactory
    {
        public const int PlayerLayer = 8;
        public const int EnemyLayer = 9;

        public static Projectile CreateProjectile(Transform templatesRoot, string name, string artPath, float maxSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(templatesRoot, false);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = maxSize * 0.35f;
            col.isTrigger = true;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = ArtCatalog.GetSingle(artPath, maxSize);
            sr.sortingOrder = 14;

            return go.AddComponent<Projectile>();
        }

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
            col.radius = ArtSpecs.PlayerHeight * 0.3f;

            Health health = go.AddComponent<Health>();
            health.Configure(Faction.Player, 100f, 0.3f);

            SideSprites samurai = ArtCatalog.GetCharacter(ArtSpecs.SamuraiFolder, ArtSpecs.PlayerHeight);
            SideSprites strel = ArtCatalog.GetCharacter(ArtSpecs.StrelFolder, ArtSpecs.PlayerHeight);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer body = visual.AddComponent<SpriteRenderer>();
            body.sprite = samurai.Get(ArtSide.Front);
            body.sortingOrder = 8;

            PlayerController controller = go.AddComponent<PlayerController>();
            PlayerCombat combat = go.AddComponent<PlayerCombat>();
            combat.Configure(playerShot, null);
            PlayerVisual.Attach(go, body, samurai, strel);
            return controller;
        }

        public static EnemyPrefabEntry[] CreateEnemyEntries(Transform templatesRoot, Projectile gunnerShot,
            Projectile miniBossShot, Projectile nexusShot, bool healthBars)
        {
            return new[]
            {
                Entry(EnemyType.Drone, false,
                    CreateEnemy(templatesRoot, "Enemy_Drone", EnemyType.Drone, ArtSpecs.DroneFolder, ArtSpecs.DroneHeight, null, healthBars)),
                Entry(EnemyType.Gunner, false,
                    CreateEnemy(templatesRoot, "Enemy_Gunner", EnemyType.Gunner, ArtSpecs.GunnerFolder, ArtSpecs.GunnerHeight, gunnerShot, healthBars)),
                Entry(EnemyType.Heavy, false,
                    CreateEnemy(templatesRoot, "Enemy_Heavy", EnemyType.Heavy, ArtSpecs.HeavyFolder, ArtSpecs.HeavyHeight, null, healthBars)),
                Entry(EnemyType.Exploder, false,
                    CreateEnemy(templatesRoot, "Enemy_Exploder", EnemyType.Exploder, ArtSpecs.ExploderFolder, ArtSpecs.ExploderHeight, null, healthBars)),
                Entry(EnemyType.MiniBoss, false,
                    CreateEnemy(templatesRoot, "Enemy_MiniBoss", EnemyType.MiniBoss, ArtSpecs.MiniBossFolder, ArtSpecs.MiniBossHeight, miniBossShot, healthBars)),
                Entry(EnemyType.MiniBoss, true,
                    CreateEnemy(templatesRoot, "Enemy_FinalBoss", EnemyType.MiniBoss, ArtSpecs.NexusFolder, ArtSpecs.NexusHeight, nexusShot, healthBars)),
            };
        }

        private static EnemyPrefabEntry Entry(EnemyType type, bool finalShipBoss, EnemyController prefab)
        {
            return new EnemyPrefabEntry { type = type, finalShipBoss = finalShipBoss, prefab = prefab };
        }

        private static EnemyController CreateEnemy(Transform templatesRoot, string name, EnemyType type, string artFolder,
            float height, Projectile shot, bool healthBar)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(templatesRoot, false);
            go.layer = EnemyLayer;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.mass = Mathf.Max(0.5f, height * height);
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = height * 0.3f;

            Health health = go.AddComponent<Health>();
            health.Configure(Faction.Enemy, EnemyDefaults.Get(type).MaxHealth, 0f);

            SideSprites set = ArtCatalog.GetCharacter(artFolder, height);
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = set.Get(ArtSide.Front);
            sr.sortingOrder = 6;

            EnemyController controller = go.AddComponent<EnemyController>();
            controller.Configure(type, shot);
            EnemyVisual.Attach(go, sr, set, height);

            if (healthBar) WorldHealthBar.Attach(go, Mathf.Max(0.9f, height * 0.9f), height * 0.5f + 0.25f);
            return controller;
        }
    }
}
