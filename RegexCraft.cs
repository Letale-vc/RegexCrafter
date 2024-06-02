using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using InputHumanizer.Input;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace RegexCrafter;

struct RegexPatterns
{
    public List<string> Include;
    public List<string> Exclude;
}
public class RegexCraft : BaseSettingsPlugin<Settings>
{
    private string _regexInputPattern = new("");
    private RegexPatterns _currentParsedPattern;

    private Stash _stash;
    private SyncTask<bool> _currentOperation;
    private IInputController _inputController;
    private List<CustomItemData> _badItems = [];
    private bool StopCicleCraft => (!GameController.Game.IngameState.IngameUi.StashElement.IsVisible || Input.IsKeyDown(Keys.Delete)) == true;
    private SharpDX.Vector2 WindowOffset => GameController.Window.GetWindowRectangleTimeCache.TopLeft;
    private List<CustomItemData> _doneCraftItem = [];

    public override bool Initialise()
    {
        Name = "RegexCraft";
        CustomItemData.InitCustomItem(this);
        _stash = new Stash(this);
        var tryGetInputController = GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            LogError("InputHumanizer method not registered.");
            return false;
        }
        _inputController = tryGetInputController(this.Name);
        return base.Initialise();
    }
    public override void DrawSettings()
    {
        base.DrawSettings();
        ImGui.InputText("Your regex pattern", ref _regexInputPattern, 1024);
    }

    public override void Render()
    {
        if (_currentOperation != null)
        {
            if (Settings.Debug)
            { DebugWindow.LogMsg("Craft is running..."); }
            TaskUtils.RunOrRestart(ref _currentOperation, () =>
            {
                return null;
            });

        }
        foreach (var item in _doneCraftItem)
        {
            Graphics.DrawFrame(item.Position, Color.Green, 2);
        }
        foreach (var item in _badItems)
        {
            Graphics.DrawFrame(item.Position, Color.Red, 2);
        }

        if (!_stash.IsVisible)
        {
            _badItems.Clear();
            _doneCraftItem.Clear();
            return;
        }
        if (Input.IsKeyDown(Settings.StartCraftHotKey.Value) && _currentOperation == null)
        {
            _currentOperation = StartMapCraft();
        }

    }

    private List<CustomItemData> GetPlayerInventoryItems()
    {
        var inventoryItems = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
        return inventoryItems.Select(x => new CustomItemData(x)).ToList();
    }


    private async SyncTask<bool> StartMapCraft()
    {
        _badItems.Clear();
        _doneCraftItem.Clear();
        if (string.IsNullOrEmpty(_regexInputPattern))
        {
            LogError("Regex pattern is empty.");
            return false;
        }
        if (_stash.IsPublicTabNow)
        {
            LogError("Currency stash tab is public. Please switch to a private.");
            return false;
        }
        if (_stash.InventoryType != InventoryType.CurrencyStash)
        {
            LogError("Open Currency stash tab.");
            return false;
        }
        if (_inputController == null)
        {
            return false;
        }


        var inventoryItems = GetPlayerInventoryItems();
        _badItems = inventoryItems.Where(x => !x.IsMap || x.IsCorrupted).ToList();
        // Check if all maps are identified
        var nonCorruptedMaps = inventoryItems.Where(x => x.IsMap && !x.IsCorrupted).ToList();
        var needIndetifies = nonCorruptedMaps.Where(x => !x.IsIdentified).ToList();
        if (needIndetifies.Count != 0)
        {
            // Apply Scroll of Wisdom
            bool result = await ApplyCurrencyToIventoryItems(needIndetifies, CurrencyType.ScrollOfWisdom, (x) => x.item.IsIdentified);
            if (!result)
            {
                return false;
            }
        }

        if (!Settings.IsT17MapCrafting)
        {
            var needScouring = nonCorruptedMaps.Where(x => (x.Rarity == ItemRarity.Rare && x.Quality < 20) || x.Rarity == ItemRarity.Magic).ToList();
            if (needScouring.Count != 0)
            {
                if (!await ApplyCurrencyToIventoryItems(needScouring, CurrencyType.OrbOfScouring, (x) => RegexCondition(x) || x.item.Rarity == ItemRarity.Normal))
                {
                    return false;
                }
            }
        }

        if (Settings.UseAddQuality && !Settings.IsT17MapCrafting)
        {
            var needAddQuality = nonCorruptedMaps.Where(x => !_doneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.Quality < 20).ToList();
            if (needAddQuality.Count != 0)
            {
                if (!await ApplyCurrencyToIventoryItems(needAddQuality, CurrencyType.CartographersChisel, (x) => RegexCondition(x) || x.item.Quality >= 20))
                {
                    return false;
                }
            }
        }

        nonCorruptedMaps = GetPlayerInventoryItems().Where(x => !_doneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Normal).ToList();
        if (nonCorruptedMaps.Count != 0)
        {

            if (!await ApplyCurrencyToIventoryItems(nonCorruptedMaps, CurrencyType.OrbOfAlchemy, (x) => RegexCondition(x) || x.item.Rarity == ItemRarity.Rare))
            {
                return false;
            }
        }

        nonCorruptedMaps = GetPlayerInventoryItems().Where(x => !_doneCraftItem.Any(item => item.Entity.Address == x.Entity.Address) && x.IsMap && !x.IsCorrupted && x.Rarity == ItemRarity.Rare).ToList();

        if (nonCorruptedMaps.Count != 0)
        {
            if (!await ApplyCurrencyToIventoryItems(nonCorruptedMaps, CurrencyType.ChaosOrb, (x) => RegexCondition(x)))
            {
                return false;
            }
        }

        return true;
    }

    private bool RegexCondition((CustomItemData item, string hoverItemText) hoverItem)
    {
        if (RegexCheck(hoverItem.hoverItemText))
        {
            _doneCraftItem.Add(hoverItem.item);
            return true;
        }

        return false;
    }


    public async SyncTask<bool> CoppiedHoverItemToBuffer()
    {
        await _inputController.KeyDown(Keys.LControlKey);
        await _inputController.KeyDown(Keys.C);
        await _inputController.KeyUp(Keys.C);
        await _inputController.KeyUp(Keys.LControlKey);
        return true;
    }

    public static string GetClipboardText()
    {
        string result = string.Empty;
        Thread staThread = new(() =>
        {
            result = Clipboard.GetText();
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();
        return result;
    }

    private RegexPatterns ParseInputRegexPattern
    {
        get
        {
            string[] parts = _regexInputPattern.Split(new[] { "\"" }, StringSplitOptions.None);

            List<string> exclude = [], include = [];

            // Split the regex pattern into include and exclude parts
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                if (trimmedPart.StartsWith("!"))
                {
                    exclude.Add(trimmedPart.Substring(1));
                }
                else if (part != " " && part != "")
                {
                    include.Add(trimmedPart);
                }
            }
            if (Settings.Debug)
            {

                LogMsg($"Include: {string.Join(", ", include)}");
                LogMsg($"Exclude: {string.Join(", ", exclude)}");
            }
            return new RegexPatterns
            {
                Include = include,
                Exclude = exclude
            };

        }
    }

    private bool RegexCheck(string textCheck)
    {
        _currentParsedPattern = ParseInputRegexPattern;
        foreach (string ex in _currentParsedPattern.Exclude)
        {
            var regex = new Regex(ex, RegexOptions.IgnoreCase);
            if (regex.IsMatch(textCheck))
            {
                return false;
            }
        }

        foreach (string inc in _currentParsedPattern.Include)
        {
            var regex = new Regex(inc, RegexOptions.IgnoreCase);
            if (!regex.IsMatch(textCheck))
            {
                return false;
            }
        }

        return true;
    }

    private async SyncTask<bool> ApplyCurrencyToIventoryItems(List<CustomItemData> items, string currencyType, Func<(CustomItemData item, string hoverItemText), bool> condition)
    {
        if (_stash.TryGetItem(currencyType, out var currency))
        {
            using (_inputController)
            {
                await MoveToItem(currency.Position.Center);
                await _inputController.Click(MouseButtons.Right);
                await _inputController.KeyDown(Keys.LShiftKey);
                foreach (var item in items)
                {
                    if (StopCicleCraft)
                    {
                        await _inputController.KeyUp(Keys.LShiftKey);
                        return false;

                    }
                    await MoveToItem(item.Position.Center);
                    while (true)
                    {
                        if (StopCicleCraft)
                        {
                            await _inputController.KeyUp(Keys.LShiftKey);
                            return false;
                        }

                        await Wait();
                        var hoverItem = await GetHoveredItem();
                        if (hoverItem.Item1 == null || hoverItem.Item2 == null)
                        {
                            LogError($"No Hover item found.");
                            await _inputController.KeyUp(Keys.LShiftKey);
                            return false;
                        }

                        if (condition(hoverItem))
                        {
                            break;
                        }


                        if (!_stash.IsHaveItem(currencyType))
                        {
                            LogError($"No {currencyType} found.");
                            await _inputController.KeyUp(Keys.LShiftKey);
                            return false;
                        }
                        await _inputController.Click(MouseButtons.Left);
                    }


                }
                await _inputController.KeyUp(Keys.LShiftKey);
            }
        }
        else
        {
            LogError($"No {currencyType} found.");
            return false;
        }
        return true;
    }
    private async SyncTask<bool> MoveToItem(SharpDX.Vector2 position)
    {
        position += WindowOffset;
        await _inputController.MoveMouse(position.ToVector2Num());
        return true;
    }
    private async SyncTask<bool> MoveAndClickToItem(MouseButtons mouseButton, SharpDX.Vector2 position)
    {
        position += WindowOffset;
        await _inputController.Click(mouseButton, position.ToVector2Num());
        return true;
    }

    private async SyncTask<(CustomItemData, string)> GetHoveredItem()
    {
        try
        {
            var uiHover = GameController.Game.IngameState.UIHover;
            if (uiHover.AsObject<HoverItemIcon>().ToolTipType != ToolTipType.ItemInChat)
            {
                var inventoryItemIcon = uiHover.AsObject<NormalInventoryItem>();
                var tooltip = inventoryItemIcon.Tooltip;
                var poeEntity = inventoryItemIcon.Item;
                if (tooltip != null && poeEntity.Address != 0 && poeEntity.IsValid)
                {
                    var item = inventoryItemIcon.Item;
                    var baseItemType = GameController.Files.BaseItemTypes.Translate(item.Path);
                    if (baseItemType != null)
                    {
                        await CoppiedHoverItemToBuffer();
                        var itemText = GetClipboardText();
                        if (Settings.Debug)
                        {
                            LogMsg($"Copied: {itemText} \n");
                        }
                        if (itemText == null || itemText == "")
                        {
                            return (null, null);
                        }
                        return (new CustomItemData(inventoryItemIcon), itemText);
                    }
                }
            }
        }
        catch
        {
            return (null, null);
        }
        return (null, null);
    }

    private static async SyncTask<bool> Wait()
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromMilliseconds(50))
        {
            await TaskUtils.NextFrame();
        }
        return true;
    }
}

