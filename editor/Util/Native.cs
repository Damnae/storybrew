﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StorybrewEditor.Util
{
    public static class Native
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.DLL")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.DLL")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

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
            foreach (ProcessThread thread in process.Threads)
                EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                {
                    handles.Add(hWnd);
                    return true;
                },
                IntPtr.Zero);

            return handles;
        }
    }
}
