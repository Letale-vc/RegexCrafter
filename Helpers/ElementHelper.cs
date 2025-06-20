using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Windows.Forms;
using Cursor = ExileCore.PoEMemory.MemoryObjects.Cursor;

namespace RegexCrafter.Helpers;

public static class ElementHelper
{
    private static RegexCrafter _core;
    private static Cursor _cursor;
    public static void Init(Cursor cursor)
    {
        _cursor = cursor;
    }

    public static async SyncTask<bool> MoveTo(this Element element)
    {
        if (element.IsVisible)
            return await Input.MoveMouseToScreenPosition(element.GetClientRectCache);
        return false;
    }

    public static async SyncTask<bool> MoveAndClick(this Element element)
    {
        if (element.IsVisible)
            return await Input.Click(element.GetClientRect());
        return false;
    }

    public static async SyncTask<bool> MoveAndClick(this Element element, MouseButtons button)
    {
        if (element.IsVisible)
            return await Input.Click(button, element.GetClientRect());
        return false;
    }

    public static async SyncTask<bool> OnTakeForUse(this Element element)
    {
        if (!element.IsVisible) return false;
        if (element.Entity == null) return false;
        if (element.Entity.Type != EntityType.Item) return false;
        if (!element.Entity.TryGetComponent<Base>(out var @base)) return false;
        if (!@base.Info.FlavourText.StartsWith("Right click")) return false;

        if (!await Input.Click(MouseButtons.Right, element.GetClientRect())) return false;

        await Wait.SleepSafe(100, 200); // Give some time for the cursor to change action
        return true;
        //return await Wait.For(() => _cursor.Action == MouseActionType.UseItem, "On take for use", 500);
    }

    public static async SyncTask<bool> OnTakeForHold(this Element element)
    {
        if (!element.IsVisible) return false;
        if (element.Entity == null) return false;
        if (element.Entity.Type != EntityType.Item) return false;

        if (!await Input.Click(element.GetClientRect())) return false;

        return await Wait.For(() => _cursor.Action == MouseActionType.HoldItem, "On take for hold", 500);
    }

    public static async SyncTask<bool> FastMove(this Element element)
    {
        if (!element.IsVisible) return false;
        if (element.Entity == null) return false;
        if (element.Entity.Type != EntityType.Item) return false;

        if (!await Input.SimulateKeyEvent(Keys.LControlKey, true, false)) return false;
        if (!await Input.Click(element.GetClientRect())) return false;
        if (!await Input.SimulateKeyEvent(Keys.LControlKey, false)) return false;

        return true;
    }
}