using System.Threading;
using System.Windows.Forms;
using ExileCore.Shared;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;


namespace RegexCrafter.Interface;

public interface IInput
{
    SyncTask<bool> SimulateKeyEvent(Keys key, bool down = true, bool up = true, Keys extraKey = Keys.None,
        CancellationToken ct = default);

    SyncTask<bool> KeyDown(Keys key, CancellationToken ct = default);
    SyncTask<bool> KeyUp(Keys key, CancellationToken ct = default);
    SyncTask<bool> SimulateKeyEvent(Keys key, CancellationToken ct = default);
    SyncTask<bool> SimulateKeyEvent(Keys key, Keys extraKey = Keys.None, CancellationToken ct = default);
    void CleanKeys();
    SyncTask<bool> MoveMouseToScreenPosition(RectangleF rec, CancellationToken ct = default);
    SyncTask<bool> MoveMouseToScreenPosition(Vector2 pos, CancellationToken ct = default);
    SyncTask<bool> MoveMouseToWorldPosition(Vector3 pos, CancellationToken ct = default);
    SyncTask<bool> Click(MouseButtons button, CancellationToken ct = default);
    SyncTask<bool> Click(MouseButtons button, Vector2 position, CancellationToken ct = default);
    SyncTask<bool> Click(MouseButtons button, RectangleF rec, CancellationToken ct = default);
    SyncTask<bool> Click(CancellationToken ct = default);
    SyncTask<bool> Click(RectangleF rec, CancellationToken ct = default);
}