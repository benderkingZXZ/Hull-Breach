using UnityEngine;
using HullBreach.Core;

namespace HullBreach.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody2D body;
        private Faction faction;
        private float damage;
        private Vector2 velocity;
        private GameObject source;
        private bool consumed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }

        public static Projectile Spawn(Projectile prefab, Vector2 position, Vector2 direction, float speed,
            float damage, Faction faction, GameObject source, float lifetime)
        {
            Projectile p;
            if (prefab != null)
            {
                p = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                GameObject go = new GameObject("Projectile (runtime)");
                go.transform.position = position;
                Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                CircleCollider2D col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.15f;
                col.isTrigger = true;
                p = go.AddComponent<Projectile>();
            }
            p.Launch(direction, speed, damage, faction, source, lifetime);
            return p;
        }

        private void Launch(Vector2 direction, float speed, float dmg, Faction fac, GameObject src, float lifetime)
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            velocity = dir * speed;
            damage = dmg;
            faction = fac;
            source = src;
            transform.right = dir;

            Collider2D[] cols = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cols.Length; i++) cols[i].isTrigger = true;

            Destroy(gameObject, Mathf.Max(0.1f, lifetime));
        }

        private void FixedUpdate()
        {
            if (consumed) return;
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed) return;

            Health target = other.GetComponentInParent<Health>();
            if (target != null)
            {
                if (target.Faction == faction || target.IsDead) return;
                target.TakeDamage(new DamageInfo(damage, source, velocity.normalized));
                Consume();
                return;
            }

            if (!other.isTrigger) Consume();
        }

        private void Consume()
        {
            consumed = true;
            Destroy(gameObject);
        }
    }
}
