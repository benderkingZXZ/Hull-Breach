using System.Collections;
using HullBreach.UI.Contracts;
using UnityEngine;

namespace HullBreach.UI.Framework
{
    /// <summary>
    /// Основа всех основных экранов. Экран собирает свою иерархию в Build(), показывается и скрывается
    /// только через UIManager (гарантия «один основной экран за раз»).
    /// Подписки на сервисы — в OnShown, отписка — в OnHidden.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        private CanvasGroup group;

        public abstract ScreenId Id { get; }
        public UIManager Manager { get; private set; }
        protected UIServices Services { get; private set; }
        public bool IsShown { get; private set; }

        public virtual float FadeInSeconds { get { return 0.2f; } }
        public virtual float FadeOutSeconds { get { return 0.12f; } }
        /// <summary>Рисовать ли фон меню под экраном. HUD — нет: под ним игровой мир.</summary>
        public virtual bool ShowsBackdrop { get { return true; } }
        /// <summary>Принимать ли касания во время появления (для HUD — да, чтобы управление не «залипало»).</summary>
        protected virtual bool InteractableDuringFade { get { return false; } }

        internal void Initialize(UIManager manager, UIServices services)
        {
            Manager = manager;
            Services = services;
            group = gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            Build();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        /// <summary>Создать иерархию элементов. Вызывается один раз.</summary>
        protected abstract void Build();
        /// <summary>Экран стал активным: прочитать данные, подписаться на события.</summary>
        protected virtual void OnShown() { }
        /// <summary>Экран скрыт: отписаться от событий, сбросить ввод.</summary>
        protected virtual void OnHidden() { }
        /// <summary>Кнопка «Назад» (Android). true — обработано.</summary>
        public virtual bool HandleBack() { return false; }

        internal IEnumerator ShowRoutine()
        {
            gameObject.SetActive(true);
            group.alpha = 0f;
            group.interactable = InteractableDuringFade;
            group.blocksRaycasts = InteractableDuringFade;
            IsShown = true;
            OnShown();

            float duration = FadeInSeconds;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        internal IEnumerator HideRoutine()
        {
            group.interactable = false;
            group.blocksRaycasts = false;

            float duration = FadeOutSeconds;
            float start = group.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / duration));
                yield return null;
            }
            group.alpha = 0f;
            OnHidden();
            IsShown = false;
            gameObject.SetActive(false);
        }
    }
}
