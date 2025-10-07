using System;
using System.Collections.Generic;
using ExileCore.Shared;
using RegexCrafter.Helpers;
using RegexCrafter.Interface;
using RegexCrafter.Models;
using SharpDX;

namespace RegexCrafter.Places;

public class MousePositionCrafting : ICraftingPlace
{
    private const string LogName = "MousePositionCrafting";
    private readonly RegexCrafter _core;
    private RectangleF? _lastItemRect;

    public MousePositionCrafting(RegexCrafter core)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
    }

    private IInput Input
    {
        get => _core.Input;
    }

    private Scripts Scripts
    {
        get => _core.Scripts;
    }

    public bool SupportChainCraft
    {
        get => false;
    }

    public SyncTask<bool> PrepareCraftingPlace()
    {
        return SyncTask.FromResult(true);
    }

    public SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync()
    {
        return TryGetItemsAsync(_ => true);
    }

    public async SyncTask<(bool Success, List<InventoryItemData> Items)> TryGetItemsAsync(
        Func<InventoryItemData, bool> conditionUse)
    {
        var (isSuccess, item) = await Scripts.WaitForHoveredItem(
            hoverItem => hoverItem != null,
            "Get item under cursor");

        if (!isSuccess)
        {
            if (_lastItemRect.HasValue)
            {
                if (!await Input.MoveMouseToScreenPosition(_lastItemRect.Value))
                {
                    GlobalLog.Error("Failed to move mouse to last item position", LogName);
                    return (false, []);
                }

                (isSuccess, item) = await Scripts.WaitForHoveredItem(
                    hoverItem => hoverItem != null,
                    "Get item under cursor after mouse move");
                if (!isSuccess)
                {
                    _lastItemRect = null;
                    GlobalLog.Error("Still no item under cursor after moving mouse to last position", LogName);
                }
            }

            _lastItemRect = null;
            GlobalLog.Error("No item under cursor", LogName);
            return (false, []);
        }


        if (!conditionUse(item))
        {
            _lastItemRect = null;
            GlobalLog.Debug("Item under cursor doesn't meet conditions use", LogName);
            return (true, []);
        }

        _lastItemRect = item.GetClientRectCache;
        return (true, [item]);
    }

    public bool CanCraft()
    {
        return _core.GameController.Game.IngameState.UIHoverTooltip.IsVisible;
    }
}
