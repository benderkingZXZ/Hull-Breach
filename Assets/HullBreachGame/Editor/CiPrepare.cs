using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace HullBreach.GameEditor
{
    /// <summary>
    /// Шаг 1 сборки в CI (отдельный запуск Unity): импорт TextMeshPro Essential Resources.
    /// Без них TMP не работает в сборке (нет TMP Settings и шейдеров).
    ///
    /// ВАЖНО: AssetDatabase.ImportPackage(путь, false) НЕ синхронный — он ставит импорт в очередь, которая
    /// обрабатывается в цикле редактора. Поэтому Run() не завершает работу сразу, а ждёт событие
    /// importPackageCompleted и только потом вызывает EditorApplication.Exit. Для этого шаг в workflow должен
    /// запускаться с manualExit: true (GameCI тогда не передаёт -quit, иначе Unity закрылся бы сразу после Run()).
    ///
    /// Запуск: -executeMethod HullBreach.GameEditor.CiPrepare.Run
    /// </summary>
    public static class CiPrepare
    {
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const double TimeoutSeconds = 300.0;

        private static bool _ciMode;
        private static bool _finished;
        private static double _startTime;

        public static void Run()
        {
            _ciMode = true;
            try
            {
                Debug.Log("[HullBreach CI] Подготовка проекта: импорт TMP Essential Resources.");
                if (!StartImport())
                {
                    // Уже импортированы — ждать нечего
                    Complete(0);
                }
                // Иначе ждём события importPackageCompleted / importPackageFailed / таймаута (см. ниже)
            }
            catch (Exception e)
            {
                Fail("Подготовка не удалась: " + e);
            }
        }

        /// <summary>
        /// Для CiBuild: проверяет, что TMP Essentials уже на месте (их импортирует шаг Prepare).
        /// Сам не импортирует — импорт асинхронный и внутри синхронного BuildAndroid дождаться его нельзя.
        /// </summary>
        public static void ImportTmpEssentials()
        {
            if (TmpEssentialsPresent())
            {
                Debug.Log("[HullBreach CI] TMP Essentials уже импортированы.");
                return;
            }

            throw new Exception("TMP Essentials не импортированы (" + TmpSettingsPath + " отсутствует). " +
                                "Шаг Prepare (CiPrepare.Run) должен отработать до сборки; " +
                                "локально: меню Hull Breach -> Game -> Import TMP Essentials.");
        }

        [MenuItem("Hull Breach/Game/Import TMP Essentials")]
        private static void ImportFromMenu()
        {
            _ciMode = false;
            if (!StartImport())
                Debug.Log("[HullBreach] TMP Essentials уже импортированы.");
        }

        // ---------------------------------------------------------------- Импорт

        /// <summary>Запускает асинхронный импорт. Возвращает false, если импортировать нечего.</summary>
        private static bool StartImport()
        {
            if (TmpEssentialsPresent())
            {
                Debug.Log("[HullBreach CI] TMP Essentials уже импортированы.");
                return false;
            }

            string package = FindEssentialsPackage();

            _finished = false;
            _startTime = EditorApplication.timeSinceStartup;

            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageFailed += OnImportFailed;
            AssetDatabase.importPackageCancelled += OnImportCancelled;
            EditorApplication.update += Watchdog;

            Debug.Log("[HullBreach CI] Импорт " + package);
            AssetDatabase.ImportPackage(package, false);
            return true;
        }

        private static string FindEssentialsPackage()
        {
            UnityEditor.PackageManager.PackageInfo info =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (info == null) throw new Exception("Не найден пакет TextMeshPro (проверьте Packages/manifest.json).");

            string package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(package)) throw new Exception("Нет файла " + package);
            return package;
        }

        private static bool TmpEssentialsPresent()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null;
        }

        private static void OnImportCompleted(string packageName)
        {
            if (_finished) return;
            Debug.Log("[HullBreach CI] Импорт завершён: " + packageName);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!TmpEssentialsPresent())
            {
                Fail("После импорта TMP Settings не найден: " + TmpSettingsPath);
                return;
            }

            Complete(0);
        }

        private static void OnImportFailed(string packageName, string errorMessage)
        {
            if (_finished) return;
            Fail("Импорт " + packageName + " не удался: " + errorMessage);
        }

        private static void OnImportCancelled(string packageName)
        {
            if (_finished) return;
            Fail("Импорт " + packageName + " отменён.");
        }

        /// <summary>Страховка: если событие так и не пришло — не висим вечно.</summary>
        private static void Watchdog()
        {
            if (_finished) return;
            if (EditorApplication.timeSinceStartup - _startTime < TimeoutSeconds) return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (TmpEssentialsPresent())
            {
                Debug.LogWarning("[HullBreach CI] Событие importPackageCompleted не пришло, но TMP Settings на месте — продолжаем.");
                Complete(0);
            }
            else
            {
                Fail("Таймаут " + TimeoutSeconds + " с: TMP Essentials не импортировались.");
            }
        }

        // ---------------------------------------------------------------- Завершение

        private static void Complete(int exitCode)
        {
            if (_finished) return;
            _finished = true;
            Unsubscribe();

            if (!_ciMode) return;

            try
            {
                // game-ci ожидает папку результата, даже если на этом шаге сборки нет
                Directory.CreateDirectory("build/Android");
                File.WriteAllText("build/Android/prepare.txt", "prepare step ok " + DateTime.UtcNow.ToString("o"));
                AssetDatabase.SaveAssets();
                Debug.Log("[HullBreach CI] Подготовка завершена.");
            }
            catch (Exception e)
            {
                Debug.LogError("[HullBreach CI] Ошибка при завершении подготовки: " + e);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        private static void Fail(string message)
        {
            Debug.LogError("[HullBreach CI] " + message);
            if (_finished) return;
            _finished = true;
            Unsubscribe();
            if (_ciMode) EditorApplication.Exit(1);
        }

        private static void Unsubscribe()
        {
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
            EditorApplication.update -= Watchdog;
        }
    }
}
