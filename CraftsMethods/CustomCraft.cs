using System.Numerics;
using ImGuiNET;
using RegexCrafter.Helpers;

namespace RegexCrafter.CraftsMethods;

public class CustomCraftState : CraftState
{
}

public class CustomCraft(RegexCrafter core) : CraftBase<CustomCraftState>(core)
{
    private const string LogName = "Custom Craft";
    protected override CustomCraftState CurrentState { get; set; } = new();
    public override string Name { get; } = "Custom Craft";
    public override void DrawSettings()
    {
        base.DrawSettings();
        ImGui.Separator();

        ImGui.Dummy(new Vector2(0, 10));

        var baseUseConditionTemp = CurrentState.Recipe.BaseUseCondition;
        if (ImGui.InputText("Base Use Condition", ref baseUseConditionTemp, 1024))
        {
            CurrentState.Recipe.BaseUseCondition = baseUseConditionTemp;
        }

        ImGui.Dummy(new Vector2(0, 20));
        var removeIndex = -1;
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
                removeIndex = i;
            }
        }

        if (removeIndex >= 0)
        {
            CurrentState.Recipe.MainConditions.RemoveAt(removeIndex);
        }

        if (CurrentState.Recipe.MainConditions.Count == 0)
        {
            CurrentState.Recipe.AddMainCondition(string.Empty);
        }

        if (ImGui.Button("Add main condition"))
        {
            CurrentState.Recipe.AddMainCondition(string.Empty);
        }

        ImGui.Dummy(new Vector2(0, 20));
        ImGui.LabelText("##CurrencyUseLabel", "Currency use steps:");
        ImGui.Dummy(new Vector2(0, 10));

        for (var i = 0; i < CurrentState.Recipe.CraftSteps.Count; i++)
        {
            if (ImGui.Button($"X###{i}"))
            {
                CurrentState.Recipe.RemoveStepAt(i);
                continue;
            }

            ImGui.SameLine();
            if (ImGui.ArrowButton($"UpArrow##{i}", ImGuiDir.Up))
            {
                if (i > 0)
                {
                    (CurrentState.Recipe.CraftSteps[i - 1], CurrentState.Recipe.CraftSteps[i]) = (CurrentState.Recipe.CraftSteps[i], CurrentState.Recipe.CraftSteps[i - 1]);
                }
            }

            ImGui.SameLine();
            if (ImGui.ArrowButton($"DownArrow##{i}", ImGuiDir.Down))
            {
                if (i < CurrentState.Recipe.CraftSteps.Count - 1)
                {
                    (CurrentState.Recipe.CraftSteps[i + 1], CurrentState.Recipe.CraftSteps[i]) = (CurrentState.Recipe.CraftSteps[i], CurrentState.Recipe.CraftSteps[i + 1]);
                }
            }

            ImGui.SameLine();

            var isOpen = ImGui.CollapsingHeader($"Currency {i + 1}: {CurrentState.Recipe.CraftSteps[i].Currency}###Header_{i}");
            if (isOpen)
            {
                ImGui.Dummy(new Vector2(0, 10));

                var currencyNameTemp = CurrentState.Recipe.CraftSteps[i].Currency;
                if (ImGui.InputText($"CurrencyName###CurrencyName_{i}", ref currencyNameTemp, 1024))
                {
                    CurrentState.Recipe.CraftSteps[i].Currency = currencyNameTemp;
                }

                var currUseConditionTemp = CurrentState.Recipe.CraftSteps[i].UseCondition;
                if (ImGui.InputText($"CurrencyUseCondition###CurrencyUseCond_{i}", ref currUseConditionTemp,
                        1024))
                {
                    CurrentState.Recipe.CraftSteps[i].UseCondition = currUseConditionTemp;
                }

                var stopUseConditionTemp = CurrentState.Recipe.CraftSteps[i].StopUseCondition;
                if (ImGui.InputText($"StopUseCondition###StopUseCond_{i}", ref stopUseConditionTemp, 1024))
                {
                    CurrentState.Recipe.CraftSteps[i].StopUseCondition = stopUseConditionTemp;
                }

                var useOnlyOneTimeTemp = CurrentState.Recipe.CraftSteps[i].IsOneTimeUse;
                if (ImGui.Checkbox($"UseOnlyOneTime###UseOnlyOneTime_{i}", ref useOnlyOneTimeTemp))
                {
                    CurrentState.Recipe.CraftSteps[i].IsOneTimeUse = useOnlyOneTimeTemp;
                }
            }
        }

        ImGui.Dummy(new Vector2(0, 20));
        if (ImGui.Button("Add currency use ##AddCurrencyUse"))
        {
            CurrentState.Recipe.AddStep(new CraftStep());
        }
    }
}
