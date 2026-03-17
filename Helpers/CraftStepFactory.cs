namespace RegexCrafter.Helpers
{
    public static class CraftStepFactory
    {
        public static CraftStep ScouringStep { get; } = new CraftStep { Currency = CurrencyNames.OrbOfScouring, UseCondition = "\"rare|magic\"", StopUseCondition = "\"normal\"" };

        public static CraftStep OnlyMagicScouringStep { get; } = new CraftStep { Currency = CurrencyNames.OrbOfScouring, UseCondition = "\"magic\"", StopUseCondition = "\"rarity:.*normal\"" };

        public static CraftStep ChaosSpamStep { get; } = new CraftStep { Currency = CurrencyNames.ChaosOrb, UseCondition = "\"rare\"", StopUseCondition = "\"!rare\"" };

        public static CraftStep OnlyRareScouringStep { get; } = new CraftStep { Currency = CurrencyNames.OrbOfScouring, UseCondition = "\"rare\"", StopUseCondition = "\"!rare\"" };

        public static CraftStep AlterationStep { get; } = new CraftStep { Currency = CurrencyNames.OrbOfAlteration, UseCondition = "\"magic\"", StopUseCondition = "\"!magic\"" };

        public static CraftStep AlchemyStep { get; } = new CraftStep { Currency = CurrencyNames.OrbOfAlchemy, UseCondition = "\"normal\"", StopUseCondition = "\"rare\"" };

        public static CraftStep TransmutationStep { get; } = new CraftStep { Currency = CurrencyNames.OrbOfTransmutation, UseCondition = "\"normal\"", StopUseCondition = "\"magic\"" };

        public static CraftStep ChiselOfAvariceStep { get; } = new CraftStep
        {
            Currency = CurrencyNames.ChiselOfAvarice,
            UseCondition = "\"!urr.*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"urr.*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };

        public static CraftStep ChiselOfDivinationStep { get; } = new CraftStep
        {
            Currency = CurrencyNames.ChiselOfDivination,
            UseCondition = "\"!div.*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"div.*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };

        public static CraftStep ChiselOfProcurementStep { get; } = new CraftStep
        {
            Currency = CurrencyNames.ChiselOfProcurement,
            UseCondition = "\"!ty\\).*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"ty\\).*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };

        public static CraftStep ChiselOfScarabsStep { get; } = new CraftStep
        {
            Currency = CurrencyNames.ChiselOfScarabs,
            UseCondition = "\"!sca.*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"sca.*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };

        public static CraftStep ChiselOfProliferationStep { get; } = new CraftStep
        {
            Currency = CurrencyNames.ChiselOfProliferation,
            UseCondition = "\"!ze\\).*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"ze\\).*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };
    }
}
