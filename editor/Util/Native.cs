using System;
using System.Runtime.InteropServices;

namespace StorybrewEditor.Util
{
    public static class Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}
