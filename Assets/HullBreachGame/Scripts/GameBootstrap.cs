using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Enemies;
using HullBreach.Meta;
using HullBreach.Modules;
using HullBreach.Persistence;
using HullBreach.Player;
using HullBreach.Rooms;
using HullBreach.RunFlow;
using HullBreach.UI.Framework;
using UnityEngine;

namespace HullBreach.Game
{
    /// <summary>
    /// ЕДИНСТВЕННЫЙ объект, нужный в сцене: собирает из кода весь игровой мир и связывает бэкенд с интерфейсом.
    /// Камера → шаблоны (снаряды, игрок, враги, комнаты) → системы бэкенда (GameCore) → UI (HullBreachUIBootstrap).
    /// Когда появятся настоящие префабы и сцена, этот класс можно заменить обычной сценой: контракты не меняются
    /// (см. docs/GAME_INTEGRATION.md).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildUi = true;
        [SerializeField] private bool enemyHealthBars = true;
        [Tooltip("Расстояние между комнатами по X (комната вместе с коридором занимает около 32 единиц).")]
        [SerializeField] private float roomSpacing = 60f;
        [SerializeField] private float cameraSize = 6.5f;

        private static GameBootstrap instance;

        private CameraFollow2D follow;
        private ShipRunManager shipRun;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            ApplyRuntimeSettings();
            Camera worldCamera = CreateCamera();
            follow = worldCamera.GetComponent<CameraFollow2D>();

            // Шаблоны лежат под неактивным родителем: их компоненты не запускаются, клоны оживают на сцене.
            GameObject templatesGo = new GameObject("PrototypeTemplates");
            templatesGo.SetActive(false);
            Transform templates = templatesGo.transform;

            GameObject roomsGo = new GameObject("RoomsRoot");
            roomsGo.transform.position = Vector3.zero;

            Projectile playerShot = PrototypeFactory.CreateProjectile(templates, "PlayerShot", new Color(0.4f, 0.92f, 1f, 1f), 0.34f);
            Projectile enemyShot = PrototypeFactory.CreateProjectile(templates, "EnemyShot", new Color(1f, 0.45f, 0.25f, 1f), 0.4f);

            PlayerController player = PrototypeFactory.CreatePlayer(playerShot);
            player.gameObject.SetActive(true);
            PlayerCombat combat = player.GetComponent<PlayerCombat>();

            EnemyPrefabEntry[] enemies = PrototypeFactory.CreateEnemyEntries(templates, enemyShot, enemyHealthBars);
            RoomTemplateSet[] roomSets = RoomTemplateFactory.CreateTemplateSets(templates);

            // Системы бэкенда на одном объекте. Объект выключен, пока не настроены все ссылки.
            GameObject core = new GameObject("GameCore");
            core.SetActive(false);
            SaveSystem save = core.AddComponent<SaveSystem>();
            ProgressionSystem progression = core.AddComponent<ProgressionSystem>();
            progression.Configure(save);
            SectorMapSystem sector = core.AddComponent<SectorMapSystem>();
            ModuleSystem modules = core.AddComponent<ModuleSystem>();
            modules.Configure(player, combat);
            EnemySpawner spawner = core.AddComponent<EnemySpawner>();
            spawner.Configure(enemies, 0.8f);
            RoomGenerator generator = core.AddComponent<RoomGenerator>();
            generator.Configure(roomSets, roomsGo.transform, roomSpacing);
            shipRun = core.AddComponent<ShipRunManager>();
            shipRun.Configure(generator, spawner, player);
            GameStateManager state = core.AddComponent<GameStateManager>();
            state.Configure(player, progression, modules, sector, shipRun);
            core.SetActive(true);

            follow.SetTarget(player.transform);
            shipRun.RoomEntered += OnRoomEntered;

            if (buildUi)
            {
                GameObject uiGo = new GameObject("HullBreachUI");
                HullBreachUIBootstrap ui = uiGo.AddComponent<HullBreachUIBootstrap>();
                ui.Configure(FontFactory.CreateTheme(), worldCamera);
            }
        }

        private void OnDestroy()
        {
            if (shipRun != null) shipRun.RoomEntered -= OnRoomEntered;
            if (instance == this) instance = null;
        }

        private void OnRoomEntered(int index, int total, RoomController room)
        {
            if (follow == null || room == null) return;
            follow.SetBounds(room.WorldBounds);
            follow.Snap();
        }

        private static void ApplyRuntimeSettings()
        {
            Physics2D.gravity = Vector2.zero;
            Physics2D.IgnoreLayerCollision(PrototypeFactory.PlayerLayer, PrototypeFactory.EnemyLayer, true);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private Camera CreateCamera()
        {
            Camera cam = Camera.main;
            GameObject go;
            if (cam != null)
            {
                go = cam.gameObject;
            }
            else
            {
                go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = cameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f, 1f);
            go.transform.position = new Vector3(0f, 0f, -10f);
            if (go.GetComponent<CameraFollow2D>() == null) go.AddComponent<CameraFollow2D>();
            return cam;
        }
    }
}
