# Инструкции интеграции (для третьего разработчика)

## 1. Подключение
1. Скопировать `Assets/HullBreach` в общий проект. Не менять пространства имён `HullBreach.*`.
2. Сборка `HullBreach.Gameplay` (autoReferenced = true) автоматически видна из Assembly-CSharp. Если UI в своей `.asmdef` — добавить ссылку на `HullBreach.Gameplay`.
3. Собрать сцену по README (GameCore + Player + префабы). Логика не требует Canvas.

## 2. Зависимости
| Зависимость | Нужна? |
|---|---|
| UnityEngine core, Physics 2D, JsonUtility | да (встроенные) |
| Tilemap | нет (только для шаблонов комнат; на код не влияет) |
| TextMeshPro / UI / Input System / DOTween / NavMesh | нет |
| Сторонние ассеты | нет |
Зависимостей от кода UI нет (проверка: в `Scripts` нет `using UnityEngine.UI`, TMPro).

## 3. Подключение интерфейса

```csharp
void OnEnable()
{
    gsm.StateChanged += OnState;              // переключение экранов
    gsm.RewardPrepared += ShowRewardScreen;   // 3 модуля: reward.ModuleOffers
    shipRun.RoomEntered += (i, total, room) => hud.SetRoom(i + 1, total);
    shipRun.ResourcesEarned += (amount, pos) => hud.Popup(amount, pos);
    player.Health.HealthChanged += (cur, max) => hud.SetHp(cur, max);
}
void OnDisable() { /* симметричная отписка */ }

// Ввод
joystick.OnChanged += v => player.SetMoveInput(v);
attackButton.OnDown += () => combat.SetAttackHeld(true);
attackButton.OnUp   += () => combat.SetAttackHeld(false);
skillButton.OnClick += () => combat.TryUseSkill();
```
> Игрок вне боя может быть неактивен: подписывайтесь на `player.Health` ссылкой, полученной при старте (через `PlayerController.Health`, работает и на неактивном объекте), а не через `Find`.

Кнопки меню: см. API.md разд. 4.3 и последовательность в разд. 11.

## 4. Отладочный драйвер без UI (временный скрипт)

```csharp
IEnumerator Start()
{
    yield return null;
    gsm.StartSectorRun();
    gsm.SelectShip(gsm.GetShipOptions()[0].Id);
    gsm.ConfirmEntryPoint(EntryPointType.EngineRoom);
}
```

## 5. Порядок событий при захвате корабля
`ShipRunManager.RoomCleared(последняя)` → (пауза `shipCompleteDelay`) → `ShipRunManager.ShipCompleted` → ресурсы начислены → `GameStateManager.StateChanged(Combat→Reward)` → `RewardPrepared`.
При гибели: `Health.Died` → `ShipRunManager.ShipFailed` → (пауза `deathScreenDelay`) → `StateChanged(Combat→Death)` → `RunFailed`.

## 6. Чек-лист интеграции
- [ ] Проект компилируется без ошибок, EditMode-тесты зелёные.
- [ ] Physics 2D: гравитация (0,0). Слои/теги не требуются.
- [ ] Все ссылки в инспекторе назначены (README → «Ссылки»), `GameStateManager` не пишет ошибок в Start.
- [ ] Камера следует за игроком и ограничена `CurrentRoom.WorldBounds`.
- [ ] UI подписывается/отписывается в OnEnable/OnDisable.
- [ ] Первый корабль проходит от входа до `Reward`; смерть ведёт в `Death`; сохранение переживает перезапуск.
- [ ] Изменения публичного API согласованы и записаны в журнал API.md.

## 7. Что менять нельзя / можно
* Нельзя без записи в журнал: имена/сигнатуры из API.md, порядок значений `EnemyType`, `ModuleRarity`.
* Можно свободно: числа баланса, тексты, префабы, шаблоны комнат, визуальные хуки (`UnityEvent` в `RoomController`).
