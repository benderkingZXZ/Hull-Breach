using System;
using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.UI.Contracts
{
    public interface IModuleService
    {
        IReadOnlyList<ModuleInstance> OwnedModules { get; }
        float GetCooldownNormalized(ModuleId id);
        float GetCooldownRemaining(ModuleId id);

        event Action OwnedModulesChanged;
        event Action<ModuleInstance> ActiveModuleUsed;
    }
}
