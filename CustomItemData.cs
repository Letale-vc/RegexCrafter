using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;


namespace RegexCrafter;

public class CustomItemData
{
    private static RegexCraft Core;
    public Entity Entity;
    public string BaseName;
    public string ClassName;
    public RectangleF Position;
    public bool IsIdentified;
    public bool IsCorrupted;
    public int Quality;
    public ItemRarity Rarity;
    public bool IsMap;
    public bool IsT17Map;
    public static void InitCustomItem(RegexCraft core)
    {
        Core = core;
    }
    public CustomItemData(NormalInventoryItem inventoryItem)
    {
        try
        {


            Entity = inventoryItem.Item;
            var baseItemType = Core.GameController.Files.BaseItemTypes.Translate(inventoryItem.Item.Path);
            ClassName = baseItemType?.ClassName ?? string.Empty;
            BaseName = baseItemType?.BaseName ?? string.Empty;


            Position = inventoryItem.GetClientRect();
            if (Entity.TryGetComponent<Quality>(out var quality))
            {
                Quality = quality.ItemQuality;
            }

            if (Entity.TryGetComponent<Base>(out var @base))
            {
                IsCorrupted = @base.isCorrupted;

            }

            if (Entity.TryGetComponent<Mods>(out var mods))
            {
                Rarity = mods.ItemRarity;
                IsIdentified = mods.Identified;
            }

            var MapTier = Entity.TryGetComponent<Map>(out var map) ? map.Tier : 0;
            IsMap = MapTier > 0;
            IsT17Map = MapTier == 17;
        }
        catch (Exception e)
        {
            Core.LogError($"RegexCrafter.CustomItem Error:\n{e}");
        }

    }
}
