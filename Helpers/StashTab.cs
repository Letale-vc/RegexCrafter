using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexCrafter.Helpers;

public class StashTab(RegexCrafter _core)
{
    private const string LogName = "StashTab";
    public int Index => _core.GameController.Game.IngameState.IngameUi.StashElement.IndexVisibleStash;
    public bool IsVisible => _core.GameController.Game.IngameState.IngameUi.StashElement.IsVisible;
    public Inventory Inventory => _core.GameController.Game?.IngameState?.IngameUi?.StashElement?.VisibleStash;
    public InventoryType TabType => _core.GameController.Game.IngameState.IngameUi.StashElement.VisibleStash.InvType;
    public string Name => _core.GameController.Game.IngameState?.IngameUi?.StashElement?.GetStashName(_core.GameController.Game.IngameState
            .IngameUi.StashElement.IndexVisibleStash);

    public List<InventoryItemData> VisibleItems => _core.GameController.Game.IngameState?.IngameUi?.StashElement
        ?.VisibleStash?.VisibleInventoryItems?.Select(x => new InventoryItemData(x)).ToList();

    public bool IsPublic => _core.GameController.Game.IngameState.ServerData.PlayerStashTabs.First(x => x.Name == Name)
        .Flags.HasFlag(InventoryTabFlags.Public);

    public bool ContainsItem(string baseName)
    {
        if (!IsVisible) return false;

        return VisibleItems.Any(x => x.BaseName.Contains(baseName, StringComparison.CurrentCultureIgnoreCase));

    }
    public async SyncTask<bool> ContainsItemAsync(string baseName)
    {
        if (!IsVisible) return false;

        var result = VisibleItems.Any(x => x.BaseName.Contains(baseName, StringComparison.CurrentCultureIgnoreCase));

        if (TabType is not InventoryType.CurrencyStash)
        {
            return result;
        }

        if (!await SwitchCurrencyTab())
        {
            GlobalLog.Error("Failed to switch to Currency tab.", LogName);
            return false;
        }
        return VisibleItems.Any(x => x.BaseName.Contains(baseName, StringComparison.CurrentCultureIgnoreCase));
    }

    public async SyncTask<bool> ContainsItemAsync(Func<InventoryItemData, bool> condition)
    {
        if (!IsVisible) return false;

        var result = VisibleItems.Any(condition);

        if (TabType is not InventoryType.CurrencyStash)
        {
            return result;
        }

        if (!await SwitchCurrencyTab())
        {
            GlobalLog.Error("Failed to switch to Currency tab.", LogName);
            return false;
        }
        return VisibleItems.Any(condition);
    }

    public async SyncTask<(bool Fount, InventoryItemData Item)> TryGetItemAsync(string name)
    {
        if (!IsVisible)
        {
            return (false, null);
        }
        var item = VisibleItems.FirstOrDefault(
            x => x.BaseName.Contains(name, StringComparison.CurrentCultureIgnoreCase));
        if (TabType is not InventoryType.CurrencyStash)
        {
            return (item != null, item);
        }
        if (!await SwitchCurrencyTab())
        {
            GlobalLog.Error("Failed to switch to Currency tab.", LogName);
            return (false, null);
        }
        item = VisibleItems.FirstOrDefault(
            x => x.BaseName.Contains(name, StringComparison.CurrentCultureIgnoreCase));

        return (item != null, item);
    }
    public async SyncTask<(bool Found, InventoryItemData Item)> TryGetItemAsync(Func<InventoryItemData, bool> condition)
    {
        if (!IsVisible)
        {
            return (false, null);
        }
        var item = VisibleItems.FirstOrDefault(condition);
        if (TabType is not InventoryType.CurrencyStash)
        {
            return (item != null, item);
        }
        if (!await SwitchCurrencyTab())
        {
            GlobalLog.Error("Failed to switch to Currency tab.", LogName);
            return (false, null);
        }
        item = VisibleItems.FirstOrDefault(condition);
        return (item != null, item);
    }

    public bool TryGetItem(string baseName, out InventoryItemData item)
    {
        if (!IsVisible)
        {
            item = null;
            return false;
        }

        item = VisibleItems.FirstOrDefault(
            x => x.BaseName.Contains(baseName, StringComparison.CurrentCultureIgnoreCase));

        return item != null;
    }

    public bool TryGetItem(Func<InventoryItemData, bool> condition, out InventoryItemData item)
    {
        item = null;
        if (!IsVisible) return false;
        item = VisibleItems.FirstOrDefault(condition);

        return item != null;
    }
    public async SyncTask<bool> SwitchCurrencyTab(CurrencyTabType typeButton)
    {
        if (!IsVisible || TabType != InventoryType.CurrencyStash) return false;
        if (!TryGetCurrencySwitchButton(typeButton, out var button)) return false;

        if (!await button.MoveAndClick()) return false;

        return await Wait.Sleep(100);
    }

    public async SyncTask<bool> SwitchCurrencyTab()
    {
        if (IsVisible || TabType != InventoryType.CurrencyStash) return false;
        var currentCurrencyTabType = GetCurrentCurrencyTabType();


        Element button = null;
        if (currentCurrencyTabType ==
            CurrencyTabType.General && !TryGetCurrencySwitchButton(CurrencyTabType.Exotic, out button))
            return false;
        if (currentCurrencyTabType == CurrencyTabType.Exotic &&
            !TryGetCurrencySwitchButton(CurrencyTabType.General, out button))
            return false;

        if (button == null) return false;

        if (!await button.MoveAndClick()) return false;

        return await Wait.Sleep(100);
    }

    public CurrencyTabType GetCurrentCurrencyTabType()
    {
        if (TabType != InventoryType.CurrencyStash) return CurrencyTabType.None;
        if (Inventory.Children[1].IsVisible) return CurrencyTabType.General;
        if (Inventory.Children[2].IsVisible) return CurrencyTabType.Exotic;
        return CurrencyTabType.None;
    }

    private bool TryGetCurrencySwitchButton(CurrencyTabType typeButton, out Element element)
    {
        element = null;
        if (!IsVisible) return false;

        element = Inventory.FindChildRecursive(x => x.Text == typeButton.ToString()).Parent;
        if (element == null)
        {
            GlobalLog.Error($"Not find button {typeButton}.", LogName);
            return false;
        }

        GlobalLog.Debug($"Find button {element.Text}.", LogName);
        return true;
    }
}