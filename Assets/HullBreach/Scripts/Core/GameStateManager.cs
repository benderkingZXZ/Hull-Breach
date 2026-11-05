using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HullBreach.Data;
using HullBreach.Meta;
using HullBreach.Modules;
using HullBreach.Enemies;
using HullBreach.Player;
using HullBreach.RunFlow;

namespace HullBreach.Core
{
    public class GameStateManager : MonoBehaviour
    {
        [Header("Системы (обязательные ссылки)")]
        [SerializeField] private PlayerController player;
        [SerializeField] private ProgressionSystem progression;
        [SerializeField] private ModuleSystem moduleSystem;
        [SerializeField] private SectorMapSystem sectorMap;
        [SerializeField] private ShipRunManager shipRun;

        [Header("Настройки")]
        [Tooltip("Скрывать объект игрока вне боя (SetActive(false)).")]
        [SerializeField] private bool hidePlayerOutsideCombat = true;
        [Tooltip("Полностью лечить робота при входе на каждый новый корабль (иначе здоровье переносится).")]
        [SerializeField] private bool restoreHealthOnShipStart = true;
        [Tooltip("Пауза между гибелью робота и состоянием Death (для анимации).")]
        [SerializeField] private float deathScreenDelay = 1.2f;

        private GameState currentState = GameState.MainMenu;
        private Coroutine deathRoutine;
        private float runStartTime;
        private int runRoomsCleared;
        private int runEnemiesKilled;

        public event Action<GameState, GameState> StateChanged;
        public event Action<ShipRewardInfo> RewardPrepared;
        public event Action<RunSummary> RunFailed;
        public event Action<RunSummary> SectorVictory;

        public GameState CurrentState { get { return currentState; } }
        public ShipDefinition SelectedShip { get; private set; }
        public EntryPointType SelectedEntryPoint { get; private set; }
        public ShipRewardInfo CurrentReward { get; private set; }
        public RunSummary LastRunSummary { get; private set; }
        public int RunShipsCaptured { get; private set; }
        public int RunResourcesEarned { get; private set; }

        private void OnEnable()
        {
            SubscribeSystems(true);
        }

        private void OnDisable()
        {
            SubscribeSystems(false);
        }

        private void SubscribeSystems(bool subscribe)
        {
            if (shipRun != null)
            {
                if (subscribe)
                {
                    shipRun.ShipCompleted += HandleShipCompleted;
                    shipRun.ShipFailed += HandleShipFailed;
                    shipRun.RoomCleared += HandleRoomCleared;
                    shipRun.EnemyKilled += HandleEnemyKilled;
                    shipRun.ResourcesEarned += HandleResourcesEarned;
                }
                else
                {
                    shipRun.ShipCompleted -= HandleShipCompleted;
                    shipRun.ShipFailed -= HandleShipFailed;
                    shipRun.RoomCleared -= HandleRoomCleared;
                    shipRun.EnemyKilled -= HandleEnemyKilled;
                    shipRun.ResourcesEarned -= HandleResourcesEarned;
                }
            }
            if (moduleSystem != null)
            {
                if (subscribe) moduleSystem.ModuleAcquired += HandleModuleAcquired;
                else moduleSystem.ModuleAcquired -= HandleModuleAcquired;
            }
        }

        public void Configure(PlayerController newPlayer, ProgressionSystem newProgression, ModuleSystem newModules,
            SectorMapSystem newSectorMap, ShipRunManager newShipRun)
        {
            bool wasSubscribed = isActiveAndEnabled;
            if (wasSubscribed) SubscribeSystems(false);
            player = newPlayer;
            progression = newProgression;
            moduleSystem = newModules;
            sectorMap = newSectorMap;
            shipRun = newShipRun;
            if (wasSubscribed) SubscribeSystems(true);
        }

        private void Start()
        {
            if (player == null || progression == null || moduleSystem == null || sectorMap == null || shipRun == null)
            {
                Debug.LogError("[HullBreach] GameStateManager: назначены не все ссылки на системы (см. README).", this);
                return;
            }
            RebuildPlayerStats(false);
            ApplyStateSideEffects(currentState);
        }

        public IReadOnlyList<ShipDefinition> GetShipOptions() { return sectorMap.CurrentOptions; }
        public IReadOnlyList<EntryPointProfile> GetEntryPoints() { return EntryPointCatalog.All; }

        public bool SelectRobot(RobotType robot)
        {
            if (currentState != GameState.MainMenu && currentState != GameState.Upgrades) return Reject("SelectRobot");
            progression.SetSelectedRobot(robot);
            RebuildPlayerStats(false);
            return true;
        }

        public bool StartSectorRun()
        {
            if (!IsTransitionAllowed(currentState, GameState.SectorMap) || currentState == GameState.Reward
                || currentState == GameState.ShipSelection || currentState == GameState.Combat)
                return Reject("StartSectorRun");

            moduleSystem.ResetForNewRun();
            sectorMap.ResetSector();
            ResetRunCounters();
            runStartTime = Time.time;
            SelectedShip = null;
            CurrentReward = null;
            RebuildPlayerStats(false);
            return TransitionTo(GameState.SectorMap);
        }

        public bool SelectShip(string shipId)
        {
            if (currentState != GameState.SectorMap) return Reject("SelectShip");
            ShipDefinition ship = sectorMap.GetOptionById(shipId);
            if (ship == null) return Reject("SelectShip (неизвестный id: " + shipId + ")");
            SelectedShip = ship;
            return TransitionTo(GameState.ShipSelection);
        }

        public bool CancelShipSelection()
        {
            if (currentState != GameState.ShipSelection) return Reject("CancelShipSelection");
            SelectedShip = null;
            return TransitionTo(GameState.SectorMap);
        }

        public bool ConfirmEntryPoint(EntryPointType entry)
        {
            if (currentState != GameState.ShipSelection || SelectedShip == null) return Reject("ConfirmEntryPoint");

            SelectedEntryPoint = entry;
            if (!TransitionTo(GameState.Combat)) return false;

            RebuildPlayerStats(true);
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat != null) combat.ResetCooldowns();
            moduleSystem.ResetCooldowns();

            bool restore = restoreHealthOnShipStart || RunShipsCaptured == 0;
            player.ResetForShip(player.transform.position, restore);

            if (!shipRun.StartShip(SelectedShip, entry))
            {
                Debug.LogError("[HullBreach] Не удалось запустить корабль — возврат к выбору входа.", this);
                TransitionTo(GameState.ShipSelection, true);
                return false;
            }
            return true;
        }

        public bool ChooseModule(int offerIndex)
        {
            if (currentState != GameState.Reward) return Reject("ChooseModule");
            if (!moduleSystem.ChooseOffer(offerIndex)) return Reject("ChooseModule (неверный индекс)");
            CurrentReward = null;
            SelectedShip = null;
            return TransitionTo(GameState.SectorMap);
        }

        public bool AbandonRun()
        {
            if (currentState != GameState.SectorMap && currentState != GameState.ShipSelection
                && currentState != GameState.Combat && currentState != GameState.Reward)
                return Reject("AbandonRun");

            if (deathRoutine != null) { StopCoroutine(deathRoutine); deathRoutine = null; }
            shipRun.AbortShip();
            progression.RegisterRunEnded(false, RunShipsCaptured);
            LastRunSummary = BuildSummary(false);
            SelectedShip = null;
            CurrentReward = null;
            return TransitionTo(GameState.MainMenu);
        }

        public bool OpenUpgrades()
        {
            if (!IsTransitionAllowed(currentState, GameState.Upgrades)) return Reject("OpenUpgrades");
            return TransitionTo(GameState.Upgrades);
        }

        public bool CloseUpgrades()
        {
            if (currentState != GameState.Upgrades) return Reject("CloseUpgrades");
            return TransitionTo(GameState.MainMenu);
        }

        public bool GoToMainMenu()
        {
            if (currentState != GameState.Death && currentState != GameState.Victory && currentState != GameState.Upgrades)
                return Reject("GoToMainMenu");
            return TransitionTo(GameState.MainMenu);
        }

        private void HandleShipCompleted(ShipCompletionResult result)
        {
            if (currentState != GameState.Combat) return;

            progression.AddResources(result.ResourcesAwarded);
            RunResourcesEarned += result.ResourcesAwarded;
            sectorMap.MarkShipCaptured(result.Ship);
            if (!result.Ship.IsBossShip) RunShipsCaptured++;
            shipRun.Cleanup();

            if (result.Ship.IsBossShip)
            {
                progression.RegisterRunEnded(true, RunShipsCaptured);
                LastRunSummary = BuildSummary(true);
                SelectedShip = null;
                TransitionTo(GameState.Victory);
                SectorVictory?.Invoke(LastRunSummary);
                return;
            }

            IReadOnlyList<ModuleInstance> offers = moduleSystem.GenerateOffers(EntryPointCatalog.Get(result.Entry));
            CurrentReward = new ShipRewardInfo(result.Ship, result.Entry, result.ResourcesAwarded, offers);
            progression.Save();
            TransitionTo(GameState.Reward);
            RewardPrepared?.Invoke(CurrentReward);
        }

        private void HandleShipFailed(ShipFailureInfo info)
        {
            if (currentState != GameState.Combat || deathRoutine != null) return;
            deathRoutine = StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            if (deathScreenDelay > 0f) yield return new WaitForSeconds(deathScreenDelay);
            deathRoutine = null;
            if (currentState != GameState.Combat) yield break;

            shipRun.Cleanup();
            progression.RegisterRunEnded(false, RunShipsCaptured);
            LastRunSummary = BuildSummary(false);
            SelectedShip = null;
            TransitionTo(GameState.Death);
            RunFailed?.Invoke(LastRunSummary);
        }

        private void HandleRoomCleared(int roomIndex) { runRoomsCleared++; }

        private void HandleEnemyKilled(EnemyController enemy)
        {
            runEnemiesKilled++;
            moduleSystem.NotifyEnemyKilled();
        }

        private void HandleResourcesEarned(int amount, Vector3 position)
        {
            progression.AddResources(amount);
            RunResourcesEarned += amount;
        }

        private void HandleModuleAcquired(ModuleInstance module)
        {
            RebuildPlayerStats(true);
        }

        private void RebuildPlayerStats(bool keepHealthRatio)
        {
            RobotType robot = progression.SelectedRobot;
            PlayerStats stats = PlayerStatsCalculator.Build(robot, progression.GetBonusHealth(),
                progression.GetDamageMultiplier(), moduleSystem.GetPassiveModifiers());
            player.ApplyRobot(robot, stats, keepHealthRatio);
        }

        private void ResetRunCounters()
        {
            RunShipsCaptured = 0;
            RunResourcesEarned = 0;
            runRoomsCleared = 0;
            runEnemiesKilled = 0;
        }

        private RunSummary BuildSummary(bool won)
        {
            List<ModuleInstance> modules = new List<ModuleInstance>(moduleSystem.OwnedModules);
            return new RunSummary(won, progression.SelectedRobot, RunShipsCaptured, runRoomsCleared, runEnemiesKilled,
                RunResourcesEarned, Time.time - runStartTime, modules);
        }

        private bool Reject(string command)
        {
            Debug.LogWarning("[HullBreach] Команда " + command + " недопустима в состоянии " + currentState + ".", this);
            return false;
        }

        private static bool IsTransitionAllowed(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.MainMenu: return to == GameState.SectorMap || to == GameState.Upgrades;
                case GameState.SectorMap: return to == GameState.ShipSelection || to == GameState.MainMenu;
                case GameState.ShipSelection: return to == GameState.Combat || to == GameState.SectorMap || to == GameState.MainMenu;
                case GameState.Combat: return to == GameState.Reward || to == GameState.Death || to == GameState.Victory || to == GameState.MainMenu;
                case GameState.Reward: return to == GameState.SectorMap || to == GameState.MainMenu;
                case GameState.Death: return to == GameState.SectorMap || to == GameState.Upgrades || to == GameState.MainMenu;
                case GameState.Upgrades: return to == GameState.MainMenu || to == GameState.SectorMap;
                case GameState.Victory: return to == GameState.MainMenu || to == GameState.Upgrades || to == GameState.SectorMap;
                default: return false;
            }
        }

        private bool TransitionTo(GameState next, bool bypassValidation = false)
        {
            if (next == currentState) return false;
            if (!bypassValidation && !IsTransitionAllowed(currentState, next))
            {
                Debug.LogWarning("[HullBreach] Переход " + currentState + " → " + next + " запрещён.", this);
                return false;
            }

            GameState previous = currentState;
            currentState = next;
            ApplyStateSideEffects(next);
            StateChanged?.Invoke(previous, next);
            return true;
        }

        private void ApplyStateSideEffects(GameState state)
        {
            if (player == null) return;
            bool combat = state == GameState.Combat;
            if (hidePlayerOutsideCombat) player.gameObject.SetActive(combat);
            player.SetInputEnabled(combat);
        }
    }
}
