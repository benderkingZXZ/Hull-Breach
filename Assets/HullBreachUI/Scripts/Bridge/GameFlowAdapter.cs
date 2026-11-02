using System;
using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Contracts;

namespace HullBreach.UI.Bridge
{
    /// <summary>IGameFlowService поверх HullBreach.Core.GameStateManager. Никакой логики — только делегирование.</summary>
    public sealed class GameFlowAdapter : IGameFlowService
    {
        private readonly GameStateManager gsm;

        public GameFlowAdapter(GameStateManager gsm)
        {
            this.gsm = gsm;
        }

        public GameState CurrentState { get { return gsm.CurrentState; } }
        public ShipDefinition SelectedShip { get { return gsm.SelectedShip; } }
        public EntryPointType SelectedEntryPoint { get { return gsm.SelectedEntryPoint; } }
        public ShipRewardInfo CurrentReward { get { return gsm.CurrentReward; } }
        public RunSummary LastRunSummary { get { return gsm.LastRunSummary; } }
        public int RunShipsCaptured { get { return gsm.RunShipsCaptured; } }
        public int RunResourcesEarned { get { return gsm.RunResourcesEarned; } }

        public IReadOnlyList<ShipDefinition> GetShipOptions() { return gsm.GetShipOptions(); }
        public IReadOnlyList<EntryPointProfile> GetEntryPoints() { return gsm.GetEntryPoints(); }

        public event Action<GameState, GameState> StateChanged
        {
            add { gsm.StateChanged += value; }
            remove { gsm.StateChanged -= value; }
        }

        public bool SelectRobot(RobotType robot) { return gsm.SelectRobot(robot); }
        public bool StartSectorRun() { return gsm.StartSectorRun(); }
        public bool SelectShip(string shipId) { return gsm.SelectShip(shipId); }
        public bool CancelShipSelection() { return gsm.CancelShipSelection(); }
        public bool ConfirmEntryPoint(EntryPointType entry) { return gsm.ConfirmEntryPoint(entry); }
        public bool ChooseModule(int offerIndex) { return gsm.ChooseModule(offerIndex); }
        public bool AbandonRun() { return gsm.AbandonRun(); }
        public bool OpenUpgrades() { return gsm.OpenUpgrades(); }
        public bool CloseUpgrades() { return gsm.CloseUpgrades(); }
        public bool GoToMainMenu() { return gsm.GoToMainMenu(); }
    }
}
