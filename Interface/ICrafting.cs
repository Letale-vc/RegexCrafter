using ExileCore.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegexCrafter.Interface
{
    internal interface ICrafting
    {
        public string Name { get; }
        public void DrawSettings();
        public SyncTask<bool> StartCrafting();
        public void OnClose();
        public void Render();
        public void Clean();

    }
}
