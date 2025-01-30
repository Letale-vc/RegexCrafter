using ExileCore.PoEMemory;
using ExileCore.Shared;
using ExileCore.Shared.Enums;

namespace RegexCrafter.Helpers;

public static class CurrencyTabHelper
{
    private const string LogName = "CurrencyTabHelper";

    public static async SyncTask<bool> SwitchCurrencyTab(CurrencyTabType typeButton)
    {
        if (!Stash.IsVisible || Stash.CurrentTab.TabType != InventoryType.CurrencyStash) return false;
        if (!TryGetCurrencySwitchButton(typeButton, out var button)) return false;

        if (!await button.MoveAndClick()) return false;

        return await Wait.Sleep(100);
    }

    public static async SyncTask<bool> SwitchCurrencyTab()
    {
        if (!Stash.IsVisible || Stash.CurrentTab.TabType != InventoryType.CurrencyStash) return false;
        var currentCurrencyTabType = GetCurrentCurrencyTabType();


        Element button = null;
        switch (currentCurrencyTabType)
        {
            case CurrencyTabType.Exotic when !TryGetCurrencySwitchButton(CurrencyTabType.General, out button):
            case CurrencyTabType.General when !TryGetCurrencySwitchButton(CurrencyTabType.Exotic, out button):
                return false;
        }

        if (!await button.MoveAndClick()) return false;

        return await Wait.Sleep(100);
    }

    public static CurrencyTabType GetCurrentCurrencyTabType()
    {
        if (!Stash.IsVisible || Stash.CurrentTab.TabType != InventoryType.CurrencyStash) return CurrencyTabType.None;
        if (Stash.CurrentTab.Inventory.Children[1].IsVisible) return CurrencyTabType.General;
        if (Stash.CurrentTab.Inventory.Children[2].IsVisible) return CurrencyTabType.Exotic;
        return CurrencyTabType.None;
    }

    private static bool TryGetCurrencySwitchButton(CurrencyTabType typeButton, out Element element)
    {
        element = null;
        if (!Stash.IsVisible) return false;

        element = Stash.CurrentTab.Inventory.FindChildRecursive(x => x.Text == typeButton.ToString());
        if (element == null)
        {
            GlobalLog.Error($"Not find button {typeButton}.", LogName);
            return false;
        }

        GlobalLog.Debug($"Find button {element.Text}.", LogName);
        return true;
    }
}