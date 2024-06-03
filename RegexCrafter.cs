using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using RegexCrafter.Methods;
using RegexCrafter.Utils;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RegexCrafter;


public partial class RegexCrafter : BaseSettingsPlugin<Settings>
{

    private static readonly List<Craft> _craftList = [new Map()];
    private static List<string> CraftNames => _craftList.Select(x => x.Name).ToList();
    private int _craftIndex = 0;
    private Craft _currentCraft = _craftList[0];
    private string RegexInputPattern = string.Empty;

    [GeneratedRegex(@"[\""].+?[\""]|[^ ]+")]
    private static partial Regex MyRegex();
    public (string[] Exclude, string[] Include) ParsedPattern
    {
        get
        {
            var matches = MyRegex().Matches(RegexInputPattern)
                                 .Cast<Match>()
                                 .Select(m => m.Value)
                                 .ToArray();

            string[] parts = RegexInputPattern.Split("\"", StringSplitOptions.None);

            List<string> exclude = [], include = [];

            foreach (string part in matches)
            {
                string trimmedPart = part.Trim('"');
                if (trimmedPart.StartsWith('!'))
                {
                    exclude.Add(trimmedPart[1..]);
                }
                else if (!string.IsNullOrWhiteSpace(trimmedPart))
                {
                    include.Add(trimmedPart);
                }
            }
            // foreach (string part in matches)
            // {
            //     string trimmedPart = part.Trim();
            //     if (trimmedPart.StartsWith('!'))
            //     {
            //         exclude.Add(trimmedPart[1..]);
            //     }
            //     else if (part != " " && part != "")
            //     {
            //         include.Add(trimmedPart);
            //     }
            // }
            return (exclude.ToArray(), include.ToArray());

        }
    }
    private readonly CancellationTokenSource _cts = new();
    private SyncTask<bool> _currentOperation;


    public override bool Initialise()
    {
        Name = "RegexCrafter";

        CustomItemData.Init(this);
        Stash.Init(this);
        Craft.Init(this);
        Scripts.Init(this);

        return base.Initialise();
    }
    public override void DrawSettings()
    {
        base.DrawSettings();
        if (ImGui.Combo("Craft Method", ref _craftIndex, [.. CraftNames], CraftNames.Count))
        {
            _currentCraft = _craftList.Any(x => x.Name == CraftNames[_craftIndex]) ? _currentCraft : _craftList[0];

        };
        _currentCraft.DrawSettings();
        ImGui.InputText("Your regex pattern", ref RegexInputPattern, 1024);
        ImGui.Text($"Exclude: {string.Join(", ", ParsedPattern.Exclude)}");
        ImGui.Text($"Include: {string.Join(", ", ParsedPattern.Include)}");
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
        if (!Stash.IsVisible)
        {
            _currentCraft.BadItems.Clear();
            _currentCraft.DoneCraftItem.Clear();
            return;
        }
        foreach (var item in _currentCraft.DoneCraftItem)
        {
            Graphics.DrawFrame(item.Position, Color.Green, 2);
        }
        foreach (var item in _currentCraft.BadItems)
        {
            Graphics.DrawFrame(item.Position, Color.Red, 2);
        }

        if (Input.IsKeyDown(Settings.StopCraftHotKey.Value) || !Stash.IsVisible)
        {
            _cts.Cancel();
        }
        if (Input.IsKeyDown(Settings.StartCraftHotKey.Value) && _currentOperation == null)
        {
            if (PreCraftCheck())
            {
                _currentOperation = _currentCraft.Start(_cts.Token);
            }
        }
    }




    private bool PreCraftCheck()
    {
        if (string.IsNullOrEmpty(RegexInputPattern))
        {
            LogError("Regex pattern is empty.");
            return false;
        }
        if (Stash.IsPublicVisibleTab)
        {
            LogError("Currency stash tab is public. Please switch to a private.");
            return false;
        }
        if (Stash.InventoryType != InventoryType.CurrencyStash)
        {
            LogError("Open Currency stash tab.");
            return false;
        }
        return true;
    }


}



