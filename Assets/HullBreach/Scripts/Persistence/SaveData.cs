using System;

namespace HullBreach.Persistence
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Resources;
        public int HealthLevel;
        public int DamageLevel;
        public int SelectedRobot;
        public int Victories;
        public int TotalRuns;
        public int BestShipsCaptured;
    }
}
