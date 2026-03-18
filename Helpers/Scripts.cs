using ExileCore.Shared;
using RegexCrafter.Enums;
using RegexCrafter.Interface;
using SharpDX;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Cursor = ExileCore.PoEMemory.MemoryObjects.Cursor;

namespace RegexCrafter.Helpers
{
    public class Scripts
        (RegexCrafter core)
    {
        private const string LogName = "Scripts";
        private IInput Input => core.Input;
        private Cursor Cursor => core.GameController.Game.IngameState.IngameUi.Cursor;
        private Wait Wait => core.Wait;
        private ICurrencyPlace CurrencyPlace => core.CurrencyPlace;
        private ICraftingPlace CraftingPlace => core.CraftingPlace;

        public async SyncTask<bool> UseCurrencyToSingleItem(RectangleF rect, string currency,
            Func<string, CraftingAction> condition, CancellationToken ct = default)
        {
            try
            {
                var success = await CurrencyPlace.TakeCurrencyForUseAsync(currency);

                if (!success)
                {
                    GlobalLog.Error($"Failed to take {currency} for use.", LogName);
                    return false;
                }

                if (!await Input.KeyDown(Keys.LShiftKey, ct))
                {
                    return false;
                }
                return await ClickUntilCondition(rect, condition, 5000, ct);
            }
            finally
            {
                Input.CleanKeys();
            }
        }
        public async SyncTask<bool> TakeCurrencyForUse(string currencyUseName, bool multiUse = false,
            CancellationToken ct = default)
        {
            var success = await CurrencyPlace.TakeCurrencyForUseAsync(currencyUseName);
            if (!success)
            {
                GlobalLog.Error($"Failed to take {currencyUseName} for use.", LogName);
                return false;
            }

            if (multiUse && !await Input.KeyDown(Keys.LShiftKey, ct))
            {
                return false;
            }

            return true;
        }
        public void ClearInput()
        {
            Input.CleanKeys();
        }

        public async SyncTask<bool> UseCurrencyOnMultipleItems(string currencyUseName,
            Func<string, CraftingAction> condition,
            CancellationToken ct = default)
        {
            var (success, items) = await CraftingPlace.GetItemsAsync();
            if (!success)
            {
                return false;
            }
            if (items.Count == 0)
            {
                GlobalLog.Debug("No items found.", LogName);
                return false;
            }

            return await UseCurrencyOnMultipleItems(items.Select(item => item.ClickRect).ToArray(), currencyUseName, condition, ct);
        }

        public async SyncTask<bool> UseCurrencyOnMultipleItems(RectangleF[] positions, string currencyUseName,
            Func<string, CraftingAction> condition,
            CancellationToken ct = default)
        {
            GlobalLog.Debug($"Start using {currencyUseName} to items.", LogName);
            try
            {
                var success = await CurrencyPlace.TakeCurrencyForUseAsync(currencyUseName);

                if (!success)
                {
                    GlobalLog.Error($"Failed to take {currencyUseName} for use.", LogName);
                    return false;
                }

                if (!await Input.KeyDown(Keys.LShiftKey, ct))
                {
                    GlobalLog.Error("Failed to hold Shift key.", LogName);
                    return false;
                }

                if (!await CraftingPlace.PrepareCraftingPlace())
                {
                    return false;
                }

                for (var i = 0; i < positions.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!await ClickUntilCondition(positions[i], condition, 5000, ct))
                    {
                        GlobalLog.Error($"Can't apply {currencyUseName} to item.", LogName);
                        return false;
                    }
                }


                return true;
            }
            finally
            {
                Input.CleanKeys();
            }
        }

        public async SyncTask<bool> ClickUntilCondition(RectangleF rect,
            Func<string, CraftingAction> condition,
            int maxCountClick = 5000, CancellationToken ct = default)
        {
            var countClick = 0;
            long currentAddress = 0;

            GlobalLog.Debug($"Clicking on item until condition is met.", LogName);
            while (CraftingPlace.CanCraft())
            {
                if (countClick >= maxCountClick)
                {
                    GlobalLog.Error($"Too many clicks. Max clicks: {maxCountClick}", LogName);
                    return false;
                }

                ct.ThrowIfCancellationRequested();

                var cursorGetClientRectCache = Cursor.GetClientRectCache;
                // Move the mouse to the item
                if (!rect.Contains(cursorGetClientRectCache.TopLeft.X, cursorGetClientRectCache.TopLeft.Y))
                {
                    await Input.MoveMouseToScreenPosition(rect, ct);
                }

                // Get the initial hovered item
                var (ok, _, text) = await GetHoveredItemData(ct);
                if (!ok) return false;

                var craftingAction = condition(text);

                if (craftingAction == CraftingAction.Complete) return true;
                if (craftingAction == CraftingAction.Skip) return true;


                // Apply the currency by clicking
                GlobalLog.Debug($"Click on item pos {rect} .", LogName);
                await Input.Click(ct);

                GlobalLog.Debug("Wait for item update after applying currency.", LogName);
                currentAddress = await WaitAddressChange(currentAddress, ct);
                if (currentAddress == 0) return false;

                countClick++;
            }

            GlobalLog.Debug("Currency is not available or crafting place cannot be used.", LogName);
            return false;
        }
        public async SyncTask<long> WaitAddressChange(long oldAddress, CancellationToken ct = default)
        {
            long resultAddress = 0;
            await Wait.For(() =>
            {
                if (TryGetHoveredItemAddress(out var current) && current != oldAddress)
                {
                    resultAddress = current;
                    return true;
                }
                return false;
            }, "Wait for address change", 500, ct);

            return resultAddress;
        }
        public async SyncTask<(bool isSuccess, string text)> GetHoverClipboardText(CancellationToken ct = default)
        {
            try
            {
                if (!await Input.SimulateKeyEvent(Keys.C, Keys.LControlKey, ct))
                {
                    GlobalLog.Error("[GetHoverClipboardText] Failed to simulate Ctrl+C key event.", LogName);
                    return (false, string.Empty);
                }
                await Wait.Sleep(50, ct);
                var text = Clipboard.GetClipboardText();
                GlobalLog.Debug($"[GetHoverClipboardText] Clipboard text: {text}", LogName);
                return (true, text);
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[GetHoverClipboardText] Exception occurred while getting clipboard text: {ex.Message}", LogName);
                return (false, string.Empty);

            }
            finally
            {
                await Input.KeyUp(Keys.LControlKey, ct);
            }
        }
        public async SyncTask<(bool success, long address, string text)> GetHoveredItemData(CancellationToken ct = default)
        {
            if (!TryGetHoveredItemAddress(out var address))
                return (false, 0, string.Empty);

            var (isOk, text) = await GetHoverClipboardText(ct);
            if (!isOk) return (false, 0, string.Empty);

            return (true, address, text);
        }
        public bool TryGetHoveredItemAddress(out long address)
        {
            address = 0;
            var uiHover = core.GameController.Game.IngameState.UIHoverTooltip;
            if (uiHover.Tooltip == null || uiHover.Entity == null)
            {
                GlobalLog.Debug("No hovered item found or tooltip is null.", LogName);
                return false;
            }

            if (uiHover.Entity.Address == 0 && !uiHover.IsValid)
            {
                GlobalLog.Debug("Hovered item is not valid.", LogName);
                return false;
            }

            address = uiHover.Entity.Address;
            return true;
        }
    }

}
