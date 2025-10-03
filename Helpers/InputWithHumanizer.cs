using System;
using System.Threading;
using System.Windows.Forms;
using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using InputHumanizer.Input;
using RegexCrafter.Interface;
using RegexCrafter.Utils;
using SharpDX;
using Vector3 = System.Numerics.Vector3;

namespace RegexCrafter.Helpers;

public class InputWithHumanizer(RegexCrafter core) : IInput
{
    private const string LogName = "Input";
    private GameController Gc => core.GameController;
    private Vector2 WindowOffset => Gc.Window.GetWindowRectangleTimeCache.TopLeft;

    public async SyncTask<bool> SimulateKeyEvent(Keys key, bool down = true, bool up = true,
        Keys extraKey = Keys.None, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }


        var inputController = tryGetInputController(core.Name);
        if (inputController == null)
        {
            GlobalLog.Error("InputController is null.", LogName);
            return false;
        }

        using (inputController)
        {
            if (down)
            {
                if (extraKey != Keys.None)
                {
                    GlobalLog.Debug($"KeyDown {extraKey}", LogName);
                    if (await inputController.KeyDown(extraKey, ct))
                    {
                        GlobalLog.Debug($"KeyDown {extraKey} success", LogName);
                    }
                    else
                    {
                        GlobalLog.Error($"KeyDown {extraKey} failed", LogName);
                        return false;
                    }
                }

                GlobalLog.Debug($"KeyDown {key}", LogName);
                if (!await inputController.KeyDown(key, ct)) return false;
            }

            if (!up) return true;
            if (extraKey != Keys.None)
            {
                GlobalLog.Debug($"KeyUp {extraKey}", LogName);
                if (!await inputController.KeyUp(extraKey, cancellationToken: ct)) return false;
            }

            GlobalLog.Debug($"KeyUp {key}", LogName);
            if (!await inputController.KeyUp(key, cancellationToken: ct)) return false;
        }

        return true;
    }

    public SyncTask<bool> KeyDown(Keys key, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return SyncTask.FromResult(false);
        }

        var inputController = tryGetInputController(core.Name);
        if (inputController == null)
        {
            GlobalLog.Error("InputController is null.", LogName);
            return SyncTask.FromResult(false);
        }

        using (inputController)

        {
            return inputController.KeyDown(key, ct);
        }
    }

    public SyncTask<bool> KeyUp(Keys key, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return SyncTask.FromResult(false);
        }

        var inputController = tryGetInputController(core.Name);

        if (inputController == null)
        {
            GlobalLog.Error("InputController is null.", LogName);
            return SyncTask.FromResult(false);
        }

        using (inputController)
        {
            return inputController.KeyUp(key, true, ct);
        }
    }

    public async SyncTask<bool> SimulateKeyEvent(Keys key, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        using (inputController)
        {
            return await inputController.KeyDown(key, ct) && await inputController.KeyUp(key, true, ct);
        }
    }

    public async SyncTask<bool> SimulateKeyEvent(Keys key, Keys extraKey, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        if (inputController == null)
        {
            GlobalLog.Error("InputController is null.", LogName);
            return false;
        }

        using (inputController)
        {
            return await inputController.KeyDown(extraKey, ct) && await inputController.KeyDown(key, ct) &&
                   await inputController.KeyUp(key, true, ct) && await inputController.KeyUp(extraKey, true, ct);
        }
    }

    public void CleanKeys()
    {
        Keyboard.KeyUp(Keys.Alt);
        Keyboard.KeyUp(Keys.Control);
        Keyboard.KeyUp(Keys.Shift);
        Keyboard.KeyUp(Keys.LShiftKey);
        Keyboard.KeyUp(Keys.LControlKey);
        Keyboard.KeyUp(Keys.RShiftKey);
        Keyboard.KeyUp(Keys.RControlKey);
    }

    public async SyncTask<bool> MoveMouseToScreenPosition(RectangleF rec, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        if (inputController == null)
        {
            GlobalLog.Error("InputController is null.", LogName);
            return false;
        }

        GlobalLog.Debug($"MoveMouseToScreenPosition {rec}", LogName);
        using (inputController)
        {
            return await inputController.MoveMouse(ToVector2Num(rec), ct);
        }
    }

    public async SyncTask<bool> MoveMouseToScreenPosition(System.Numerics.Vector2 pos, CancellationToken ct = default)
    {
        GlobalLog.Debug($"MoveMouseToScreenPosition {pos}", LogName);

        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        using (inputController)
        {
            return await inputController.MoveMouse(pos, ct);
        }
    }

    public async SyncTask<bool> MoveMouseToWorldPosition(Vector3 pos, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        var screenPos = Gc.Game.IngameState.Camera.WorldToScreen(pos);
        GlobalLog.Debug($"MoveMouseToWorldPosition {screenPos}", LogName);
        using (inputController)
        {
            return await inputController.MoveMouse(screenPos, ct);
        }
    }

    public async SyncTask<bool> Click(MouseButtons button, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        GlobalLog.Debug($"Click {button}", LogName);
        using (inputController)
        {
            return await inputController.Click(button, ct);
        }
    }

    public async SyncTask<bool> Click(MouseButtons button, System.Numerics.Vector2 position,
        CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        GlobalLog.Debug($"Click {button} {position}", LogName);
        using (inputController)
        {
            return await inputController.Click(button, position, ct);
        }
    }

    public async SyncTask<bool> Click(MouseButtons button, RectangleF rec, CancellationToken ct = default)
    {
        if (rec.Width <= 0 || rec.Height <= 0)
        {
            GlobalLog.Error($"Invalid RectangleF for click: {rec}", LogName);
            throw new ArgumentException("Invalid RectangleF for click", nameof(rec));
        }

        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        GlobalLog.Debug($"Move and click {button} {rec}", LogName);
        using (inputController)
        {
            return await inputController.Click(button, ToVector2Num(rec), ct);
        }
    }

    public async SyncTask<bool> Click(CancellationToken ct = default)
    {
        GlobalLog.Debug($"Click {MouseButtons.Left}", LogName);
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        using (inputController)
        {
            return await inputController.Click(ct);
        }
    }

    public async SyncTask<bool> Click(RectangleF rec, CancellationToken ct = default)
    {
        var tryGetInputController =
            core.GameController.PluginBridge.GetMethod<Func<string, IInputController>>(
                "InputHumanizer.TryGetInputController");
        if (tryGetInputController == null)
        {
            GlobalLog.Error("InputHumanizer method not registered.", LogName);
            return false;
        }

        var inputController = tryGetInputController(core.Name);
        GlobalLog.Debug($"Move and Click {MouseButtons.Left}", LogName);
        using (inputController)
        {
            return await inputController.Click(MouseButtons.Left, ToVector2Num(rec), ct);
        }
    }

    private System.Numerics.Vector2 ToVector2Num(RectangleF rec)
    {
        if (rec.Width <= 0 || rec.Height <= 0)
        {
            GlobalLog.Error($"Invalid RectangleF for click: {rec}", LogName);
            throw new ArgumentException("Invalid RectangleF for click", nameof(rec));
        }

        var position = rec.ClickRandomNum(20, 20);
        position += WindowOffset.ToVector2Num();
        return position;
    }
}