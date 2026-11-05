using HullBreach.Core;
using UnityEngine;

namespace HullBreach.UI.Theme
{
    public static class UIColors
    {
        public static Color Hex(string hex)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            return c;
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static readonly Color Void = Hex("#050914");
        public static readonly Color Hull = Hex("#0D1524");
        public static readonly Color HullLight = Hex("#18253D");
        public static readonly Color Steel = Hex("#3A5378");
        public static readonly Color Cyan = Hex("#2BD9FF");
        public static readonly Color CyanDim = Hex("#1B8EAD");
        public static readonly Color Amber = Hex("#FFB938");
        public static readonly Color Red = Hex("#FF4560");
        public static readonly Color RedDark = Hex("#3A0F1A");
        public static readonly Color Green = Hex("#46F0A0");
        public static readonly Color Text = Hex("#E8F2FF");
        public static readonly Color TextDim = Hex("#8DA2BF");
        public static readonly Color TextOnAccent = Hex("#04101A");
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);

        public static readonly Color RarityCommon = Hex("#BCC8DA");
        public static readonly Color RarityRare = Hex("#3FA8FF");
        public static readonly Color RarityEpic = Hex("#C45CFF");

        public static readonly Color RiskSafe = Hex("#46F0A0");
        public static readonly Color RiskStandard = Hex("#FFC53D");
        public static readonly Color RiskRisky = Hex("#FF4560");
        public static readonly Color RiskBoss = Hex("#D64DFF");

        public static readonly Color EntryEngine = Hex("#FF8A3D");
        public static readonly Color EntryCargo = Hex("#FFC53D");
        public static readonly Color EntryBridge = Hex("#7C8CFF");

        public static Color Rarity(ModuleRarity r)
        {
            switch (r)
            {
                case ModuleRarity.Rare: return RarityRare;
                case ModuleRarity.Epic: return RarityEpic;
                default: return RarityCommon;
            }
        }

        public static Color Risk(ShipRisk r)
        {
            switch (r)
            {
                case ShipRisk.Safe: return RiskSafe;
                case ShipRisk.Risky: return RiskRisky;
                case ShipRisk.Boss: return RiskBoss;
                default: return RiskStandard;
            }
        }

        public static Color Entry(EntryPointType e)
        {
            switch (e)
            {
                case EntryPointType.CargoHold: return EntryCargo;
                case EntryPointType.Bridge: return EntryBridge;
                default: return EntryEngine;
            }
        }

        public static Color HealthColor(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (ratio > 0.5f) return Color.Lerp(Amber, Green, (ratio - 0.5f) * 2f);
            return Color.Lerp(Red, Amber, ratio * 2f);
        }
    }
}
