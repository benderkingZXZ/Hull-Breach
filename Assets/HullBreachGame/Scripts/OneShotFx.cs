using UnityEngine;

namespace HullBreach.Game
{
    public sealed class OneShotFx : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Color color;
        private Vector3 from;
        private Vector3 to;
        private float duration;
        private float t;

        public static OneShotFx Spawn(Sprite sprite, Vector3 position, float rotationZ, Color color,
            Vector2 startSize, Vector2 endSize, float duration, int sortingOrder = 12)
        {
            GameObject go = new GameObject("Fx");
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

            OneShotFx fx = go.AddComponent<OneShotFx>();
            fx.sr = go.AddComponent<SpriteRenderer>();
            fx.sr.sprite = sprite;
            fx.sr.sortingOrder = sortingOrder;
            fx.color = color;
            fx.from = new Vector3(startSize.x, startSize.y, 1f);
            fx.to = new Vector3(endSize.x, endSize.y, 1f);
            fx.duration = Mathf.Max(0.05f, duration);
            fx.Apply(0f);
            return fx;
        }

        private void Update()
        {
            t += Time.deltaTime;
            float p = t / duration;
            if (p >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            Apply(p);
        }

        private void Apply(float p)
        {
            transform.localScale = Vector3.Lerp(from, to, 1f - (1f - p) * (1f - p));
            Color c = color;
            c.a = color.a * (1f - p);
            sr.color = c;
        }
    }
}
