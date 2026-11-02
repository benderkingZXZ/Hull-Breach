# FRONTEND_API.md: контракт между интерфейсом и игровым модулем

Версия контракта фронтенда: **1.0**, под бэкенд `API.md` v1.0 (из архива `HullBreach_Backend.zip`).
Статус: код не компилировался и не запускался в Unity (подробности в `README.md` и `KNOWN_ISSUES.md`).

## 1. Принципы

1. UI **не содержит игровой логики**. Он показывает данные и превращает действия игрока в вызовы методов.
2. UI видит игровой модуль только через интерфейсы в `Assets/HullBreachUI/Scripts/Contracts`. Экраны не ссылаются на `GameStateManager`, `ProgressionSystem` и т.д. напрямую.
3. Реализация интерфейсов для настоящего бэкенда: `Scripts/Bridge` (тонкие адаптеры без логики). Для проверки без бэкенда: `Scripts/Mock/MockGameHost`.
4. Из бэкенда UI использует напрямую только **неизменяемые данные и перечисления**: `ShipDefinition`, `EntryPointProfile`, `ModuleInstance`, `ShipRewardInfo`, `RunSummary`, `RobotDefinitions`, `GameState`, `RobotType`, `UpgradeType` и т.п. Поэтому сборка `HullBreach.UI` ссылается на `HullBreach.Gameplay`. Изменение этих типов ломает компиляцию UI, и это нужно.
5. UI сам не меняет `GameState`. Экран выбирается по событию `StateChanged`.

## 2. Сборки

| Сборка | Ссылается на | Назначение |
|---|---|---|
| `HullBreach.UI` | `HullBreach.Gameplay`, `Unity.TextMeshPro`, `UnityEngine.UI` | весь интерфейс, мост, заглушка |
| `HullBreach.UI.Editor` | `HullBreach.UI` | пункты меню редактора |

`HullBreach.Gameplay` на `HullBreach.UI` **не ссылается**.

## 3. Интерфейсы и соответствие бэкенду

### 3.1. `IGameFlowService` ← `HullBreach.Core.GameStateManager`

| Член | Бэкенд |
|---|---|
| `CurrentState`, `SelectedShip`, `SelectedEntryPoint`, `CurrentReward`, `LastRunSummary`, `RunShipsCaptured`, `RunResourcesEarned` | одноимённые свойства |
| `GetShipOptions()`, `GetEntryPoints()` | одноимённые методы |
| `event StateChanged(prev, next)` | `StateChanged` |
| `SelectRobot`, `StartSectorRun`, `SelectShip(id)`, `CancelShipSelection`, `ConfirmEntryPoint(entry)`, `ChooseModule(index)`, `AbandonRun`, `OpenUpgrades`, `CloseUpgrades`, `GoToMainMenu` | одноимённые команды (возвращают `bool`) |

### 3.2. `IProgressionService` ← `HullBreach.Meta.ProgressionSystem`

`Resources`, `SelectedRobot`, `Victories`, `TotalRuns`, `BestShipsCaptured`, `GetUpgradeLevel`, `GetMaxUpgradeLevel`, `GetUpgradeCost` (−1 = максимум), `CanPurchaseUpgrade`, `TryPurchaseUpgrade`, `GetBonusHealth`, `GetDamageMultiplier`, `ResetAllProgress`; события `ResourcesChanged`, `UpgradeLevelChanged`, `SelectedRobotChanged`. Все одноимённые.

### 3.3. `IModuleService` ← `HullBreach.Modules.ModuleSystem`

`OwnedModules`, `GetCooldownNormalized(id)`, `GetCooldownRemaining(id)`; события `OwnedModulesChanged`, `ActiveModuleUsed`. Выбор модуля идёт через `IGameFlowService.ChooseModule`, а не через `ModuleSystem`.

### 3.4. `ICombatFeed` (данные боя, только чтение) ← `Health` / `PlayerController` / `PlayerCombat` / `ShipRunManager`

| Член UI | Откуда в бэкенде |
|---|---|
| `CurrentHealth`, `MaxHealth` | `player.Health.CurrentHealth`, `.MaxHealth` |
| `Shield` | `Health.ShieldAmount` (опрос каждый кадр, события нет) |
| `IsAlive` | `!Health.IsDead` |
| `Robot` | `PlayerController.RobotType` |
| `SkillCooldownNormalized`, `SkillCooldownRemaining` | `PlayerCombat` (опрос каждый кадр) |
| `IsRunning`, `CurrentRoomIndex`, `TotalRooms`, `EnemiesRemaining` | `ShipRunManager` (опрос каждый кадр) |
| `TryGetPlayerPosition` | `player.transform.position` |
| `TryGetTargetPosition` | цель из `PlayerCombat.TargetChanged` (если жива) |
| `HealthChanged(cur, max)` | `Health.HealthChanged` |
| `PlayerDamaged(DamageEvent)` | `Health.Damaged` (берётся второй аргумент: фактический урон после щита; 0 = щит поглотил всё) |
| `SkillUsed` | `PlayerCombat.SkillUsed` |
| `RobotChanged` | `PlayerController.RobotChanged` |
| `ShipStarted(ship, entry)` | `ShipRunManager.ShipStarted` |
| `RoomEntered(RoomInfo)` | `ShipRunManager.RoomEntered` (`RoomInfo.Role` берётся из `RoomController.Role`) |
| `RoomCleared(index)` | `ShipRunManager.RoomCleared` |
| `ResourcesEarned(amount, worldPos)` | `ShipRunManager.ResourcesEarned` |
| `ShipFailed` | `ShipRunManager.ShipFailed` |

### 3.5. `ICombatInputSink` (ввод игрока) → бэкенд

| Член | Бэкенд |
|---|---|
| `SetMoveInput(Vector2)` | `PlayerController.SetMoveInput` |
| `SetAttackHeld(bool)` | `PlayerCombat.SetAttackHeld` |
| `TryAttack()` | `PlayerCombat.TryAttack` (UI не использует, оставлено в контракте) |
| `TryUseSkill()` | `PlayerCombat.TryUseSkill` (одновременно срабатывают готовые активные модули, это логика бэкенда) |

### 3.6. `UIServices`

Контейнер `Flow`, `Progression`, `Modules`, `Combat`, `Input` и флаг `IsMock`. Собирается в `BackendBinder.TryCreate` (бэкенд) или `MockGameHost.CreateServices` (заглушка).

## 4. Матрица: экран → вызовы и подписки

| Экран | Читает | Подписки (пока экран виден) | Вызывает |
|---|---|---|---|
| MainMenu | `Progression.Resources/SelectedRobot/Victories/TotalRuns/BestShipsCaptured`, `RobotDefinitions.Get` | `ResourcesChanged`, `SelectedRobotChanged` | `SelectRobot`, `StartSectorRun`, `OpenUpgrades`; настройки (там `ResetAllProgress`) |
| SectorMap | `GetShipOptions`, `RunShipsCaptured`, `RunResourcesEarned`, `Modules.OwnedModules` | `ResourcesChanged` | `SelectShip(id)`; `AbandonRun` (через подтверждение) |
| EntrySelection | `SelectedShip`, `GetEntryPoints` | `ResourcesChanged` | `ConfirmEntryPoint(entry)`, `CancelShipSelection` |
| HUD | весь `ICombatFeed`, `Modules`, `Progression.Resources`, `Flow.SelectedShip/SelectedEntryPoint/GetEntryPoints` | **постоянные** (см. п. 5.1): `HealthChanged`, `PlayerDamaged`, `ShipStarted`, `RoomEntered`, `RoomCleared`, `ResourcesEarned`, `ShipFailed`, `ActiveModuleUsed`, `OwnedModulesChanged`, `ResourcesChanged` | ввод: `SetMoveInput`, `SetAttackHeld`, `TryUseSkill`; пауза: `AbandonRun` (через подтверждение) |
| ModuleSelection | `CurrentReward` (`Ship`, `ResourcesAwarded`, `ModuleOffers`), `Modules.OwnedModules` | `ResourcesChanged` | `ChooseModule(index)` |
| Death / Victory | `LastRunSummary`, `Progression.*` | нет | `StartSectorRun`, `OpenUpgrades`, `GoToMainMenu` |
| Upgrades | `Progression.GetUpgradeLevel/GetMaxUpgradeLevel/GetUpgradeCost/GetBonusHealth/GetDamageMultiplier/Resources` | `ResourcesChanged`, `UpgradeLevelChanged` | `TryPurchaseUpgrade` (после подтверждения), `CloseUpgrades`, `StartSectorRun` |
| UIManager | `CurrentState` | `StateChanged` (всегда) | нет |

## 5. Порядок событий и особенности бэкенда, которые учтены в UI

1. **`StateChanged(Combat)` приходит раньше `ShipStarted` и `RoomEntered`.** HUD появляется с задержкой (скрытие прошлого экрана), поэтому HUD подписан на события боя постоянно и хранит последний баннер до момента показа.
2. **`CurrentReward` и `LastRunSummary` назначаются до `StateChanged`.** Экраны читают их в `OnShown`, ожидать `RewardPrepared`, `RunFailed` и `SectorVictory` не нужно.
3. **Бэкенд обнуляет ввод при смене комнат** (`SetInputEnabled(false)` на время перехода). Поэтому стик шлёт вектор **каждый кадр**, пока палец на экране, а флаг атаки подтверждается каждый кадр, пока кнопка зажата.
4. **Объект игрока неактивен вне боя.** Адаптер держит прямые ссылки и подписывается один раз. Поиск систем идёт с `includeInactive`.
5. **Пауза.** Бэкенд не трогает `Time.timeScale`. UI ставит 0 при открытии паузы и возвращает прежнее значение при закрытии или смене состояния. Все анимации UI идут по `unscaledDeltaTime`.
6. **События для щита, перезарядки навыка и числа врагов в API v1.0 нет**, поэтому HUD опрашивает свойства каждый кадр (без аллокаций; текст обновляется только при изменении значения).
7. **Улучшения вступают в силу на следующем старте** (так в API бэкенда). Экран прокачки пишет об этом в подзаголовке.
8. **Двойные нажатия.** После нажатия «Подтвердить выбор модуля» и «Начать абордаж» кнопка блокируется до следующей смены экрана (бэкенд вернул бы `false`).
9. Все команды возвращают `bool`. При `false` UI показывает тост и остаётся на экране.

## 6. Что UI считает сам (только отображение)

| Что | Где | Допущение |
|---|---|---|
| Деления сложности корабля (1–5) | `SectorMapUI.DifficultyPips` | `ceil((DifficultyMultiplier − 0.6) / 0.4)`, у босса всегда 5. Только для наглядности |
| Длина полос «Врагов», «Сила», «Ресурсы», «Редкие модули» | `EntrySelectionUI` | нормируются по максимуму среди трёх входов; подписи берут множители из `EntryPointProfile` |
| «Дальше: +X» на экране прокачки | `UpgradeScreenUI` | линейная экстраполяция `бонус / уровень × (уровень + 1)`; на уровне 0 не показывается |
| «из 4» в подписях | `UIStrings.SectorShipCount = 4` | совпадает с `SectorMapSystem.RegularShipCount` |
| Тексты роботов («Ближний бой…», описание навыка) | `UIStrings` | в `RobotDefinition` описания нет; название навыка и числа берутся из бэкенда |

Награды за корабль UI **не вычисляет**: на карте сектора показывает `BaseResourceReward` и пишет «Итог зависит от точки входа»; фактическую сумму показывает экран награды (`ResourcesAwarded`).

## 7. Что UI не делает

Генерацию комнат, ИИ, боевые расчёты, генерацию наград, сохранение и модель прокачки. UI хранит в `PlayerPrefs` только свои настройки (`hb_ui_*`: громкость, вибрация, числа урона, режим стика, размер и прозрачность кнопок).
`UISettings.MasterVolume` управляет `AudioListener.volume`; в бэкенде звука нет.

## 8. Подключение (резюме)

- `HullBreachUIBootstrap` (режимы `Auto` / `Backend` / `Mock`) находит системы бэкенда сам или берёт ссылки из Inspector.
- Другая реализация: реализовать пять интерфейсов и передать `UIServices` в `UIManager.Initialize` (см. заглушку).

## 9. Предложения бэкенд-разработчику (необязательные, по приоритету)

1. **Значения следующего уровня прокачки**: `GetNextBonusHealth()` и `GetNextDamageMultiplier()` либо публичные `HealthPerLevel` и `DamageBonusPerLevel`. Сейчас «Дальше: +X» экстраполируется, а на уровне 0 не показывается.
2. **События вместо опроса**: `Health.ShieldChanged`, `PlayerCombat.SkillReady`, `RoomController.EnemiesChanged`.
3. **Описание навыка робота** в `RobotDefinition` (текст), чтобы убрать тексты из `UIStrings`.
4. **`RunResourcesEarned`**: уточнить, обновляется ли оно во время забега или только при завершении корабля. UI показывает его на карте сектора как «Добыто за забег».
5. **Оценка награды**: `EstimateReward(ship, entry)`, если захотим показывать итог ещё на выборе входа. Сейчас UI показывает только множители.
6. **Пауза**: если нужно управлять паузой в бэкенде (например, чтобы заморозить только бой), добавить `Pause()` и `Resume()`; UI подключится в `UIManager.SetPaused`.
7. **Ввод при смене комнат**: бэкенд может помнить последний вектор стика и восстанавливать его. UI это уже обходит (шлёт каждый кадр).
