using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using RegexCrafter.Helpers;
using RegexCrafter.Interface;
using RegexCrafter.Models;
using SharpDX;
using System;

namespace RegexCrafter.Places
{
    public class MousePosition : ICraftingPlace
    {
        private const string LogName = "MousePositionCrafting";
        private readonly RegexCrafter _core;
        private RectangleF? _lastItemRect;

        public MousePosition(RegexCrafter core)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

        private IInput Input => _core.Input;

        private Scripts Scripts => _core.Scripts;

        public bool SupportChainCraft => false;

        public SyncTask<bool> PrepareCraftingPlace()
        {
            return SyncTask.FromResult(true);
        }

        public SyncTask<GetItemsResult> GetItemsAsync()
        {
            var uiHover = _core.GameController.Game.IngameState.UIHover;
            if (uiHover.Tooltip == null || uiHover.Entity == null)
            {
                GlobalLog.Debug("No hovered item found or tooltip is null.", LogName);
                return SyncTask.FromResult(new GetItemsResult(false, []));
            }

            if (uiHover.Entity.Address == 0 && !uiHover.IsValid)
            {
                GlobalLog.Debug("Hovered item is not valid.", LogName);
                return SyncTask.FromResult(new GetItemsResult(false, []));
            }

            if (_core.GameController.Files.BaseItemTypes.Translate(uiHover.Entity.Path) == null)
            {
                GlobalLog.Debug($"Base item type not found for path: {uiHover.Entity.Path}", LogName);
                return SyncTask.FromResult(new GetItemsResult(false, []));
            }
            var entity = uiHover.Entity;

            var baseComp = entity.GetComponent<Base>();
            var stack = entity.GetComponent<Stack>();

            return SyncTask.FromResult(new GetItemsResult(true, [new ItemData(
                entity.Address,
                baseComp?.Name ?? string.Empty,
                stack?.Size ?? 1,
                uiHover.GetClientRectCache
            )]));
        }
        public bool CanCraft()
        {
            return _core.GameController.Game.IngameState.UIHoverTooltip.IsVisible;
        }
    }
}
