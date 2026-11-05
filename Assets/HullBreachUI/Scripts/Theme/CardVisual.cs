using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Theme
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class CardVisual : MonoBehaviour
    {
        public Image Edge;
        public Image Glow;
        public Color Accent = Color.white;
        public float AppearDelay;

        private CanvasGroup group;
        private float selectT;
        private bool selected;
        private float appearT;
        private const float AppearDuration = 0.3f;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            Restart(AppearDelay);
        }

        public void Restart(float delay)
        {
            AppearDelay = delay;
            appearT = -delay;
            selectT = selected ? 1f : 0f;
            Apply();
        }

        public void SetSelected(bool value)
        {
            selected = value;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (appearT < AppearDuration) appearT += dt;
            selectT = Mathf.MoveTowards(selectT, selected ? 1f : 0f, dt * 7f);
            Apply();
        }

        private void Apply()
        {
            if (group == null) return;
            float p = Mathf.Clamp01(appearT / AppearDuration);
            float e = 1f - (1f - p) * (1f - p);
            group.alpha = e;
            float s = Mathf.Lerp(0.92f, 1f, e) * Mathf.Lerp(1f, 1.04f, selectT);
            transform.localScale = new Vector3(s, s, 1f);
            if (Edge != null) Edge.color = Color.Lerp(UIColors.WithAlpha(Accent, 0.55f), Accent, selectT);
            if (Glow != null) Glow.color = UIColors.WithAlpha(Accent, 0.28f * selectT);
        }
    }
}
