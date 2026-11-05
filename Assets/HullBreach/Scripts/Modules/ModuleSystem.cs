using System;
using System.Collections.Generic;
using UnityEngine;
using HullBreach.Combat;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Enemies;
using HullBreach.Player;

namespace HullBreach.Modules
{
    public class ModuleSystem : MonoBehaviour
    {
        public const int OffersPerReward = 3;

        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCombat playerCombat;
        [Header("Базовые веса редкости (сумма не обязана быть 100)")]
        [SerializeField] private float commonWeight = 70f;
        [SerializeField] private float rareWeight = 25f;
        [SerializeField] private float epicWeight = 5f;

        private readonly List<ModuleInstance> owned = new List<ModuleInstance>();
        private readonly List<ModuleInstance> offers = new List<ModuleInstance>();
        private readonly Dictionary<ModuleId, float> readyTimes = new Dictionary<ModuleId, float>();
        private readonly List<Health> buffer = new List<Health>(32);
        private System.Random rng = new System.Random(Environment.TickCount);
        private bool hasOffers;

        public event Action<IReadOnlyList<ModuleInstance>> OffersGenerated;
        public event Action<ModuleInstance> ModuleAcquired;
        public event Action<ModuleInstance> ActiveModuleUsed;
        public event Action OwnedModulesChanged;

        public IReadOnlyList<ModuleInstance> OwnedModules { get { return owned; } }
        public IReadOnlyList<ModuleInstance> CurrentOffers { get { return hasOffers ? offers : null; } }

        public void Configure(PlayerController newPlayer, PlayerCombat newPlayerCombat)
        {
            if (isActiveAndEnabled && playerCombat != null) playerCombat.SkillButtonPressed -= HandleSkillButton;
            player = newPlayer;
            playerCombat = newPlayerCombat;
            if (isActiveAndEnabled && playerCombat != null) playerCombat.SkillButtonPressed += HandleSkillButton;
        }

        private void OnEnable()
        {
            if (playerCombat != null) playerCombat.SkillButtonPressed += HandleSkillButton;
        }

        private void OnDisable()
        {
            if (playerCombat != null) playerCombat.SkillButtonPressed -= HandleSkillButton;
        }

        public void ResetForNewRun()
        {
            owned.Clear();
            offers.Clear();
            hasOffers = false;
            readyTimes.Clear();
            OwnedModulesChanged?.Invoke();
        }

        public void ResetCooldowns()
        {
            readyTimes.Clear();
        }

        public IReadOnlyList<ModuleInstance> GenerateOffers(EntryPointProfile entry)
        {
            float rareBonus = entry != null ? entry.RareModuleBonus : 0f;

            List<ModuleDefinition> pool = new List<ModuleDefinition>();
            IReadOnlyList<ModuleDefinition> all = ModuleCatalog.All;
            for (int i = 0; i < all.Count; i++)
                if (FindOwned(all[i].Id) == null) pool.Add(all[i]);
            for (int i = 0; i < all.Count && pool.Count < OffersPerReward; i++)
                if (!pool.Contains(all[i])) pool.Add(all[i]);

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                ModuleDefinition tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
            }

            offers.Clear();
            for (int i = 0; i < OffersPerReward && i < pool.Count; i++)
                offers.Add(new ModuleInstance(pool[i], RollRarity(rareBonus)));
            hasOffers = true;

            OffersGenerated?.Invoke(offers);
            return offers;
        }

        private ModuleRarity RollRarity(float rareBonus)
        {
            float shift = Mathf.Clamp01(rareBonus) * 100f;
            float common = Mathf.Max(0f, commonWeight - shift);
            float moved = commonWeight - common;
            float rare = rareWeight + moved * 0.75f;
            float epic = epicWeight + moved * 0.25f;

            float roll = (float)(rng.NextDouble() * (common + rare + epic));
            if (roll < common) return ModuleRarity.Common;
            if (roll < common + rare) return ModuleRarity.Rare;
            return ModuleRarity.Epic;
        }

        public bool ChooseOffer(int index)
        {
            if (!hasOffers || index < 0 || index >= offers.Count) return false;

            ModuleInstance chosen = offers[index];
            offers.Clear();
            hasOffers = false;
            owned.Add(chosen);

            ModuleAcquired?.Invoke(chosen);
            OwnedModulesChanged?.Invoke();
            return true;
        }

        private ModuleInstance FindOwned(ModuleId id)
        {
            for (int i = 0; i < owned.Count; i++)
                if (owned[i].Id == id) return owned[i];
            return null;
        }

        public StatModifiers GetPassiveModifiers()
        {
            StatModifiers m = new StatModifiers();
            for (int i = 0; i < owned.Count; i++)
            {
                ModuleInstance mod = owned[i];
                switch (mod.Id)
                {
                    case ModuleId.ArmorPlating: m.FlatHealth += mod.Value; break;
                    case ModuleId.OverclockedCore: m.DamageBonus += mod.Value / 100f; break;
                    case ModuleId.ServoBoosters: m.MoveSpeedBonus += mod.Value / 100f; break;
                    case ModuleId.TargetingArray: m.AttackSpeedBonus += mod.Value / 100f; break;
                }
            }
            return m;
        }

        public void NotifyEnemyKilled()
        {
            ModuleInstance nano = FindOwned(ModuleId.NanoRepair);
            if (nano != null && player != null && player.IsAlive) player.Health.Heal(nano.Value);
        }

        public float GetCooldownRemaining(ModuleId id)
        {
            float ready;
            return readyTimes.TryGetValue(id, out ready) ? Mathf.Max(0f, ready - Time.time) : 0f;
        }

        public float GetCooldownNormalized(ModuleId id)
        {
            ModuleInstance m = FindOwned(id);
            if (m == null || m.Cooldown <= 0f) return 0f;
            return Mathf.Clamp01(GetCooldownRemaining(id) / m.Cooldown);
        }

        private void HandleSkillButton()
        {
            for (int i = 0; i < owned.Count; i++)
                if (owned[i].IsActive) TryActivateModule(owned[i].Id);
        }

        public bool TryActivateModule(ModuleId id)
        {
            ModuleInstance m = FindOwned(id);
            if (m == null || !m.IsActive || player == null || !player.IsAlive || !player.InputEnabled) return false;
            if (GetCooldownRemaining(id) > 0f) return false;

            switch (id)
            {
                case ModuleId.EnergyShield:
                    player.Health.AddShield(m.Value, ModuleCatalog.ShieldDurationSeconds);
                    break;
                case ModuleId.EmpBurst:
                    ApplyEmp(m);
                    break;
                case ModuleId.RepairInjector:
                    player.Health.Heal(player.Health.MaxHealth * m.Value / 100f);
                    break;
                default:
                    return false;
            }

            readyTimes[id] = Time.time + m.Cooldown;
            ActiveModuleUsed?.Invoke(m);
            return true;
        }

        private void ApplyEmp(ModuleInstance m)
        {
            Vector2 origin = player.transform.position;
            buffer.Clear();
            TargetRegistry.CollectEnemiesInRadius(origin, ModuleCatalog.EmpRadius, buffer);
            for (int i = 0; i < buffer.Count; i++)
            {
                Health h = buffer[i];
                if (h == null || h.IsDead) continue;
                EnemyController enemy = h.GetComponentInParent<EnemyController>();
                if (enemy != null) enemy.ApplyStun(enemy.IsBoss ? m.Value * 0.5f : m.Value);
                Vector2 dir = (Vector2)h.transform.position - origin;
                h.TakeDamage(new DamageInfo(m.SecondaryValue, player.gameObject, dir.normalized));
            }
        }
    }
}
