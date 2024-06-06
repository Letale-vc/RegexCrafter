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
using System.Threading.Tasks;

namespace RegexCrafter;


public partial class RegexCrafter : BaseSettingsPlugin<Settings>
{

	private static readonly List<ICraft> _craftList = [];
	private int _craftIndex = 0;
	private string RegexInputPattern = string.Empty;
	private CancellationTokenSource _cts;
	private SyncTask<bool> _currentOperation;


	public override bool Initialise()
	{
		Name = "RegexCrafter";

		CustomItemData.Init(this);
		Stash.Init(this);
		PlayerInventory.Init(this);
		Scripts.Init(this);

		_craftList.Add(new Map(this));
		return base.Initialise();
	}
	public override void DrawSettings()
	{
		base.DrawSettings();
		ImGui.Spacing();
		if (_craftList.Count != 0)
		{ ImGui.Combo("Craft Method", ref _craftIndex, _craftList.Select(x => x.Name).ToArray(), _craftList.Count); }
		ImGui.Spacing();
		_craftList[_craftIndex].DrawSettings();
	}

	public override void Render()
	{
		if (_currentOperation != null)
		{
			DebugWindow.LogMsg("Craft is running...");
			TaskUtils.RunOrRestart(ref _currentOperation, () =>
			{
				return null;
			});
		}
		if (!Stash.IsVisible)
		{
			_craftList[_craftIndex].BadItems.Clear();
			_craftList[_craftIndex].DoneCraftItem.Clear();
			return;
		}
		foreach (var item in _craftList[_craftIndex].DoneCraftItem)
		{
			Graphics.DrawFrame(item.Position, Color.Green, 2);
		}
		foreach (var item in _craftList[_craftIndex].BadItems)
		{
			Graphics.DrawFrame(item.Position, Color.Red, 2);
		}

		if (Input.IsKeyDown(Settings.StopCraftHotKey.Value) || !Stash.IsVisible)
		{
			_cts.Cancel();
		}
		if (Input.IsKeyDown(Settings.StartCraftHotKey.Value) && _currentOperation == null)
		{
			_craftList[_craftIndex].BadItems.Clear();
			_craftList[_craftIndex].DoneCraftItem.Clear();
			if (_craftList[_craftIndex].PreCraftCheck())
			{
				_cts = new CancellationTokenSource();
				_currentOperation = _craftList[_craftIndex].Start(_cts.Token);
			}
		}
	}

}



