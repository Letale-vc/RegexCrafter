using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using RegexCrafter.Helpers;
using SharpDX;

namespace RegexCrafter.Models
{
    public partial class InventoryItemData
    {
        private const string LogName = "InventoryItemData";
        public readonly Element Element;
        public string BaseName;
        public string ClassName;
        public string ClipboardText = string.Empty;
        public List<string> CustomTextLines;
        public int DeliriumOrbNumber;
        public List<string> EnchantedStats;
        public Entity Entity;
        public int ExplicitModsCount;
        public RectangleF GetClientRectCache;
        public List<string> HumanCraftStats;
        public List<string> HumanImpStats;
        public List<string> HumanStats;
        public bool IsCorrupted;
        public bool IsCrusader = false;
        public bool IsElder = false;
        public bool IsHunter = false;
        public bool IsIdentified = true;
        public bool IsMap;
        public bool IsRedeemer;
        public bool IsShaper = false;
        public bool IsSynthesized = false;
        public bool IsT17Map;
        public bool IsWarlord = false;

        //public string[] ToolTipStrings;
        public string ItemClass;
        public int MapTier;
        public int Quality;
        public ItemRarity Rarity;
        public List<string> Requirements = [];
        public string Sockets;
        public int StackSize;
        public string UniqueName;

        public InventoryItemData(Element el)
        {
            try
            {
                Element = el;
                Entity = el.Entity;
                GetClientRectCache = el.GetClientRectCache;

                if (Entity.TryGetComponent<Stack>(out var stack))
                {
                    StackSize = stack.Size;
                }

                if (Entity.TryGetComponent<Quality>(out var quality))
                {
                    Quality = quality.ItemQuality;
                }

                if (Entity.TryGetComponent<Base>(out var baseInf))
                {
                    BaseName = baseInf.Name;
                    ClassName = baseInf.Info.BaseItemTypeDat.ClassName;
                    ItemClass = NormalizeText(ClassName);
                    IsCorrupted = baseInf.isCorrupted;
                    IsRedeemer = baseInf.isRedeemer;
                }

                if (Entity.TryGetComponent<Sockets>(out var sockets))
                {
                    var socketString = sockets.SocketInfoByLinkGroup.ConvertAll(group =>
                        string.Join("-", group.Select(y => y.SocketColor.ToString()[0])));
                    Sockets = string.Join(" ", socketString);
                }

                if (Entity.TryGetComponent<AttributeRequirements>(out var req))
                {
                    if (req.intelligence > 0)
                    {
                        Requirements.Add($"Int: {req.intelligence}");
                    }
                    if (req.strength > 0)
                    {
                        Requirements.Add($"Str: {req.strength}");
                    }
                    if (req.dexterity > 0)
                    {
                        Requirements.Add($"Dex: {req.dexterity}");
                    }
                }

                if (Entity.TryGetComponent<Mods>(out var mods))
                {
                    Rarity = mods.ItemRarity;
                    UniqueName = mods.UniqueName;
                    IsIdentified = mods.Identified;
                    if (mods.RequiredLevel > 0)
                    {
                        Requirements.Add($"Level {mods.RequiredLevel}");
                    }
                    HumanStats = mods.HumanStats;
                    HumanImpStats = mods.HumanImpStats;
                    HumanCraftStats = mods.HumanCraftedStats;
                    EnchantedStats = mods.EnchantedStats;
                    ExplicitModsCount = mods.ExplicitMods.Count;
                }

                if (EnchantedStats != null)
                {
                    DeliriumOrbNumber = EnchantedStats.Count(x => x.Contains("Delirium Reward"));
                }

                MapTier = Entity.TryGetComponent<Map>(out var map) ? map.Tier : 0;
                IsMap = MapTier > 0;
                IsT17Map = MapTier == 17;
                CustomTextLines = GetCustomTextLines();
            }
            catch (Exception e)
            {
                GlobalLog.Error($"RegexCrafter.CustomItem Error:\n{e}.", LogName);
            }
        }


        public override string ToString()
        {
            return $"{BaseName} : {ClassName}";
        }

        private static string NormalizeText(string input)
        {
            return input.Contains(' ') ? input.ToLower() : MyRegex().Replace(input, " $1").ToLower();
        }

        public List<string> GetCustomTextLines()
        {
            List<string> textList =
            [
                $"Item Class: {ItemClass}",
                $"Rarity: {Rarity}",
                BaseName
            ];

            if (!string.IsNullOrEmpty(UniqueName))
            {
                textList.Add(UniqueName);
            }
            if (!string.IsNullOrEmpty(Sockets))
            {
                textList.Add($"Sockets: {Sockets}");
            }
            if (Quality > 0)
            {
                textList.Add($"Quality: {Quality}%");
            }
            if (IsCorrupted)
            {
                textList.Add("Corrupted");
            }
            HumanStats?.ForEach(x => textList.Add(x));
            HumanImpStats?.ForEach(x => textList.Add(x));
            Requirements.ForEach(x => textList.Add(x));
            EnchantedStats?.ForEach(x => textList.Add(x));
            if (IsMap)
            {
                textList.Add($"Map Tier: {MapTier}");
            }
            if (IsElder)
            {
                textList.Add("Elder");
            }
            if (IsShaper)
            {
                textList.Add("Shaper");
            }
            if (IsRedeemer)
            {
                textList.Add("Redeemer");
            }
            if (IsHunter)
            {
                textList.Add("Hunter");
            }
            if (IsWarlord)
            {
                textList.Add("Warlord");
            }
            if (IsCrusader)
            {
                textList.Add("Crusader");
            }
            if (IsSynthesized)
            {
                textList.Add("Synthesized");
            }
            textList.Add($"explicit:{ExplicitModsCount}");

            return textList;
        }

        public SyncTask<bool> MoveMouseToItem()
        {
            return Element.MoveTo();
        }

        public SyncTask<bool> Click()
        {
            return Element.MoveAndClick();
        }

        public SyncTask<bool> Click(MouseButtons button)
        {
            return Element.MoveAndClick(button);
        }

        public async SyncTask<bool> MoveAndTakeForUse()
        {
            return await Element.OnTakeForUse();
        }

        [GeneratedRegex("(?<!^)([A-Z])")]
        private static partial Regex MyRegex();
    }
}
