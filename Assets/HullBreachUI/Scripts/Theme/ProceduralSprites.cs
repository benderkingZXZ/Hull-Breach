using System;
using System.Collections.Generic;
using UnityEngine;

namespace HullBreach.UI.Theme
{
    /// <summary>
    /// Временные визуальные ресурсы, нарисованные кодом: панели со скошенными углами, круги, иконки, звёзды.
    /// Нужны, пока нет финального арта. Любой спрайт подменяется через UITheme (ключ → Sprite).
    /// Все спрайты белые (alpha = форма) и красятся через Image.color.
    /// </summary>
    public static class ProceduralSprites
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cache.Clear();
        }

        // ------------------------------------------------------------------ Публичные спрайты

        /// <summary>9-slice панель со скошенными углами (граница 24 px).</summary>
        public static Sprite Panel { get { return Cached("panel", () => BuildChamfer(64, 14f, 0f, 24)); } }
        public static Sprite PanelEdge { get { return Cached("panel_edge", () => BuildChamfer(64, 14f, 2.6f, 24)); } }
        /// <summary>Маленькая плашка для полос и пипсов (граница 12 px).</summary>
        public static Sprite Bar { get { return Cached("bar", () => BuildChamfer(32, 7f, 0f, 12)); } }
        public static Sprite BarEdge { get { return Cached("bar_edge", () => BuildChamfer(32, 7f, 1.8f, 12)); } }
        public static Sprite Circle { get { return Cached("circle", () => BuildCircle(128, 0f)); } }
        public static Sprite Ring { get { return Cached("ring", () => BuildCircle(128, 7f)); } }
        public static Sprite White { get { return Cached("white", BuildWhite); } }
        public static Sprite Vignette { get { return Cached("vignette", BuildVignette); } }
        /// <summary>Вертикальный градиент: сверху непрозрачный, снизу прозрачный.</summary>
        public static Sprite GradientTop { get { return Cached("gradient_top", BuildGradient); } }
        /// <summary>Тайлящийся рисунок металлических швов и заклёпок (для Image.Type.Tiled).</summary>
        public static Sprite Grid { get { return Cached("grid", BuildGrid); } }

        public static Sprite Icon(string name)
        {
            return Cached("icon." + name, () => BuildIcon(name));
        }

        private static Sprite Cached(string key, Func<Sprite> build)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            s = UIAssets.GetOverride(key);
            if (s == null) s = build();
            cache[key] = s;
            return s;
        }

        // ------------------------------------------------------------------ Текстуры

        private static Texture2D NewTexture(int w, int h, TextureWrapMode wrap)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = wrap;
            t.name = "HB_Procedural";
            return t;
        }

        private static Sprite Finish(Texture2D tex, Color32[] pixels, Vector4 border)
        {
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
        }

        private static Sprite BuildWhite()
        {
            Texture2D tex = NewTexture(4, 4, TextureWrapMode.Clamp);
            Color32[] px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            return Finish(tex, px, Vector4.zero);
        }

        private static Sprite BuildChamfer(int size, float cut, float edge, int border)
        {
            Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
            Color32[] px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float ax = Mathf.Abs(x + 0.5f - half);
                    float ay = Mathf.Abs(y + 0.5f - half);
                    float dRect = Mathf.Max(ax - half, ay - half);
                    float dCut = (ax + ay - (half + half - cut)) * 0.70710678f;
                    float d = Mathf.Max(dRect, dCut);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (edge > 0f) a *= Mathf.Clamp01(d + edge + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            return Finish(tex, px, new Vector4(border, border, border, border));
        }

        private static Sprite BuildCircle(int size, float ring)
        {
            Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
            Color32[] px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - half;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (ring > 0f) a *= Mathf.Clamp01(d + ring + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            return Finish(tex, px, Vector4.zero);
        }

        private static Sprite BuildVignette()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.SmoothStep(0.45f, 1.3f, r);
                    a *= a;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            return Finish(tex, px, Vector4.zero);
        }

        private static Sprite BuildGradient()
        {
            const int w = 4;
            const int h = 64;
            Texture2D tex = NewTexture(w, h, TextureWrapMode.Clamp);
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                byte a = (byte)Mathf.RoundToInt(255f * y / (h - 1));
                for (int x = 0; x < w; x++) px[y * w + x] = new Color32(255, 255, 255, a);
            }
            return Finish(tex, px, Vector4.zero);
        }

        private static Sprite BuildGrid()
        {
            const int size = 128;
            Texture2D tex = NewTexture(size, size, TextureWrapMode.Repeat);
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte a = 0;
                    if (x == 0 || y == 0) a = 90;
                    bool nearX = (x >= 6 && x <= 8) || (x >= size - 9 && x <= size - 7);
                    bool nearY = (y >= 6 && y <= 8) || (y >= size - 9 && y <= size - 7);
                    if (nearX && nearY) a = 150;
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Тайлящаяся текстура звёзд на прозрачном фоне (для RawImage с Repeat).</summary>
        public static Texture2D StarTexture(int size, int count, int seed, float maxAlpha)
        {
            Texture2D tex = NewTexture(size, size, TextureWrapMode.Repeat);
            Color32[] px = new Color32[size * size];
            System.Random rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                int x = rng.Next(size);
                int y = rng.Next(size);
                float b = (float)rng.NextDouble();
                b = b * b * maxAlpha;
                float tint = (float)rng.NextDouble();
                byte r = 255, g = 255, bl = 255;
                if (tint < 0.2f) { r = 170; g = 220; }
                else if (tint > 0.88f) { g = 205; bl = 160; }
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(b) * 255f);
                px[y * size + x] = new Color32(r, g, bl, alpha);
                if (b > 0.55f)
                {
                    byte soft = (byte)(alpha / 3);
                    int xp = (x + 1) % size, xm = (x + size - 1) % size, yp = (y + 1) % size, ym = (y + size - 1) % size;
                    px[y * size + xp] = new Color32(r, g, bl, soft);
                    px[y * size + xm] = new Color32(r, g, bl, soft);
                    px[yp * size + x] = new Color32(r, g, bl, soft);
                    px[ym * size + x] = new Color32(r, g, bl, soft);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // ------------------------------------------------------------------ Иконки (растеризация фигур)

        private struct Shape
        {
            public int Kind;      // 0 — многоугольник, 1 — круг, 2 — кольцо, 3 — прямоугольник
            public float[] P;
            public float A, B, C, D;
            public bool Cut;
        }

        private static Shape Poly(params float[] xy) { Shape s = new Shape(); s.Kind = 0; s.P = xy; return s; }
        private static Shape Circ(float x, float y, float r) { Shape s = new Shape(); s.Kind = 1; s.A = x; s.B = y; s.C = r; return s; }
        private static Shape RingS(float x, float y, float r, float t) { Shape s = new Shape(); s.Kind = 2; s.A = x; s.B = y; s.C = r; s.D = t; return s; }
        private static Shape Box(float x, float y, float hw, float hh) { Shape s = new Shape(); s.Kind = 3; s.A = x; s.B = y; s.C = hw; s.D = hh; return s; }
        private static Shape Cut(Shape s) { s.Cut = true; return s; }

        private static float[] Scaled(float[] xy, float k)
        {
            float[] r = new float[xy.Length];
            for (int i = 0; i < xy.Length; i++) r[i] = xy[i] * k;
            return r;
        }

        private static float[] Rotated(float[] xy, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            float[] r = new float[xy.Length];
            for (int i = 0; i < xy.Length; i += 2)
            {
                r[i] = xy[i] * c - xy[i + 1] * s;
                r[i + 1] = xy[i] * s + xy[i + 1] * c;
            }
            return r;
        }

        private static float[] Translated(float[] xy, float dx, float dy)
        {
            float[] r = new float[xy.Length];
            for (int i = 0; i < xy.Length; i += 2) { r[i] = xy[i] + dx; r[i + 1] = xy[i + 1] + dy; }
            return r;
        }

        private static float[] Mirrored(float[] xy)
        {
            float[] r = new float[xy.Length];
            for (int i = 0; i < xy.Length; i += 2) { r[i] = -xy[i]; r[i + 1] = xy[i + 1]; }
            return r;
        }

        private static float[] RegPoly(int n, float radius, float rotDeg)
        {
            float[] r = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float a = (rotDeg + 360f * i / n) * Mathf.Deg2Rad;
                r[i * 2] = Mathf.Cos(a) * radius;
                r[i * 2 + 1] = Mathf.Sin(a) * radius;
            }
            return r;
        }

        private static float[] StarPoly(int points, float outer, float inner)
        {
            float[] r = new float[points * 4];
            for (int i = 0; i < points * 2; i++)
            {
                float a = (90f + 180f * i / points) * Mathf.Deg2Rad;
                float rad = (i % 2 == 0) ? outer : inner;
                r[i * 2] = Mathf.Cos(a) * rad;
                r[i * 2 + 1] = Mathf.Sin(a) * rad;
            }
            return r;
        }

        private static float[] BoxPoly(float cx, float cy, float hw, float hh)
        {
            return new[] { cx - hw, cy - hh, cx + hw, cy - hh, cx + hw, cy + hh, cx - hw, cy + hh };
        }

        private static float[] GearPoly(int teeth, float outer, float inner)
        {
            float[] r = new float[teeth * 8];
            float step = 360f / teeth;
            for (int i = 0; i < teeth; i++)
            {
                float c = i * step;
                float[] angles = { c - step * 0.30f, c - step * 0.18f, c + step * 0.18f, c + step * 0.30f };
                float[] radii = { inner, outer, outer, inner };
                for (int k = 0; k < 4; k++)
                {
                    float a = angles[k] * Mathf.Deg2Rad;
                    r[(i * 4 + k) * 2] = Mathf.Cos(a) * radii[k];
                    r[(i * 4 + k) * 2 + 1] = Mathf.Sin(a) * radii[k];
                }
            }
            return r;
        }

        private static bool PointInPoly(float[] p, float x, float y)
        {
            bool inside = false;
            int n = p.Length / 2;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = p[i * 2], yi = p[i * 2 + 1], xj = p[j * 2], yj = p[j * 2 + 1];
                if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi)) inside = !inside;
            }
            return inside;
        }

        private static bool Contains(Shape s, float x, float y)
        {
            switch (s.Kind)
            {
                case 1:
                {
                    float dx = x - s.A, dy = y - s.B;
                    return dx * dx + dy * dy <= s.C * s.C;
                }
                case 2:
                {
                    float dx = x - s.A, dy = y - s.B;
                    float d2 = dx * dx + dy * dy;
                    float ri = s.C - s.D;
                    return d2 <= s.C * s.C && d2 >= ri * ri;
                }
                case 3:
                    return Mathf.Abs(x - s.A) <= s.C && Mathf.Abs(y - s.B) <= s.D;
                default:
                    return PointInPoly(s.P, x, y);
            }
        }

        private static Sprite BuildIcon(string name)
        {
            Shape[] shapes = IconShapes(name);
            if (shapes == null) shapes = new[] { Circ(0f, 0f, 0.85f), Cut(Circ(0f, 0f, 0.55f)) };

            const int size = 64;
            const int ss = 3;
            Texture2D tex = NewTexture(size, size, TextureWrapMode.Clamp);
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = ((x + (sx + 0.5f) / ss) / size) * 2f - 1f;
                            float v = ((y + (sy + 0.5f) / ss) / size) * 2f - 1f;
                            bool inside = false;
                            for (int i = 0; i < shapes.Length; i++)
                            {
                                if (Contains(shapes[i], u, v)) inside = !shapes[i].Cut;
                            }
                            if (inside) hits++;
                        }
                    }
                    byte a = (byte)(255 * hits / (ss * ss));
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            return Finish(tex, px, Vector4.zero);
        }

        private static Shape[] IconShapes(string name)
        {
            switch (name)
            {
                case "shield":
                {
                    float[] o = { 0f, 0.92f, 0.78f, 0.62f, 0.74f, -0.1f, 0.42f, -0.55f, 0f, -0.92f, -0.42f, -0.55f, -0.74f, -0.1f, -0.78f, 0.62f };
                    return new[] { Poly(o), Cut(Poly(Scaled(o, 0.72f))), Poly(Scaled(o, 0.45f)) };
                }
                case "bubble":
                {
                    float[] o = { 0f, 0.92f, 0.78f, 0.62f, 0.74f, -0.1f, 0.42f, -0.55f, 0f, -0.92f, -0.42f, -0.55f, -0.74f, -0.1f, -0.78f, 0.62f };
                    return new[] { RingS(0f, 0f, 0.95f, 0.1f), Poly(Scaled(o, 0.55f)) };
                }
                case "bolt":
                    return new[] { Poly(0.15f, 0.95f, -0.55f, 0f, -0.08f, 0f, -0.25f, -0.95f, 0.55f, 0.1f, 0.08f, 0.1f) };
                case "core":
                    return new[] { Poly(StarPoly(8, 0.95f, 0.55f)), Cut(Circ(0f, 0f, 0.28f)), Circ(0f, 0f, 0.13f) };
                case "chevrons":
                    return new[]
                    {
                        Poly(-0.8f, 0.7f, -0.3f, 0.7f, 0.2f, 0f, -0.3f, -0.7f, -0.8f, -0.7f, -0.3f, 0f),
                        Poly(-0.25f, 0.7f, 0.25f, 0.7f, 0.75f, 0f, 0.25f, -0.7f, -0.25f, -0.7f, 0.25f, 0f)
                    };
                case "crosshair":
                    return new[]
                    {
                        RingS(0f, 0f, 0.62f, 0.12f), Box(0f, 0.8f, 0.07f, 0.17f), Box(0f, -0.8f, 0.07f, 0.17f),
                        Box(0.8f, 0f, 0.17f, 0.07f), Box(-0.8f, 0f, 0.17f, 0.07f), Circ(0f, 0f, 0.12f)
                    };
                case "plus":
                    return new[] { Poly(-0.28f, 0.85f, 0.28f, 0.85f, 0.28f, 0.28f, 0.85f, 0.28f, 0.85f, -0.28f, 0.28f, -0.28f, 0.28f, -0.85f, -0.28f, -0.85f, -0.28f, -0.28f, -0.85f, -0.28f, -0.85f, 0.28f, -0.28f, 0.28f) };
                case "close":
                    return new[] { Poly(Rotated(new[] { -0.2f, 0.8f, 0.2f, 0.8f, 0.2f, 0.2f, 0.8f, 0.2f, 0.8f, -0.2f, 0.2f, -0.2f, 0.2f, -0.8f, -0.2f, -0.8f, -0.2f, -0.2f, -0.8f, -0.2f, -0.8f, 0.2f, -0.2f, 0.2f }, 45f)) };
                case "emp":
                    return new[] { RingS(0f, 0f, 0.95f, 0.1f), RingS(0f, 0f, 0.62f, 0.1f), Circ(0f, 0f, 0.26f) };
                case "injector":
                    return new[]
                    {
                        Poly(Rotated(BoxPoly(-0.05f, 0f, 0.5f, 0.2f), 45f)),
                        Cut(Poly(Rotated(BoxPoly(-0.05f, 0f, 0.38f, 0.09f), 45f))),
                        Poly(Rotated(BoxPoly(0.78f, 0f, 0.3f, 0.045f), 45f)),
                        Poly(Rotated(BoxPoly(-0.55f, 0f, 0.07f, 0.34f), 45f)),
                        Poly(Rotated(BoxPoly(-0.78f, 0f, 0.09f, 0.14f), 45f))
                    };
                case "hex":
                    return new[] { Poly(RegPoly(6, 0.92f, 0f)), Cut(Poly(RegPoly(6, 0.55f, 0f))), Poly(RegPoly(6, 0.3f, 0f)) };
                case "gear":
                    return new[] { Poly(GearPoly(8, 0.95f, 0.74f)), Cut(Circ(0f, 0f, 0.34f)) };
                case "crate":
                    return new[]
                    {
                        Box(0f, 0f, 0.85f, 0.85f), Cut(Box(0f, 0f, 0.68f, 0.68f)),
                        Poly(-0.7f, 0.55f, -0.55f, 0.7f, 0.7f, -0.55f, 0.55f, -0.7f),
                        Poly(0.7f, 0.55f, 0.55f, 0.7f, -0.7f, -0.55f, -0.55f, -0.7f)
                    };
                case "radar":
                    return new[] { RingS(0f, 0f, 0.92f, 0.1f), RingS(0f, 0f, 0.55f, 0.08f), Poly(0f, 0f, 0.85f, 0.05f, 0.62f, 0.62f), Circ(0.35f, -0.3f, 0.1f) };
                case "pause":
                    return new[] { Box(-0.34f, 0f, 0.17f, 0.64f), Box(0.34f, 0f, 0.17f, 0.64f) };
                case "back":
                    return new[] { Poly(-0.85f, 0f, -0.1f, 0.65f, -0.1f, 0.25f, 0.85f, 0.25f, 0.85f, -0.25f, -0.1f, -0.25f, -0.1f, -0.65f) };
                case "arrow_right":
                    return new[] { Poly(0.85f, 0f, 0.1f, 0.65f, 0.1f, 0.25f, -0.85f, 0.25f, -0.85f, -0.25f, 0.1f, -0.25f, 0.1f, -0.65f) };
                case "play":
                    return new[] { Poly(-0.55f, 0.8f, 0.85f, 0f, -0.55f, -0.8f) };
                case "check":
                    return new[] { Poly(-0.85f, 0f, -0.6f, 0.25f, -0.25f, -0.1f, 0.6f, 0.75f, 0.85f, 0.5f, -0.25f, -0.62f) };
                case "lock":
                    return new[] { Box(0f, -0.3f, 0.62f, 0.48f), RingS(0f, 0.28f, 0.4f, 0.13f) };
                case "ship_a":
                    return new[]
                    {
                        Poly(1f, 0f, 0.55f, 0.28f, -0.7f, 0.3f, -0.95f, 0.18f, -0.95f, -0.18f, -0.7f, -0.3f, 0.55f, -0.28f),
                        Cut(Box(0.2f, 0f, 0.12f, 0.06f)), Cut(Box(-0.15f, 0f, 0.12f, 0.06f))
                    };
                case "ship_b":
                    return new[]
                    {
                        Poly(1f, 0f, 0.4f, 0.18f, -0.1f, 0.22f, -0.55f, 0.55f, -0.8f, 0.55f, -0.62f, 0.15f, -0.92f, 0.15f, -0.92f, -0.15f, -0.62f, -0.15f, -0.8f, -0.55f, -0.55f, -0.55f, -0.1f, -0.22f, 0.4f, -0.18f),
                        Cut(Box(0.3f, 0f, 0.1f, 0.05f))
                    };
                case "ship_c":
                    return new[]
                    {
                        Poly(0.95f, 0f, 0.6f, 0.18f, 0.5f, 0.5f, 0.15f, 0.55f, 0.05f, 0.25f, -0.4f, 0.3f, -0.6f, 0.62f, -0.9f, 0.62f, -0.8f, 0.2f, -0.95f, 0.2f, -0.95f, -0.2f, -0.8f, -0.2f, -0.9f, -0.62f, -0.6f, -0.62f, -0.4f, -0.3f, 0.05f, -0.25f, 0.15f, -0.55f, 0.5f, -0.5f, 0.6f, -0.18f),
                        Cut(Box(0.35f, 0f, 0.1f, 0.05f))
                    };
                case "ship_boss":
                    return new[]
                    {
                        Poly(1f, 0f, 0.45f, 0.25f, 0.5f, 0.75f, 0.1f, 0.45f, -0.2f, 0.9f, -0.45f, 0.4f, -0.95f, 0.5f, -0.7f, 0.1f, -0.95f, 0f, -0.7f, -0.1f, -0.95f, -0.5f, -0.45f, -0.4f, -0.2f, -0.9f, 0.1f, -0.45f, 0.5f, -0.75f, 0.45f, -0.25f),
                        Cut(Circ(0.1f, 0f, 0.17f)), Circ(0.1f, 0f, 0.07f)
                    };
                case "robot_samurai":
                    return new[]
                    {
                        Poly(-0.5f, -0.75f, -0.55f, 0.15f, -0.2f, 0.5f, 0.2f, 0.5f, 0.55f, 0.15f, 0.5f, -0.75f, 0.2f, -0.55f, -0.2f, -0.55f),
                        Poly(-0.4f, 0.35f, -0.95f, 0.95f, -0.15f, 0.6f),
                        Poly(0.4f, 0.35f, 0.95f, 0.95f, 0.15f, 0.6f),
                        Cut(Box(0f, -0.02f, 0.38f, 0.09f)), Cut(Box(0f, -0.36f, 0.2f, 0.05f))
                    };
                case "robot_techno":
                    return new[]
                    {
                        Poly(Translated(RegPoly(8, 0.78f, 22.5f), 0f, -0.12f)),
                        Box(0f, 0.74f, 0.05f, 0.16f), Circ(0f, 0.9f, 0.1f),
                        Box(-0.86f, -0.12f, 0.07f, 0.22f), Box(0.86f, -0.12f, 0.07f, 0.22f),
                        Cut(Box(0f, -0.08f, 0.5f, 0.15f)),
                        Circ(-0.24f, -0.08f, 0.075f), Circ(0.24f, -0.08f, 0.075f)
                    };
                default:
                    return null;
            }
        }
    }
}
