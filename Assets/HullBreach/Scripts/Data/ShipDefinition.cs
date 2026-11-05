using HullBreach.Core;

namespace HullBreach.Data
{
    public sealed class ShipDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int Tier { get; }
        public ShipRisk Risk { get; }
        public bool IsBossShip { get; }
        public float DifficultyMultiplier { get; }
        public int BaseResourceReward { get; }

        public ShipDefinition(string id, string displayName, int tier, ShipRisk risk, bool isBossShip,
            float difficultyMultiplier, int baseResourceReward)
        {
            Id = id;
            DisplayName = displayName;
            Tier = tier;
            Risk = risk;
            IsBossShip = isBossShip;
            DifficultyMultiplier = difficultyMultiplier;
            BaseResourceReward = baseResourceReward;
        }
    }
}
