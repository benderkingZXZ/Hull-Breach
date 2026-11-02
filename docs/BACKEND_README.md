# Hull Breach — Backend / Gameplay Systems (Unity 2D, C#)

Комплект игровой логики для мобильного 2D roguelike action (Android, горизонтальная ориентация).
Интерфейс делает другой разработчик: здесь **нет UI**, взаимодействие — только через публичные методы и события (см. **API.md**, контракт v1.0).

## ⚠️ Статус проверки (честно)

* Код **не компилировался и не запускался**: в среде разработки нет Unity и компилятора C#.
* Выполнено только: ручное ревью на ошибки компиляции и логические противоречия + автоматическая проверка баланса скобок во всех `.cs`.
* Тесты (`Tests/Editor`) написаны, но **не прогонялись**.
* Возможны мелкие ошибки компиляции/настройки. Первым шагом интеграции — открыть проект и прогнать компиляцию и EditMode-тесты (Window → General → Test Runner).

## Требования

* Unity **2022.3 LTS** или новее (код не использует API новее 2021; совместим с Unity 6, возможны только предупреждения).
* Пакеты: только встроенные модули Physics 2D и Core. Сторонних зависимостей нет (подробнее — в INTEGRATION.md).
* Active Input Handling: любой. Клавиатура для отладки работает только при Legacy/Both (`ENABLE_LEGACY_INPUT_MANAGER`), иначе блок компилируется «вхолостую».

## Что внутри

```
HullBreach_Backend/
  README.md  API.md  INTEGRATION.md  KNOWN_ISSUES.md  FILE_LIST.md
  Assets/HullBreach/  (скопировать в Assets/ общего проекта)
```
Структура папок, пространства имён и зависимости — в API.md, раздел 2. Список файлов — FILE_LIST.md.

## Быстрый запуск (отладочная сцена)

1. Скопировать `Assets/HullBreach` в проект. Подождать компиляции (сборка `HullBreach.Gameplay`).
2. **Project Settings → Physics 2D**: Gravity = (0, 0) (рекомендуется; у Rigidbody2D гравитация и так обнуляется скриптами).
3. Создать сцену `Gameplay` и объект **`GameCore`** с компонентами: `SaveSystem`, `ProgressionSystem`, `ModuleSystem`, `SectorMapSystem`, `ShipRunManager`, `EnemySpawner`, `RoomGenerator`, `GameStateManager`.
4. Создать префабы по разделам ниже и связать ссылки в инспекторе (таблица «Ссылки»).
5. Для проверки без UI: в `GameStateManager` вызвать из временного скрипта по порядку `StartSectorRun() → SelectShip(id) → ConfirmEntryPoint(...)` (пример — INTEGRATION.md).

### Префаб игрока `Player`
* `SpriteRenderer` (любой спрайт), `Rigidbody2D` (Dynamic, Gravity Scale 0, Freeze Rotation Z — скрипты выставят сами), `CircleCollider2D` (не триггер).
* Компоненты: `Health` (**Faction = Player**, Max Health любой, *Invulnerability After Hit* ≈ 0.3), `PlayerController`, `PlayerCombat`.
* `PlayerCombat`: `Projectile Prefab` — префаб снаряда (необязателен: без него создаётся невидимый), `Attack Origin` — дочерний пустой Transform (необязательно).
* Объект находится в сцене (не в корне комнат). Допускается неактивным.

### Префабы врагов (5 штук: Drone, Gunner, Heavy, Exploder, MiniBoss)
* `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D` (не триггер), `Health` (**Faction = Enemy**), `EnemyController`.
* В `EnemyController` выбрать `Enemy Type`. Для Gunner и MiniBoss назначить `Projectile Prefab` (опционально).
* Характеристики по умолчанию — в `EnemyDefaults.cs`; точечно переопределяются галочкой *Override Stats*.
* Назначить префабы в `EnemySpawner → Enemy Prefabs` (по одному на каждый тип, включая MiniBoss).

### Префаб снаряда `Projectile` (необязательный)
* `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `Projectile`. Триггер и Kinematic выставляются скриптом.

### Префабы комнат (10–12 шаблонов)
Корень префаба — `RoomController`. Дочерние объекты:
* `PlayerSpawn` (пустой Transform) → поле *Player Spawn Point*.
* `EnemySpawn_1..N` (пустые Transform, ≥ 3 рекомендуется) → *Enemy Spawn Points* (для Normal обязательны).
* `BossSpawn` → *Boss Spawn Point* (для Reactor).
* Стены: `Tilemap`/коллайдеры без триггера (+ `TilemapCollider2D`/`CompositeCollider2D`). Они же останавливают снаряды.
* Двери-преграды: объекты с коллайдерами → массив *Door Blockers* (отключаются при зачистке).
* `ExitTrigger`: объект с `BoxCollider2D` (isTrigger) + `RoomExitTrigger` → *Exit Trigger* (только Normal). Ставить в проём двери.
* Для Reactor: *Role = Reactor*, объект реактора → *Reactor Object*, анимации — через *Reactor Destroyed* (UnityEvent).
* *Bounds Size/Offset* — размер комнаты для камеры (рисуется гизмо). Размер комнаты должен быть меньше `Room Spacing` генератора (по умолчанию 60).
* Проверка: ПКМ по компоненту → **Validate Room Setup**.
* Размещение по концепции: 3 набора (машинное отделение, трюм, мостик). Минимум на набор: 3 Normal + 1 Reactor = 12 шаблонов в сумме; шаблоны можно переиспользовать между наборами.

### Ссылки в инспекторе

| Компонент | Поле | Значение |
|---|---|---|
| GameStateManager | Player / Progression / Module System / Sector Map / Ship Run | соответствующие компоненты |
| ShipRunManager | Room Generator / Enemy Spawner / Player | RoomGenerator, EnemySpawner, Player |
| RoomGenerator | Template Sets (3 элемента) | `Entry Point` + Normal Rooms + Reactor Rooms; Rooms Root — пустой Transform в сцене (позиция вне камеры, напр. (0, 500)) |
| ModuleSystem | Player, Player Combat | компоненты на Player |
| ProgressionSystem | Save System | SaveSystem (если пусто — добавится автоматически) |
| EnemySpawner | Enemy Prefabs | 5 записей |

### Камера
Игровая логика камеру не двигает. Следите за `PlayerController` и ограничивайте движение по `ShipRunManager.CurrentRoom.WorldBounds` (событие `RoomEntered`).

## Как проверить основные сценарии вручную

1. Три точки входа дают 3 / 5 / 4 комнаты (`ShipRunManager.TotalRooms`).
2. Двери открываются только после гибели всех врагов; выход активен только тогда.
3. Последняя комната: мини-босс + реактор; после победы — `Reward` (или `Victory` для «Нексус-Прайм»).
4. Смерть → `Death`; ресурсы и улучшения сохранены; `StartSectorRun()` начинает с первого корабля без модулей.
5. Перезапуск игры: ресурсы/уровни/выбранный робот загружены из `persistentDataPath/hullbreach_save.json`.

## Правила баланса (все числа настраиваются в `Data/*` и инспекторе)
Базовые значения роботов — `RobotDefinitions.cs`, врагов — `EnemyDefaults.cs`, точек входа — `EntryPointCatalog.cs`, модулей — `ModuleCatalog.cs`, цены улучшений — поля `ProgressionSystem`.
