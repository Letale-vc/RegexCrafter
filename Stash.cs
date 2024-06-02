using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Linq;

namespace RegexCrafter;

public class Stash(RegexCraft core)
{
    private RegexCraft _core = core;
    public RegexCraft Core { get => _core; set => _core = value; }
    public bool IsVisible => Core.GameController.Game.IngameState.IngameUi.StashElement.IsVisible;
    public InventoryType InventoryType => Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.InvType;
    public string StashTabName => Core.GameController.Game.IngameState.IngameUi.StashElement.GetStashName(Core.GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash);
    public List<CustomItemData> ItemsInStashTab
    {
        get
        {
            if (IsVisible)
            {
                return Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems.Select(x => new CustomItemData(x)).ToList();
            }
            return [];
        }
    }

    public bool IsPublicTabNow
    {
        get
        {
            var playerStashTabs = Core.GameController.Game.IngameState.ServerData.PlayerStashTabs;
            var tab = playerStashTabs.First(x => x.Name == StashTabName);
            return tab.Flags.HasFlag(InventoryTabFlags.Public);
        }
    }

    public bool IsHaveItem(string baseName)
    {
        if (!IsVisible)
        {
            return false;
        }
        return Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems
            .Select(x => new CustomItemData(x))
            .Any(x => x.BaseName == baseName);
    }
    public bool TryGetItem(string baseName, out CustomItemData item)
    {
        if (!IsVisible)
        {
            item = null;
            return false;
        }
        item = Core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems
                .Select(x => new CustomItemData(x))
                .FirstOrDefault(x => x.BaseName == baseName);
        return item != null;
    }


}
