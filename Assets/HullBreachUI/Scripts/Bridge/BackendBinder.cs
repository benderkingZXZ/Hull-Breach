using System;
using HullBreach.Core;
using HullBreach.Meta;
using HullBreach.Modules;
using HullBreach.Player;
using HullBreach.RunFlow;
using HullBreach.UI.Contracts;
using UnityEngine;

namespace HullBreach.UI.Bridge
{
    public sealed class BackendRefs
    {
        public GameStateManager State;
        public ProgressionSystem Progression;
        public ModuleSystem Modules;
        public ShipRunManager ShipRun;
        public PlayerController Player;
    }

    public static class BackendBinder
    {
        private sealed class BridgeHandle : IDisposable
        {
            private readonly CombatAdapter combat;
            public BridgeHandle(CombatAdapter combat) { this.combat = combat; }
            public void Dispose() { combat.Dispose(); }
        }

        public static UIServices TryCreate(BackendRefs refs, out IDisposable disposable, out string error)
        {
            disposable = null;
            error = null;

            GameStateManager state = refs.State != null ? refs.State : Find<GameStateManager>();
            if (state == null) { error = "GameStateManager не найден в сцене."; return null; }

            ProgressionSystem progression = refs.Progression != null ? refs.Progression : Find<ProgressionSystem>();
            if (progression == null) { error = "ProgressionSystem не найден в сцене."; return null; }

            ModuleSystem modules = refs.Modules != null ? refs.Modules : Find<ModuleSystem>();
            if (modules == null) { error = "ModuleSystem не найден в сцене."; return null; }

            ShipRunManager run = refs.ShipRun != null ? refs.ShipRun : Find<ShipRunManager>();
            if (run == null) { error = "ShipRunManager не найден в сцене."; return null; }

            PlayerController player = refs.Player != null ? refs.Player : Find<PlayerController>();
            if (player == null) { error = "PlayerController не найден в сцене (объект игрока может быть неактивным, но должен существовать)."; return null; }

            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat == null) { error = "На объекте игрока нет компонента PlayerCombat."; return null; }

            CombatAdapter combatAdapter = new CombatAdapter(run, player, combat);

            UIServices services = new UIServices();
            services.Flow = new GameFlowAdapter(state);
            services.Progression = new ProgressionAdapter(progression);
            services.Modules = new ModuleAdapter(modules);
            services.Combat = combatAdapter;
            services.Input = combatAdapter;
            services.IsMock = false;

            disposable = new BridgeHandle(combatAdapter);
            return services;
        }

        private static T Find<T>() where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
            return UnityEngine.Object.FindObjectOfType<T>(true);
#endif
        }
    }
}
