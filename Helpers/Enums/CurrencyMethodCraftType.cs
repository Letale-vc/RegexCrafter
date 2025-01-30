using System;
using System.ComponentModel;

namespace RegexCrafter.Helpers;

public enum CurrencyMethodCraftType
{
    [Description("Chaos Orb")] Chaos,
    [Description("Scouring and Alchemy")] ScouringAndAlchemy
}

// public static class EnumExtensions
// {
//     public static string GetDescription(this Enum enumerationValue)
//     {
//         var type = enumerationValue.GetType();
//         if (!type.IsEnum)
//             throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
//
//         //Tries to find a DescriptionAttribute for a potential friendly name
//         //for the enum
//         var memberInfo = type.GetMember(enumerationValue.ToString());
//
//         if (memberInfo is not { Length: > 0 }) return enumerationValue.ToString();
//         var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
//
//         return attrs is { Length: > 0 }
//             ?
//             //Pull out the description value
//             ((DescriptionAttribute)attrs[0]).Description
//             :
//             //If we have no description attribute, just return the ToString of the enum
//             enumerationValue.ToString();
//     }
// }