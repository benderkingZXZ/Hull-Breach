using HullBreach.Combat;
using UnityEngine;

namespace HullBreach.Game
{
    public sealed class WorldHealthBar : MonoBehaviour
    {
        private Health health;
        private Transform fill;
        private SpriteRenderer fillRenderer;
        private SpriteRenderer backRenderer;
        private float width;

        public static WorldHealthBar Attach(GameObject owner, float width, float yOffset)
        {
            GameObject root = new GameObject("HealthBar");
            root.transform.SetParent(owner.transform, false);
            root.transform.localPosition = new Vector3(0f, yOffset, 0f);

            GameObject back = new GameObject("Back");
            back.transform.SetParent(root.transform, false);
            back.transform.localScale = new Vector3(width + 0.1f, 0.2f, 1f);
            SpriteRenderer backSr = back.AddComponent<SpriteRenderer>();
            backSr.sprite = RuntimeSprites.Square;
            backSr.color = new Color(0f, 0f, 0f, 0.7f);
            backSr.sortingOrder = 30;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(root.transform, false);
            SpriteRenderer fillSr = fillGo.AddComponent<SpriteRenderer>();
            fillSr.sprite = RuntimeSprites.Square;
            fillSr.color = new Color(0.27f, 0.94f, 0.63f, 1f);
            fillSr.sortingOrder = 31;

            WorldHealthBar bar = root.AddComponent<WorldHealthBar>();
            bar.width = width;
            bar.fill = fillGo.transform;
            bar.fillRenderer = fillSr;
            bar.backRenderer = backSr;
            bar.SetRatio(1f);
            return bar;
        }

        private void OnEnable()
        {
            health = GetComponentInParent<Health>();
            if (health == null) return;
            health.HealthChanged += OnHealthChanged;
            OnHealthChanged(health.CurrentHealth, health.MaxHealth);
        }

        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= OnHealthChanged;
        }

        private void LateUpdate()
        {
            transform.rotation = Quaternion.identity;
        }

        private void OnHealthChanged(float current, float max)
        {
            SetRatio(max > 0f ? current / max : 0f);
        }

        private void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            bool visible = ratio < 0.999f && ratio > 0f;
            if (backRenderer != null) backRenderer.enabled = visible;
            if (fillRenderer != null)
            {
                fillRenderer.enabled = visible;
                fillRenderer.color = Color.Lerp(new Color(1f, 0.27f, 0.38f, 1f), new Color(0.27f, 0.94f, 0.63f, 1f), ratio);
            }
            if (fill != null)
            {
                float w = width * ratio;
                fill.localScale = new Vector3(Mathf.Max(0.001f, w), 0.12f, 1f);
                fill.localPosition = new Vector3(-width * 0.5f + w * 0.5f, 0f, 0f);
            }
        }
    }
}
