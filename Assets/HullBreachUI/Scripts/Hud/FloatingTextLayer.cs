using System.Collections.Generic;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;

namespace HullBreach.UI.Hud
{
    public sealed class FloatingTextLayer : MonoBehaviour
    {
        private sealed class Item
        {
            public RectTransform Rect;
            public TMP_Text Text;
            public bool Active;
            public float Age;
            public float Duration;
            public float Lift;
            public Vector2 Start;
            public Color Color;
        }

        private const int PoolSize = 24;
        private readonly List<Item> items = new List<Item>(PoolSize);
        private RectTransform layer;
        private int cursor;

        public static FloatingTextLayer Create(Transform parent)
        {
            RectTransform rt = UIFactory.Rect("FloatingText", parent);
            UIFactory.Stretch(rt);
            FloatingTextLayer l = rt.gameObject.AddComponent<FloatingTextLayer>();
            l.layer = rt;
            for (int i = 0; i < PoolSize; i++) l.items.Add(l.CreateItem());
            return l;
        }

        private Item CreateItem()
        {
            TMP_Text t = UIFactory.Label(layer, "Number", "", 44f, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
            t.enableWordWrapping = false;
            RectTransform r = t.rectTransform;
            UIFactory.Anchor(r, 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 300f, 70f);
            t.gameObject.SetActive(false);
            Item it = new Item();
            it.Rect = r;
            it.Text = t;
            return it;
        }

        public void Spawn(string text, Color color, Vector2 localPosition, float size = 44f, float lift = 110f, float duration = 0.95f)
        {
            Item it = items[cursor];
            cursor = (cursor + 1) % items.Count;
            it.Text.text = text;
            it.Text.fontSize = size;
            it.Active = true;
            it.Age = 0f;
            it.Duration = duration;
            it.Lift = lift;
            it.Color = color;
            it.Start = localPosition + new Vector2(Random.Range(-28f, 28f), 0f);
            it.Rect.anchoredPosition = it.Start;
            it.Text.color = UIColors.WithAlpha(color, 0f);
            it.Text.gameObject.SetActive(true);
        }

        public bool TryWorldToLocal(Camera cam, Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, new Vector2(sp.x, sp.y), null, out local);
        }

        public void ClearAll()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Active = false;
                items[i].Text.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < items.Count; i++)
            {
                Item it = items[i];
                if (!it.Active) continue;
                it.Age += dt;
                float p = it.Age / it.Duration;
                if (p >= 1f)
                {
                    it.Active = false;
                    it.Text.gameObject.SetActive(false);
                    continue;
                }
                float rise = 1f - (1f - p) * (1f - p);
                it.Rect.anchoredPosition = it.Start + new Vector2(0f, it.Lift * rise);
                float pop = p < 0.12f ? Mathf.Lerp(1.5f, 1f, p / 0.12f) : 1f;
                it.Rect.localScale = new Vector3(pop, pop, 1f);
                float alpha = p < 0.1f ? p / 0.1f : 1f - Mathf.Pow((p - 0.1f) / 0.9f, 2.2f);
                it.Text.color = UIColors.WithAlpha(it.Color, Mathf.Clamp01(alpha));
            }
        }
    }
}
