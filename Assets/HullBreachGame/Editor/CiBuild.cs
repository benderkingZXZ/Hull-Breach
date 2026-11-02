using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using HullBreach.Game;

namespace HullBreach.GameEditor
{
    /// <summary>
    /// Шаг 2 сборки в CI: создаёт сцену Main (один объект GameBootstrap), настраивает Player Settings для Android
    /// и собирает APK в build/Android/HullBreach.apk.
    /// Запуск: -executeMethod HullBreach.GameEditor.CiBuild.BuildAndroid [-hbVersion 1.0.5] [-hbVersionCode 5] [-includeArmv7]
    /// </summary>
    public static class CiBuild
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string ApkPath = "build/Android/HullBreach.apk";
        private const string ApplicationId = "com.hullbreach.game";

        public static void BuildAndroid()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string version = GetArg(args, "-hbVersion", "1.0.0");
                int versionCode = GetIntArg(args, "-hbVersionCode", 1);
                bool includeArmv7 = HasFlag(args, "-includeArmv7");

                // Запасной путь: если шаг подготовки не отработал, пробуем импортировать TMP здесь
                CiPrepare.ImportTmpEssentials();

                EnsureMainScene();
                ConfigureAndroid(version, versionCode, includeArmv7);

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = new[] { ScenePath };
                options.locationPathName = ApkPath;
                options.target = BuildTarget.Android;
                options.targetGroup = BuildTargetGroup.Android;
                options.options = BuildOptions.None;

                Debug.Log("[HullBreach CI] Сборка APK: версия " + version + " (" + versionCode + "), ARMv7=" + includeArmv7);
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                Debug.Log("[HullBreach CI] Результат: " + summary.result + ", размер " + summary.totalSize + " байт, ошибок " + summary.totalErrors);

                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 3);
            }
            catch (Exception e)
            {
                Debug.LogError("[HullBreach CI] Сборка не удалась: " + e);
                EditorApplication.Exit(2);
            }
        }

        /// <summary>Создаёт и сохраняет сцену Main: пустая сцена и один объект GameBootstrap (остальное строится в рантайме).</summary>
        public static void EnsureMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("HullBreach");
            go.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void ConfigureAndroid(string version, int versionCode, bool includeArmv7)
        {
            PlayerSettings.companyName = "HullBreach";
            PlayerSettings.productName = "Hull Breach";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = versionCode;

            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.targetArchitectures = includeArmv7
                ? (AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7)
                : AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

            // Горизонтальная ориентация, оба поворота
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        // ---------------------------------------------------------------- Аргументы командной строки

        private static string GetArg(string[] args, string name, string fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return fallback;
        }

        private static int GetIntArg(string[] args, string name, int fallback)
        {
            int value;
            return int.TryParse(GetArg(args, name, null), out value) ? value : fallback;
        }

        private static bool HasFlag(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name) return true;
            return false;
        }

        // ---------------------------------------------------------------- Меню для локальной работы

        [MenuItem("Hull Breach/Game/Create Main Scene")]
        private static void CreateSceneFromMenu()
        {
            EnsureMainScene();
            Debug.Log("[HullBreach] Сцена создана: " + ScenePath + ". Откройте её и нажмите Play.");
        }
    }
}
