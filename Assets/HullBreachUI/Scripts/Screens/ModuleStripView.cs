using System.Collections.Generic;
using HullBreach.Core;
using HullBreach.Data;
using HullBreach.UI.Contracts;
using HullBreach.UI.Framework;
using HullBreach.UI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HullBreach.UI.Screens
{
    public sealed class ModuleStripView
    {
        private struct Entry
        {
            public ModuleId Id;
            public bool IsActive;
            public Image Overlay;
        }

        public RectTransform Root { get; private set; }

        private readonly IModuleService modules;
        private readonly RectTransform row;
        private readonly TMP_Text emptyLabel;
        private readonly float tileSize;
        private readonly bool showCooldowns;
        private readonly List<Entry> entries = new List<Entry>();

        public ModuleStripView(Transform parent, string name, IModuleService modules, float tileSize, bool showCooldowns, string emptyText)
        {
            this.modules = modules;
            this.tileSize = tileSize;
            this.showCooldowns = showCooldowns;

            Root = UIFactory.Rect(name, parent);

            row = UIFactory.Rect("Row", Root);
            UIFactory.Stretch(row);
            UIFactory.HorizontalGroup(row, 8f, TextAnchor.MiddleLeft);

            emptyLabel = UIFactory.Label(Root, "Empty", emptyText, Mathf.Max(18f, tileSize * 0.38f), UIColors.TextDim,
                FontStyles.Italic, TextAlignmentOptions.Left);
            UIFactory.Stretch(emptyLabel.rectTransform);
            UIFactory.FitOneLine(emptyLabel, 14f, Mathf.Max(18f, tileSize * 0.38f));
        }

        public void Refresh(IReadOnlyList<ModuleInstance> list)
        {
            if (list == null) list = modules != null ? modules.OwnedModules : null;

            for (int i = row.childCount - 1; i >= 0; i--)
            {
                GameObject child = row.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
            entries.Clear();

            int count = list != null ? list.Count : 0;
            emptyLabel.gameObject.SetActive(count == 0);
            for (int i = 0; i < count; i++) CreateTile(list[i]);
        }

        private void CreateTile(ModuleInstance m)
        {
            Color rarity = UIColors.Rarity(m.Rarity);
            RectTransform tile = UIFactory.Rect("Module_" + m.Id, row);
            UIFactory.AddLayout(tile, tileSize, tileSize, 0f, tileSize, tileSize, 0f);

            UIFactory.Img(tile, "Fill", ProceduralSprites.Bar, UIColors.WithAlpha(UIColors.Hull, 0.95f), Image.Type.Sliced);
            Image icon = UIFactory.Img(tile, "Icon", UIIcons.Get(UIIcons.ForModule(m.Id)), rarity);
            icon.preserveAspect = true;
            UIFactory.Stretch(icon.rectTransform, tileSize * 0.16f, tileSize * 0.16f, tileSize * 0.16f, tileSize * 0.16f);

            Image overlay = null;
            if (showCooldowns && m.IsActive)
            {
                overlay = UIFactory.Img(tile, "Cooldown", ProceduralSprites.White, UIColors.WithAlpha(Color.black, 0.62f));
                overlay.type = Image.Type.Filled;
                overlay.fillMethod = Image.FillMethod.Vertical;
                overlay.fillOrigin = (int)Image.OriginVertical.Top;
                overlay.fillAmount = 0f;
            }
            UIFactory.Img(tile, "Edge", ProceduralSprites.BarEdge, rarity, Image.Type.Sliced);

            Entry e = new Entry();
            e.Id = m.Id;
            e.IsActive = m.IsActive;
            e.Overlay = overlay;
            entries.Add(e);
        }

        public void Tick()
        {
            if (!showCooldowns || modules == null) return;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Overlay == null) continue;
                entries[i].Overlay.fillAmount = modules.GetCooldownNormalized(entries[i].Id);
            }
        }
    }
}
