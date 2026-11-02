using System;
using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Modules;
using HullBreach.UI.Contracts;

namespace HullBreach.UI.Bridge
{
    /// <summary>IModuleService поверх HullBreach.Modules.ModuleSystem (только чтение).</summary>
    public sealed class ModuleAdapter : IModuleService
    {
        private readonly ModuleSystem modules;

        public ModuleAdapter(ModuleSystem modules)
        {
            this.modules = modules;
        }

        public IReadOnlyList<ModuleInstance> OwnedModules { get { return modules.OwnedModules; } }
        public float GetCooldownNormalized(ModuleId id) { return modules.GetCooldownNormalized(id); }
        public float GetCooldownRemaining(ModuleId id) { return modules.GetCooldownRemaining(id); }

        public event Action OwnedModulesChanged
        {
            add { modules.OwnedModulesChanged += value; }
            remove { modules.OwnedModulesChanged -= value; }
        }

        public event Action<ModuleInstance> ActiveModuleUsed
        {
            add { modules.ActiveModuleUsed += value; }
            remove { modules.ActiveModuleUsed -= value; }
        }
    }
}
