using System;
using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.UI.Contracts
{
    /// <summary>
    /// Навигация и состояние забега. Реализация для реального бэкенда — Bridge/GameFlowAdapter
    /// (обёртка над HullBreach.Core.GameStateManager). Все команды возвращают true, если приняты.
    /// </summary>
    public interface IGameFlowService
    {
        GameState CurrentState { get; }
        ShipDefinition SelectedShip { get; }
        EntryPointType SelectedEntryPoint { get; }
        ShipRewardInfo CurrentReward { get; }
        RunSummary LastRunSummary { get; }
        int RunShipsCaptured { get; }
        int RunResourcesEarned { get; }

        IReadOnlyList<ShipDefinition> GetShipOptions();
        IReadOnlyList<EntryPointProfile> GetEntryPoints();

        /// <summary>(предыдущее, новое). Вызывается ПОСЛЕ смены CurrentState.</summary>
        event Action<GameState, GameState> StateChanged;

        bool SelectRobot(RobotType robot);
        bool StartSectorRun();
        bool SelectShip(string shipId);
        bool CancelShipSelection();
        bool ConfirmEntryPoint(EntryPointType entry);
        bool ChooseModule(int offerIndex);
        bool AbandonRun();
        bool OpenUpgrades();
        bool CloseUpgrades();
        bool GoToMainMenu();
    }
}
