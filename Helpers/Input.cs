using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using InputHumanizer.Input;
using Microsoft.VisualBasic.Devices;
using SharpDX;
using Vector3 = System.Numerics.Vector3;

namespace RegexCrafter.Helpers;

public static class Input
{
    private const string LogName = "Input";
    private static RegexCrafter _core;
    private static readonly List<Keys> CurrentDownKeys = [];
    private static CancellationToken CancellationToken => _core.Cts.Token;
    private static GameController Gc => _core.GameController;
    private static Vector2 WindowOffset => _core.GameController.Window.GetWindowRectangleTimeCache.TopLeft;

    public static bool Init(RegexCrafter core)
    {
        _core = core;
        return true; ;
    }

    public static async SyncTask<bool> SimulateKeyEvent(Keys key, bool down = true, bool up = true,
        Keys extraKey = Keys.None)
    {

        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
        }

        var inputController = tryGetInputController(_core.Name);
        using (inputController)
        {
            if (down)
            {
                if (extraKey != Keys.None)
                {
                    GlobalLog.Debug($"KeyDown {extraKey}", LogName);
                    if (await inputController.KeyDown(extraKey, CancellationToken))
                    {
                        GlobalLog.Debug($"KeyDown {extraKey} success", LogName);
                        CurrentDownKeys.Add(extraKey);
                    }
                    else
                    {
                        GlobalLog.Error($"KeyDown {extraKey} failed", LogName);
                        return false;
                    }

                }

                GlobalLog.Debug($"KeyDown {key}", LogName);
                CurrentDownKeys.Add(key);
                if (!await inputController.KeyDown(key, CancellationToken)) return false;

            }

            if (!up) return true;
            if (extraKey != Keys.None)
            {
                GlobalLog.Debug($"KeyUp {extraKey}", LogName);
                if (!await inputController.KeyUp(extraKey, cancellationToken: CancellationToken)) return false;
                CurrentDownKeys.Remove(extraKey);
            }

            GlobalLog.Debug($"KeyUp {key}", LogName);
            if (!await inputController.KeyUp(key, cancellationToken: CancellationToken)) return false;
            CurrentDownKeys.Remove(key);
        }

        return true;
    }

    public static void CleanKeys()
    {
        Utils.Keyboard.KeyUp(Keys.Alt);
        Utils.Keyboard.KeyUp(Keys.Control);
        Utils.Keyboard.KeyUp(Keys.Shift);
        Utils.Keyboard.KeyUp(Keys.LShiftKey);
        Utils.Keyboard.KeyUp(Keys.LControlKey);
        Utils.Keyboard.KeyUp(Keys.RShiftKey);
        Utils.Keyboard.KeyUp(Keys.RControlKey);
    }

    public static async SyncTask<bool> MoveMouseToScreenPosition(RectangleF rec)
    {
        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(_core.Name);
        GlobalLog.Debug($"MoveMouseToScreenPosition {rec}", LogName);
        using (inputController)
        {
            return await inputController.MoveMouse(ToVector2Num(rec), CancellationToken);
        }
    }

    public static async SyncTask<bool> MoveMouseToScreenPosition(System.Numerics.Vector2 pos)
    {
        GlobalLog.Debug($"MoveMouseToScreenPosition {pos}", LogName);

        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(_core.Name);
        using (inputController)
        {
            return await inputController.MoveMouse(pos, CancellationToken);
        }
    }

    public static async SyncTask<bool> MoveMouseToWorldPosition(Vector3 pos)
    {
        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(_core.Name);
        var screenPos = Gc.Game.IngameState.Camera.WorldToScreen(pos);
        GlobalLog.Debug($"MoveMouseToWorldPosition {screenPos}", LogName);
        using (inputController)
        {
            return await inputController.MoveMouse(screenPos, CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(MouseButtons button)
    {
        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(_core.Name);
        GlobalLog.Debug($"Click {button}", LogName);
        using (inputController)
        {
            return await inputController.Click(button, CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(MouseButtons button, System.Numerics.Vector2 position)
    {
        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(_core.Name);
        GlobalLog.Debug($"Click {button} {position}", LogName);
        using (inputController)
        {
            return await inputController.Click(button, position, CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(MouseButtons button, RectangleF rec)
    {
        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(_core.Name);
        GlobalLog.Debug($"Move and click {button} {rec}", LogName);
        using (inputController)
        {
            return await inputController.Click(button, ToVector2Num(rec), CancellationToken);
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
        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }
        var inputController = tryGetInputController(_core.Name);
        using (inputController)
        {
            return await inputController.Click(CancellationToken);
        }
    }

    public static async SyncTask<bool> Click(RectangleF rec)
    {
        var tryGetInputController = _core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>("InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(_core.Name);
        GlobalLog.Debug($"Move and Click {MouseButtons.Left}", LogName);
        using (inputController)
        {
            return await inputController.Click(MouseButtons.Left, ToVector2Num(rec), CancellationToken);
        }
    }
}