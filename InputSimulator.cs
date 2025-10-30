using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace cAutoInput
{
    public static class InputSimulator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        // Convert screen coords to absolute for SendInput (0..65535)
        private static int ToAbsoluteX(int x) => (int)(x * (65535.0 / (GetSystemMetrics(0) - 1)));
        private static int ToAbsoluteY(int y) => (int)(y * (65535.0 / (GetSystemMetrics(1) - 1)));

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public static void ClickLeft(int x, int y)
        {
            SetCursorPos(x, y);
            var inputs = new INPUT[2];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].U.mi = new MOUSEINPUT { dx = 0, dy = 0, dwFlags = MOUSEEVENTF_LEFTDOWN, dwExtraInfo = UIntPtr.Zero };
            inputs[1].type = INPUT_MOUSE;
            inputs[1].U.mi = new MOUSEINPUT { dx = 0, dy = 0, dwFlags = MOUSEEVENTF_LEFTUP, dwExtraInfo = UIntPtr.Zero };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void ClickRight(int x, int y)
        {
            SetCursorPos(x, y);
            var inputs = new INPUT[2];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].U.mi = new MOUSEINPUT { dx = 0, dy = 0, dwFlags = MOUSEEVENTF_RIGHTDOWN, dwExtraInfo = UIntPtr.Zero };
            inputs[1].type = INPUT_MOUSE;
            inputs[1].U.mi = new MOUSEINPUT { dx = 0, dy = 0, dwFlags = MOUSEEVENTF_RIGHTUP, dwExtraInfo = UIntPtr.Zero };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void KeyPress(ushort vk)
        {
            var inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = 0, dwExtraInfo = UIntPtr.Zero };
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = KEYEVENTF_KEYUP, dwExtraInfo = UIntPtr.Zero };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void KeyDown(ushort vk)
        {
            var input = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk } } };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void KeyUp(ushort vk)
        {
            var input = new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } } };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
