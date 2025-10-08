using System;
using System.Threading;

namespace RegexCrafter.Helpers
{
    public class Clipboard : IDisposable
    {

        private static readonly object _lock = new();
        private static Clipboard _instance;
        private readonly AutoResetEvent _eventStart = new(false);
        private readonly AutoResetEvent _eventStop = new(false);
        private readonly Thread _staThread;
        private Action _action;
        private bool _isDisposed;
        private Clipboard()
        {
            _staThread = new Thread(StaThreadProc);
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.IsBackground = true;
            _staThread.Start();
        }

        public static Clipboard Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null || _instance._isDisposed)
                    {
                        _instance = new Clipboard();
                    }
                    return _instance;
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            _eventStart.Set();
            _staThread.Join();
            _eventStart.Dispose();
            _eventStop.Dispose();
            _instance = null;
        }
        private void StaThreadProc()
        {
            while (!_isDisposed)
            {
                _eventStart.WaitOne();
                if (_isDisposed)
                {
                    break;
                }

                try
                {
                    _action?.Invoke();
                }
                catch (Exception e)
                {
                    GlobalLog.Error(e.Message, "Clipboard");
                }
                _eventStop.Set();
            }
        }
        private void ExecuteOnSTAThread(Action action)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(Clipboard));
            }
            _action = action;
            _eventStart.Set();
            _eventStop.WaitOne();
        }


        public static string GetClipboardText()
        {
            return Instance.GetText();
        }

        public static void CleanClipboard()
        {
            Instance.Clear();
        }

        public static void SetClipboardText(string text)
        {
            Thread staThread = new(() => System.Windows.Forms.Clipboard.SetText(text));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }
        public string GetText()
        {
            try
            {
                var text = string.Empty;
                ExecuteOnSTAThread(() => text = System.Windows.Forms.Clipboard.GetText());
                return text;
            }
            catch
            {
                return string.Empty;
            }
        }

        public void Clear()
        {
            try
            {
                ExecuteOnSTAThread(System.Windows.Forms.Clipboard.Clear);
            }
            catch (Exception e)
            {
                GlobalLog.Error(e.Message, "Clipboard");
            }
        }
    }
}
