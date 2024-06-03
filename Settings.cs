using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;

namespace RegexCrafter;

public class Settings : ISettings
{

	public ToggleNode Enable { get; set; } = new ToggleNode(true);
	public ToggleNode Debug { get; set; } = new ToggleNode(false);
	public HotkeyNode StartCraftHotKey { get; set; } = new(Keys.F9);
	public HotkeyNode StopCraftHotKey { get; set; } = new(Keys.Delete);
	public ToggleNode UseRandomPosition { get; set; } = new ToggleNode(true);
}