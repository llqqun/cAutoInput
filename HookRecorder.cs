using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;

namespace cAutoInput
{
    public class HookRecorder : IDisposable
    {
        // events: Raised when a new ActionItem is recorded
        public event Action<ActionItem> OnActionRecorded;
        private IntPtr _keyboardHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;

        private LowLevelKeyboardProc _kbProc;
        private LowLevelMouseProc _mouseProc;

        private DateTime _lastTime;

        public HookRecorder()
        {
            _kbProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
        }

        public void Start()
        {
            _lastTime = DateTime.Now;
            _keyboardHookId = SetHook(_kbProc, WH.KEYBOARD_LL);
            _mouseHookId = SetHook(_mouseProc, WH.MOUSE_LL);
        }

        public void Stop()
        {
            if (_keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }
            if (_mouseHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(Delegate proc, WH hookType)
        {
            using (Process curProc = Process.GetCurrentProcess())
            using (ProcessModule curMod = curProc.MainModule)
            {
                return SetWindowsHookEx((int)hookType, proc, GetModuleHandle(curMod.ModuleName), 0);
            }
        }

        // keyboard callback
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vk = Marshal.ReadInt32(lParam);
                var msg = (WM)wParam;
                // filter out our hotkeys: F9/F10/F11
                if (vk == (int)Keys.F9 || vk == (int)Keys.F10 || vk == (int)Keys.F11)
                    return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);

                var delta = (int)(DateTime.Now - _lastTime).TotalMilliseconds;
                if (delta > 0)
                {
                    OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.DelayMs, DurationMs = delta });
                }
                _lastTime = DateTime.Now;

                if (msg == WM.KEYDOWN || msg == WM.SYSKEYDOWN)
                {
                    OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.KeyDown, KeyCode = vk });
                }
                else if (msg == WM.KEYUP || msg == WM.SYSKEYUP)
                {
                    OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.KeyUp, KeyCode = vk });
                }
            }
            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        // mouse callback
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var msg = (WM)wParam;
                var delta = (int)(DateTime.Now - _lastTime).TotalMilliseconds;
                if (delta > 0)
                {
                    OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.DelayMs, DurationMs = delta });
                }
                _lastTime = DateTime.Now;

                if (msg == WM.LBUTTONDOWN) OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.MouseLeftDown, X = mouseStruct.pt.x, Y = mouseStruct.pt.y });
                if (msg == WM.LBUTTONUP) OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.MouseLeftUp, X = mouseStruct.pt.x, Y = mouseStruct.pt.y });
                if (msg == WM.RBUTTONDOWN) OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.MouseRightDown, X = mouseStruct.pt.x, Y = mouseStruct.pt.y });
                if (msg == WM.RBUTTONUP) OnActionRecorded?.Invoke(new ActionItem { Type = ActionType.MouseRightUp, X = mouseStruct.pt.x, Y = mouseStruct.pt.y });
            }
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        public void Dispose() => Stop();

        #region WinAPI
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private enum WH : int { KEYBOARD_LL = 13, MOUSE_LL = 14 }
        private enum WM : int
        {
            KEYDOWN = 0x0100, KEYUP = 0x0101, SYSKEYDOWN = 0x0104, SYSKEYUP = 0x0105,
            MOUSEMOVE = 0x0200, LBUTTONDOWN = 0x0201, LBUTTONUP = 0x0202,
            RBUTTONDOWN = 0x0204, RBUTTONUP = 0x0205,
            // add if needed
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }
}

