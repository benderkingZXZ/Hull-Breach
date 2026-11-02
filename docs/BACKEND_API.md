# Hull Breach — API игровой логики для интерфейса

> **Статус контракта: ЗАФИКСИРОВАН (v1.0).** Публичные имена, сигнатуры, события и порядок состояний из этого документа
> не меняются без записи в раздел «Журнал изменений контракта» (в конце файла) и уведомления разработчика интерфейса.
> Код **не компилировался в Unity** (см. KNOWN_ISSUES.md) — контракт определён до реализации, но реализация проверена только ревью.

## 1. Принципы

1. Игровые системы **не знают** о UI: нет ссылок на `UnityEngine.UI`, TMP, Canvas и т.д.
2. UI → логика: вызов **публичных методов** (команды). Логика → UI: **C#-события** и **публичные свойства** для чтения.
3. Все события — обычные `System.Action<...>`. Подписка в `OnEnable`, отписка в `OnDisable`.
4. Единственная «точка входа» для навигации по экранам — `GameStateManager`. Остальные системы UI в основном **читает**.
5. Все методы-команды возвращают `bool` (`true` — команда принята). При `false` ничего не изменилось (в консоль пишется warning).
6. Игровая логика не вызывает `Time.timeScale`, не загружает сцены, не создаёт Canvas.

## 2. Пространства имён и папки

```
Assets/HullBreach/
  HullBreach.Gameplay.asmdef
  Scripts/
    Core/         namespace HullBreach.Core         GameEnums, GameStateManager
    Data/         namespace HullBreach.Data         чистые данные и каталоги (без MonoBehaviour)
    Combat/       namespace HullBreach.Combat       Health, DamageInfo, Projectile, TargetRegistry
    Player/       namespace HullBreach.Player       PlayerController, PlayerCombat
    Enemies/      namespace HullBreach.Enemies      EnemyController, EnemySpawner
    Rooms/        namespace HullBreach.Rooms        RoomController, RoomExitTrigger, RoomGenerator, RoomLayout
    RunFlow/      namespace HullBreach.RunFlow      ShipRunManager, SectorMapSystem
    Modules/      namespace HullBreach.Modules      ModuleSystem
    Meta/         namespace HullBreach.Meta         ProgressionSystem
    Persistence/  namespace HullBreach.Persistence  SaveSystem, SaveData
  Tests/Editor/   namespace HullBreach.Tests        EditMode-тесты чистой логики (не запускались)
```

Зависимости между пространствами (стрелка = «использует»): `Core → все`, `RunFlow → Rooms, Enemies, Player, Data`, `Modules → Player, Combat, Enemies, Data`,
`Meta → Persistence, Data`, `Rooms → Enemies, Player, Data`, `Enemies → Combat, Data`, `Player → Combat, Data`, `Combat → Core`, `Data → Core`.
Циклов между **сборками** нет (всё в одной `HullBreach.Gameplay`).

## 3. Перечисления (`HullBreach.Core`)

| Enum | Значения |
|---|---|
| `GameState` | `MainMenu, SectorMap, ShipSelection, Combat, Reward, Death, Upgrades, Victory` |
| `RobotType` | `Samurai, Techno` |
| `EntryPointType` | `EngineRoom, CargoHold, Bridge` |
| `EnemyType` | `Drone, Gunner, Heavy, Exploder, MiniBoss` |
| `ModuleRarity` | `Common, Rare, Epic` |
| `ModuleId` | `ArmorPlating, OverclockedCore, ServoBoosters, TargetingArray, NanoRepair, EnergyShield, EmpBurst, RepairInjector` |
| `UpgradeType` | `Health, Damage` |
| `ShipRisk` | `Safe, Standard, Risky, Boss` |
| `RoomRole` | `Normal, Reactor` |
| `RoomState` | `Inactive, Active, Cleared` |
| `Faction` | `Player, Enemy` |

## 4. GameStateManager (`HullBreach.Core`) — главный фасад для UI

### 4.1 Состояния и допустимые переходы

```
MainMenu      → SectorMap (StartSectorRun) | Upgrades (OpenUpgrades)
SectorMap     → ShipSelection (SelectShip) | MainMenu (AbandonRun)
ShipSelection → Combat (ConfirmEntryPoint) | SectorMap (CancelShipSelection) | MainMenu (AbandonRun)
Combat        → Reward (авто, корабль захвачен) | Victory (авто, захвачен корабль-босс)
                | Death (авто, робот погиб) | MainMenu (AbandonRun)
Reward        → SectorMap (ChooseModule) | MainMenu (AbandonRun)
Death         → SectorMap (StartSectorRun) | Upgrades (OpenUpgrades) | MainMenu (GoToMainMenu)
Upgrades      → MainMenu (CloseUpgrades / GoToMainMenu) | SectorMap (StartSectorRun)
Victory       → MainMenu (GoToMainMenu) | Upgrades (OpenUpgrades) | SectorMap (StartSectorRun)
```
Начальное состояние: `MainMenu`. Недопустимый переход → метод возвращает `false`.

### 4.2 Свойства (чтение)

```csharp
GameState CurrentState { get; }
ShipDefinition SelectedShip { get; }            // выбран в SectorMap, null вне ShipSelection/Combat/Reward
EntryPointType SelectedEntryPoint { get; }
ShipRewardInfo CurrentReward { get; }           // не null только в состоянии Reward
RunSummary LastRunSummary { get; }              // итог последнего забега (Death / Victory)
int RunShipsCaptured { get; }                   // захвачено обычных кораблей в текущем забеге
int RunResourcesEarned { get; }                 // ресурсов получено в текущем забеге
IReadOnlyList<ShipDefinition> GetShipOptions()  // варианты кораблей на карте сектора (2–3 шт., либо только босс)
IReadOnlyList<EntryPointProfile> GetEntryPoints() // 3 точки входа с описанием/параметрами
```

### 4.3 Команды (UI → логика)

```csharp
bool SelectRobot(RobotType robot);                 // только MainMenu / Upgrades
bool StartSectorRun();                             // новый забег (MainMenu/Upgrades/Death/Victory → SectorMap)
bool SelectShip(string shipId);                    // SectorMap → ShipSelection; id берётся из ShipDefinition.Id
bool CancelShipSelection();                        // ShipSelection → SectorMap
bool ConfirmEntryPoint(EntryPointType entry);      // ShipSelection → Combat (генерация и старт боя)
bool ChooseModule(int offerIndex);                 // Reward: 0..2 → SectorMap
bool AbandonRun();                                 // выход из забега в главное меню (ресурсы сохраняются)
bool OpenUpgrades();                               // → Upgrades
bool CloseUpgrades();                              // Upgrades → MainMenu
bool GoToMainMenu();                               // Death/Victory/Upgrades → MainMenu
```

### 4.4 События

```csharp
event Action<GameState, GameState> StateChanged;   // (предыдущее, новое) — срабатывает ПОСЛЕ смены CurrentState
event Action<ShipRewardInfo> RewardPrepared;       // после StateChanged(→Reward)
event Action<RunSummary> RunFailed;                // после StateChanged(→Death)
event Action<RunSummary> SectorVictory;            // после StateChanged(→Victory)
```
UI может использовать **либо** `StateChanged` + чтение свойств, **либо** специализированные события — данные идентичны.

## 5. Модели данных (`HullBreach.Data`) — неизменяемые (get-only)

```csharp
ShipDefinition   { string Id; string DisplayName; int Tier /*1..4, 5=босс*/; ShipRisk Risk; bool IsBossShip;
                   float DifficultyMultiplier; int BaseResourceReward; }
EntryPointProfile{ EntryPointType Entry; string DisplayName; string Description; int RoomCount /*3|5|4*/;
                   float EnemyCountMultiplier; float EnemyStrengthMultiplier; float ResourceMultiplier; float RareModuleBonus; }
ModuleInstance   { ModuleId Id; ModuleRarity Rarity; string DisplayName; string Description; bool IsActive;
                   float Value; float SecondaryValue; float Cooldown; ModuleDefinition Definition; }
ShipRewardInfo   { ShipDefinition Ship; EntryPointType Entry; int ResourcesAwarded; IReadOnlyList<ModuleInstance> ModuleOffers; }
RunSummary       { bool Won; RobotType Robot; int ShipsCaptured; int RoomsCleared; int EnemiesKilled; int ResourcesEarned;
                   float DurationSeconds; IReadOnlyList<ModuleInstance> Modules; }
ShipCompletionResult { ShipDefinition Ship; EntryPointType Entry; int ResourcesAwarded; int RoomsCleared; int EnemiesKilled; float DurationSeconds; }
ShipFailureInfo  { ShipDefinition Ship; EntryPointType Entry; int RoomIndex; int RoomsCleared; int EnemiesKilled; }
```
Тексты (`DisplayName`, `Description`) — на русском, зашиты в `ModuleCatalog`/`EntryPointCatalog`/`SectorMapSystem`. UI может игнорировать их и локализовать по `Id`.

## 6. Ввод в бою (UI → персонаж)

Экранные элементы управления вызывают эти методы. Они безопасны вне боя (ничего не делают, если ввод выключен).

```csharp
// PlayerController (HullBreach.Player)
void SetMoveInput(Vector2 direction);        // виртуальный стик; длина вектора ≤ 1 (больше — обрезается)
// PlayerCombat
void SetAttackHeld(bool held);               // кнопка атаки: true при нажатии, false при отпускании (авто-повтор по скорости атаки)
bool TryAttack();                            // одиночная атака (если UI хочет «тап»)
bool TryUseSkill();                          // кнопка навыка; true — сработал навык робота
```
> **Решение по навыкам:** кнопка навыка запускает навык робота **и** все готовые активные модули (`ModuleSystem` слушает `PlayerCombat.SkillButtonPressed`).
> Если интерфейсу нужны отдельные кнопки на модули — `ModuleSystem.TryActivateModule(ModuleId)`.

## 7. Данные боя для HUD (логика → UI)

```csharp
// Health (HullBreach.Combat) — компонент на игроке и врагах; PlayerController.Health
float MaxHealth; float CurrentHealth; float Normalized; bool IsDead; float ShieldAmount;
event Action<float,float> HealthChanged;       // (текущее, максимум)
event Action<DamageInfo,float> Damaged;        // (инфо об уроне, фактически нанесено) — для всплывающих цифр
event Action<Health,DamageInfo> Died;

// PlayerController
RobotType RobotType; PlayerStats Stats; bool IsAlive; Vector2 FacingDirection;
event Action<RobotType> RobotChanged; event Action<PlayerStats> StatsApplied; event Action Died;

// PlayerCombat
float SkillCooldownRemaining; float SkillCooldownNormalized;  // 0..1
event Action<Vector2,Vector2,RobotType> Attacked;   // (точка, направление, робот) — для VFX/звука
event Action<RobotType,Vector2> SkillUsed;
event Action<Health> TargetChanged;                 // текущая цель автонаведения (может быть null) — для маркера

// ShipRunManager (HullBreach.RunFlow)
bool IsRunning; ShipDefinition CurrentShip; EntryPointType CurrentEntry; int CurrentRoomIndex; int TotalRooms;
RoomController CurrentRoom; int EnemiesRemaining; int LastSeed;
event Action<ShipDefinition,EntryPointType> ShipStarted;
event Action<int,int,RoomController> RoomEntered;   // (индекс, всего комнат, комната) — прогресс «комната 2/5», границы камеры: room.WorldBounds
event Action<int> RoomCleared;                      // двери открыты
event Action<EnemyController> EnemyKilled;
event Action<int,Vector3> ResourcesEarned;          // (сумма, мировая позиция) — «+5» над трупом
event Action<ShipCompletionResult> ShipCompleted;   // информативно: GameStateManager сам переключит состояние
event Action<ShipFailureInfo> ShipFailed;           // робот погиб; состояние Death наступит через deathScreenDelay (по умолч. 1.2 с)

// RoomController
RoomState State; int EnemiesAlive; Bounds WorldBounds; RoomRole Role;
event Action<RoomController> Entered, Cleared, ExitReached; event Action<EnemyController> EnemyKilled; event Action<bool> DoorsStateChanged;

// SectorMapSystem
int ShipsCaptured; int CurrentTier; bool IsBossStage; IReadOnlyList<ShipDefinition> CurrentOptions;
event Action<IReadOnlyList<ShipDefinition>> OptionsChanged;
```
`PlayerStats`: `MaxHealth, Damage, MoveSpeed, AttackInterval, AttackRange, SkillCooldown` (get-only).

## 8. Модули (`HullBreach.Modules.ModuleSystem`)

```csharp
IReadOnlyList<ModuleInstance> OwnedModules { get; }          // собранные в текущем забеге
IReadOnlyList<ModuleInstance> CurrentOffers { get; }         // 3 предложения (не null только в Reward)
float GetCooldownRemaining(ModuleId id); float GetCooldownNormalized(ModuleId id);
bool TryActivateModule(ModuleId id);                         // активный модуль по отдельной кнопке (опционально)
event Action<IReadOnlyList<ModuleInstance>> OffersGenerated;
event Action<ModuleInstance> ModuleAcquired;
event Action<ModuleInstance> ActiveModuleUsed;               // для VFX/индикатора перезарядки
event Action OwnedModulesChanged;
```
Выбор модуля — только через `GameStateManager.ChooseModule(index)`.

Таблица модулей (Common / Rare / Epic):

| Id | Тип | Эффект |
|---|---|---|
| ArmorPlating | пассивный | +20 / +35 / +55 к макс. здоровью |
| OverclockedCore | пассивный | +15 / +25 / +40 % урона |
| ServoBoosters | пассивный | +10 / +18 / +28 % скорости передвижения |
| TargetingArray | пассивный | +15 / +25 / +40 % скорости атаки |
| NanoRepair | пассивный | +2 / +4 / +7 HP за убитого врага |
| EnergyShield | активный | щит 30 / 50 / 80 на 5 с; перезарядка 14 / 12 / 10 с |
| EmpBurst | активный | оглушение 1.5 / 2.2 / 3.0 с и урон 10 / 20 / 35 в радиусе 4.5; КД 15 / 13 / 11 с |
| RepairInjector | активный | лечит 25 / 40 / 60 % макс. HP; КД 25 / 22 / 18 с |

## 9. Прогрессия (`HullBreach.Meta.ProgressionSystem`)

```csharp
int Resources { get; }                       RobotType SelectedRobot { get; }
int GetUpgradeLevel(UpgradeType type);       int GetMaxUpgradeLevel();
int GetUpgradeCost(UpgradeType type);        // цена следующего уровня; -1 если максимум
bool CanPurchaseUpgrade(UpgradeType type);
bool TryPurchaseUpgrade(UpgradeType type);   // списывает ресурсы, автосохранение
float GetBonusHealth();  float GetDamageMultiplier();   // для отображения «+10 HP», «×1.16»
int Victories; int TotalRuns; int BestShipsCaptured;
event Action<int> ResourcesChanged;
event Action<UpgradeType,int> UpgradeLevelChanged;
event Action<RobotType> SelectedRobotChanged;
```
Улучшения вступают в силу на **следующем** старте корабля/забега. Покупка разрешена в любом состоянии, UI показывает её на экране Upgrades.

## 10. Сохранение (`HullBreach.Persistence.SaveSystem`)

Сохраняется **только постоянный прогресс** (`SaveData`: ресурсы, уровни здоровья/урона, выбранный робот, статистика). Файл `persistentDataPath/hullbreach_save.json`
(+ `.bak`). Автосохранение: покупка улучшения, захват корабля, конец забега, `OnApplicationPause(true)`, `OnApplicationQuit`.
Состояние незавершённого забега **не сохраняется** (при выходе забег считается прерванным).
Для UI: `SaveSystem.DeleteSave()` + `ProgressionSystem.ResetAllProgress()` — кнопка «Сбросить прогресс» (опционально).

## 11. Типовые последовательности

```csharp
// Главное меню
gsm.SelectRobot(RobotType.Techno); gsm.StartSectorRun();            // → SectorMap
// Карта сектора
foreach (var ship in gsm.GetShipOptions()) { /* карточки */ }
gsm.SelectShip(ship.Id);                                            // → ShipSelection
// Выбор входа
foreach (var ep in gsm.GetEntryPoints()) { /* 3 кнопки */ }
gsm.ConfirmEntryPoint(EntryPointType.Bridge);                       // → Combat
// Бой: стик → player.SetMoveInput(v); кнопки → combat.SetAttackHeld(b) / combat.TryUseSkill()
// Корабль захвачен → StateChanged(Combat→Reward), CurrentReward.ModuleOffers (3 шт.)
gsm.ChooseModule(1);                                                // → SectorMap (следующий уровень кораблей)
// Робот погиб → ShipFailed → через ~1.2 с StateChanged(→Death), LastRunSummary
gsm.OpenUpgrades(); progression.TryPurchaseUpgrade(UpgradeType.Health); gsm.StartSectorRun();
// Корабль-босс побеждён → StateChanged(→Victory)
```

## 12. Гарантии и негарантии

* Все события вызываются в главном потоке Unity.
* `StateChanged` вызывается строго после обновления `CurrentState`; внутри обработчика можно вызывать новые команды.
* События боя (`RoomEntered`, `EnemyKilled`…) приходят только в состоянии `Combat` (кроме `ShipFailed`, `ShipCompleted`, которые завершают его).
* Игровые объекты (игрок) могут быть деактивированы вне боя (`hidePlayerOutsideCombat`). UI не должен хранить ссылки на `GameObject` игрока без проверки; подписывайтесь на события через `PlayerController`, а не на `GameObject`.

## 13. Журнал изменений контракта

| Версия | Дата | Изменение | Причина |
|---|---|---|---|
| 1.0 | 2026-10-22 | Первая фиксация | — |
| 1.1 | 2026-10-29 | **Добавлены** (без изменения существующих) публичные методы программной настройки `Configure(...)` у `Health`, `PlayerCombat`, `EnemyController`, `EnemySpawner`, `RoomController`, `RoomExitTrigger`, `RoomGenerator`, `ShipRunManager`, `ModuleSystem`, `ProgressionSystem`, `GameStateManager` | Сборка мира из кода без префабов и инспектора (Assets/HullBreachGame), нужна для автосборки APK. Контракт для UI (раздел 3–12) не затронут |
