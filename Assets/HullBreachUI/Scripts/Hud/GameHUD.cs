using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Contracts;
using HullBreach.UI.Controls;
using HullBreach.UI.Framework;
using HullBreach.UI.Screens;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Hud
{
    public sealed class GameHUD : UIScreen
    {
        public override ScreenId Id { get { return ScreenId.Hud; } }
        public override bool ShowsBackdrop { get { return false; } }
        public override float FadeInSeconds { get { return 0.1f; } }
        public override float FadeOutSeconds { get { return 0.15f; } }
        protected override bool InteractableDuringFade { get { return true; } }

        private ICombatFeed feed;
        private HealthBarView health;
        private WalletView wallet;
        private ModuleStripView modules;
        private TMP_Text shipName;
        private TMP_Text shipMeta;
        private TMP_Text roomLabel;
        private TMP_Text enemiesLabel;
        private PipsView roomPips;
        private BannerView banner;
        private VirtualJoystick joystick;
        private CombatButtonsUI buttons;
        private RectTransform fxRoot;
        private FloatingTextLayer floating;
        private DamageVignette vignette;
        private TargetMarker marker;

        private int roomIndex;
        private int totalRooms = 1;
        private int lastEnemies = -1;
        private float lastHealth;
        private float suppressPopupsUntil;
        private float lastVibrate = -10f;
        private string pendingBannerText;
        private Color pendingBannerColor;
        private float pendingBannerSeconds;

        protected override void Build()
        {
            feed = Services.Combat;
            RectTransform root = (RectTransform)transform;

            health = HealthBarView.Create(root, 460f, 64f);
            UIFactory.Anchor(health.Rect, 0f, 1f, 0f, 1f, 0f, 0f, 460f, 64f);

            wallet = UIFactory.CreateWallet(root, "Wallet");
            UIFactory.Anchor(wallet.Rect, 0f, 1f, 0f, 1f, 4f, -76f, 300f, 52f);

            modules = new ModuleStripView(root, "Modules", Services.Modules, 56f, true, "");
            UIFactory.Anchor(modules.Root, 0f, 1f, 0f, 1f, 0f, -138f, 460f, 58f);

            RectTransform plate = UIFactory.Rect("ShipPlate", root);
            UIFactory.Anchor(plate, 0.5f, 1f, 0.5f, 1f, 0f, 0f, 460f, 150f);
            UIFactory.Panel(plate, "Panel", UIColors.WithAlpha(UIColors.Hull, 0.82f), UIColors.WithAlpha(UIColors.Steel, 0.9f));

            shipName = UIFactory.Label(plate, "Name", "", 34f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Center, true);
            UIFactory.AnchorStretchX(shipName.rectTransform, 0.04f, 0.96f, 1f, 1f, -8f, 44f);
            UIFactory.FitOneLine(shipName, 18f, 34f);

            shipMeta = UIFactory.Label(plate, "Meta", "", 24f, UIColors.TextDim, FontStyles.Normal, TextAlignmentOptions.Center);
            UIFactory.AnchorStretchX(shipMeta.rectTransform, 0.04f, 0.96f, 1f, 1f, -52f, 32f);
            UIFactory.FitOneLine(shipMeta, 14f, 24f);

            roomLabel = UIFactory.Label(plate, "Room", "", 28f, UIColors.Text, FontStyles.Bold, TextAlignmentOptions.Left);
            UIFactory.AnchorStretchX(roomLabel.rectTransform, 0.05f, 0.55f, 1f, 1f, -88f, 34f);
            UIFactory.FitOneLine(roomLabel, 16f, 28f);

            enemiesLabel = UIFactory.Label(plate, "Enemies", "", 28f, UIColors.Red, FontStyles.Bold, TextAlignmentOptions.Right);
            UIFactory.AnchorStretchX(enemiesLabel.rectTransform, 0.45f, 0.95f, 1f, 1f, -88f, 34f);
            UIFactory.FitOneLine(enemiesLabel, 16f, 28f);

            RectTransform pipsRect = UIFactory.Rect("RoomPips", plate);
            UIFactory.AnchorStretchX(pipsRect, 0.05f, 0.95f, 1f, 1f, -124f, 16f);
            UIFactory.HorizontalGroup(pipsRect, 6f, TextAnchor.MiddleCenter);
            roomPips = new PipsView(pipsRect, 64f, 12f);

            RectTransform pause = UIFactory.Rect("Pause", root);
            UIFactory.Anchor(pause, 1f, 1f, 1f, 1f, 0f, 0f, 92f, 92f);
            Image pauseBack = UIFactory.AddImage(pause.gameObject, ProceduralSprites.Circle, UIColors.WithAlpha(UIColors.Hull, 0.8f), Image.Type.Simple, true);
            UIFactory.Img(pause, "Ring", ProceduralSprites.Ring, UIColors.WithAlpha(UIColors.Cyan, 0.8f));
            Image pauseIcon = UIFactory.Img(pause, "Icon", UIIcons.Get("pause"), UIColors.Text);
            pauseIcon.preserveAspect = true;
            UIFactory.Stretch(pauseIcon.rectTransform, 27f, 27f, 27f, 27f);
            Button pauseButton = pause.gameObject.AddComponent<Button>();
            pauseButton.targetGraphic = pauseBack;
            Navigation nav = pauseButton.navigation;
            nav.mode = Navigation.Mode.None;
            pauseButton.navigation = nav;
            pauseButton.onClick.AddListener(() => Manager.OpenPauseMenu());
            pause.gameObject.AddComponent<PressScale>();

            banner = BannerView.Create(root, "EventBanner", 780f, 78f, 34f);
            UIFactory.Anchor(banner.Rect, 0.5f, 1f, 0.5f, 1f, 0f, -170f, 780f, 78f);

            joystick = VirtualJoystick.Create(root, Services.Input);
            buttons = CombatButtonsUI.Create(root, Services.Input, feed, Services.Modules);

            fxRoot = UIFactory.Rect("HudFx", Manager.Root.FxLayer);
            UIFactory.Stretch(fxRoot);
            vignette = DamageVignette.Create(fxRoot);
            floating = FloatingTextLayer.Create(fxRoot);
            marker = TargetMarker.Create(fxRoot, feed, Manager.Root);
            fxRoot.gameObject.SetActive(false);

            Subscribe();
        }

        private void Subscribe()
        {
            feed.HealthChanged += OnHealthChanged;
            feed.PlayerDamaged += OnDamaged;
            feed.ShipStarted += OnShipStarted;
            feed.RoomEntered += OnRoomEntered;
            feed.RoomCleared += OnRoomCleared;
            feed.ResourcesEarned += OnResourcesEarned;
            feed.ShipFailed += OnShipFailed;
            Services.Modules.ActiveModuleUsed += OnModuleUsed;
            Services.Modules.OwnedModulesChanged += OnModulesChanged;
            Services.Progression.ResourcesChanged += OnResourcesChanged;
        }

        private void OnDestroy()
        {
            if (feed == null) return;
            feed.HealthChanged -= OnHealthChanged;
            feed.PlayerDamaged -= OnDamaged;
            feed.ShipStarted -= OnShipStarted;
            feed.RoomEntered -= OnRoomEntered;
            feed.RoomCleared -= OnRoomCleared;
            feed.ResourcesEarned -= OnResourcesEarned;
            feed.ShipFailed -= OnShipFailed;
            if (Services != null)
            {
                if (Services.Modules != null)
                {
                    Services.Modules.ActiveModuleUsed -= OnModuleUsed;
                    Services.Modules.OwnedModulesChanged -= OnModulesChanged;
                }
                if (Services.Progression != null) Services.Progression.ResourcesChanged -= OnResourcesChanged;
            }
            if (fxRoot != null) Destroy(fxRoot.gameObject);
        }

        protected override void OnShown()
        {
            fxRoot.gameObject.SetActive(true);
            marker.gameObject.SetActive(true);
            vignette.ResetState();
            floating.ClearAll();
            banner.HideNow();

            health.SetValues(feed.CurrentHealth, feed.MaxHealth);
            health.SetShield(feed.Shield);
            health.Snap();
            lastHealth = feed.CurrentHealth;
            vignette.SetLowHealth(feed.IsAlive && feed.CurrentHealth / Mathf.Max(1f, feed.MaxHealth) < 0.3f);

            wallet.SetValue(Services.Progression.Resources, true);
            modules.Refresh(null);

            ShipDefinition ship = feed.IsRunning ? null : Services.Flow.SelectedShip;
            EntryPointType entry = Services.Flow.SelectedEntryPoint;
            if (ship != null) SetShipInfo(ship, entry);

            roomIndex = feed.IsRunning ? feed.CurrentRoomIndex : 0;
            totalRooms = feed.IsRunning ? Mathf.Max(1, feed.TotalRooms) : EntryRoomCount(entry);
            lastEnemies = -1;
            UpdateRoomUi();

            suppressPopupsUntil = Time.unscaledTime + 0.8f;

            if (pendingBannerText != null)
            {
                banner.Show(pendingBannerText, pendingBannerColor, pendingBannerSeconds);
                pendingBannerText = null;
            }
        }

        protected override void OnHidden()
        {
            fxRoot.gameObject.SetActive(false);
            floating.ClearAll();
        }

        public override bool HandleBack()
        {
            Manager.OpenPauseMenu();
            return true;
        }

        private void Update()
        {
            if (feed == null) return;

            health.SetValues(feed.CurrentHealth, feed.MaxHealth);
            health.SetShield(feed.Shield);

            int enemies = feed.EnemiesRemaining;
            if (enemies != lastEnemies)
            {
                lastEnemies = enemies;
                if (enemies > 0)
                {
                    enemiesLabel.text = "ВРАГОВ: " + enemies;
                    enemiesLabel.color = UIColors.Red;
                }
                else
                {
                    enemiesLabel.text = "ЧИСТО";
                    enemiesLabel.color = UIColors.Green;
                }
            }

            modules.Tick();
        }

        private void OnHealthChanged(float current, float max)
        {
            bool critical = current > 0f && current / Mathf.Max(1f, max) < 0.3f;
            vignette.SetLowHealth(critical);

            float gained = current - lastHealth;
            lastHealth = current;
            if (!IsShown || gained < 0.5f) return;

            health.Pulse(false);
            if (Time.unscaledTime < suppressPopupsUntil || !UISettings.DamageNumbers) return;
            Vector2 local;
            if (TryPlayerLocal(out local)) floating.Spawn("+" + Mathf.RoundToInt(gained), UIColors.Green, local + new Vector2(0f, 70f), 40f);
        }

        private void OnDamaged(DamageEvent e)
        {
            if (!IsShown) return;
            Vector2 local;
            bool hasPos = TryPlayerLocal(out local);

            if (e.Absorbed)
            {
                if (hasPos && UISettings.DamageNumbers) floating.Spawn("ЩИТ", UIColors.Cyan, local + new Vector2(0f, 70f), 36f);
                return;
            }

            float max = Mathf.Max(1f, feed.MaxHealth);
            vignette.Flash(Mathf.Clamp01(0.3f + e.Amount / max * 2.5f));
            health.Pulse(true);
            if (hasPos && UISettings.DamageNumbers)
                floating.Spawn("-" + Mathf.CeilToInt(e.Amount), UIColors.Red, local + new Vector2(0f, 70f), 46f);
            if (e.Amount >= max * 0.12f) Vibrate();
        }

        private void OnShipStarted(ShipDefinition ship, EntryPointType entry)
        {
            SetShipInfo(ship, entry);
            roomIndex = 0;
            totalRooms = Mathf.Max(1, feed.TotalRooms);
            lastEnemies = -1;
            UpdateRoomUi();
            suppressPopupsUntil = Time.unscaledTime + 0.8f;
            ShowBanner("АБОРДАЖ: " + EntryName(entry).ToUpperInvariant(), UIColors.Cyan, 2.6f);
        }

        private void OnRoomEntered(RoomInfo room)
        {
            roomIndex = room.Index;
            totalRooms = Mathf.Max(1, room.Total);
            lastEnemies = -1;
            UpdateRoomUi();

            if (room.Role == RoomRole.Reactor)
                ShowBanner("ОТСЕК РЕАКТОРА: уничтожьте мини-босса", UIColors.Red, 3.2f);
            else if (room.Index > 0)
                ShowBanner("ОТСЕК " + (room.Index + 1) + " ИЗ " + room.Total, UIColors.Cyan, 1.6f);
        }

        private void OnRoomCleared(int index)
        {
            if (index + 1 >= totalRooms) ShowBanner("РЕАКТОР УНИЧТОЖЕН", UIColors.Green, 2.4f);
            else ShowBanner("ОТСЕК ЗАЧИЩЕН. ДВЕРИ ОТКРЫТЫ", UIColors.Green, 2.2f);
            roomPips.Set(Mathf.Min(totalRooms, index + 1), totalRooms, UIColors.Green, UIColors.WithAlpha(UIColors.Steel, 0.55f));
        }

        private void OnResourcesEarned(int amount, Vector3 worldPosition)
        {
            if (!IsShown || !UISettings.DamageNumbers) return;
            Vector2 local;
            if (floating.TryWorldToLocal(Manager.Root.ResolveCamera(), worldPosition, out local))
                floating.Spawn("+" + amount, UIColors.Amber, local, 38f, 90f, 0.9f);
        }

        private void OnShipFailed()
        {
            vignette.Flash(1f);
            ShowBanner("СИСТЕМЫ ОТКАЗАЛИ", UIColors.Red, 2.6f);
        }

        private void OnModuleUsed(ModuleInstance module)
        {
            if (IsShown) banner.Show(module.DisplayName + ": активирован", UIColors.Rarity(module.Rarity), 1.3f);
        }

        private void OnModulesChanged()
        {
            modules.Refresh(null);
        }

        private void OnResourcesChanged(int value)
        {
            wallet.SetValue(value, false);
        }

        private void ShowBanner(string text, Color color, float seconds)
        {
            if (IsShown)
            {
                banner.Show(text, color, seconds);
            }
            else
            {
                pendingBannerText = text;
                pendingBannerColor = color;
                pendingBannerSeconds = seconds;
            }
        }

        private bool TryPlayerLocal(out Vector2 local)
        {
            local = Vector2.zero;
            Vector3 world;
            if (!feed.TryGetPlayerPosition(out world)) return false;
            return floating.TryWorldToLocal(Manager.Root.ResolveCamera(), world, out local);
        }

        private void SetShipInfo(ShipDefinition ship, EntryPointType entry)
        {
            shipName.text = ship.DisplayName;
            string risk = ColorUtility.ToHtmlStringRGB(UIColors.Risk(ship.Risk));
            shipMeta.text = "<color=#" + risk + ">" + UIStrings.Risk(ship.Risk) + "</color>   " + EntryName(entry);
        }

        private string EntryName(EntryPointType entry)
        {
            System.Collections.Generic.IReadOnlyList<EntryPointProfile> list = Services.Flow.GetEntryPoints();
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Entry == entry) return list[i].DisplayName;
            }
            return UIStrings.EntryShortName(entry);
        }

        private int EntryRoomCount(EntryPointType entry)
        {
            System.Collections.Generic.IReadOnlyList<EntryPointProfile> list = Services.Flow.GetEntryPoints();
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Entry == entry) return list[i].RoomCount;
            }
            return 3;
        }

        private void UpdateRoomUi()
        {
            roomLabel.text = "ОТСЕК " + (roomIndex + 1) + "/" + totalRooms;
            roomPips.Set(Mathf.Clamp(roomIndex, 0, totalRooms), totalRooms, UIColors.Green, UIColors.WithAlpha(UIColors.Steel, 0.55f));
            roomPips.Highlight(Mathf.Clamp(roomIndex, 0, totalRooms - 1), UIColors.Cyan);
        }

        private void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!UISettings.Vibration) return;
            if (Time.unscaledTime - lastVibrate < 1.5f) return;
            lastVibrate = Time.unscaledTime;
            Handheld.Vibrate();
#endif
        }
    }
}
