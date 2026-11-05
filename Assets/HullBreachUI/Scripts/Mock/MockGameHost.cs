using System;
using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Contracts;
using UnityEngine;

namespace HullBreach.UI.Mock
{
    public sealed class MockGameHost : MonoBehaviour,
        IGameFlowService, IProgressionService, IModuleService, ICombatFeed, ICombatInputSink
    {
        private const int MaxUpgradeLevel = 10;

        private static readonly string[] ShipNames =
        {
            "Брошенный грузовоз", "Патрульный катер", "Рейдер «Серп»", "Лайнер «Аврора»",
            "Танкер «Горизонт»", "Эсминец «Клинок»", "Крейсер «Молох»", "Транспорт «Вега»"
        };

        private GameState state = GameState.MainMenu;
        private ShipDefinition selectedShip;
        private EntryPointType selectedEntry;
        private ShipRewardInfo reward;
        private RunSummary lastSummary;
        private int shipsCaptured;
        private int runResources;
        private int stage;
        private List<ShipDefinition> options = new List<ShipDefinition>();

        private int resources = 250;
        private RobotType robot = RobotType.Samurai;
        private int victories;
        private int totalRuns;
        private int bestShips;
        private readonly int[] upgradeLevels = new int[2];

        private readonly List<ModuleInstance> owned = new List<ModuleInstance>();
        private readonly Dictionary<ModuleId, float> moduleReadyTime = new Dictionary<ModuleId, float>();

        private bool running;
        private float health;
        private float maxHealth = 100f;
        private float shieldUntil;
        private int roomIndex;
        private int totalRooms;
        private int enemies;
        private bool roomActive;
        private float nextRoomTimer;
        private float damageTimer;
        private float killTimer;
        private float deathTimer = -1f;
        private bool attackHeld;
        private Vector2 moveInput;
        private float skillReadyTime;
        private float runStartTime;
        private int roomsClearedTotal;
        private int enemiesKilledTotal;

        public UIServices CreateServices()
        {
            UIServices s = new UIServices();
            s.Flow = this;
            s.Progression = this;
            s.Modules = this;
            s.Combat = this;
            s.Input = this;
            s.IsMock = true;
            return s;
        }

        public GameState CurrentState { get { return state; } }
        public ShipDefinition SelectedShip { get { return selectedShip; } }
        public EntryPointType SelectedEntryPoint { get { return selectedEntry; } }
        public ShipRewardInfo CurrentReward { get { return reward; } }
        public RunSummary LastRunSummary { get { return lastSummary; } }
        public int RunShipsCaptured { get { return shipsCaptured; } }
        public int RunResourcesEarned { get { return runResources; } }

        public event Action<GameState, GameState> StateChanged;

        public IReadOnlyList<ShipDefinition> GetShipOptions() { return options; }
        public IReadOnlyList<EntryPointProfile> GetEntryPoints() { return EntryPointCatalog.All; }

        private void SetState(GameState next)
        {
            GameState prev = state;
            state = next;
            Action<GameState, GameState> h = StateChanged;
            if (h != null) h(prev, next);
        }

        public bool SelectRobot(RobotType r)
        {
            if (state != GameState.MainMenu && state != GameState.Upgrades) return false;
            if (robot == r) return true;
            robot = r;
            Action<RobotType> h = SelectedRobotChanged;
            if (h != null) h(r);
            Action<RobotType> h2 = RobotChanged;
            if (h2 != null) h2(r);
            return true;
        }

        public bool StartSectorRun()
        {
            if (state != GameState.MainMenu && state != GameState.Death && state != GameState.Victory && state != GameState.Upgrades) return false;
            owned.Clear();
            moduleReadyTime.Clear();
            Action oc = OwnedModulesChanged;
            if (oc != null) oc();
            shipsCaptured = 0;
            runResources = 0;
            roomsClearedTotal = 0;
            enemiesKilledTotal = 0;
            stage = 0;
            runStartTime = Time.time;
            BuildOptions();
            SetState(GameState.SectorMap);
            return true;
        }

        public bool SelectShip(string shipId)
        {
            if (state != GameState.SectorMap) return false;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Id == shipId)
                {
                    selectedShip = options[i];
                    SetState(GameState.ShipSelection);
                    return true;
                }
            }
            return false;
        }

        public bool CancelShipSelection()
        {
            if (state != GameState.ShipSelection) return false;
            SetState(GameState.SectorMap);
            return true;
        }

        public bool ConfirmEntryPoint(EntryPointType entry)
        {
            if (state != GameState.ShipSelection || selectedShip == null) return false;
            selectedEntry = entry;
            EntryPointProfile profile = EntryPointCatalog.Get(entry);

            totalRooms = profile.RoomCount;
            roomIndex = 0;
            maxHealth = RobotDefinitions.Get(robot).BaseHealth + GetBonusHealth();
            health = maxHealth;
            deathTimer = -1f;
            running = true;
            attackHeld = false;

            SetState(GameState.Combat);

            Action<ShipDefinition, EntryPointType> started = ShipStarted;
            if (started != null) started(selectedShip, entry);
            Action<float, float> hc = HealthChanged;
            if (hc != null) hc(health, maxHealth);
            EnterRoom(0);
            return true;
        }

        public bool ChooseModule(int offerIndex)
        {
            if (state != GameState.Reward || reward == null) return false;
            if (offerIndex < 0 || offerIndex >= reward.ModuleOffers.Count) return false;
            owned.Add(reward.ModuleOffers[offerIndex]);
            Action oc = OwnedModulesChanged;
            if (oc != null) oc();
            reward = null;
            stage++;
            BuildOptions();
            SetState(GameState.SectorMap);
            return true;
        }

        public bool AbandonRun()
        {
            if (state != GameState.SectorMap && state != GameState.ShipSelection && state != GameState.Combat && state != GameState.Reward) return false;
            running = false;
            totalRuns++;
            bestShips = Mathf.Max(bestShips, shipsCaptured);
            lastSummary = BuildSummary(false);
            SetState(GameState.MainMenu);
            return true;
        }

        public bool OpenUpgrades()
        {
            if (state != GameState.MainMenu && state != GameState.Death && state != GameState.Victory) return false;
            SetState(GameState.Upgrades);
            return true;
        }

        public bool CloseUpgrades()
        {
            if (state != GameState.Upgrades) return false;
            SetState(GameState.MainMenu);
            return true;
        }

        public bool GoToMainMenu()
        {
            if (state != GameState.Death && state != GameState.Victory) return false;
            SetState(GameState.MainMenu);
            return true;
        }

        public int Resources { get { return resources; } }
        public RobotType SelectedRobot { get { return robot; } }
        public int Victories { get { return victories; } }
        public int TotalRuns { get { return totalRuns; } }
        public int BestShipsCaptured { get { return bestShips; } }

        public event Action<int> ResourcesChanged;
        public event Action<UpgradeType, int> UpgradeLevelChanged;
        public event Action<RobotType> SelectedRobotChanged;

        public int GetUpgradeLevel(UpgradeType type) { return upgradeLevels[(int)type]; }
        public int GetMaxUpgradeLevel() { return MaxUpgradeLevel; }

        public int GetUpgradeCost(UpgradeType type)
        {
            int level = GetUpgradeLevel(type);
            return level >= MaxUpgradeLevel ? -1 : 60 + 45 * level;
        }

        public bool CanPurchaseUpgrade(UpgradeType type)
        {
            int cost = GetUpgradeCost(type);
            return cost >= 0 && resources >= cost;
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            if (!CanPurchaseUpgrade(type)) return false;
            resources -= GetUpgradeCost(type);
            upgradeLevels[(int)type]++;
            Action<int> rc = ResourcesChanged;
            if (rc != null) rc(resources);
            Action<UpgradeType, int> lc = UpgradeLevelChanged;
            if (lc != null) lc(type, upgradeLevels[(int)type]);
            return true;
        }

        public float GetBonusHealth() { return upgradeLevels[(int)UpgradeType.Health] * 10f; }
        public float GetDamageMultiplier() { return 1f + upgradeLevels[(int)UpgradeType.Damage] * 0.08f; }

        public void ResetAllProgress()
        {
            resources = 0;
            victories = 0;
            totalRuns = 0;
            bestShips = 0;
            upgradeLevels[0] = 0;
            upgradeLevels[1] = 0;
            Action<int> rc = ResourcesChanged;
            if (rc != null) rc(resources);
            Action<UpgradeType, int> lc = UpgradeLevelChanged;
            if (lc != null) { lc(UpgradeType.Health, 0); lc(UpgradeType.Damage, 0); }
        }

        private void AddResources(int amount)
        {
            resources += amount;
            Action<int> rc = ResourcesChanged;
            if (rc != null) rc(resources);
        }

        public IReadOnlyList<ModuleInstance> OwnedModules { get { return owned; } }
        public event Action OwnedModulesChanged;
        public event Action<ModuleInstance> ActiveModuleUsed;

        public float GetCooldownRemaining(ModuleId id)
        {
            float ready;
            return moduleReadyTime.TryGetValue(id, out ready) ? Mathf.Max(0f, ready - Time.time) : 0f;
        }

        public float GetCooldownNormalized(ModuleId id)
        {
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i].Id == id && owned[i].Cooldown > 0f)
                    return Mathf.Clamp01(GetCooldownRemaining(id) / owned[i].Cooldown);
            }
            return 0f;
        }

        public float CurrentHealth { get { return health; } }
        public float MaxHealth { get { return maxHealth; } }
        public float Shield { get { return Time.time < shieldUntil ? 30f : 0f; } }
        public bool IsAlive { get { return health > 0f; } }
        public RobotType Robot { get { return robot; } }

        public float SkillCooldownRemaining { get { return Mathf.Max(0f, skillReadyTime - Time.time); } }

        public float SkillCooldownNormalized
        {
            get { return Mathf.Clamp01(SkillCooldownRemaining / RobotDefinitions.Get(robot).SkillCooldown); }
        }

        public bool IsRunning { get { return running; } }
        public int CurrentRoomIndex { get { return roomIndex; } }
        public int TotalRooms { get { return totalRooms; } }
        public int EnemiesRemaining { get { return enemies; } }

        public event Action<float, float> HealthChanged;
        public event Action<DamageEvent> PlayerDamaged;
        public event Action SkillUsed;
        public event Action<RobotType> RobotChanged;
        public event Action<ShipDefinition, EntryPointType> ShipStarted;
        public event Action<RoomInfo> RoomEntered;
        public event Action<int> RoomCleared;
        public event Action<int, Vector3> ResourcesEarned;
        public event Action ShipFailed;

        public bool TryGetPlayerPosition(out Vector3 worldPosition)
        {
            return PointInFrontOfCamera(0f, out worldPosition);
        }

        public bool TryGetTargetPosition(out Vector3 worldPosition)
        {
            if (!running || enemies <= 0) { worldPosition = Vector3.zero; return false; }
            return PointInFrontOfCamera(2.6f, out worldPosition);
        }

        private static bool PointInFrontOfCamera(float rightOffset, out Vector3 point)
        {
            Camera cam = Camera.main;
            if (cam == null) { point = Vector3.zero; return false; }
            Transform t = cam.transform;
            point = t.position + t.forward * 10f + t.right * rightOffset;
            return true;
        }

        public Vector2 LastMoveInput { get { return moveInput; } }
        public void SetMoveInput(Vector2 direction) { moveInput = direction; }
        public void SetAttackHeld(bool held) { attackHeld = held && state == GameState.Combat; }
        public bool TryAttack() { return state == GameState.Combat && enemies > 0; }

        public bool TryUseSkill()
        {
            if (state != GameState.Combat || !running) return false;

            for (int i = 0; i < owned.Count; i++)
            {
                ModuleInstance m = owned[i];
                if (!m.IsActive || GetCooldownRemaining(m.Id) > 0.01f) continue;
                moduleReadyTime[m.Id] = Time.time + m.Cooldown;
                Action<ModuleInstance> used = ActiveModuleUsed;
                if (used != null) used(m);
                if (m.Id == ModuleId.EnergyShield) shieldUntil = Time.time + 5f;
            }

            if (Time.time < skillReadyTime) return false;
            skillReadyTime = Time.time + RobotDefinitions.Get(robot).SkillCooldown;
            Action used2 = SkillUsed;
            if (used2 != null) used2();
            for (int k = 0; k < 3; k++) KillEnemy();
            return true;
        }

        private void Update()
        {
            if (state != GameState.Combat) return;
            float dt = Time.deltaTime;

            if (deathTimer >= 0f)
            {
                deathTimer -= dt;
                if (deathTimer < 0f) FinishDeath();
                return;
            }
            if (!running) return;

            if (roomActive)
            {
                if (enemies > 0)
                {
                    damageTimer -= dt;
                    if (damageTimer <= 0f)
                    {
                        damageTimer = UnityEngine.Random.Range(1.6f, 2.8f);
                        float scale = selectedShip != null ? selectedShip.DifficultyMultiplier : 1f;
                        TakeDamage(UnityEngine.Random.Range(5f, 12f) * scale);
                    }
                    if (attackHeld)
                    {
                        killTimer -= dt;
                        if (killTimer <= 0f)
                        {
                            killTimer = 0.55f;
                            KillEnemy();
                        }
                    }
                }
            }
            else if (nextRoomTimer > 0f)
            {
                nextRoomTimer -= dt;
                if (nextRoomTimer <= 0f)
                {
                    if (roomIndex + 1 >= totalRooms) CompleteShip();
                    else EnterRoom(roomIndex + 1);
                }
            }
        }

        private void EnterRoom(int index)
        {
            roomIndex = index;
            bool reactor = index == totalRooms - 1;
            EntryPointProfile profile = EntryPointCatalog.Get(selectedEntry);
            enemies = Mathf.Max(2, Mathf.RoundToInt(UnityEngine.Random.Range(3, 6) * profile.EnemyCountMultiplier)) + (reactor ? 1 : 0);
            roomActive = true;
            damageTimer = 2f;
            killTimer = 0.4f;
            Action<RoomInfo> h = RoomEntered;
            if (h != null) h(new RoomInfo(index, totalRooms, reactor ? RoomRole.Reactor : RoomRole.Normal));
        }

        private void KillEnemy()
        {
            if (!roomActive || enemies <= 0) return;
            enemies--;
            enemiesKilledTotal++;
            int drop = UnityEngine.Random.Range(3, 7);
            AddResources(drop);
            runResources += drop;
            Vector3 pos;
            TryGetTargetPosition(out pos);
            Action<int, Vector3> h = ResourcesEarned;
            if (h != null) h(drop, pos);

            if (enemies <= 0)
            {
                roomActive = false;
                roomsClearedTotal++;
                nextRoomTimer = 1.4f;
                Action<int> rc = RoomCleared;
                if (rc != null) rc(roomIndex);
            }
        }

        private void TakeDamage(float amount)
        {
            float dealt = amount;
            if (Time.time < shieldUntil) dealt = 0f;
            health = Mathf.Max(0f, health - dealt);
            Action<DamageEvent> dh = PlayerDamaged;
            if (dh != null) dh(new DamageEvent(dealt));
            Action<float, float> hc = HealthChanged;
            if (hc != null) hc(health, maxHealth);
            if (health <= 0f) Die();
        }

        private void Die()
        {
            running = false;
            roomActive = false;
            attackHeld = false;
            enemies = 0;
            deathTimer = 1.4f;
            Action f = ShipFailed;
            if (f != null) f();
        }

        private void FinishDeath()
        {
            totalRuns++;
            bestShips = Mathf.Max(bestShips, shipsCaptured);
            lastSummary = BuildSummary(false);
            SetState(GameState.Death);
        }

        private void CompleteShip()
        {
            running = false;
            roomActive = false;
            attackHeld = false;
            shipsCaptured++;

            EntryPointProfile profile = EntryPointCatalog.Get(selectedEntry);
            int awarded = Mathf.RoundToInt(selectedShip.BaseResourceReward * profile.ResourceMultiplier);
            AddResources(awarded);
            runResources += awarded;

            if (selectedShip.IsBossShip)
            {
                victories++;
                totalRuns++;
                bestShips = Mathf.Max(bestShips, shipsCaptured);
                lastSummary = BuildSummary(true);
                SetState(GameState.Victory);
                return;
            }

            reward = new ShipRewardInfo(selectedShip, selectedEntry, awarded, BuildOffers(profile));
            SetState(GameState.Reward);
        }

        private void BuildOptions()
        {
            options = new List<ShipDefinition>();
            if (stage >= 4)
            {
                options.Add(new ShipDefinition("mock_boss", "Нексус-Прайм", 5, ShipRisk.Boss, true, 2.2f, 160));
                return;
            }
            float baseMultiplier = 0.9f + 0.3f * stage;
            int count = 2 + (stage % 2);
            ShipRisk[] risks = { ShipRisk.Safe, ShipRisk.Standard, ShipRisk.Risky };
            for (int i = 0; i < count; i++)
            {
                ShipRisk risk = risks[i + (3 - count)];
                float mult = baseMultiplier * (risk == ShipRisk.Safe ? 0.85f : risk == ShipRisk.Risky ? 1.2f : 1f);
                int reward = (risk == ShipRisk.Safe ? 30 : risk == ShipRisk.Risky ? 65 : 45) + stage * 20;
                string name = ShipNames[(stage * 2 + i) % ShipNames.Length];
                options.Add(new ShipDefinition("mock_s" + stage + "_" + i, name, stage + 1, risk, false, mult, reward));
            }
        }

        private IReadOnlyList<ModuleInstance> BuildOffers(EntryPointProfile profile)
        {
            List<ModuleDefinition> pool = new List<ModuleDefinition>(ModuleCatalog.All);
            List<ModuleInstance> offers = new List<ModuleInstance>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                float roll = UnityEngine.Random.value - profile.RareModuleBonus * 0.4f;
                ModuleRarity rarity = roll < 0.12f ? ModuleRarity.Epic : roll < 0.45f ? ModuleRarity.Rare : ModuleRarity.Common;
                offers.Add(new ModuleInstance(pool[pick], rarity));
                pool.RemoveAt(pick);
            }
            return offers;
        }

        private RunSummary BuildSummary(bool won)
        {
            return new RunSummary(won, robot, shipsCaptured, roomsClearedTotal, enemiesKilledTotal, runResources,
                Time.time - runStartTime, new List<ModuleInstance>(owned));
        }

        public void DebugDamage(float amount) { if (state == GameState.Combat && running) TakeDamage(amount); }
        public void DebugKillEnemies() { if (state == GameState.Combat && running) { while (enemies > 0) KillEnemy(); } }
        public void DebugKillRobot() { if (state == GameState.Combat && running) TakeDamage(maxHealth * 2f); }
        public void DebugAddResources(int amount) { AddResources(amount); }
    }
}
