using HullBreach.Combat;
using HullBreach.Enemies;
using UnityEngine;

namespace HullBreach.Game
{
    public sealed class EnemyVisual : MonoBehaviour
    {
        private const float HurtDuration = 0.12f;
        private const float DeathFadeDuration = 0.35f;
        private static readonly Color HurtTint = new Color(1f, 0.35f, 0.35f, 1f);

        private EnemyController enemy;
        private Health health;
        private SpriteRenderer body;
        private SideSprites sprites;
        private float height = 1f;
        private ArtSide side = ArtSide.Front;
        private float hurtTimer;
        private float deathTimer = -1f;

        public static EnemyVisual Attach(GameObject enemyObject, SpriteRenderer renderer, SideSprites set, float visibleHeight)
        {
            EnemyVisual visual = enemyObject.AddComponent<EnemyVisual>();
            visual.body = renderer;
            visual.sprites = set;
            visual.height = visibleHeight;
            return visual;
        }

        private void OnEnable()
        {
            enemy = GetComponent<EnemyController>();
            health = GetComponent<Health>();
            if (health != null) health.Damaged += OnDamaged;
            if (enemy != null)
            {
                enemy.Died += OnDied;
                enemy.Fired += OnFired;
            }
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            if (enemy != null)
            {
                enemy.Died -= OnDied;
                enemy.Fired -= OnFired;
            }
        }

        private void OnDamaged(DamageInfo info, float dealt)
        {
            if (dealt > 0f) hurtTimer = HurtDuration;
        }

        private void OnDied(EnemyController dead)
        {
            if (deathTimer >= 0f) return;
            deathTimer = 0f;
            OneShotFx.Spawn(RuntimeSprites.Ring, transform.position, 0f, new Color(1f, 0.6f, 0.3f, 0.9f),
                new Vector2(height * 0.6f, height * 0.6f), new Vector2(height * 1.6f, height * 1.6f), 0.3f);
        }

        private void OnFired(EnemyController shooter)
        {
            AudioManager.PlaySfx("strel_shot", 0.35f, 1f);
        }

        private void LateUpdate()
        {
            if (body == null) return;
            if (deathTimer >= 0f)
            {
                deathTimer += Time.deltaTime;
                Color faded = Color.white;
                faded.a = 1f - Mathf.Clamp01(deathTimer / DeathFadeDuration);
                body.color = faded;
                return;
            }
            if (enemy != null && sprites != null)
            {
                side = ArtCatalog.SideFromDirection(enemy.FacingDirection, side);
                Sprite sprite = sprites.Get(side);
                if (sprite != null) body.sprite = sprite;
            }
            if (hurtTimer > 0f)
            {
                hurtTimer -= Time.deltaTime;
                body.color = HurtTint;
            }
            else
            {
                body.color = Color.white;
            }
        }
    }
}
