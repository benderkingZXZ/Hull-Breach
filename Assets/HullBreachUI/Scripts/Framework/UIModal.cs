using HullBreach.UI.Contracts;
using HullBreach.UI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Framework
{
    public abstract class UIModal : MonoBehaviour
    {
        protected UIManager Manager { get; private set; }
        protected UIServices Services { get; private set; }
        public bool IsOpen { get; private set; }

        internal void Initialize(UIManager manager, UIServices services)
        {
            Manager = manager;
            Services = services;

            Image dim = UIFactory.Img(transform, "Dim", ProceduralSprites.White, UIColors.Dim);
            dim.raycastTarget = true;

            RectTransform content = UIFactory.Rect("Content", transform);
            UIFactory.Stretch(content);
            BuildContent(content);

            gameObject.SetActive(false);
        }

        protected abstract void BuildContent(RectTransform content);
        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
        public virtual void HandleBack() { Close(); }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            Manager.RegisterModal(this);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            OnOpened();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Manager.UnregisterModal(this);
            gameObject.SetActive(false);
            OnClosed();
        }
    }
}
