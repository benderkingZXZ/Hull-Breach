using System;
using HullBreach.Core;
using HullBreach.Meta;
using HullBreach.Modules;
using HullBreach.Player;
using HullBreach.RunFlow;
using HullBreach.UI.Bridge;
using HullBreach.UI.Contracts;
using HullBreach.UI.Mock;
using HullBreach.UI.Theme;
using UnityEngine;

namespace HullBreach.UI.Framework
{
    [DefaultExecutionOrder(-20)]
    public sealed class HullBreachUIBootstrap : MonoBehaviour
    {
        public enum ServiceMode
        {
            Auto,
            Backend,
            Mock
        }

        [Header("Режим")]
        [SerializeField] private ServiceMode serviceMode = ServiceMode.Auto;

        [Header("Оформление и камера")]
        [Tooltip("Необязательно. Подмена шрифтов (нужна кириллица!) и спрайтов.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Камера игрового мира для привязки чисел урона к врагам. Если пусто — Camera.main.")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private int canvasSortingOrder = 100;
        [SerializeField] private bool forceLandscape = true;
        [SerializeField] private bool dontDestroyOnLoad = false;

        [Header("Ссылки на бэкенд (необязательно — иначе ищутся автоматически)")]
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private ProgressionSystem progression;
        [SerializeField] private ModuleSystem moduleSystem;
        [SerializeField] private ShipRunManager shipRunManager;
        [SerializeField] private PlayerController player;

        private UIServices services;
        private IDisposable bridge;
        private MockGameHost mockHost;
        private UIRootRefs root;

        public UIServices Services { get { return services; } }

        public void Configure(UITheme newTheme, Camera newWorldCamera)
        {
            theme = newTheme;
            worldCamera = newWorldCamera;
        }

        private void Start()
        {
            UIAssets.Initialize(theme);
            UISettings.Load();

            if (forceLandscape)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }

            EventSystemGuard.Ensure();
            services = CreateServices();

            root = UIRootBuilder.Build(canvasSortingOrder, worldCamera);
            if (dontDestroyOnLoad) DontDestroyOnLoad(root.RootObject);

            UIManager manager = root.RootObject.AddComponent<UIManager>();
            manager.Initialize(services, root);
            manager.Begin();

            if (services.IsMock && mockHost != null) MockDebugPanel.Create(root, mockHost);
        }

        private UIServices CreateServices()
        {
            if (serviceMode != ServiceMode.Mock)
            {
                BackendRefs refs = new BackendRefs();
                refs.State = gameStateManager;
                refs.Progression = progression;
                refs.Modules = moduleSystem;
                refs.ShipRun = shipRunManager;
                refs.Player = player;

                string error;
                UIServices created = BackendBinder.TryCreate(refs, out bridge, out error);
                if (created != null)
                {
                    Debug.Log("[HullBreachUI] Подключён игровой модуль (бэкенд).");
                    return created;
                }

                if (serviceMode == ServiceMode.Backend)
                    Debug.LogError("[HullBreachUI] Бэкенд не подключён: " + error + " Запускаю заглушку.");
                else
                    Debug.Log("[HullBreachUI] Бэкенд не найден (" + error + "). Работает заглушка MockGameHost.");
            }

            GameObject go = new GameObject("HullBreachMockServices");
            mockHost = go.AddComponent<MockGameHost>();
            return mockHost.CreateServices();
        }

        private void OnDestroy()
        {
            if (bridge != null) bridge.Dispose();
            bridge = null;
            if (root != null && root.RootObject != null) Destroy(root.RootObject);
            if (mockHost != null) Destroy(mockHost.gameObject);
        }
    }
}
