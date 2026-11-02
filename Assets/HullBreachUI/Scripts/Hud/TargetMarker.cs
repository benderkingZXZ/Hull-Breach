using HullBreach.UI.Contracts;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Hud
{
    /// <summary>Прицельная рамка на цели автонаведения: показывает, по кому робот бьёт.</summary>
    public sealed class TargetMarker : MonoBehaviour
    {
        private ICombatFeed feed;
        private UIRootRefs root;
        private RectTransform rect;
        private Image image;

        public static TargetMarker Create(Transform parent, ICombatFeed feed, UIRootRefs root)
        {
            RectTransform rt = UIFactory.Rect("TargetMarker", parent);
            UIFactory.Anchor(rt, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 110f, 110f);
            Image img = UIFactory.AddImage(rt.gameObject, UIIcons.Get("crosshair"), UIColors.WithAlpha(UIColors.Red, 0.9f));
            img.preserveAspect = true;
            TargetMarker m = rt.gameObject.AddComponent<TargetMarker>();
            m.feed = feed;
            m.root = root;
            m.rect = rt;
            m.image = img;
            rt.gameObject.SetActive(false);
            return m;
        }

        private void Update()
        {
            Vector3 world;
            Camera cam = root.ResolveCamera();
            if (feed == null || cam == null || !feed.TryGetTargetPosition(out world))
            {
                if (image.enabled) image.enabled = false;
                return;
            }

            Vector3 sp = cam.WorldToScreenPoint(world);
            Vector2 local;
            if (sp.z < 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent, new Vector2(sp.x, sp.y), null, out local))
            {
                if (image.enabled) image.enabled = false;
                return;
            }
            image.enabled = true;
            rect.anchoredPosition = local;
            rect.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 40f);
            float s = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f);
            rect.localScale = new Vector3(s, s, 1f);
        }
    }
}
