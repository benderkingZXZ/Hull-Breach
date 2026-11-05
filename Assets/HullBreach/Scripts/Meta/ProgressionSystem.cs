using System;
using UnityEngine;
using HullBreach.Core;
using HullBreach.Persistence;

namespace HullBreach.Meta
{
    public class ProgressionSystem : MonoBehaviour
    {
        [SerializeField] private SaveSystem saveSystem;
        [SerializeField, Min(1)] private int maxLevel = 10;
        [Tooltip("Цена перехода с уровня L на L+1 = baseCost + costPerLevel * L.")]
        [SerializeField] private int baseCost = 50;
        [SerializeField] private int costPerLevel = 40;
        [Tooltip("Прибавка к максимальному здоровью за уровень (плоская).")]
        [SerializeField] private float healthPerLevel = 10f;
        [Tooltip("Прибавка к урону за уровень (доля, 0.08 = +8 %).")]
        [SerializeField] private float damageBonusPerLevel = 0.08f;
        [SerializeField] private bool autosaveOnPause = true;

        private SaveData data;

        public event Action<int> ResourcesChanged;
        public event Action<UpgradeType, int> UpgradeLevelChanged;
        public event Action<RobotType> SelectedRobotChanged;

        public int Resources { get { EnsureLoaded(); return data.Resources; } }
        public RobotType SelectedRobot { get { EnsureLoaded(); return (RobotType)Mathf.Clamp(data.SelectedRobot, 0, 1); } }
        public int Victories { get { EnsureLoaded(); return data.Victories; } }
        public int TotalRuns { get { EnsureLoaded(); return data.TotalRuns; } }
        public int BestShipsCaptured { get { EnsureLoaded(); return data.BestShipsCaptured; } }

        private void Awake()
        {
            EnsureLoaded();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && autosaveOnPause && data != null) Save();
        }

        private void OnApplicationQuit()
        {
            if (data != null) Save();
        }

        public void Configure(SaveSystem newSaveSystem)
        {
            saveSystem = newSaveSystem;
        }

        private SaveSystem EnsureSaveSystem()
        {
            if (saveSystem == null)
            {
                saveSystem = GetComponent<SaveSystem>();
                if (saveSystem == null) saveSystem = gameObject.AddComponent<SaveSystem>();
            }
            return saveSystem;
        }

        private void EnsureLoaded()
        {
            if (data != null) return;
            data = EnsureSaveSystem().Load();
        }

        public void Load()
        {
            data = EnsureSaveSystem().Load();
            ResourcesChanged?.Invoke(data.Resources);
            SelectedRobotChanged?.Invoke(SelectedRobot);
        }

        public bool Save()
        {
            EnsureLoaded();
            return EnsureSaveSystem().Save(data);
        }

        public void AddResources(int amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();
            data.Resources += amount;
            ResourcesChanged?.Invoke(data.Resources);
        }

        public int GetMaxUpgradeLevel() { return maxLevel; }

        public int GetUpgradeLevel(UpgradeType type)
        {
            EnsureLoaded();
            return type == UpgradeType.Health ? data.HealthLevel : data.DamageLevel;
        }

        public int GetUpgradeCost(UpgradeType type)
        {
            int level = GetUpgradeLevel(type);
            if (level >= maxLevel) return -1;
            return baseCost + costPerLevel * level;
        }

        public bool CanPurchaseUpgrade(UpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            return cost >= 0 && Resources >= cost;
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            if (!CanPurchaseUpgrade(type)) return false;
            int cost = GetUpgradeCost(type);
            data.Resources -= cost;
            if (type == UpgradeType.Health) data.HealthLevel++;
            else data.DamageLevel++;

            ResourcesChanged?.Invoke(data.Resources);
            UpgradeLevelChanged?.Invoke(type, GetUpgradeLevel(type));
            Save();
            return true;
        }

        public float GetBonusHealth() { return GetUpgradeLevel(UpgradeType.Health) * healthPerLevel; }

        public float GetDamageMultiplier() { return 1f + GetUpgradeLevel(UpgradeType.Damage) * damageBonusPerLevel; }

        public void SetSelectedRobot(RobotType robot)
        {
            EnsureLoaded();
            if (data.SelectedRobot == (int)robot) return;
            data.SelectedRobot = (int)robot;
            SelectedRobotChanged?.Invoke(robot);
            Save();
        }

        public void RegisterRunEnded(bool victory, int shipsCaptured)
        {
            EnsureLoaded();
            data.TotalRuns++;
            if (victory) data.Victories++;
            if (shipsCaptured > data.BestShipsCaptured) data.BestShipsCaptured = shipsCaptured;
            Save();
        }

        public void ResetAllProgress()
        {
            EnsureSaveSystem().DeleteSave();
            data = new SaveData();
            ResourcesChanged?.Invoke(data.Resources);
            UpgradeLevelChanged?.Invoke(UpgradeType.Health, 0);
            UpgradeLevelChanged?.Invoke(UpgradeType.Damage, 0);
            SelectedRobotChanged?.Invoke(SelectedRobot);
            Save();
        }
    }
}
