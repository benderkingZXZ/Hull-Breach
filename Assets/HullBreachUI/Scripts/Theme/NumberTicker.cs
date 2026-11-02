using System.Globalization;
using TMPro;
using UnityEngine;

namespace HullBreach.UI.Theme
{
    /// <summary>Плавно «накручивает» число в TMP-тексте (ресурсы, награды). Использует unscaled-время.</summary>
    public sealed class NumberTicker : MonoBehaviour
    {
        public TMP_Text Label;
        public string Prefix = "";
        public string Suffix = "";
        public float Speed = 2.2f;   // доля полного пути в секунду, но не менее MinPerSecond
        public float MinPerSecond = 40f;

        private float shown;
        private int target;
        private int lastRendered = int.MinValue;

        public int Target { get { return target; } }

        public void Set(int value, bool instant)
        {
            target = value;
            if (instant) shown = value;
            Render();
        }

        private void OnEnable()
        {
            lastRendered = int.MinValue;
            Render();
        }

        private void Update()
        {
            if (Mathf.Approximately(shown, target)) return;
            float distance = Mathf.Abs(target - shown);
            float step = Mathf.Max(distance * Speed, MinPerSecond) * Time.unscaledDeltaTime;
            shown = Mathf.MoveTowards(shown, target, step);
            Render();
        }

        private void Render()
        {
            if (Label == null) return;
            int v = Mathf.RoundToInt(shown);
            if (v == lastRendered) return;
            lastRendered = v;
            Label.text = Prefix + UIFormat.Number(v) + Suffix;
        }
    }

    /// <summary>Форматирование чисел и времени для интерфейса.</summary>
    public static class UIFormat
    {
        public static string Number(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
        }

        public static string Time(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        /// <summary>Русское склонение: 1 отсек, 2 отсека, 5 отсеков.</summary>
        public static string Plural(int n, string one, string few, string many)
        {
            int m100 = Mathf.Abs(n) % 100;
            int m10 = m100 % 10;
            if (m100 >= 11 && m100 <= 14) return many;
            if (m10 == 1) return one;
            if (m10 >= 2 && m10 <= 4) return few;
            return many;
        }
    }
}
