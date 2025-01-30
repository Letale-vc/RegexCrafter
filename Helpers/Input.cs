using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using InputHumanizer.Input;
using SharpDX;
using Vector3 = System.Numerics.Vector3;

namespace RegexCrafter.Helpers;

public static class Input
{
    private const string LogName = "Input";
    private static IInputController _inputController;
    private static RegexCrafter _core;
    private static readonly List<Keys> CurrentDownKeys = [];
    private static CancellationToken CancellationToken => _core.Cts.Token;
    private static GameController Gc => _core.GameController;
    private static Vector2 WindowOffset => _core.GameController.Window.GetWindowRectangleTimeCache.TopLeft;

    public static bool Init(RegexCrafter core)
    {
        _core = core;
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        _inputController = tryGetInputController(core.Name);

        if (_inputController != null) return true;
        GlobalLog.Error("InputHumanizer controller not found.", LogName);
        return false;
    }

    public static async SyncTask<bool> SimulateKeyEvent(Keys key, bool down = true, bool up = true,
        Keys extraKey = Keys.None)
    {
        using (_inputController)
        {
            if (down)
            {
                if (extraKey != Keys.None)
                {
                    GlobalLog.Debug($"KeyDown {extraKey}", LogName);
                    if (!await _inputController.KeyDown(extraKey, CancellationToken)) return false;
                    CurrentDownKeys.Add(extraKey);
                }

                GlobalLog.Debug($"KeyDown {key}", LogName);
                if (!await _inputController.KeyDown(key, CancellationToken)) return false;
                CurrentDownKeys.Add(key);
            }

            if (!up) return true;
            if (extraKey != Keys.None)
            {
                GlobalLog.Debug($"KeyUp {extraKey}", LogName);
                if (!await _inputController.KeyUp(extraKey, cancellationToken: CancellationToken)) return false;
                CurrentDownKeys.Remove(extraKey);
            }

            GlobalLog.Debug($"KeyUp {key}", LogName);
            if (!await _inputController.KeyUp(key, cancellationToken: CancellationToken)) return false;
            CurrentDownKeys.Remove(key);
        }

        return true;
    }

    public static async SyncTask<bool> CleanKeys()
    {
        using (_inputController)
        {
            foreach (var key in CurrentDownKeys)
            {
                GlobalLog.Debug($"KeyUp {key}", LogName);
                if (!await _inputController.KeyUp(key)) return false;
            }

            CurrentDownKeys.Clear();
        }

        return true;
    }

    public static async SyncTask<bool> MoveMouseToScreenPosition(RectangleF rec)
    {
        GlobalLog.Debug($"MoveMouseToScreenPosition {rec}", LogName);
        using (_inputController)
        {
            return await _inputController.MoveMouse(ToVector2Num(rec), CancellationToken);
        }
    }

    public static async SyncTask<bool> MoveMouseToScreenPosition(System.Numerics.Vector2 pos)
    {
        GlobalLog.Debug($"MoveMouseToScreenPosition {pos}", LogName);
        using (_inputController)
        {
            return await _inputController.MoveMouse(pos, CancellationToken);
        }
    }

    public static async SyncTask<bool> MoveMouseToWorldPosition(Vector3 pos)
    {
        var screenPos = Gc.Game.IngameState.Camera.WorldToScreen(pos);
        GlobalLog.Debug($"MoveMouseToWorldPosition {screenPos}", LogName);
        using (_inputController)
        {
            return await _inputController.MoveMouse(screenPos, CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(MouseButtons button)
    {
        GlobalLog.Debug($"Click {button}", LogName);
        using (_inputController)
        {
            return await _inputController.Click(button, CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(MouseButtons button, System.Numerics.Vector2 position)
    {
        GlobalLog.Debug($"Click {button} {position}", LogName);
        using (_inputController)
        {
            return await _inputController.Click(button, position, CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(MouseButtons button, RectangleF rec)
    {
        GlobalLog.Debug($"Move and click {button} {rec}", LogName);
        using (_inputController)
        {
            return await _inputController.Click(button, ToVector2Num(rec), CancellationToken);
        }
    }

    private static System.Numerics.Vector2 ToVector2Num(RectangleF rec)
    {
        var position = rec.ClickRandomNum(20, 20);
        position += WindowOffset.ToVector2Num();
        return position;
    }

    public static async SyncTask<bool> Click()
    {
        GlobalLog.Debug($"Click {MouseButtons.Left}", LogName);
        using (_inputController)
        {
            return await _inputController.Click(CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(RectangleF rec)
    {
        GlobalLog.Debug($"Move and Click {MouseButtons.Left}", LogName);
        using (_inputController)
        {
            return await _inputController.Click(MouseButtons.Left, ToVector2Num(rec), CancellationToken);
        }
    }
}