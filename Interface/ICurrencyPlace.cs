﻿using ExileCore.Shared;

namespace RegexCrafter.Interface;

public interface ICurrencyPlace
{
    SyncTask<bool> HasCurrencyAsync(string currency);
    SyncTask<bool> TakeCurrencyForUseAsync(string currency);
    bool HasCurrency(string currency);
}
