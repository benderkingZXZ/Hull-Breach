namespace HullBreach.Core
{
    public enum GameState { MainMenu, SectorMap, ShipSelection, Combat, Reward, Death, Upgrades, Victory }
    public enum RobotType { Samurai, Techno }
    public enum EntryPointType { EngineRoom, CargoHold, Bridge }
    // Порядок значений используется как индекс в EntryPointProfile.GetEnemyWeight — не менять.
    public enum EnemyType { Drone, Gunner, Heavy, Exploder, MiniBoss }
    // Порядок значений используется как индекс в таблицах модулей — не менять.
    public enum ModuleRarity { Common, Rare, Epic }
    public enum ModuleId { ArmorPlating, OverclockedCore, ServoBoosters, TargetingArray, NanoRepair, EnergyShield, EmpBurst, RepairInjector }
    public enum UpgradeType { Health, Damage }
    public enum ShipRisk { Safe, Standard, Risky, Boss }
    public enum RoomRole { Normal, Reactor }
    public enum RoomState { Inactive, Active, Cleared }
    public enum Faction { Player, Enemy }
}
