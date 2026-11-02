using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Theme
{
    /// <summary>
    /// Фон меню: космос, два слоя звёзд с параллаксом и едва заметная «обшивка» кораблей.
    /// В бою отключается (HUD накладывается на игровой мир).
    /// </summary>
    public sealed class StarfieldBackdrop : MonoBehaviour
    {
        private RawImage far;
        private RawImage near;
        private RectTransform rect;
        private Vector2 farOffset;
        private Vector2 nearOffset;
        private const float FarTile = 720f;
        private const float NearTile = 1080f;

        public static StarfieldBackdrop Build(RectTransform parent)
        {
            RectTransform root = UIFactory.Rect("Backdrop", parent);
            UIFactory.Stretch(root);
            StarfieldBackdrop sb = root.gameObject.AddComponent<StarfieldBackdrop>();
            sb.rect = root;

            UIFactory.Img(root, "Base", ProceduralSprites.White, UIColors.Void);

            Image shade = UIFactory.Img(root, "Shade", ProceduralSprites.GradientTop, UIColors.WithAlpha(UIColors.Hex("#0B1E3C"), 0.75f));
            shade.rectTransform.localScale = new Vector3(1f, -1f, 1f);   // голубоватое свечение у нижнего края

            Image hull = UIFactory.Img(root, "HullPlates", ProceduralSprites.Grid, UIColors.WithAlpha(UIColors.Cyan, 0.07f), Image.Type.Tiled);
            hull.raycastTarget = false;

            sb.far = CreateStars(root, "FarStars", StarTexture(512, 260, 11, 0.7f));
            sb.near = CreateStars(root, "NearStars", StarTexture(512, 70, 29, 1f));
            return sb;
        }

        private static Texture2D StarTexture(int size, int count, int seed, float alpha)
        {
            return ProceduralSprites.StarTexture(size, count, seed, alpha);
        }

        private static RawImage CreateStars(RectTransform parent, string name, Texture2D tex)
        {
            RectTransform rt = UIFactory.Rect(name, parent);
            UIFactory.Stretch(rt);
            RawImage img = rt.gameObject.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            return img;
        }

        private void Update()
        {
            if (rect == null) return;
            float dt = Time.unscaledDeltaTime;
            farOffset += new Vector2(0.004f, 0.0006f) * dt;
            nearOffset += new Vector2(0.011f, 0.0015f) * dt;
            Vector2 size = rect.rect.size;
            if (size.x < 1f || size.y < 1f) return;
            far.uvRect = new Rect(farOffset.x, farOffset.y, size.x / FarTile, size.y / FarTile);
            near.uvRect = new Rect(nearOffset.x, nearOffset.y, size.x / NearTile, size.y / NearTile);
        }
    }
}
