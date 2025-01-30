
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms.VisualStyles;

namespace RegexCrafter;

public static class PlayerInventory
{
    private static RegexCrafter _core;
    public static void Init(RegexCrafter core) => _core = core;
    public static bool IsVisible => _core.GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible;
    public static List<InventoryItemData> Items =>
        _core.GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems.Select(x => new InventoryItemData(x)).ToList();

    public static List<InventoryItemData> NonCorruptItems => Items.Where(x => !x.IsCorrupted).ToList();

    public static List<InventoryItemData> GetConditionsItems(Func<InventoryItemData, bool> condition) => Items.Where(condition).ToList();
}