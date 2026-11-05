using System.Collections.Generic;
using UnityEngine;

namespace HullBreach.Game
{
    public static class RuntimeSprites
    {
        private static Sprite square;
        private static Sprite circle;
        private static Sprite ring;
        private static readonly Dictionary<string, Sprite> gridTiles = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            square = null;
            circle = null;
            ring = null;
            gridTiles.Clear();
        }

        public static Sprite Square
        {
            get
            {
                if (square == null) square = MakeSquare();
                return square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = MakeCircle(64, false);
                return circle;
            }
        }

        public static Sprite Ring
        {
            get
            {
                if (ring == null) ring = MakeCircle(128, true);
                return ring;
            }
        }

        public static Sprite GridTile(Color floor, Color line)
        {
            string key = ColorUtility.ToHtmlStringRGBA(floor) + "_" + ColorUtility.ToHtmlStringRGBA(line);
            Sprite cached;
            if (gridTiles.TryGetValue(key, out cached) && cached != null) return cached;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            Color32 f = floor;
            Color32 l = line;
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < 2 || y < 2;
                    px[y * size + x] = edge ? l : f;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
            gridTiles[key] = sprite;
            return sprite;
        }

        private static Sprite MakeSquare()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            Color32[] px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite MakeCircle(int size, bool ringOnly)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float r = size * 0.5f;
            Vector2 center = new Vector2(r - 0.5f, r - 0.5f);
            float thickness = size * 0.07f;
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float a = Mathf.Clamp01(r - d);
                    if (ringOnly) a = Mathf.Min(a, Mathf.Clamp01(d - (r - thickness)));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
