# Интеграция фронтенда и бэкенда

## 1. Схема

```
сцена Main: один объект GameBootstrap (HullBreach.Game)
 │
 ├─ камера (CameraFollow2D) ──── слушает ShipRunManager.RoomEntered → границы комнаты
 ├─ PrototypeTemplates (неактивный родитель): шаблоны снарядов, 5 врагов, 12 комнат
 ├─ Player (Health, PlayerController, PlayerCombat, PlayerVisual)
 ├─ GameCore: SaveSystem, ProgressionSystem, SectorMapSystem, ModuleSystem,
 │            EnemySpawner, RoomGenerator, ShipRunManager, GameStateManager
 └─ HullBreachUI (HullBreachUIBootstrap) ── BackendBinder находит GameCore и Player
        └─ адаптеры: GameFlowAdapter, ProgressionAdapter, ModuleAdapter, CombatAdapter
```

Контракты не менялись: интерфейс использует тот же API бэкенда v1.0 (`docs/BACKEND_API.md`), который описан в `docs/FRONTEND_API.md`.

## 2. Что изменено в исходниках

Все изменения **добавляющие** (существующие сигнатуры не тронуты):

| Файл | Добавлено | Зачем |
|---|---|---|
| бэкенд: `Health`, `PlayerCombat`, `EnemyController`, `EnemySpawner`, `RoomController`, `RoomExitTrigger`, `RoomGenerator`, `ShipRunManager`, `ModuleSystem`, `ProgressionSystem`, `GameStateManager` | публичный метод `Configure(...)` | задавать ссылки и параметры из кода вместо инспектора (в сборке без префабов и сцены) |
| бэкенд: `GameStateManager` | подписки вынесены в `SubscribeSystems(bool)` | `Configure` безопасно переустанавливает подписки |
| фронтенд: `HullBreachUIBootstrap` | `Configure(UITheme, Camera)` | передать тему со шрифтом и камеру мира, когда UI создаётся кодом |
| бэкенд: `docs/BACKEND_API.md` | строка v1.1 в журнале | фиксация изменения контракта (аддитивное) |

Тесты бэкенда перенесены в `Assets/HullBreach/Tests~`, чтобы они не участвовали в CI-компиляции.

## 3. Соответствие «экран UI → система бэкенда»

| Экран / часть UI | Источник данных и команды |
|---|---|
| Главное меню, выбор робота | `GameStateManager.SelectRobot / StartSectorRun / OpenUpgrades`, `ProgressionSystem` |
| Карта сектора | `GetShipOptions()`, `SelectShip(id)` |
| Выбор точки входа | `GetEntryPoints()`, `ConfirmEntryPoint(entry)` |
| HUD боя | `PlayerController.Health`, `PlayerCombat` (КД навыка, цель), `ShipRunManager` (комнаты, враги, ресурсы), `ModuleSystem` |
| Стик / кнопки | `PlayerController.SetMoveInput`, `PlayerCombat.SetAttackHeld / TryUseSkill` |
| Награда | `GameStateManager.CurrentReward`, `ChooseModule(index)` |
| Смерть / победа | `RunFailed`, `SectorVictory`, `LastRunSummary` |
| Прокачка | `ProgressionSystem.TryPurchaseUpgrade` |

## 4. Мир из кода (прототип) и переход на настоящие префабы

`GameBootstrap` — временное решение, чтобы получить играбельный APK без сцены и арта.
Графика — одноцветные спрайты, рисуемые кодом (`RuntimeSprites`): игрок и враги — круги/квадраты, комнаты — сетка и блоки-стены.

Переход на свои ассеты:
1. Сделайте префабы и сцену по `docs/BACKEND_README.md` (игрок, враги, снаряды, комнаты) и добавьте в сцену `GameCore` с компонентами бэкенда и объект с `HullBreachUIBootstrap`.
2. Уберите `GameBootstrap` из сцены.
3. В `CiBuild.EnsureMainScene` (`Assets/HullBreachGame/Editor/CiBuild.cs`) замените создание сцены на использование вашей: уберите вызов `EnsureMainScene()` из `BuildAndroid` и поставьте свой путь в `ScenePath`; уберите `/Assets/Scenes/` из `.gitignore`.
4. Для кириллицы положите в сцену `UITheme` со своим шрифтом либо оставьте `FontFactory`.

Геймплейные параметры (баланс) — в `Assets/HullBreach/Scripts/Data/*`.

## 5. Управление
Касание: виртуальный стик слева, справа — кнопки атаки и навыка (фронтенд). Кнопка навыка запускает навык робота и все готовые активные модули (решение бэкенда, см. `BACKEND_API.md`, п. 6). Системная кнопка «Назад» = Esc (пауза/назад).

## 6. Ограничения интеграции
См. `docs/KNOWN_ISSUES.md`.
