using System;
using System.Collections.Generic;
using System.Reflection;

namespace RegexCrafter.Helpers
{
    public enum CurrencyTabType
    {
        None,
        General,
        Atlas,
        League
    }

    public static class CurrencyNames
    {
        [Currency(CurrencyTabType.General)] public const string ChaosOrb = "Chaos Orb";

        [Currency(CurrencyTabType.General)] public const string OrbOfAlteration = "Orb of Alteration";

        // add transmutation, augmentation, regal
        [Currency(CurrencyTabType.General)] public const string OrbOfTransmutation = "Orb of Transmutation";

        [Currency(CurrencyTabType.General)] public const string OrbOfAugmentation = "Orb of Augmentation";

        [Currency(CurrencyTabType.General)] public const string RegalOrb = "Regal Orb";

        [Currency(CurrencyTabType.General)] public const string OrbOfScouring = "Orb of Scouring";

        [Currency(CurrencyTabType.General)] public const string OrbOfAlchemy = "Orb of Alchemy";

        [Currency(CurrencyTabType.General)] public const string ExaltedOrb = "Exalted Orb";

        [Currency(CurrencyTabType.General)] public const string DivineOrb = "Divine Orb";

        [Currency(CurrencyTabType.General)] public const string ScrollOfWisdom = "Scroll of Wisdom";

        [Currency(CurrencyTabType.Atlas)] public const string ChiselOfAvarice = "Maven's Chisel of Avarice";

        [Currency(CurrencyTabType.Atlas)] public const string ChiselOfDivination = "Maven's Chisel of Divination";

        [Currency(CurrencyTabType.Atlas)] public const string ChiselOfProcurement = "Maven's Chisel of Procurement";

        [Currency(CurrencyTabType.Atlas)] public const string ChiselOfScarabs = "Maven's Chisel of Scarabs";

        [Currency(CurrencyTabType.Atlas)] public const string ChiselOfProliferation = "Maven's Chisel of Proliferation";

        private static Dictionary<string, CurrencyTabType> _currencyTypeCache;

        public static CurrencyTabType GetCurrencyType(string currencyName)
        {
            if (_currencyTypeCache != null)
            {
                return _currencyTypeCache.GetValueOrDefault(currencyName, CurrencyTabType.None);
            }
            _currencyTypeCache = [];

            // find all fields with CurrencyAttribute
            var fields = typeof(CurrencyNames).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                // get CurrencyAttribute from field
                var attribute = (CurrencyAttribute)Attribute.GetCustomAttribute(field, typeof(CurrencyAttribute));
                if (attribute == null)
                {
                    continue;
                }
                // get value of field
                var fieldValue = field.GetValue(null)?.ToString();
                if (fieldValue != null)
                // add to cache
                {
                    _currencyTypeCache[fieldValue] = attribute.CurrencyType;
                }
            }

            return _currencyTypeCache.GetValueOrDefault(currencyName, CurrencyTabType.None);
        }

        public static List<string> GetChiselNames()
        {
            return
            [
                ChiselOfProliferation, ChiselOfProcurement,
                ChiselOfScarabs, ChiselOfDivination, ChiselOfAvarice
            ];
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class CurrencyAttribute(CurrencyTabType currencyType) : Attribute
    {
        public CurrencyTabType CurrencyType { get; } = currencyType;
    }
}
