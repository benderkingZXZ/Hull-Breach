using UnityEngine;

namespace HullBreach.UI.Theme
{
    /// <summary>Плавное появление при каждом включении объекта (прозрачность и масштаб, unscaled-время).</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AppearAnimation : MonoBehaviour
    {
        public float Delay;
        public float Duration = 0.28f;
        public float StartScale = 0.94f;

        private CanvasGroup group;
        private float t;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            Restart(Delay);
        }

        public void Restart(float delay)
        {
            Delay = delay;
            t = -delay;
            Apply(0f);
        }

        private void Update()
        {
            if (t >= Duration) return;
            t += Time.unscaledDeltaTime;
            Apply(Mathf.Clamp01(t / Duration));
        }

        private void Apply(float p)
        {
            float e = 1f - (1f - p) * (1f - p);
            group.alpha = e;
            float s = Mathf.Lerp(StartScale, 1f, e);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
