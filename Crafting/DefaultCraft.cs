using ImGuiNET;
using RegexCrafter.Enums;
using RegexCrafter.Helpers;
using System;
using System.Linq;
using System.Numerics;

namespace RegexCrafter.Crafting
{
    public class DefaultCraftState : CraftState
    {
        public CurrencyMethodCraftType CurrencyMethodCraftType { get; set; } = CurrencyMethodCraftType.Chaos;
    }

    public class DefaultCraft : CraftBase<DefaultCraftState>
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
        private readonly string[] _typeMethodCraftNames;

        public DefaultCraft(RegexCrafter core) : base(core)
        {
            // Инициализируем массив имен один раз при создании объекта
            _typeMethodCraftNames = _typeMethodCraft.Select(x => x.GetDescription()).ToArray();
        }
        public override void DrawSettings()
        {
            base.DrawSettings();
            var selectedIndex = Array.IndexOf(_typeMethodCraft, CurrentState.CurrencyMethodCraftType);

            if (ImGui.Combo("Type Method craft", ref selectedIndex,
                    _typeMethodCraftNames, _typeMethodCraftNames.Length))
            {
                CurrentState.CurrencyMethodCraftType = _typeMethodCraft[selectedIndex];
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
            var scouringStep = CraftStepFactory.ScouringStep;
            var alchemyStep = CraftStepFactory.AlchemyStep;
            CurrentState.Recipe.AddRangeStep([scouringStep, alchemyStep]);
        }

        private void LoadAlterationSpam()
        {
            CurrentState.Recipe.RemoveAllSteps();
            var onlyRareScouringStep = CraftStepFactory.OnlyRareScouringStep;
            var transmutationStep = CraftStepFactory.TransmutationStep;
            var alterationStep = CraftStepFactory.AlterationStep;
            CurrentState.Recipe.AddRangeStep([onlyRareScouringStep, transmutationStep, alterationStep]);
        }

        private void LoadChaosSpam()
        {
            CurrentState.Recipe.RemoveAllSteps();
            var scouringStep = CraftStepFactory.OnlyMagicScouringStep;
            var alchemyStep = CraftStepFactory.AlchemyStep;
            var chaosStep = CraftStepFactory.ChaosSpamStep;
            CurrentState.Recipe.AddRangeStep([scouringStep, alchemyStep, chaosStep]);
        }
    }
}
