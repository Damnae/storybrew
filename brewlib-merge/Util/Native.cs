using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BrewLib.Util
{
    public static class Native
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)] public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
        [DllImport("user32.dll")] public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
        [DllImport("user32.dll")] public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetWindowText(IntPtr hWnd)
        {
            var length = GetWindowTextLength(hWnd);
            if (length == 0) return string.Empty;

            var sb = new StringBuilder(length);
            GetWindowText(hWnd, sb, length + 1);
            return sb.ToString();
        }
        public static IEnumerable<IntPtr> EnumerateProcessWindowHandles(Process process)
        {
            var handles = new List<IntPtr>();
            foreach (ProcessThread thread in process.Threads) EnumThreadWindows(thread.Id, (hWnd, lParam) =>
            {
                handles.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            return handles;
        }
        public static IntPtr FindProcessWindow(string title)
        {
            foreach (var hWnd in EnumerateProcessWindowHandles(Process.GetCurrentProcess())) if (GetWindowText(hWnd) == title)
                return hWnd;

            return IntPtr.Zero;
        }

        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr MemSet(IntPtr dest, int value, int count);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(byte[] b1, byte[] b2, long count);

        public static bool ArrayEquals(byte[] b1, byte[] b2) => b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
    }
}