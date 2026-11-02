using System;

namespace HullBreach.Persistence
{
    /// <summary>Постоянный прогресс между сессиями. Сериализуется JsonUtility (только публичные поля).</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Resources;
        public int HealthLevel;
        public int DamageLevel;
        /// <summary>Значение RobotType (0 — Samurai, 1 — Techno).</summary>
        public int SelectedRobot;
        public int Victories;
        public int TotalRuns;
        public int BestShipsCaptured;
    }
}
