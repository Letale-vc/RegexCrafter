using ExileCore;

namespace RegexCrafter.Helpers;

internal static class GlobalLog
{
    private static RegexCrafter _core;
    public static void Init(RegexCrafter core)
    {
        _core = core;
    }

    public static void Debug(string message, string logName)
    {
        if (_core.Settings.Debug)
            _core.LogMsg($"[{logName}] {message}");
    }

    public static void Info(string message, string logName)
    {
        _core.LogMessage($"[{logName}] {message}");
    }

    public static void Error(string message, string logName)
    {
        _core.LogError($"[{logName}] {message}");
    }
}
