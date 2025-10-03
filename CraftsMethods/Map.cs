using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RegexCrafter.Helpers;
using RegexCrafter.Helpers.Enums;

namespace RegexCrafter.CraftsMethods;

public class MapState : CraftState
{
    public int TypeChisel;
    public bool UseAddQuality;
    public CurrencyMethodCraftType CurrencyMethodCraftType { get; set; } = CurrencyMethodCraftType.Chaos;
}

public class Map(RegexCrafter core) : CraftBase<MapState>(core)
{
    private const string LogName = "CraftMap";

    private const string BaseUseCondition = "\"!corrupted|currency\" \"map\"";

    private readonly string[] _chiselList = CurrencyNames.GetChiselNames().ToArray();

    private readonly CurrencyMethodCraftType[] _typeMethodCraft =
    [
        CurrencyMethodCraftType.Chaos,
        CurrencyMethodCraftType.ScouringAndAlchemy
    ];

    protected override MapState CurrentState { get; set; } = new();
    public override string Name { get; } = "Map";

    public override void DrawSettings()
    {
        base.DrawSettings();
        var selectedMethod = (int)CurrentState.CurrencyMethodCraftType;
        if (ImGui.Combo("Type Method craft", ref selectedMethod,
                _typeMethodCraft.Select(x => x.GetDescription()).ToArray(), _typeMethodCraft.Length))
        {
            CurrentState.CurrencyMethodCraftType = (CurrencyMethodCraftType)selectedMethod;
        }

        ImGui.Checkbox("Use Add Quality", ref CurrentState.UseAddQuality);

        if (CurrentState.UseAddQuality)
        {
            ImGui.SameLine();
            ImGui.Combo("Type Chisel", ref CurrentState.TypeChisel, _chiselList, _chiselList.Length);
        }

        ImGui.Separator();
        if (CurrentState.Recipe.MainConditions.Count == 0)
        {
            CurrentState.Recipe.AddMainCondition(string.Empty);
        }

        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
        ImGui.LabelText("##MainConditionsMap", "Main Conditions");

        var indexToRemove = -1;
        for (var i = 0; i < CurrentState.Recipe.MainConditions.Count; i++)
        {
            var patternTemp = CurrentState.Recipe.MainConditions[i];
            if (ImGui.InputText($"Your regex pattern {i}", ref patternTemp, 1024))
            {
                CurrentState.Recipe.MainConditions[i] = patternTemp;
            }

            ImGui.SameLine();
            if (ImGui.Button($"Remove##{i}"))
            {
                indexToRemove = i;
            }
            //tempPatternList.Add(i);
        }

        if (indexToRemove >= 0)
        {
            CurrentState.Recipe.MainConditions.RemoveAt(indexToRemove);
        }

        if (ImGui.Button("Add Regex Pattern"))
        {
            CurrentState.Recipe.AddMainCondition(string.Empty);
        }
    }

    private void ApplyMethodPreset()
    {
        switch (CurrentState.CurrencyMethodCraftType)
        {
            case CurrencyMethodCraftType.Chaos:
                GlobalLog.Debug("Load Chaos spam preset.", LogName);
                LoadChaosSpam();
                break;
            case CurrencyMethodCraftType.ScouringAndAlchemy:
                GlobalLog.Debug("Load Scouring and Alchemy preset.", LogName);
                LoadScouringAndAlchemy();
                break;
            case CurrencyMethodCraftType.AlterationSpam:
                GlobalLog.Debug("Load Alteration spam preset.", LogName);
                LoadAlterationSpam();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ApplyQualityStep()
    {
        if (CurrentState.Recipe.CraftSteps[0].Currency == _chiselList[CurrentState.TypeChisel])
        {
            return;
        }

        if (_chiselList.Contains(CurrentState.Recipe.CraftSteps[0].Currency))
        {
            CurrentState.Recipe.RemoveStepAt(0);
        }

        var chiselStep = _chiselList[CurrentState.TypeChisel] switch
        {
            CurrencyNames.CartographersChisel => CraftStepFactory.GetCartographersChiselStep(),
            CurrencyNames.ChiselOfAvarice => CraftStepFactory.GetChiselOfAvariceStep(),
            CurrencyNames.ChiselOfDivination => CraftStepFactory.GetChiselOfDivinationStep(),
            CurrencyNames.ChiselOfProcurement => CraftStepFactory.GetChiselOfProcurementStep(),
            CurrencyNames.ChiselOfScarabs => CraftStepFactory.GetChiselOfScarabsStep(),
            CurrencyNames.ChiselOfProliferation => CraftStepFactory.GetChiselOfProliferationStep(),
            _ => throw new ArgumentOutOfRangeException()
        };

        CurrentState.Recipe.InsertStep(chiselStep);
    }

    protected override void BeforeStart()
    {
        if (!CurrentState.Recipe.BaseUseCondition.Contains(BaseUseCondition))
        {
            CurrentState.Recipe.BaseUseCondition = BaseUseCondition;
        }

        ApplyMethodPreset();
        ApplyQualityStep();
    }

    private void LoadScouringAndAlchemy()
    {
        CurrentState.Recipe.RemoveAllSteps();
        var scouringStep = CraftStepFactory.GetScouringStep();
        var alchemyStep = CraftStepFactory.GetAlchemyStep();
        CurrentState.Recipe.AddRangeStep([scouringStep, alchemyStep]);
    }

    private void LoadAlterationSpam()
    {
        CurrentState.Recipe.RemoveAllSteps();
        var onlyRareScouringStep = CraftStepFactory.GetOnlyRareScouringStep();
        var transmutationStep = CraftStepFactory.GetTransmutationStep();
        var alterationStep = CraftStepFactory.GetAlterationStep();
        CurrentState.Recipe.AddRangeStep([onlyRareScouringStep, transmutationStep, alterationStep]);
    }

    private void LoadChaosSpam()
    {
        CurrentState.Recipe.RemoveAllSteps();
        var scouringStep = CraftStepFactory.GetOnlyMagicScouringStep();
        var alchemyStep = CraftStepFactory.GetAlchemyStep();
        var chaosStep = CraftStepFactory.GetChaosSpamStep();
        CurrentState.Recipe.AddRangeStep([scouringStep, alchemyStep, chaosStep]);
    }
}
