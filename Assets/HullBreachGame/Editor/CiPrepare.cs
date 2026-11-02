using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace HullBreach.GameEditor
{
    /// <summary>
    /// Шаг 1 сборки в CI (отдельный запуск Unity): импорт TextMeshPro Essential Resources.
    /// Без них TMP не работает в сборке (нет TMP Settings и шейдеров). Отдельный запуск нужен, чтобы импорт
    /// гарантированно завершился до сборки. Запуск: -executeMethod HullBreach.GameEditor.CiPrepare.Run
    /// </summary>
    public static class CiPrepare
    {
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        public static void Run()
        {
            try
            {
                Debug.Log("[HullBreach CI] Подготовка проекта: импорт TMP Essential Resources.");
                ImportTmpEssentials();

                // game-ci ожидает папку результата, даже если на этом шаге сборки нет
                Directory.CreateDirectory("build/Android");
                File.WriteAllText("build/Android/prepare.txt", "prepare step ok " + DateTime.UtcNow.ToString("o"));

                AssetDatabase.SaveAssets();
                Debug.Log("[HullBreach CI] Подготовка завершена.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[HullBreach CI] Подготовка не удалась: " + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Импортирует TMP Essentials, если их ещё нет. Используется и из меню, и из CiBuild как запасной путь.</summary>
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null)
            {
                Debug.Log("[HullBreach CI] TMP Essentials уже импортированы.");
                return;
            }

            UnityEditor.PackageManager.PackageInfo info =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (info == null) throw new Exception("Не найден пакет TextMeshPro (проверьте Packages/manifest.json).");

            string package = Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(package)) throw new Exception("Нет файла " + package);

            Debug.Log("[HullBreach CI] Импорт " + package);
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) == null)
                throw new Exception("После импорта TMP Settings не найден: " + TmpSettingsPath);
        }

        [MenuItem("Hull Breach/Game/Import TMP Essentials")]
        private static void ImportFromMenu()
        {
            ImportTmpEssentials();
        }
    }
}
