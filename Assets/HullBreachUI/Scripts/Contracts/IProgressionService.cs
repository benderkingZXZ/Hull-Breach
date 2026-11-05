using System;
using HullBreach.Core;

namespace HullBreach.UI.Contracts
{
    public interface IProgressionService
    {
        int Resources { get; }
        RobotType SelectedRobot { get; }
        int Victories { get; }
        int TotalRuns { get; }
        int BestShipsCaptured { get; }

        int GetUpgradeLevel(UpgradeType type);
        int GetMaxUpgradeLevel();
        int GetUpgradeCost(UpgradeType type);
        bool CanPurchaseUpgrade(UpgradeType type);
        bool TryPurchaseUpgrade(UpgradeType type);
        float GetBonusHealth();
        float GetDamageMultiplier();
        void ResetAllProgress();

        event Action<int> ResourcesChanged;
        event Action<UpgradeType, int> UpgradeLevelChanged;
        event Action<RobotType> SelectedRobotChanged;
    }
}
