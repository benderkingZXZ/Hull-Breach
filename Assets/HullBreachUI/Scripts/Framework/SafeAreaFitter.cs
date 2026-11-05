using UnityEngine;

namespace HullBreach.UI.Framework
{
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip("Дополнительный внутренний отступ в единицах холста.")]
        public float Padding = 14f;

        private RectTransform rt;
        private Rect lastSafe;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            rt = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastWidth || Screen.height != lastHeight) Apply();
        }

        private void Apply()
        {
            if (rt == null) rt = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Screen.safeArea;
            lastSafe = safe;
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = new Vector2(Padding, Padding);
            rt.offsetMax = new Vector2(-Padding, -Padding);
        }
    }
}
