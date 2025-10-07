using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RegexCrafter.Enums;
using RegexCrafter.Helpers;

namespace RegexCrafter.Crafting;

public class DefaultCraftState : CraftState
{
    public CurrencyMethodCraftType CurrencyMethodCraftType { get; set; } = CurrencyMethodCraftType.Chaos;
}

public class DefaultCraft(RegexCrafter core) : CraftBase<DefaultCraftState>(core)
{
    private const string BaseUseCondition = "\"!corrupted|currency\"";

    private readonly CurrencyMethodCraftType[] _typeMethodCraft =
    [
        CurrencyMethodCraftType.Chaos,
        CurrencyMethodCraftType.ScouringAndAlchemy,
        CurrencyMethodCraftType.AlterationSpam
    ];

    protected override DefaultCraftState CurrentState { get; set; } = new();
    public override string Name { get; } = "Default craft";

    public override void DrawSettings()
    {
        base.DrawSettings();
        var selectedMethod = (int)CurrentState.CurrencyMethodCraftType;
        if (ImGui.Combo("Type Method craft", ref selectedMethod,
                _typeMethodCraft.Select(x => x.GetDescription()).ToArray(), _typeMethodCraft.Length))
        {
            CurrentState.CurrencyMethodCraftType = (CurrencyMethodCraftType)selectedMethod;
            ApplyMethodPreset();
        }

        ImGui.Dummy(new Vector2(0, 10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 10));
        if (CurrentState.Recipe.MainConditions.Count == 0)
        {
            CurrentState.Recipe.AddMainCondition(string.Empty);
        }

        ImGui.Dummy(new Vector2(0, 20));

        var indexToRemove = -1;
        for (var i = 0; i < CurrentState.Recipe.MainConditions.Count; i++)
        {
            var patternTemp = CurrentState.Recipe.MainConditions[i];
            ImGui.InputText($"Your regex pattern {i}", ref patternTemp, 1024);
            CurrentState.Recipe.MainConditions[i] = patternTemp;
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
                LoadChaosSpam();
                break;
            case CurrencyMethodCraftType.ScouringAndAlchemy:
                LoadScouringAndAlchemy();
                break;
            case CurrencyMethodCraftType.AlterationSpam:
                LoadAlterationSpam();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void BeforeStart()
    {
        if (!CurrentState.Recipe.BaseUseCondition.Contains(BaseUseCondition))
        {
            CurrentState.Recipe.BaseUseCondition = BaseUseCondition;
        }

        ApplyMethodPreset();
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
