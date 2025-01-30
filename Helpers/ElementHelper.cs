using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegexCrafter.Helpers;

public static class ElementHelper
{
    private static RegexCrafter _core;
    private static ExileCore.PoEMemory.MemoryObjects.Cursor Cursor => _core.GameController.Game.IngameState.IngameUi.Cursor;
    public static void Init(RegexCrafter core) => _core = core;
    public static async SyncTask<bool> MoveTo(this Element element)
    {
        if (element.IsVisible)
            return await Input.MoveMouseToScreenPosition(element.GetClientRectCache);
        return false;
    }

    public static async SyncTask<bool> MoveAndClick(this Element element)
    {
        if (element.IsVisible)
            return await Input.Click(element.GetClientRectCache);
        return false;
    }
    public static async SyncTask<bool> MoveAndClick(this Element element, MouseButtons button)
    {
        if (element.IsVisible)
            return await Input.Click(button, element.GetClientRectCache);
        return false;
    }

    public static async SyncTask<bool> OnTakeForUse(this Element element)
    {
        if (!element.IsVisible) return false;
        if (element.Entity == null) return false;
        if (element.Entity.Type != EntityType.Item) return false;
        if (!element.Entity.TryGetComponent<Base>(out var @base)) return false;
        if (!@base.Info.FlavourText.StartsWith("Right click")) return false;

        if (!await Input.Click(MouseButtons.Right, element.GetClientRectCache)) return false;

        return await Wait.For(() => Cursor.Action == MouseActionType.UseItem, "On take for use", 500);
    }

    public static async SyncTask<bool> OnTakeForHold(this Element element)
    {
        if (!element.IsVisible) return false;
        if (element.Entity == null) return false;
        if (element.Entity.Type != EntityType.Item) return false;

        if (!await Input.Click(element.GetClientRectCache)) return false;

        return await Wait.For(() => Cursor.Action == MouseActionType.HoldItem, "On take for hold", 500);
    }

    public static async SyncTask<bool> FastMove(this Element element)
    {
        if (!element.IsVisible) return false;
        if (element.Entity == null) return false;
        if (element.Entity.Type != EntityType.Item) return false;

        if (!await Input.SimulateKeyEvent(Keys.LControlKey, true, false)) return false;
        if (!await Input.Click(element.GetClientRectCache)) return false;
        if (!await Input.SimulateKeyEvent(Keys.LControlKey, false, true)) return false;

        return true;
    }
}
