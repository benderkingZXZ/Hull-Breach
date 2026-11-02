using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HullBreach.UI.Controls
{
    /// <summary>
    /// Мультитач-кнопка: реагирует на нажатие и отпускание, следит за своим pointerId.
    /// Второй палец на той же кнопке игнорируется; отпускание «мимо» кнопки всё равно приходит.
    /// </summary>
    public sealed class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private int pointerId = int.MinValue;

        public bool IsHeld { get; private set; }
        public event Action Pressed;
        public event Action Released;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld) return;
            IsHeld = true;
            pointerId = eventData.pointerId;
            Action handler = Pressed;
            if (handler != null) handler();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsHeld || eventData.pointerId != pointerId) return;
            Release();
        }

        private void OnDisable()
        {
            if (IsHeld) Release();
        }

        private void Release()
        {
            IsHeld = false;
            pointerId = int.MinValue;
            Action handler = Released;
            if (handler != null) handler();
        }
    }
}
