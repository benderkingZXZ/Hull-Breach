# Hull Breach — Unity-проект (бэкенд + фронтенд + автосборка APK)

2D roguelike action для Android (горизонтальная ориентация). В репозитории объединены:

| Папка | Что это |
|---|---|
| `Assets/HullBreach` | игровая логика (бэкенд): игрок, враги, комнаты, модули, прогрессия, сохранение |
| `Assets/HullBreachUI` | интерфейс (фронтенд): меню, карта сектора, HUD, награда, прокачка |
| `Assets/HullBreachGame` | **связка**: строит мир из кода (игрок, враги, 12 комнат, камера), подключает UI к бэкенду, скрипты сборки CI |
| `.github/workflows/build-android.yml` | **автосборка APK** на GitHub Actions |
| `docs/` | API бэкенда и фронтенда, интеграция, известные проблемы |

## ⚠️ Честный статус

* Код **ни разу не компилировался и не запускался** — в среде, где он написан, нет Unity. Проверено только чтением кода и скриптами (баланс скобок, наличие пространств имён, совпадение сигнатур вызовов).
* **Первый запуск GitHub Actions — это первая компиляция проекта.** Возможны ошибки компиляции или настройки; в таком случае пришлите лог упавшего шага — я исправлю. Типовые причины и решения — раздел «Если сборка упала».
* APK **не приложен**: его собирает GitHub Actions, и без вашего Unity-аккаунта это невозможно сделать (нужна лицензия, см. ниже).

## Быстрый старт: получить APK за 6 шагов

### 1. Создайте пустой репозиторий на GitHub
github.com → **New repository** → имя, например, `hull-breach` → **без** README/gitignore → Create.

### 2. Загрузите проект в репозиторий (выберите способ)

**Способ А — GitHub Desktop или командная строка (если умеете).** Распакуйте архив и в папке проекта (где лежат `Assets`, `Packages`, `.github`) выполните:
```bash
git init
git add .
git commit -m "Hull Breach: backend + frontend + CI"
git branch -M main
git remote add origin https://github.com/<ВАШ_ЛОГИН>/hull-breach.git
git push -u origin main
```

**Способ Б — только браузер (если сайт GitHub не принимает папки: «слишком много файлов», не грузится `.github`).**
Сайт GitHub принимает максимум 100 файлов за одну загрузку, а в проекте 122 файла, поэтому проект грузится **одним zip-файлом**, а workflow создаётся прямо на сайте:
1. Репозиторий → **Add file → Upload files** → перетащите **один файл `HullBreach_Game.zip`, не распаковывая** → **Commit changes**.
2. **Add file → Create new file** → в поле имени введите `.github/workflows/build-android.yml` (каждая `/` создаёт папку) → вставьте всё содержимое файла `build-android.yml` из архива с ответом → **Commit changes**. Это действие запускает первую сборку.
3. Если первый запуск упал из-за отсутствия секретов — добавьте их (шаг 4) и откройте **Actions → упавший запуск → Re-run all jobs**.
4. Обновления проекта: **Add file → Upload files** → новый `HullBreach_Game.zip` с тем же именем заменит старый, сборка запустится сама.

Workflow сам определяет режим: есть `HullBreach_Game.zip` в корне — распаковывает его в `project/`, иначе ждёт проект Unity прямо в корне репозитория.

### 3. Получите лицензию Unity (бесплатный Personal подходит)
Сборка идёт в Docker-образе GameCI и требует активации Unity. Нужны три секрета:

| Секрет | Что вписать |
|---|---|
| `UNITY_EMAIL` | почта вашего Unity ID |
| `UNITY_PASSWORD` | пароль Unity ID |
| `UNITY_LICENSE` | **полное содержимое** файла лицензии `Unity_lic.ulf` |

Как получить `.ulf`: установите Unity Hub, войдите в аккаунт, активируйте бесплатную Personal-лицензию. Файл лежит
(по документации GameCI): Windows `C:\ProgramData\Unity\Unity_lic.ulf`, macOS `/Library/Application Support/Unity/Unity_lic.ulf`,
Linux `~/.local/share/unity3d/Unity/Unity_lic.ulf`. Актуальные подробности (Unity иногда меняет схему активации): https://game.ci/docs/github/activation

### 4. Добавьте секреты в репозиторий
Репозиторий → **Settings → Secrets and variables → Actions → New repository secret** — создайте три секрета из таблицы выше.

### 5. Запустите сборку
Сборка стартует сама при каждом `git push` в `main`. Вручную: вкладка **Actions → Build Android APK → Run workflow**.
Первая сборка долгая (скачивание образа ~10 ГБ, импорт, IL2CPP): ожидайте 30–60 минут; следующие быстрее за счёт кэша.
Если секретов нет, сборка сразу остановится с понятным сообщением.

### 6. Скачайте APK
* **Releases** (справа на главной странице репозитория) → последний релиз `Hull Breach APK #N` → `HullBreach.apk` — прямая ссылка, можно открыть с телефона;
* либо **Actions → нужный запуск → Artifacts → HullBreach-apk-N** (zip с APK).

Установка на телефон: разрешите установку из неизвестных источников → откройте APK. Нужен Android 7.0+ (ARM64). APK подписан отладочным ключом — для личного тестирования достаточно, для Google Play потребуется свой keystore и AAB.

## Что делает workflow

1. проверка секретов;
2. `actions/checkout`, поиск проекта (распаковка `HullBreach_Game.zip`, если он есть), освобождение места на диске, кэш папки `Library`;
3. **Prepare** (`CiPrepare.Run`): отдельный запуск Unity — импорт TextMeshPro Essentials (без них не работает текст);
4. **Build** (`CiBuild.BuildAndroid`): создаёт сцену `Main` (один объект `GameBootstrap`), настраивает Android (IL2CPP, ARM64, minSdk 24, горизонталь), собирает `build/Android/HullBreach.apk`;
5. загрузка APK артефактом и публикация GitHub Release `build-N`.

Версия Unity задаётся файлом `ProjectSettings/ProjectVersion.txt` (сейчас `2022.3.62f2`). 32-битную сборку (ARMv7) можно включить галочкой при ручном запуске.

## Как всё соединено (кратко)

`GameBootstrap` (единственный объект в сцене) при старте:
1. настраивает физику и камеру (`CameraFollow2D`, границы берутся из `RoomEntered` бэкенда);
2. создаёт из кода шаблоны: снаряды, игрока, 5 врагов, 12 комнат (3 набора × 3 обычные + реактор);
3. создаёт объект `GameCore` с системами бэкенда и настраивает их через новые методы `Configure(...)`;
4. создаёт `HullBreachUIBootstrap` — фронтенд сам находит бэкенд (`BackendBinder`) и работает через адаптеры; кириллический шрифт строится из `Resources/Fonts`.

Подробности, таблица соответствий и инструкция по замене прототипной графики настоящими префабами: [`docs/GAME_INTEGRATION.md`](docs/GAME_INTEGRATION.md).

## Если сборка упала

| Симптом в логе | Причина | Что делать |
|---|---|---|
| `Secret is missing` | не заданы секреты | шаг 4, затем Re-run all jobs |
| `Project not found` / `Bad zip` | в репозитории нет проекта или zip повреждён | загрузите `HullBreach_Game.zip` в корень репозитория (шаг 2, способ Б) |
| ошибка лицензии / `activation` / `Machine bindings` | `.ulf` устарел, неверные email/пароль | заново получить `.ulf`, проверить секреты, см. документацию GameCI |
| `manifest unknown` / `not found` на образе `unityci/editor:...` | нет Docker-образа для этой версии Unity | в `ProjectSettings/ProjectVersion.txt` поставить другую 2022.3.x из https://game.ci/docs/docker/versions |
| `error CS....` | ошибка компиляции (код не проверялся в Unity) | пришлите строки с `error CS` — исправлю |
| `TMP Settings не найден` | не импортировались TMP Essentials | перезапустить; если повторяется — пришлите лог шага Prepare |
| Gradle / Android SDK / NDK | проблема образа Android | пришлите последние ~100 строк лога |
| APK ставится, но чёрный экран / вылет | рантайм-ошибка | `adb logcat -s Unity` и пришлите вывод |
| вместо русских букв квадраты | не создался TMP-шрифт | пришлите `adb logcat` (ищите `[HullBreach]`) |

## Локальная работа в Unity (по желанию)

1. Откройте проект в Unity 2022.3 (при первом открытии Unity создаст `.meta` — закоммитьте их).
2. Меню **Hull Breach → Game → Import TMP Essentials**, затем **Hull Breach → Game → Create Main Scene**.
3. Откройте `Assets/Scenes/Main.unity` и нажмите Play. Клавиатура (при Legacy Input): WASD — движение, Space — атака, E — навык, Esc — пауза/назад.
4. Автотесты логики бэкенда лежат в `Assets/HullBreach/Tests~` (Unity игнорирует папки с `~`): переименуйте в `Tests` и установите пакет Test Framework.

## Арт и звук

Персонажи, мобы, снаряды, музыка и звуки лежат в `Assets/HullBreachGame/Resources` и подключаются автоматически. Что где и как подгоняется размер, описано в `docs/ART_AND_AUDIO.md`.

## Лицензии
Шрифты Liberation Sans (`Assets/HullBreachGame/Resources/Fonts`) — SIL Open Font License 1.1, текст — `Assets/HullBreachGame/FONTS_LICENSE.txt`.
