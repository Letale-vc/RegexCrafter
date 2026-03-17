using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using RegexCrafter.Helpers;
using RegexCrafter.Interface;
using RegexCrafter.Models;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RegexCrafter.Places
{
    public class PlayerInventory : ICurrencyPlace, ICraftingPlace
    {
        private readonly RegexCrafter _core;

        public PlayerInventory(RegexCrafter core)
        {
            _core = core ??
                    throw new ArgumentNullException(nameof(core),
                        "PlayerInventory requires a valid RegexCrafter instance.");
            GlobalLog.Debug("PlayerInventory initialized.", "PlayerInventory");
        }

        public bool IsVisible => _core.GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible;
        public bool SupportChainCraft { get; } = true;
        private IEnumerable<ItemData> GetItems()
        {
            var inventory = _core.GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            if (inventory == null) yield break;

            foreach (var itemElement in inventory.VisibleInventoryItems)
            {
                var entity = itemElement.Entity;
                if (entity == null || entity.Address == 0) continue;

                var baseComp = entity.GetComponent<Base>();
                var stack = entity.GetComponent<Stack>();

                yield return new ItemData(
                    entity.Address,
                    baseComp?.Name ?? string.Empty,
                    stack?.Size ?? 1,
                    itemElement.GetClientRectCache
                );
            }
        }

        #region ICurrencyPlace Implementation

        public SyncTask<bool> HasCurrencyAsync(string currency)
        {
            if (!IsVisible)
            {
                GlobalLog.Error("Player inventory is not visible.", "PlayerInventory");
                return SyncTask.FromResult(false);
            }

            if (string.IsNullOrEmpty(currency))
            {
                GlobalLog.Error("Currency name cannot be null or empty.", "PlayerInventory");
                return SyncTask.FromResult(false);
            }
            return SyncTask.FromResult(GetItems().Any(x => x.BaseName.Contains(currency) || x.BaseName == currency));
        }

        public bool HasCurrency(string currency)
        {
            if (!IsVisible)
            {
                GlobalLog.Error("Player inventory is not visible.", "PlayerInventory");
                return false;
            }

            if (string.IsNullOrEmpty(currency))
            {
                GlobalLog.Error("Currency name cannot be null or empty.", "PlayerInventory");
                return false;
            }

            return GetItems().Any(x => x.BaseName.Contains(currency) || x.BaseName == currency);
        }
        public async SyncTask<bool> TakeCurrencyForUseAsync(string currency)
        {

            RectangleF? clickRect = null;

            foreach (var item in GetItems())
            {
                if (item.BaseName.Contains(currency, StringComparison.OrdinalIgnoreCase))
                {
                    if (clickRect == null)
                    {
                        clickRect = item.ClickRect;
                        break;
                    }
                }
            }
            if (clickRect == null)
            {
                GlobalLog.Error($"No {currency} found in player inventory.", "PlayerInventory");
                return false;
            }

            if (!await _core.Input.MoveMouseToScreenPosition(clickRect.Value))
            {
                return false;
            }

            return await _core.Input.Click();
        }

        #endregion

        #region ICraftingPlace Implementation


        public SyncTask<GetItemsResult> GetItemsAsync()
        {
            return SyncTask.FromResult(new GetItemsResult(true, GetItems().ToList()));
        }

        public SyncTask<bool> PrepareCraftingPlace()
        {
            return SyncTask.FromResult(true);
        }

        public bool CanCraft()
        {
            return IsVisible;
        }

        #endregion
    }
}
