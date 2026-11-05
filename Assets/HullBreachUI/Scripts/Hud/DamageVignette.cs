using HullBreach.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Hud
{
    public sealed class DamageVignette : MonoBehaviour
    {
        private Image image;
        private float flash;
        private bool lowHealth;

        public static DamageVignette Create(Transform parent)
        {
            RectTransform rt = UIFactory.Rect("DamageVignette", parent);
            UIFactory.Stretch(rt);
            Image img = UIFactory.AddImage(rt.gameObject, ProceduralSprites.Vignette, UIColors.WithAlpha(UIColors.Red, 0f));
            img.raycastTarget = false;
            DamageVignette v = rt.gameObject.AddComponent<DamageVignette>();
            v.image = img;
            return v;
        }

        public void Flash(float strength)
        {
            flash = Mathf.Max(flash, Mathf.Clamp01(strength));
        }

        public void SetLowHealth(bool value)
        {
            lowHealth = value;
        }

        public void ResetState()
        {
            flash = 0f;
            lowHealth = false;
            if (image != null) image.color = UIColors.WithAlpha(UIColors.Red, 0f);
        }

        private void Update()
        {
            if (image == null) return;
            flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime * 2.2f);
            float pulse = lowHealth ? 0.22f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
            image.color = UIColors.WithAlpha(UIColors.Red, Mathf.Clamp01(Mathf.Max(flash * 0.85f, pulse)));
        }
    }
}
