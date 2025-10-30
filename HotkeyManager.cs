using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace cAutoInput
{
    public class HotkeyManager : IDisposable
    {
        private IntPtr _hWnd;
        private int _baseId = 0x1000;

        public event Action<int> OnHotkey; // id

        public HotkeyManager(IntPtr hWnd) => _hWnd = hWnd;

        public bool RegisterHotkey(Keys key, uint modifiers, int id)
        {
            return RegisterHotKey(_hWnd, id, modifiers, (uint)key);
        }

        public void UnregisterAll()
        {
            for (int i = _baseId; i < _baseId + 10; i++) UnregisterHotKey(_hWnd, i);
        }

        public void Dispose() => UnregisterAll();

        #region PInvoke
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
    }
}
