using OpenTK;
using System;

namespace BrewLib.Util
{
    public static class GameWindowExtensions
    {
        public static IntPtr GetWindowHandle(this GameWindow window)
        {
            var handle = Native.FindProcessWindow(window.Title);
            if (handle != IntPtr.Zero) return handle;

            // This handle is incorrect for some users, only use it if the window couldn't be found by title
            return window.WindowInfo.Handle;
        }
    }
}