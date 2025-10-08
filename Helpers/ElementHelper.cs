using System.Windows.Forms;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using RegexCrafter.Interface;
using Cursor = ExileCore.PoEMemory.MemoryObjects.Cursor;

namespace RegexCrafter.Helpers
{
    public static class ElementHelper
    {
        private static IInput _input;
        private static Cursor _cursor;
        private static Wait _wait;

        public static void Init(Cursor cursor, IInput input, Wait wait)
        {
            _cursor = cursor;
            _input = input;
            _wait = wait;
        }

        public static async SyncTask<bool> MoveTo(this Element element)
        {
            if (element.IsVisible)
            {
                return await _input.MoveMouseToScreenPosition(element.GetClientRectCache);
            }
            return false;
        }

        public static async SyncTask<bool> MoveAndClick(this Element element)
        {
            if (element.IsVisible)
            {
                return await _input.Click(element.GetClientRectCache);
            }
            return false;
        }

        public static async SyncTask<bool> MoveAndClick(this Element element, MouseButtons button)
        {
            if (element.IsVisible)
            {
                return await _input.Click(button, element.GetClientRectCache);
            }
            return false;
        }

        public static async SyncTask<bool> OnTakeForUse(this Element element)
        {
            if (!element.IsVisible)
            {
                return false;
            }
            if (element.Entity == null)
            {
                return false;
            }
            if (element.Entity.Type != EntityType.Item)
            {
                return false;
            }
            if (!element.Entity.TryGetComponent<Base>(out var @base))
            {
                return false;
            }
            if (!@base.Info.FlavourText.StartsWith("Right click"))
            {
                return false;
            }

            if (!await _input.Click(MouseButtons.Right, element.GetClientRectCache))
            {
                return false;
            }

            await _wait.SleepSafe(100, 200); // Give some time for the cursor to change action
            return true;
            //return await Wait.For(() => _cursor.Action == MouseActionType.UseItem, "On take for use", 500);
        }

        public static async SyncTask<bool> OnTakeForHold(this Element element)
        {
            if (!element.IsVisible)
            {
                return false;
            }
            if (element.Entity == null)
            {
                return false;
            }
            if (element.Entity.Type != EntityType.Item)
            {
                return false;
            }

            if (!await _input.Click(element.GetClientRect()))
            {
                return false;
            }

            return await _wait.For(() => _cursor.Action == MouseActionType.HoldItem, "On take for hold", 500);
        }

        public static async SyncTask<bool> FastMove(this Element element)
        {
            if (!element.IsVisible)
            {
                return false;
            }
            if (element.Entity == null)
            {
                return false;
            }
            if (element.Entity.Type != EntityType.Item)
            {
                return false;
            }

            if (!await _input.SimulateKeyEvent(Keys.LControlKey, true, false))
            {
                return false;
            }
            if (!await _input.Click(element.GetClientRect()))
            {
                return false;
            }
            if (!await _input.SimulateKeyEvent(Keys.LControlKey, false))
            {
                return false;
            }

            return true;
        }
    }
}
