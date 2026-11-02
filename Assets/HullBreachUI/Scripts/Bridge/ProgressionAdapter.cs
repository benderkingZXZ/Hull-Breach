using System;
using HullBreach.Core;
using HullBreach.Meta;
using HullBreach.UI.Contracts;

namespace HullBreach.UI.Bridge
{
    /// <summary>IProgressionService поверх HullBreach.Meta.ProgressionSystem.</summary>
    public sealed class ProgressionAdapter : IProgressionService
    {
        private readonly ProgressionSystem progression;

        public ProgressionAdapter(ProgressionSystem progression)
        {
            this.progression = progression;
        }

        public int Resources { get { return progression.Resources; } }
        public RobotType SelectedRobot { get { return progression.SelectedRobot; } }
        public int Victories { get { return progression.Victories; } }
        public int TotalRuns { get { return progression.TotalRuns; } }
        public int BestShipsCaptured { get { return progression.BestShipsCaptured; } }

        public int GetUpgradeLevel(UpgradeType type) { return progression.GetUpgradeLevel(type); }
        public int GetMaxUpgradeLevel() { return progression.GetMaxUpgradeLevel(); }
        public int GetUpgradeCost(UpgradeType type) { return progression.GetUpgradeCost(type); }
        public bool CanPurchaseUpgrade(UpgradeType type) { return progression.CanPurchaseUpgrade(type); }
        public bool TryPurchaseUpgrade(UpgradeType type) { return progression.TryPurchaseUpgrade(type); }
        public float GetBonusHealth() { return progression.GetBonusHealth(); }
        public float GetDamageMultiplier() { return progression.GetDamageMultiplier(); }
        public void ResetAllProgress() { progression.ResetAllProgress(); }

        public event Action<int> ResourcesChanged
        {
            add { progression.ResourcesChanged += value; }
            remove { progression.ResourcesChanged -= value; }
        }

        public event Action<UpgradeType, int> UpgradeLevelChanged
        {
            add { progression.UpgradeLevelChanged += value; }
            remove { progression.UpgradeLevelChanged -= value; }
        }

        public event Action<RobotType> SelectedRobotChanged
        {
            add { progression.SelectedRobotChanged += value; }
            remove { progression.SelectedRobotChanged -= value; }
        }
    }
}
