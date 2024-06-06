
using System.Collections.Generic;
using System.Threading;
using ExileCore.Shared;
using RegexCrafter.Utils;
using Newtonsoft.Json;
using System.IO;
using ImGuiNET;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using ExileCore.Shared.Enums;


namespace RegexCrafter.Methods;

public class CraftState
{
	public string Name = string.Empty;
	public List<string> RegexPatterns = [];
}
public interface ICraft
{
	string Name { get; }
	void DrawSettings();
	List<CustomItemData> BadItems { get; }
	List<CustomItemData> DoneCraftItem { get; }
	SyncTask<bool> Start(CancellationToken ct);
	bool PreCraftCheck();
}

public abstract class Craft<State> : ICraft where State : CraftState
{
	private static RegexCrafter Core;
	public static Settings Settings => Core.Settings;
	public abstract State CraftState { get; set; }
	public abstract string Name { get; }
	public List<CustomItemData> BadItems { get; } = [];
	public List<CustomItemData> DoneCraftItem { get; } = [];
	public List<State> StateList = [];
	private int _stateIndex = 0;
	public string PathFileState
	{
		get { return Path.Combine(Core.ConfigDirectory, Name); }
	}

	public Craft(RegexCrafter core)
	{
		Core = core;
		StateList = GetFileState();
	}
	public void UpdateState(State state)
	{


		if (StateList.Any(x => x.Name == state.Name))
		{
			StateList.FirstOrDefault(x => x.Name == state.Name).RegexPatterns = state.RegexPatterns;
		}
		else
		{
			StateList.Add(state);
		}
		File.WriteAllText(PathFileState, JsonConvert.SerializeObject(StateList, Formatting.Indented));
		Core.LogMessage($"Update file: {PathFileState}");
	}
	private void CreateFile()
	{
		if (Path.Exists(PathFileState)) return;

		File.WriteAllText(PathFileState, JsonConvert.SerializeObject(new List<State>(), Formatting.Indented));
		Core.LogMessage($"Created file: {PathFileState}");
	}
	private List<State> GetFileState()
	{
		if (Path.Exists(PathFileState))
		{
			return JsonConvert.DeserializeObject<List<State>>(File.ReadAllText(PathFileState));
		}
		CreateFile();
		return [];
	}
	public virtual void DrawSettings()
	{
		var stateNameList = StateList.Select(x => x.Name).ToArray();
		_ = ImGui.Combo("States", ref _stateIndex, stateNameList, StateList.Count);
		ImGui.SameLine();
		if (ImGui.Button("Load State"))
		{
			CraftState = StateList[_stateIndex];
		}
		ImGui.SameLine();
		if (ImGui.Button("Remove State"))
		{
			StateList.RemoveAt(_stateIndex);
		}
		ImGui.Separator();
		_ = ImGui.InputText("State Name", input: ref CraftState.Name, 100);
		if (ImGui.Button("Save Current State"))
		{
			UpdateState(CraftState);
		}
		ImGui.Separator();
		if (CraftState.RegexPatterns.Count == 0)
		{
			CraftState.RegexPatterns.Add(string.Empty);
		}

		var tempPatterns = new List<string>(CraftState.RegexPatterns);
		for (int i = 0; i < tempPatterns.Count; i++)
		{
			string pattern = tempPatterns[i];
			ImGui.InputText($"Your regex pattern {i}", ref pattern, 1024);
			ImGui.SameLine();
			tempPatterns[i] = pattern;
			if (ImGui.Button("Remove"))
			{
				tempPatterns.RemoveAt(i);
			}
		}
		CraftState.RegexPatterns = tempPatterns;

		if (ImGui.Button("Add Regex Pattern"))
		{
			CraftState.RegexPatterns.Add(string.Empty);
		}

	}
	public bool PreCraftCheck()
	{
		if (CraftState.RegexPatterns.Count == 0)
		{
			Core.LogError("Regex pattern is empty.");
			return false;
		}
		foreach (var pattern in CraftState.RegexPatterns)
		{
			if (string.IsNullOrEmpty(pattern))
			{
				Core.LogError("Regex pattern is empty.");
				return false;
			}
		}

		if (Stash.IsPublicVisibleTab)
		{
			Core.LogError("Currency stash tab is public. Please switch to a private.");
			return false;
		}
		if (Stash.InventoryType != InventoryType.CurrencyStash)
		{
			Core.LogError("Open Currency stash tab.");
			return false;
		}
		return true;
	}
	public (string[] Exclude, string[] Include) ParsedPattern(string sourceRegex)
	{
		var matches = new Regex(@"[\""].+?[\""]|[^ ]+").Matches(sourceRegex)
							 .Cast<Match>()
							 .Select(m => m.Value)
							 .ToArray();

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
		return (exclude.ToArray(), include.ToArray());


	}
	public bool RegexCondition((CustomItemData Item, string Text) hoverItem)
	{
		foreach (var pattern in CraftState.RegexPatterns)
		{
			var (Exclude, Include) = ParsedPattern(pattern);
			if (RegexUtils.MatchesAnyPattern(hoverItem.Text, Exclude, out var applyPatterns))
			{
				if (Core.Settings.Debug)
				{
					Core.LogMessage($"Excluded: {string.Join(", ", applyPatterns)} \n");
				}
				return false;
			}
			else if (RegexUtils.MatchesAllPatterns(hoverItem.Text, Include, out var applyPatterns2))
			{
				if (Core.Settings.Debug)
				{
					Core.LogMsg($"Included: {string.Join(", ", applyPatterns2)} \n");
				}
				DoneCraftItem.Add(hoverItem.Item);
				return true;
			}
		}
		return false;
	}
	public abstract SyncTask<bool> Start(CancellationToken ct);
	public override string ToString()
	{
		return $"{Name}";
	}

}