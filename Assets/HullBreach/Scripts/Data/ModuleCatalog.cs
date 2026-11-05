using System.Collections.Generic;
using System.Globalization;
using HullBreach.Core;

namespace HullBreach.Data
{
    public sealed class ModuleDefinition
    {
        private readonly float[] values;
        private readonly float[] secondaryValues;
        private readonly float[] cooldowns;

        public ModuleId Id { get; }
        public string DisplayName { get; }
        public string DescriptionTemplate { get; }
        public bool IsActive { get; }

        public ModuleDefinition(ModuleId id, string displayName, string descriptionTemplate, bool isActive,
            float[] values, float[] secondaryValues, float[] cooldowns)
        {
            Id = id; DisplayName = displayName; DescriptionTemplate = descriptionTemplate; IsActive = isActive;
            this.values = values; this.secondaryValues = secondaryValues; this.cooldowns = cooldowns;
        }

        public float GetValue(ModuleRarity r) { return values[(int)r]; }
        public float GetSecondaryValue(ModuleRarity r) { return secondaryValues != null ? secondaryValues[(int)r] : 0f; }
        public float GetCooldown(ModuleRarity r) { return cooldowns != null ? cooldowns[(int)r] : 0f; }

        public string FormatDescription(ModuleRarity r)
        {
            return string.Format(CultureInfo.InvariantCulture, DescriptionTemplate, GetValue(r), GetSecondaryValue(r), GetCooldown(r));
        }
    }

    public sealed class ModuleInstance
    {
        public ModuleDefinition Definition { get; }
        public ModuleRarity Rarity { get; }

        public ModuleInstance(ModuleDefinition definition, ModuleRarity rarity)
        {
            Definition = definition;
            Rarity = rarity;
        }

        public ModuleId Id { get { return Definition.Id; } }
        public string DisplayName { get { return Definition.DisplayName; } }
        public string Description { get { return Definition.FormatDescription(Rarity); } }
        public bool IsActive { get { return Definition.IsActive; } }
        public float Value { get { return Definition.GetValue(Rarity); } }
        public float SecondaryValue { get { return Definition.GetSecondaryValue(Rarity); } }
        public float Cooldown { get { return Definition.GetCooldown(Rarity); } }
    }

    public static class ModuleCatalog
    {
        public const float ShieldDurationSeconds = 5f;
        public const float EmpRadius = 4.5f;

        private static readonly ModuleDefinition[] all =
        {
            new ModuleDefinition(ModuleId.ArmorPlating, "Усиленная броня", "+{0:0} к максимальному здоровью", false,
                new[] { 20f, 35f, 55f }, null, null),
            new ModuleDefinition(ModuleId.OverclockedCore, "Разогнанное ядро", "+{0:0}% к урону", false,
                new[] { 15f, 25f, 40f }, null, null),
            new ModuleDefinition(ModuleId.ServoBoosters, "Сервоускорители", "+{0:0}% к скорости передвижения", false,
                new[] { 10f, 18f, 28f }, null, null),
            new ModuleDefinition(ModuleId.TargetingArray, "Целеуказатель", "+{0:0}% к скорости атаки", false,
                new[] { 15f, 25f, 40f }, null, null),
            new ModuleDefinition(ModuleId.NanoRepair, "Нанорегенерация", "+{0:0} здоровья за каждого убитого врага", false,
                new[] { 2f, 4f, 7f }, null, null),
            new ModuleDefinition(ModuleId.EnergyShield, "Энергощит", "Щит на {0:0} урона (5 с). Перезарядка {2:0} с", true,
                new[] { 30f, 50f, 80f }, null, new[] { 14f, 12f, 10f }),
            new ModuleDefinition(ModuleId.EmpBurst, "ЭМИ-импульс", "Оглушает врагов вокруг на {0:0.#} с и наносит {1:0} урона. Перезарядка {2:0} с", true,
                new[] { 1.5f, 2.2f, 3.0f }, new[] { 10f, 20f, 35f }, new[] { 15f, 13f, 11f }),
            new ModuleDefinition(ModuleId.RepairInjector, "Ремонтный инжектор", "Восстанавливает {0:0}% здоровья. Перезарядка {2:0} с", true,
                new[] { 25f, 40f, 60f }, null, new[] { 25f, 22f, 18f }),
        };

        public static IReadOnlyList<ModuleDefinition> All { get { return all; } }

        public static ModuleDefinition Get(ModuleId id)
        {
            for (int i = 0; i < all.Length; i++)
                if (all[i].Id == id) return all[i];
            return all[0];
        }
    }
}
