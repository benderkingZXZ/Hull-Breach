using System;
using UnityEngine;

namespace HullBreach.UI.Framework
{
    /// <summary>
    /// Настройки интерфейса (не игровой прогресс): громкость, вибрация, числа урона, поведение стика.
    /// Хранятся в PlayerPrefs; игровой прогресс UI не сохраняет — это зона SaveSystem бэкенда.
    /// </summary>
    public static class UISettings
    {
        private const string KeyVolume = "hb_ui_volume";
        private const string KeyVibration = "hb_ui_vibration";
        private const string KeyDamageNumbers = "hb_ui_damage_numbers";
        private const string KeyFloating = "hb_ui_floating_stick";
        private const string KeyScale = "hb_ui_control_scale";
        private const string KeyOpacity = "hb_ui_control_opacity";

        public static float MasterVolume { get; private set; } = 1f;
        public static bool Vibration { get; private set; } = true;
        public static bool DamageNumbers { get; private set; } = true;
        public static bool FloatingJoystick { get; private set; }
        public static float ControlScale { get; private set; } = 1f;
        public static float ControlOpacity { get; private set; } = 0.85f;

        public static event Action Changed;

        public static void Load()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolume, 1f));
            Vibration = PlayerPrefs.GetInt(KeyVibration, 1) != 0;
            DamageNumbers = PlayerPrefs.GetInt(KeyDamageNumbers, 1) != 0;
            FloatingJoystick = PlayerPrefs.GetInt(KeyFloating, 0) != 0;
            ControlScale = Mathf.Clamp(PlayerPrefs.GetFloat(KeyScale, 1f), 0.8f, 1.3f);
            ControlOpacity = Mathf.Clamp(PlayerPrefs.GetFloat(KeyOpacity, 0.85f), 0.4f, 1f);
            ApplyAudio();
            Raise();
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyVolume, MasterVolume);
            ApplyAudio();
            Raise();
        }

        public static void SetVibration(bool value)
        {
            Vibration = value;
            PlayerPrefs.SetInt(KeyVibration, value ? 1 : 0);
            Raise();
        }

        public static void SetDamageNumbers(bool value)
        {
            DamageNumbers = value;
            PlayerPrefs.SetInt(KeyDamageNumbers, value ? 1 : 0);
            Raise();
        }

        public static void SetFloatingJoystick(bool value)
        {
            FloatingJoystick = value;
            PlayerPrefs.SetInt(KeyFloating, value ? 1 : 0);
            Raise();
        }

        public static void SetControlScale(float value)
        {
            ControlScale = Mathf.Clamp(value, 0.8f, 1.3f);
            PlayerPrefs.SetFloat(KeyScale, ControlScale);
            Raise();
        }

        public static void SetControlOpacity(float value)
        {
            ControlOpacity = Mathf.Clamp(value, 0.4f, 1f);
            PlayerPrefs.SetFloat(KeyOpacity, ControlOpacity);
            Raise();
        }

        /// <summary>Записать настройки на диск (при закрытии окна настроек и при сворачивании приложения).</summary>
        public static void Flush()
        {
            PlayerPrefs.Save();
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = MasterVolume;
        }

        private static void Raise()
        {
            Action handler = Changed;
            if (handler != null) handler();
        }
    }
}
