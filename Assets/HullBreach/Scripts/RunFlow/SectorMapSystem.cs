using System;
using System.Collections.Generic;
using UnityEngine;
using HullBreach.Core;
using HullBreach.Data;

namespace HullBreach.RunFlow
{
    public class SectorMapSystem : MonoBehaviour
    {
        public const int RegularShipCount = 4;

        [SerializeField, Range(2, 3)] private int minOptions = 2;
        [SerializeField, Range(2, 3)] private int maxOptions = 3;
        [Tooltip("0 — случайно; иначе фиксированный seed генерации вариантов.")]
        [SerializeField] private int fixedSeed = 0;

        private static readonly float[] TierDifficulty = { 1.0f, 1.25f, 1.5f, 1.8f };
        private static readonly int[] TierReward = { 40, 60, 90, 130 };
        private static readonly string[][] TierNames =
        {
            new[] { "Брошенный грузовоз", "Дрейфующий транспорт", "Мёртвый сухогруз" },
            new[] { "Рейдер «Шакал»", "Патрульный катер", "Фрегат связи" },
            new[] { "Эсминец «Грозный»", "Крейсер-лаборатория", "Штурмовой носитель" },
            new[] { "Линкор «Каинов путь»", "Дредноут «Тишина»", "Станция-крепость" },
        };

        private readonly List<ShipDefinition> options = new List<ShipDefinition>();
        private System.Random rng;

        public int ShipsCaptured { get; private set; }
        public bool BossCaptured { get; private set; }
        public bool IsBossStage { get { return ShipsCaptured >= RegularShipCount && !BossCaptured; } }
        public bool IsSectorCleared { get { return BossCaptured; } }
        public int CurrentTier { get { return IsBossStage ? 5 : Mathf.Min(ShipsCaptured + 1, RegularShipCount); } }
        public IReadOnlyList<ShipDefinition> CurrentOptions { get { return options; } }

        public event Action<IReadOnlyList<ShipDefinition>> OptionsChanged;

        public void ResetSector()
        {
            ShipsCaptured = 0;
            BossCaptured = false;
            rng = new System.Random(fixedSeed != 0 ? fixedSeed : Environment.TickCount);
            GenerateOptions();
        }

        public ShipDefinition GetOptionById(string id)
        {
            for (int i = 0; i < options.Count; i++)
                if (options[i].Id == id) return options[i];
            return null;
        }

        public void MarkShipCaptured(ShipDefinition ship)
        {
            if (ship == null) return;
            if (ship.IsBossShip) BossCaptured = true;
            else ShipsCaptured = Mathf.Min(ShipsCaptured + 1, RegularShipCount);
            GenerateOptions();
        }

        private void GenerateOptions()
        {
            if (rng == null) rng = new System.Random(Environment.TickCount);
            options.Clear();

            if (BossCaptured)
            {
            }
            else if (IsBossStage)
            {
                options.Add(new ShipDefinition("nexus_prime", "Нексус-Прайм", 5, ShipRisk.Boss, true, 2.2f, 250));
            }
            else
            {
                int tier = CurrentTier;
                int count = Mathf.Clamp(rng.Next(minOptions, Mathf.Max(minOptions, maxOptions) + 1), 2, 3);

                List<ShipRisk> risks = new List<ShipRisk> { ShipRisk.Safe, ShipRisk.Standard, ShipRisk.Risky };
                for (int i = risks.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    ShipRisk tmp = risks[i]; risks[i] = risks[j]; risks[j] = tmp;
                }

                string[] names = TierNames[tier - 1];
                int nameStart = rng.Next(names.Length);
                for (int i = 0; i < count; i++)
                {
                    ShipRisk risk = risks[i];
                    float diffFactor = risk == ShipRisk.Safe ? 0.9f : (risk == ShipRisk.Risky ? 1.15f : 1.0f);
                    float rewardFactor = risk == ShipRisk.Safe ? 0.85f : (risk == ShipRisk.Risky ? 1.25f : 1.0f);
                    string id = "t" + tier + "_" + risk.ToString().ToLowerInvariant();
                    string displayName = names[(nameStart + i) % names.Length];
                    options.Add(new ShipDefinition(id, displayName, tier, risk, false,
                        TierDifficulty[tier - 1] * diffFactor,
                        Mathf.RoundToInt(TierReward[tier - 1] * rewardFactor)));
                }
            }

            OptionsChanged?.Invoke(options);
        }
    }
}
