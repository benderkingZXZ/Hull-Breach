using System;
using System.IO;
using UnityEngine;

namespace HullBreach.Persistence
{
    /// <summary>
    /// Чтение/запись постоянного прогресса в JSON (Application.persistentDataPath).
    /// Запись: временный файл → копия предыдущей версии в .bak → замена основного файла.
    /// При повреждении основного файла читается .bak; если и он повреждён — возвращается новый SaveData.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [SerializeField] private string fileName = "hullbreach_save.json";

        public event Action<string> SaveFailed;

        public string SavePath { get { return Path.Combine(Application.persistentDataPath, fileName); } }
        private string BackupPath { get { return SavePath + ".bak"; } }
        private string TempPath { get { return SavePath + ".tmp"; } }

        public bool HasSave() { return File.Exists(SavePath); }

        public SaveData Load()
        {
            SaveData data = TryRead(SavePath);
            if (data == null) data = TryRead(BackupPath);
            if (data == null) return new SaveData();

            // Место для миграций: if (data.Version < 2) { ... }
            data.Version = SaveData.CurrentVersion;
            data.Resources = Mathf.Max(0, data.Resources);
            data.HealthLevel = Mathf.Max(0, data.HealthLevel);
            data.DamageLevel = Mathf.Max(0, data.DamageLevel);
            return data;
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[HullBreach] Не удалось прочитать сохранение " + path + ": " + e.Message);
                return null;
            }
        }

        public bool Save(SaveData data)
        {
            if (data == null) return false;
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(TempPath, json);
                if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, true);
                File.Copy(TempPath, SavePath, true);
                File.Delete(TempPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[HullBreach] Ошибка сохранения: " + e.Message);
                SaveFailed?.Invoke(e.Message);
                return false;
            }
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[HullBreach] Ошибка удаления сохранения: " + e.Message);
            }
        }
    }
}
