using System;
using System.Collections;
using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.UI.Contracts;
using HullBreach.UI.Hud;
using HullBreach.UI.Screens;
using HullBreach.UI.Theme;
using UnityEngine;

namespace HullBreach.UI.Framework
{
    public sealed class UIManager : MonoBehaviour
    {
        private readonly Dictionary<ScreenId, UIScreen> screens = new Dictionary<ScreenId, UIScreen>();
        private readonly List<UIModal> modals = new List<UIModal>();

        private UIServices services;
        private UIRootRefs root;
        private UIScreen current;
        private ScreenId requested = ScreenId.None;
        private Coroutine switchRoutine;
        private bool started;
        private bool paused;
        private float savedTimeScale = 1f;

        private BannerView toast;
        private ConfirmDialogUI confirmDialog;
        private SettingsPanelUI settingsPanel;
        private PauseMenuUI pauseMenu;

        public UIServices Services { get { return services; } }
        public UIRootRefs Root { get { return root; } }
        public ScreenId CurrentScreenId { get; private set; }
        public bool IsPaused { get { return paused; } }
        public bool HasOpenModal { get { return modals.Count > 0; } }

        public void Initialize(UIServices svc, UIRootRefs rootRefs)
        {
            services = svc;
            root = rootRefs;

            AddScreen<MainMenuUI>("MainMenu");
            AddScreen<SectorMapUI>("SectorMap");
            AddScreen<EntrySelectionUI>("EntrySelection");
            AddScreen<GameHUD>("GameHUD");
            AddScreen<ModuleSelectionUI>("ModuleSelection");
            AddScreen<DeathScreenUI>("DeathScreen");
            AddScreen<UpgradeScreenUI>("UpgradeScreen");
            AddScreen<VictoryScreenUI>("VictoryScreen");

            confirmDialog = AddModal<ConfirmDialogUI>("ConfirmDialog");
            settingsPanel = AddModal<SettingsPanelUI>("SettingsPanel");
            pauseMenu = AddModal<PauseMenuUI>("PauseMenu");

            toast = BannerView.Create(root.OverlayLayer, "Toast", 900f, 84f, 34f);
            UIFactory.Anchor(toast.Rect, 0.5f, 0f, 0.5f, 0f, 0f, 150f, 900f, 84f);
        }

        public void Begin()
        {
            services.Flow.StateChanged += HandleStateChanged;
            started = true;
            ShowForState(services.Flow.CurrentState);
        }

        private T AddScreen<T>(string name) where T : UIScreen
        {
            RectTransform rt = UIFactory.Rect(name, root.ScreensLayer);
            UIFactory.Stretch(rt);
            T screen = rt.gameObject.AddComponent<T>();
            screen.Initialize(this, services);
            screens[screen.Id] = screen;
            return screen;
        }

        private T AddModal<T>(string name) where T : UIModal
        {
            RectTransform rt = UIFactory.Rect(name, root.ModalLayer);
            UIFactory.Stretch(rt);
            T modal = rt.gameObject.AddComponent<T>();
            modal.Initialize(this, services);
            return modal;
        }

        private void OnDestroy()
        {
            if (services != null && services.Flow != null) services.Flow.StateChanged -= HandleStateChanged;
            if (paused) Time.timeScale = savedTimeScale;
            paused = false;
        }

        private void HandleStateChanged(GameState previous, GameState next)
        {
            CloseAllModals();
            SetPaused(false);
            ShowForState(next);
        }

        public static ScreenId ScreenFor(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu: return ScreenId.MainMenu;
                case GameState.SectorMap: return ScreenId.SectorMap;
                case GameState.ShipSelection: return ScreenId.EntrySelection;
                case GameState.Combat: return ScreenId.Hud;
                case GameState.Reward: return ScreenId.ModuleSelection;
                case GameState.Death: return ScreenId.Death;
                case GameState.Upgrades: return ScreenId.Upgrades;
                case GameState.Victory: return ScreenId.Victory;
                default: return ScreenId.MainMenu;
            }
        }

        public void ShowForState(GameState state)
        {
            Show(ScreenFor(state));
        }

        public void Show(ScreenId id)
        {
            requested = id;
            if (switchRoutine == null) switchRoutine = StartCoroutine(SwitchLoop());
        }

        private IEnumerator SwitchLoop()
        {
            while (current == null || current.Id != requested)
            {
                if (current != null)
                {
                    UIScreen old = current;
                    yield return StartCoroutine(old.HideRoutine());
                    current = null;
                }

                UIScreen next;
                if (!screens.TryGetValue(requested, out next))
                {
                    Debug.LogError("[HullBreachUI] Нет экрана " + requested);
                    break;
                }

                root.BackdropLayer.gameObject.SetActive(next.ShowsBackdrop);
                current = next;
                CurrentScreenId = next.Id;
                yield return StartCoroutine(next.ShowRoutine());
            }
            switchRoutine = null;
        }

        internal void RegisterModal(UIModal modal)
        {
            if (!modals.Contains(modal)) modals.Add(modal);
        }

        internal void UnregisterModal(UIModal modal)
        {
            modals.Remove(modal);
        }

        public void CloseAllModals()
        {
            UIModal[] copy = modals.ToArray();
            for (int i = copy.Length - 1; i >= 0; i--) copy[i].Close();
        }

        public void Confirm(string title, string message, string confirmLabel, Action onConfirm, bool danger = false, string cancelLabel = null)
        {
            confirmDialog.Present(title, message, confirmLabel, cancelLabel ?? UIStrings.Cancel, onConfirm, danger);
        }

        public void OpenSettings(bool allowProgressReset)
        {
            settingsPanel.Present(allowProgressReset);
        }

        public void OpenPauseMenu()
        {
            if (services == null || services.Flow.CurrentState != GameState.Combat || pauseMenu.IsOpen) return;
            SetPaused(true);
            pauseMenu.Open();
        }

        public void SetPaused(bool value)
        {
            if (value == paused) return;
            paused = value;
            if (value)
            {
                savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = savedTimeScale;
            }
        }

        public void Toast(string message, bool warning = false)
        {
            if (toast != null) toast.Show(message, warning ? UIColors.Amber : UIColors.Cyan, 2.2f);
        }

        public void HandleBack()
        {
            if (modals.Count > 0)
            {
                modals[modals.Count - 1].HandleBack();
                return;
            }
            if (current != null && current.IsShown) current.HandleBack();
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) HandleBack();
#endif
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) return;
            UISettings.Flush();
            if (started && services != null && services.Flow.CurrentState == GameState.Combat) OpenPauseMenu();
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
