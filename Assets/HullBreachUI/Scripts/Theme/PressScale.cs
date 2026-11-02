using UnityEngine;
using UnityEngine.EventSystems;

namespace HullBreach.UI.Theme
{
    /// <summary>Лёгкая реакция на нажатие: элемент слегка уменьшается, пока палец на нём.</summary>
    public sealed class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float PressedScale = 0.95f;
        public float Speed = 18f;

        private float target = 1f;
        private float current = 1f;

        private void OnEnable()
        {
            target = 1f;
            current = 1f;
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData) { target = PressedScale; }
        public void OnPointerUp(PointerEventData eventData) { target = 1f; }
        public void OnPointerExit(PointerEventData eventData) { target = 1f; }

        private void Update()
        {
            if (Mathf.Approximately(current, target)) return;
            current = Mathf.MoveTowards(current, target, Speed * Time.unscaledDeltaTime * 0.5f);
            transform.localScale = new Vector3(current, current, 1f);
        }
    }
}
