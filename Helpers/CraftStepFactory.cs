namespace RegexCrafter.Helpers;

public static class CraftStepFactory
{
    public static CraftStep GetScouringStep()
    {
        return new CraftStep { Currency = CurrencyNames.OrbOfScouring, UseCondition = "\"rare|magic\"", StopUseCondition = "\"normal\"" };
    }

    public static CraftStep GetOnlyMagicScouringStep()
    {
        return new CraftStep { Currency = CurrencyNames.OrbOfScouring, UseCondition = "\"magic\"", StopUseCondition = "\"rarity:.*normal\"" };
    }

    public static CraftStep GetChaosSpamStep()
    {
        return new CraftStep { Currency = CurrencyNames.ChaosOrb, UseCondition = "\"rare\"", StopUseCondition = "\"!rare\"" };
    }

    public static CraftStep GetOnlyRareScouringStep()
    {
        return new CraftStep { Currency = CurrencyNames.OrbOfScouring, UseCondition = "\"rare\"", StopUseCondition = "\"!rare\"" };
    }

    public static CraftStep GetAlterationStep()
    {
        return new CraftStep { Currency = CurrencyNames.OrbOfAlteration, UseCondition = "\"magic\"", StopUseCondition = "\"!magic\"" };
    }

    public static CraftStep GetAlchemyStep()
    {
        return new CraftStep { Currency = CurrencyNames.OrbOfAlchemy, UseCondition = "\"normal\"", StopUseCondition = "\"rare\"" };
    }

    public static CraftStep GetTransmutationStep()
    {
        return new CraftStep { Currency = CurrencyNames.OrbOfTransmutation, UseCondition = "\"normal\"", StopUseCondition = "\"magic\"" };
    }

    public static CraftStep GetCartographersChiselStep()
    {
        return new CraftStep
        {
            Currency = CurrencyNames.CartographersChisel,
            UseCondition = "\"!lity:.*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"lity:.*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };
    }

    public static CraftStep GetChiselOfAvariceStep()
    {
        return new CraftStep
        {
            Currency = CurrencyNames.ChiselOfAvarice, UseCondition = "\"!urr.*([2-9].|1..)%\" \"map\"", StopUseCondition = "\"!map\" \"urr.*([2-9].|1..)%\"", IsOneTimeUse = true
        };
    }

    public static CraftStep GetChiselOfDivinationStep()
    {
        return new CraftStep
        {
            Currency = CurrencyNames.ChiselOfDivination, UseCondition = "\"!div.*([2-9].|1..)%\" \"map\"", StopUseCondition = "\"!map\" \"div.*([2-9].|1..)%\"", IsOneTimeUse = true
        };
    }

    public static CraftStep GetChiselOfProcurementStep()
    {
        return new CraftStep
        {
            Currency = CurrencyNames.ChiselOfProcurement,
            UseCondition = "\"!ty\\).*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"ty\\).*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };
    }

    public static CraftStep GetChiselOfScarabsStep()
    {
        return new CraftStep
        {
            Currency = CurrencyNames.ChiselOfScarabs, UseCondition = "\"!sca.*([2-9].|1..)%\" \"map\"", StopUseCondition = "\"!map\" \"sca.*([2-9].|1..)%\"", IsOneTimeUse = true
        };
    }

    public static CraftStep GetChiselOfProliferationStep()
    {
        return new CraftStep
        {
            Currency = CurrencyNames.ChiselOfProliferation,
            UseCondition = "\"!ze\\).*([2-9].|1..)%\" \"map\"",
            StopUseCondition = "\"!map\" \"ze\\).*([2-9].|1..)%\"",
            IsOneTimeUse = true
        };
    }
}
