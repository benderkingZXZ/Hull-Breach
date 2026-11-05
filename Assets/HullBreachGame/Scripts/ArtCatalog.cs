using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace HullBreach.Game
{
    public enum ArtSide { Left, Right, Front, Back }

    public sealed class SideSprites
    {
        private readonly Sprite[] sprites = new Sprite[4];

        public Sprite Get(ArtSide side)
        {
            return sprites[(int)side];
        }

        public void Set(ArtSide side, Sprite sprite)
        {
            sprites[(int)side] = sprite;
        }
    }

    public static class ArtSpecs
    {
        public const string SamuraiFolder = "Art/Characters/samurai";
        public const string StrelFolder = "Art/Characters/strel";
        public const string DroneFolder = "Art/Enemies/drone";
        public const string GunnerFolder = "Art/Enemies/gunner";
        public const string HeavyFolder = "Art/Enemies/heavy";
        public const string ExploderFolder = "Art/Enemies/exploder";
        public const string MiniBossFolder = "Art/Enemies/miniboss";
        public const string NexusFolder = "Art/Enemies/nexus";

        public const string StrelShotPath = "Art/Projectiles/strel_shot";
        public const string GunnerPulsePath = "Art/Projectiles/gunner_pulse";
        public const string MiniBossPulsePath = "Art/Projectiles/miniboss_pulse";
        public const string NexusPulsePath = "Art/Projectiles/nexus_pulse";

        public const float PlayerHeight = 1.7f;
        public const float DroneHeight = 1.0f;
        public const float GunnerHeight = 1.5f;
        public const float HeavyHeight = 2.2f;
        public const float ExploderHeight = 1.1f;
        public const float MiniBossHeight = 3.2f;
        public const float NexusHeight = 3.6f;

        public const float StrelShotSize = 0.55f;
        public const float GunnerPulseSize = 0.45f;
        public const float MiniBossPulseSize = 0.6f;
        public const float NexusPulseSize = 0.7f;
    }

    public static class ArtCatalog
    {
        private const float AlphaThreshold = 0.03f;
        private const int SampleSize = 128;

        private static readonly Dictionary<string, SideSprites> characters = new Dictionary<string, SideSprites>();
        private static readonly Dictionary<string, Sprite> singles = new Dictionary<string, Sprite>();

        public static SideSprites GetCharacter(string folder, float visibleHeight)
        {
            string key = folder + "|" + visibleHeight.ToString("F3", CultureInfo.InvariantCulture);
            SideSprites cached;
            if (characters.TryGetValue(key, out cached)) return cached;

            SideSprites set = new SideSprites();
            int loaded = 0;
            for (int i = 0; i < 4; i++)
            {
                ArtSide side = (ArtSide)i;
                Texture2D texture = Resources.Load<Texture2D>(folder + "/" + SideFileName(side));
                if (texture == null) continue;
                Rect visible = MeasureVisible(texture);
                float pixelsHigh = Mathf.Max(1f, visible.height * texture.height);
                set.Set(side, MakeSprite(texture, visible, pixelsHigh / visibleHeight));
                loaded++;
            }

            if (loaded == 0)
            {
                Debug.LogWarning("[HullBreach] Нет спрайтов в Resources/" + folder + ", показываю запасную фигуру.");
                set = FallbackSet(visibleHeight);
            }
            else
            {
                FillMissingSides(set);
            }

            characters[key] = set;
            return set;
        }

        public static Sprite GetSingle(string path, float maxSideSize)
        {
            string key = path + "|" + maxSideSize.ToString("F3", CultureInfo.InvariantCulture);
            Sprite cached;
            if (singles.TryGetValue(key, out cached)) return cached;

            Sprite sprite;
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                Rect visible = MeasureVisible(texture);
                float pixelsMax = Mathf.Max(1f, Mathf.Max(visible.width * texture.width, visible.height * texture.height));
                sprite = MakeSprite(texture, visible, pixelsMax / maxSideSize);
            }
            else
            {
                Debug.LogWarning("[HullBreach] Нет спрайта Resources/" + path + ", показываю запасную фигуру.");
                sprite = FallbackSprite(maxSideSize);
            }

            singles[key] = sprite;
            return sprite;
        }

        public static ArtSide SideFromDirection(Vector2 direction, ArtSide current)
        {
            if (direction.sqrMagnitude < 0.0001f) return current;
            float ax = Mathf.Abs(direction.x);
            float ay = Mathf.Abs(direction.y);
            bool horizontal;
            if (current == ArtSide.Left || current == ArtSide.Right) horizontal = !(ay > ax * 1.2f);
            else horizontal = ax > ay * 1.2f;
            if (horizontal) return direction.x < 0f ? ArtSide.Left : ArtSide.Right;
            return direction.y > 0f ? ArtSide.Back : ArtSide.Front;
        }

        public static string SideFileName(ArtSide side)
        {
            switch (side)
            {
                case ArtSide.Left: return "left";
                case ArtSide.Right: return "right";
                case ArtSide.Front: return "front";
                default: return "back";
            }
        }

        private static void FillMissingSides(SideSprites set)
        {
            Sprite fallback = null;
            for (int i = 0; i < 4 && fallback == null; i++) fallback = set.Get((ArtSide)i);
            for (int i = 0; i < 4; i++)
            {
                if (set.Get((ArtSide)i) == null) set.Set((ArtSide)i, fallback);
            }
        }

        private static SideSprites FallbackSet(float visibleHeight)
        {
            Sprite sprite = FallbackSprite(visibleHeight);
            SideSprites set = new SideSprites();
            for (int i = 0; i < 4; i++) set.Set((ArtSide)i, sprite);
            return set;
        }

        private static Sprite FallbackSprite(float size)
        {
            Texture2D texture = RuntimeSprites.Circle.texture;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                texture.height / Mathf.Max(0.01f, size), 0, SpriteMeshType.FullRect);
        }

        private static Sprite MakeSprite(Texture2D texture, Rect visible, float pixelsPerUnit)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), visible.center,
                Mathf.Max(1f, pixelsPerUnit), 0, SpriteMeshType.FullRect);
        }

        private static Rect MeasureVisible(Texture2D source)
        {
            RenderTexture target = RenderTexture.GetTemporary(SampleSize, SampleSize, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            Texture2D sample = new Texture2D(SampleSize, SampleSize, TextureFormat.RGBA32, false);
            sample.ReadPixels(new Rect(0f, 0f, SampleSize, SampleSize), 0, 0, false);
            sample.Apply(false, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            Color32[] pixels = sample.GetPixels32();
            Object.Destroy(sample);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = -1;
            int maxY = -1;
            float threshold = AlphaThreshold * 255f;
            for (int y = 0; y < SampleSize; y++)
            {
                for (int x = 0; x < SampleSize; x++)
                {
                    if (pixels[y * SampleSize + x].a <= threshold) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return new Rect(0f, 0f, 1f, 1f);
            float size = SampleSize;
            return Rect.MinMaxRect(minX / size, minY / size, (maxX + 1) / size, (maxY + 1) / size);
        }
    }
}
