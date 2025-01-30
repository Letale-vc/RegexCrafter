using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace RegexCrafter.Helpers;

public class StashTab(RegexCrafter core)
{
    private const string LogName = "StashTab";
    public int Index => core.GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash;
    public bool IsVisible => core.GameController.Game.IngameState.IngameUi.StashElement.IsVisible;
    public Inventory Inventory => core.GameController.Game?.IngameState?.IngameUi?.StashElement?.VisibleStash;
    public InventoryType TabType => core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.InvType;

    public string Name =>
        core.GameController.Game.IngameState?.IngameUi?.StashElement?.GetStashName(core.GameController.Game.IngameState
            .IngameUi.StashElement.IndexVisibleStash);

    public List<InventoryItemData> VisibleItems => core.GameController.Game.IngameState?.IngameUi?.StashElement
        ?.VisibleStash?.VisibleInventoryItems?.Select(x => new InventoryItemData(x)).ToList();

    public bool IsPublic => core.GameController.Game.IngameState.ServerData.PlayerStashTabs.First(x => x.Name == Name)
        .Flags.HasFlag(InventoryTabFlags.Public);

    public bool ContainsItem(string baseName)
    {
        return IsVisible && VisibleItems.Any(x => x.BaseName == baseName);
    }

    public bool ContainsItem(Func<InventoryItemData, bool> condition)
    {
        return IsVisible && VisibleItems.Any(condition);
    }

    public bool TryGetItem(string baseName, out InventoryItemData item)
    {
        if (!IsVisible)
        {
            item = null;
            return false;
        }

        item = VisibleItems.FirstOrDefault(x => x.BaseName == baseName);

        return item != null;
    }

    public bool TryGetItem(Func<InventoryItemData, bool> condition, out InventoryItemData item)
    {
        item = null;
        if (!IsVisible) return false;
        item = VisibleItems.FirstOrDefault(condition);

        return item != null;
    }
}