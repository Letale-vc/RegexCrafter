using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using RegexCrafter.Helpers.Enums;

namespace RegexCrafter;

public class Settings : ISettings
{
    public ToggleNode Debug { get; set; } = new(false);
    public HotkeyNode StartCraftHotKey { get; set; } = new(Keys.F9);
    public HotkeyNode StopCraftHotKey { get; set; } = new(Keys.Delete);
    public TabSettings TabSettings { get; set; } = new();
    public CraftPlaceType CraftPlace { get; set; } = 0;
    public ToggleNode Enable { get; set; } = new(true);
}

public class TabSettings
{
    public string CurrencyTab { get; set; } = string.Empty;
    public string DeliriumTab { get; set; } = string.Empty;

    // public string DumpTab { get; set; } = string.Empty;
    // public string DelveTab { get; set; } = string.Empty;
    public string WhereCraftItemTab { get; set; } = string.Empty;
    // public string WhereDoneItemTab { get; set; } = string.Empty;
}