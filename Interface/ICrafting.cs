using System.Threading;
using ExileCore.Shared;

namespace RegexCrafter.Interface
{
    internal interface ICrafting
    {
        public string Name { get; }
        public void DrawSettings();
        public SyncTask<bool> Start(CancellationToken ct);
        public void OnClose();
        public void Render();
        public void Clean();
    }
}
