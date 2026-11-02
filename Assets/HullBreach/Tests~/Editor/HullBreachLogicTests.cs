using NUnit.Framework;
using UnityEngine;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.Persistence;
using HullBreach.RunFlow;

namespace HullBreach.Tests
{
    /// <summary>EditMode-тесты чистой логики (без сцены и префабов). НЕ ЗАПУСКАЛИСЬ автором — требуется прогон в Unity.</summary>
    public class HullBreachLogicTests
    {
        [Test]
        public void EntryPoints_HaveRoomCountsFromConcept()
        {
            Assert.AreEqual(3, EntryPointCatalog.Get(EntryPointType.EngineRoom).RoomCount);
            Assert.AreEqual(5, EntryPointCatalog.Get(EntryPointType.CargoHold).RoomCount);
            Assert.AreEqual(4, EntryPointCatalog.Get(EntryPointType.Bridge).RoomCount);
            Assert.AreEqual(3, EntryPointCatalog.All.Count);
        }

        [Test]
        public void ModuleCatalog_HasEightModules_AndRarityIncreasesPower()
        {
            Assert.AreEqual(8, ModuleCatalog.All.Count);
            int active = 0;
            foreach (ModuleDefinition def in ModuleCatalog.All)
            {
                if (def.IsActive) active++;
                Assert.Less(def.GetValue(ModuleRarity.Common), def.GetValue(ModuleRarity.Rare), def.DisplayName);
                Assert.Less(def.GetValue(ModuleRarity.Rare), def.GetValue(ModuleRarity.Epic), def.DisplayName);
                Assert.IsNotEmpty(def.FormatDescription(ModuleRarity.Epic));
            }
            Assert.AreEqual(3, active);
        }

        [Test]
        public void StatsCalculator_AppliesPermanentAndModuleBonuses()
        {
            StatModifiers mods = new StatModifiers { FlatHealth = 20f, DamageBonus = 0.15f, MoveSpeedBonus = 0.1f, AttackSpeedBonus = 0.25f };
            PlayerStats s = PlayerStatsCalculator.Build(RobotType.Samurai, 10f, 1.08f, mods);
            Assert.AreEqual(150f, s.MaxHealth, 0.001f);
            Assert.AreEqual(14f * 1.08f * 1.15f, s.Damage, 0.001f);
            Assert.AreEqual(5.5f * 1.1f, s.MoveSpeed, 0.001f);
            Assert.AreEqual(0.45f / 1.25f, s.AttackInterval, 0.001f);
        }

        [Test]
        public void EnemyDefaults_AreSane_ForAllTypes()
        {
            foreach (EnemyType t in System.Enum.GetValues(typeof(EnemyType)))
            {
                EnemyStats s = EnemyDefaults.Get(t);
                Assert.Greater(s.MaxHealth, 0f, t.ToString());
                Assert.Greater(s.Damage, 0f, t.ToString());
                Assert.Greater(s.MoveSpeed, 0f, t.ToString());
            }
        }

        [Test]
        public void SaveData_JsonRoundtrip_KeepsValues()
        {
            SaveData d = new SaveData { Resources = 123, HealthLevel = 3, DamageLevel = 2, SelectedRobot = 1, Victories = 1, TotalRuns = 7, BestShipsCaptured = 4 };
            SaveData r = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d));
            Assert.AreEqual(123, r.Resources);
            Assert.AreEqual(3, r.HealthLevel);
            Assert.AreEqual(2, r.DamageLevel);
            Assert.AreEqual(1, r.SelectedRobot);
            Assert.AreEqual(4, r.BestShipsCaptured);
        }

        [Test]
        public void SectorMap_FourShipsThenBoss()
        {
            GameObject go = new GameObject("SectorMapTest");
            try
            {
                SectorMapSystem map = go.AddComponent<SectorMapSystem>();
                map.ResetSector();
                for (int stage = 0; stage < SectorMapSystem.RegularShipCount; stage++)
                {
                    Assert.IsFalse(map.IsBossStage);
                    Assert.AreEqual(stage + 1, map.CurrentTier);
                    Assert.That(map.CurrentOptions.Count, Is.InRange(2, 3));
                    map.MarkShipCaptured(map.CurrentOptions[0]);
                }
                Assert.IsTrue(map.IsBossStage);
                Assert.AreEqual(1, map.CurrentOptions.Count);
                Assert.IsTrue(map.CurrentOptions[0].IsBossShip);
                map.MarkShipCaptured(map.CurrentOptions[0]);
                Assert.IsTrue(map.IsSectorCleared);
                Assert.AreEqual(0, map.CurrentOptions.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
