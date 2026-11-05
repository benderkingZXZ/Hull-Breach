using HullBreach.Core;
using UnityEngine;

namespace HullBreach.UI.Theme
{
    public static class UIIcons
    {
        public static Sprite Get(string name) { return ProceduralSprites.Icon(name); }

        public static string ForModule(ModuleId id)
        {
            switch (id)
            {
                case ModuleId.ArmorPlating: return "shield";
                case ModuleId.OverclockedCore: return "core";
                case ModuleId.ServoBoosters: return "chevrons";
                case ModuleId.TargetingArray: return "crosshair";
                case ModuleId.NanoRepair: return "plus";
                case ModuleId.EnergyShield: return "bubble";
                case ModuleId.EmpBurst: return "emp";
                case ModuleId.RepairInjector: return "injector";
                default: return "hex";
            }
        }

        public static string ForEntry(EntryPointType e)
        {
            switch (e)
            {
                case EntryPointType.CargoHold: return "crate";
                case EntryPointType.Bridge: return "radar";
                default: return "gear";
            }
        }

        public static string ForShip(ShipRisk risk)
        {
            switch (risk)
            {
                case ShipRisk.Safe: return "ship_a";
                case ShipRisk.Risky: return "ship_c";
                case ShipRisk.Boss: return "ship_boss";
                default: return "ship_b";
            }
        }

        public static string ForRobot(RobotType r)
        {
            return r == RobotType.Techno ? "robot_techno" : "robot_samurai";
        }
    }
}
