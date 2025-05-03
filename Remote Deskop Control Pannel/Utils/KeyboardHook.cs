using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NetworkLibrary.Utils;

namespace RemoteDeskopControlPannel.Utils
{
    internal class KeyboardHook
    {
        [DllImport("user32.dll")]
        private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, nint hInstance,
            uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(nint instance);

        [DllImport("user32.dll")]
        private static extern nint CallNextHookEx(nint idHook, int code, nint wParam, nint lParam);
        [DllImport("kernel32.dll")]
        private static extern nint GetModuleHandle(string lpFileName);

        private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

        public delegate bool KeyboardCallback(int nCode, int wParam, int vkCode);
        private static readonly ConcurrentBag<KeyboardCallback> _callbacks = [];

        private const int WhKeyboardLl = 13;
        private static nint _hookID = nint.Zero;

        public static bool Hooked { get; private set; }

        public const int KeyDown = 256;
        public const int KeyUp = 257;
        public const int SystemKeyDown = 260;
        public const int SystemKeyUp = 261;

        private KeyboardHook()
        {
            SetupHook();
        }

        ~KeyboardHook()
        {
            ShutdownHook();
        }

        private static void SetupHook()
        {
            if (Hooked) return;
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule;
            _hookID = SetWindowsHookEx(WhKeyboardLl, HookCallback, GetModuleHandle(module.ModuleName), 0);
            Hooked = true;
        }

        private static void ShutdownHook()
        {
            if (!Hooked) return;
            UnhookWindowsHookEx(_hookID);
            Hooked = false;
        }

        public static void AddCallback(KeyboardCallback callback)
        {
            _callbacks.Add(callback);
        }

        public static void RemoveCallback(KeyboardCallback callback)
        {
            _callbacks.Remove(callback);
        }

        private static nint HookCallback(int code, nint wParam, nint lParam)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            if (code < 0) return CallNextHookEx(_hookID, code, wParam, lParam);
            if (_callbacks.Any(callback => callback.Invoke(code, (int)wParam, vkCode)))
            {
                return 1;
            }

            return CallNextHookEx(_hookID, code, wParam, lParam);
        }
    }
}
