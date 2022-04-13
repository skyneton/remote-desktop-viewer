using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using RemoteDesktopViewer.Utils;

namespace RemoteClientViewer.Hook
{
    public static class KeyboardManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance,
            uint threadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr instance);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr idHook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpFileName);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        private static LowLevelKeyboardProc _lowLevelKeyboardProc = HookCallback;
        
        public delegate bool KeyboardCallback(int nCode, int wParam, int vkCode);
        private static ConcurrentBag<KeyboardCallback> _callbacks = new();

        private const int WhKeyboardLl = 13;
        private static IntPtr _hookID = IntPtr.Zero;
        
        public static bool Hooked { get; private set; }
        
        public const int KeyDown = 256;
        public const int KeyUp = 257;
        public const int SystemKeyDown = 260;
        public const int SystemKeyUp = 261;

        public static void SetupHook()
        {
            if (Hooked) return;
            _hookID = SetHook();
            Hooked = true;
        }

        public static void ShutdownHook()
        {
            if (!Hooked) return;
            UnhookWindowsHookEx(_hookID);
            Hooked = false;
        }
        
        private static IntPtr SetHook()
        {
            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule;
            return SetWindowsHookEx(WhKeyboardLl, _lowLevelKeyboardProc, GetModuleHandle(module.ModuleName), 0);
        }

        public static void AddCallback(KeyboardCallback callback)
        {
            _callbacks.Add(callback);
        }

        public static void RemoveCallback(KeyboardCallback callback)
        {
            _callbacks.Remove(callback);
        }
        
        private static IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            if (code < 0) return CallNextHookEx(_hookID, code, wParam, lParam);
            if (_callbacks.Any(callback => callback.Invoke(code, (int) wParam, vkCode)))
            {
                return (IntPtr) 1;
            }

            return CallNextHookEx(_hookID, code, wParam, lParam);
        }
    }
}