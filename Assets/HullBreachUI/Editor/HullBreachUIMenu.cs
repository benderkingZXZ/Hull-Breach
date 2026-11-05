using HullBreach.UI.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HullBreach.UI.EditorTools
{
    public static class HullBreachUIMenu
    {
        [MenuItem("Hull Breach/UI/Add UI Bootstrap To Scene")]
        private static void AddBootstrap()
        {
            if (Object.FindObjectOfType<HullBreachUIBootstrap>() != null)
            {
                EditorUtility.DisplayDialog("Hull Breach UI", "В сцене уже есть HullBreachUIBootstrap.", "OK");
                return;
            }
            GameObject go = new GameObject("HullBreachUI");
            go.AddComponent<HullBreachUIBootstrap>();
            Undo.RegisterCreatedObjectUndo(go, "Add Hull Breach UI Bootstrap");
            Selection.activeGameObject = go;
        }

        [MenuItem("Hull Breach/UI/Create UI Sandbox Scene (Mock)")]
        private static void CreateSandbox()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string path = EditorUtility.SaveFilePanelInProject("Сохранить сцену-песочницу", "HullBreachUISandbox", "unity",
                "Сцена с камерой и интерфейсом, работающим на заглушке MockGameHost (без бэкенда).");
            if (string.IsNullOrEmpty(path)) return;

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject go = new GameObject("HullBreachUI");
            HullBreachUIBootstrap bootstrap = go.AddComponent<HullBreachUIBootstrap>();

            SerializedObject so = new SerializedObject(bootstrap);
            SerializedProperty mode = so.FindProperty("serviceMode");
            if (mode != null)
            {
                mode.enumValueIndex = (int)HullBreachUIBootstrap.ServiceMode.Mock;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[HullBreachUI] Создана сцена-песочница: " + path + ". Нажмите Play.");
        }
    }
}
