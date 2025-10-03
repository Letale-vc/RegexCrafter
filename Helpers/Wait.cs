using System;
using System.Diagnostics;
using System.Threading;
using ExileCore;
using ExileCore.Shared;

namespace RegexCrafter.Helpers;

public class Wait
{
    private const string LogName = "Wait";

    public Wait(GameController gc)
    {
        Gc = gc;
    }

    private GameController Gc { get; }
    private int Latency => Gc.Game.IngameState.ServerData.Latency;

    public SyncTask<bool> SleepSafe(int min, int max, CancellationToken ct = default)
    {
        var delay = Latency > max ? Latency : new Random().Next(min, max);
        GlobalLog.Debug($"[{LogName}]  {delay} ms.", LogName);
        return Sleep(delay, ct);
    }

    public SyncTask<bool> SleepSafe(int ms, CancellationToken ct = default)
    {
        var delay = Math.Max(Latency, ms);
        return Sleep(delay, ct);
    }

    public SyncTask<bool> Sleep(int min, int max, CancellationToken ct = default)
    {
        var delay = new Random().Next(min, max);
        return Sleep(delay, ct);
    }

    public SyncTask<bool> LatencySleep(CancellationToken ct = default)
    {
        var timeout = Math.Max((int)(Latency * 1.15), 35);
        return Sleep(timeout, ct);
    }

    public async SyncTask<bool> For(Func<bool> condition, string desc, int timeout = 3000,
        CancellationToken ct = default)
    {
        if (condition())
            return true;

        var timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < timeout)
        {
            ct.ThrowIfCancellationRequested();
            GlobalLog.Info(
                $"Waiting for {desc} ({Math.Round(timer.ElapsedMilliseconds / 1000f, 2)}/{timeout / 1000f}).", LogName);
            await TaskUtils.NextFrame();
            if (condition())
                return true;
        }

        GlobalLog.Error($"Wait for {desc} timeout.", LogName);
        return false;
    }

    public async SyncTask<bool> Sleep(int ms, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        GlobalLog.Debug($"Wait {ms} ms.", LogName);
        while (sw.Elapsed < TimeSpan.FromMilliseconds(ms))
        {
            ct.ThrowIfCancellationRequested();
            await TaskUtils.NextFrame();
        }

        return true;
    }
}