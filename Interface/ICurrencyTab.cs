using System;
using ExileCore.Shared;
using RegexCrafter.Enums;

namespace RegexCrafter.Interface;

public interface ICurrencyTab
{
    SyncTask<bool> SwitchCurrencyTab(CurrencyTabType typeButton);
    SyncTask<bool> SwitchCurrencyTab();
    CurrencyTabType GetCurrentCurrencyTabType();
}
