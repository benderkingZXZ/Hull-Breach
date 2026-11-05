using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HullBreach.UI.Framework
{
    public static class EventSystemGuard
    {
        public static void Ensure()
        {
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                GameObject go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
                AddModule(go);
            }
            else if (es.GetComponent<BaseInputModule>() == null)
            {
                AddModule(es.gameObject);
            }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            else if (es.GetComponent<StandaloneInputModule>() != null)
            {
                Debug.LogWarning("[HullBreachUI] Активен только новый Input System, а на EventSystem стоит StandaloneInputModule. " +
                                 "Замените его на InputSystemUIInputModule (кнопка «Replace with InputSystemUIInputModule») " +
                                 "или включите Active Input Handling = Both.");
            }
#endif
            es.pixelDragThreshold = 2;
        }

        private static void AddModule(GameObject go)
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Type t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t != null)
            {
                go.AddComponent(t);
                return;
            }
#endif
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
