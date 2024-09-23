namespace RegexCrafter.Utils;
using System.Collections.Generic;

public struct CurrencyNames
{
	public const string ChaosOrb = "Chaos Orb";
	public const string OrbOfScouring = "Orb of Scouring";
	public const string OrbOfAlchemy = "Orb of Alchemy";
	public const string CartographersChisel = "Cartographer's Chisel";
	public const string ExaltedOrb = "Exalted Orb";
	public const string DivineOrb = "Divine Orb";
	public const string ScrollOfWisdom = "Scroll of Wisdom";
	public const string ChiselOfAvarice = "Maven's Chisel of Avarice";
	public const string ChiselOfDivination = "Maven's Chisel of Divination";
	public const string ChiselOfProcurement = "Maven's Chisel of Procurement";
	public const string ChiselOfScarabs = "Maven's Chisel of Scarabs";
	public const string ChiselOfProliferation = "Maven's Chisel of Proliferation";

	private static readonly Dictionary<string, CurrencyType> _currencyTypes = new Dictionary<string, CurrencyType>
	{
		{ChaosOrb, CurrencyType.General},
		{OrbOfAlchemy, CurrencyType.General},
		{ExaltedOrb, CurrencyType.General},
		{OrbOfScouring, CurrencyType.General},
		{CartographersChisel, CurrencyType.General},
		{DivineOrb, CurrencyType.General},
		{ScrollOfWisdom, CurrencyType.General},
		{ChiselOfAvarice, CurrencyType.Exotic},
		{ChiselOfDivination, CurrencyType.Exotic},
		{ChiselOfProcurement, CurrencyType.Exotic},
		{ChiselOfScarabs, CurrencyType.Exotic},
		{ChiselOfProliferation, CurrencyType.Exotic}
	};
	public static CurrencyType GetCurrencyType(string currencyName)
	{
		return _currencyTypes.GetValueOrDefault(currencyName);
	}
}



public enum CurrencyType
{
	General,
	Exotic
}
