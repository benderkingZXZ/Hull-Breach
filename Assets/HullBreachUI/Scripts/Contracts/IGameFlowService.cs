using System;
using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.UI.Contracts
{
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
