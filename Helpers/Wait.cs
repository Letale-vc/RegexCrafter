using System;
using System.Diagnostics;
using System.Threading;
using ExileCore;
using ExileCore.Shared;

namespace RegexCrafter.Helpers;

internal static class Wait
{
    private const string LogName = "Wait";
    private static RegexCrafter _core;

    private static GameController Gc => _core.GameController;
    private static int Latency => Gc.Game.IngameState.ServerData.Latency;
    private static CancellationToken CancellationToken => _core.Cts.Token;

    public static void Init(RegexCrafter core)
    {
        _core = core;
    }

    public static SyncTask<bool> SleepSafe(int min, int max)
    {
        var delay = Latency > max ? Latency : new Random().Next(min, max);
        GlobalLog.Debug($"[{LogName}]  {delay} ms.", LogName);
        return Sleep(delay);
    }

    public static SyncTask<bool> SleepSafe(int ms)
    {
        var delay = Math.Max(Latency, ms);
        return Sleep(delay);
    }

    public static SyncTask<bool> Sleep(int min, int max)
    {
        var delay = new Random().Next(min, max);
        return Sleep(delay);
    }

    public static SyncTask<bool> LatencySleep()
    {
        var timeout = Math.Max((int)(Latency * 1.15), 35);
        return Sleep(timeout);
    }

    public static async SyncTask<bool> For(Func<bool> condition, string desc, int timeout = 3000)
    {
        if (condition())
            return true;

        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < timeout)
        {
            CancellationToken.ThrowIfCancellationRequested();
            GlobalLog.Info(
                $"Waiting for {desc} ({Math.Round(timer.ElapsedMilliseconds / 1000f, 2)}/{timeout / 1000f}).", LogName);
            await TaskUtils.NextFrame();
            if (condition())
                return true;
        }

        GlobalLog.Error($"Wait for {desc} timeout.", LogName);
        return false;
    }

    public static async SyncTask<bool> Sleep(int ms)
    {
        var sw = Stopwatch.StartNew();
        GlobalLog.Debug($"Wait {ms} ms.", LogName);
        while (sw.Elapsed < TimeSpan.FromMilliseconds(ms))
        {
            CancellationToken.ThrowIfCancellationRequested();
            await TaskUtils.NextFrame();
        }

        return true;
    }
}