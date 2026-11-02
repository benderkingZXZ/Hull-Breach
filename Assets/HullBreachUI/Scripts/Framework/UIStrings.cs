using HullBreach.Core;

namespace HullBreach.UI.Framework
{
    /// <summary>Все тексты интерфейса в одном месте (задел под локализацию). Тексты данных (названия модулей и т.п.) приходят из бэкенда.</summary>
    public static class UIStrings
    {
        /// <summary>Количество обычных кораблей в секторе (SectorMapSystem.RegularShipCount = 4). Используется только для подписей «N из 4».</summary>
        public const int SectorShipCount = 4;

        public const string GameTitle = "HULL BREACH";
        public const string GameSubtitle = "ПРОБОИНА В КОРПУСЕ";
        public const string Tagline = "Мёртвый сектор захвачен ИИ «Нексус». Пробей борт и зачисти отсеки.";

        public const string Start = "НАЧАТЬ АБОРДАЖ";
        public const string Upgrades = "ПРОКАЧКА РОБОТА";
        public const string Settings = "НАСТРОЙКИ";
        public const string Quit = "ВЫХОД";
        public const string Back = "НАЗАД";
        public const string ToMenu = "В МЕНЮ";
        public const string Cancel = "Отмена";
        public const string Confirm = "Подтвердить";
        public const string Resume = "ПРОДОЛЖИТЬ";
        public const string AbandonRun = "ПОКИНУТЬ ЗАБЕГ";

        public static string Rarity(ModuleRarity r)
        {
            switch (r)
            {
                case ModuleRarity.Rare: return "РЕДКИЙ";
                case ModuleRarity.Epic: return "ЭПИЧЕСКИЙ";
                default: return "ОБЫЧНЫЙ";
            }
        }

        public static string Risk(ShipRisk r)
        {
            switch (r)
            {
                case ShipRisk.Safe: return "БЕЗОПАСНЫЙ";
                case ShipRisk.Risky: return "РИСКОВАННЫЙ";
                case ShipRisk.Boss: return "БОСС";
                default: return "СТАНДАРТНЫЙ";
            }
        }

        public static string RiskHint(ShipRisk r)
        {
            switch (r)
            {
                case ShipRisk.Safe: return "Слабее охрана и скромнее добыча. Надёжный выбор.";
                case ShipRisk.Risky: return "Сильная охрана, но и добыча богаче.";
                case ShipRisk.Boss: return "Флагман ИИ «Нексус». Отступать некуда.";
                default: return "Сбалансированная цель.";
            }
        }

        public static string RobotRole(RobotType r)
        {
            return r == RobotType.Techno ? "Дальний бой. Снаряды с автонаведением." : "Ближний бой. Дуговой удар клинком.";
        }

        public static string RobotSkillText(RobotType r)
        {
            return r == RobotType.Techno ? "Кольцо снарядов во все стороны" : "Удар по площади вокруг робота";
        }

        public static string UpgradeName(UpgradeType t)
        {
            return t == UpgradeType.Health ? "Здоровье" : "Урон";
        }

        public static string EntryShortName(EntryPointType e)
        {
            switch (e)
            {
                case EntryPointType.CargoHold: return "Грузовой трюм";
                case EntryPointType.Bridge: return "Мостик";
                default: return "Машинное отделение";
            }
        }
    }
}
