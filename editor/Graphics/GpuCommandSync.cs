using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StorybrewEditor.Graphics
{
    /// <summary>
    /// [requires: v3.2 or ARB_sync|VERSION_3_2]
    /// </summary>
    public class GpuCommandSync : IDisposable
    {
        private List<SyncRange> syncRanges = new List<SyncRange>();

        public int RangeCount => syncRanges.Count;

        /// <summary>
        /// Wait for all draw operations to be completed.
        /// </summary>
        public bool WaitForAll()
        {
            if (syncRanges.Count == 0)
                return false;

            var blocked = syncRanges[syncRanges.Count - 1].Wait();

            foreach (var syncRange in syncRanges)
                syncRange.Dispose();
            syncRanges.Clear();

            return blocked;
        }

        /// <summary>
        /// Wait for draw operations on this range to be completed.
        /// </summary>
        public bool WaitForRange(int index, int length)
        {
            for (int i = syncRanges.Count - 1; i >= 0; i--)
            {
                var syncRange = syncRanges[i];
                if (index < syncRange.Index + syncRange.Length && syncRange.Index < index + length)
                {
                    var blocked = syncRange.Wait();

                    for (int j = i; j >= 0; j--)
                        syncRanges[j].Dispose();
                    syncRanges.RemoveRange(0, i + 1);

                    return blocked;
                }
                else if (i % 8 == 0)
                {
                    var wouldBlock = syncRange.Wait(false);
                    if (!wouldBlock)
                    {
                        for (int j = i; j >= 0; j--)
                            syncRanges[j].Dispose();
                        syncRanges.RemoveRange(0, i + 1);

                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Lock this range until draw operations are completed up to this point.
        /// </summary>
        public void LockRange(int index, int length)
        {
            syncRanges.Add(new SyncRange(index, length));
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var syncRange in syncRanges)
                        syncRange.Dispose();
                    syncRanges.Clear();
                }
                syncRanges = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public static bool HasCapabilities()
        {
            return DrawState.HasCapabilities(3, 2, "GL_ARB_sync");
        }

        private class SyncRange : IDisposable
        {
            public int Index;
            public int Length;
            public IntPtr Fence = IntPtr.Zero;

            public SyncRange(int index, int length)
            {
                Index = index;
                Length = length;
                Fence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            }

            public bool Wait(bool canBlock = true)
            {
                if (Fence == IntPtr.Zero)
                    return false;

                var blocked = false;
                var waitSyncFlags = ClientWaitSyncFlags.None;
                var timeout = 0L;

                while (true)
                {
                    switch (GL.ClientWaitSync(Fence, waitSyncFlags, timeout))
                    {
                        case WaitSyncStatus.AlreadySignaled:
                            return blocked;

                        case WaitSyncStatus.ConditionSatisfied:
                            Debug.Assert(blocked); // Should never happen
                            return true;

                        case WaitSyncStatus.WaitFailed:
                            throw new Exception("ClientWaitSync failed");

                        case WaitSyncStatus.TimeoutExpired:
                            if (!canBlock)
                                return true;

                            blocked = true;
                            waitSyncFlags = ClientWaitSyncFlags.SyncFlushCommandsBit;
                            timeout = 1000000000;
                            break;
                    }
                }
            }

            public override string ToString() => $"{Index} - {Index + Length - 1} ({Length})";

            #region IDisposable Support

            private bool disposedValue = false;
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                    }
                    GL.DeleteSync(Fence);
                    Fence = IntPtr.Zero;
                    disposedValue = true;
                }
            }

            ~SyncRange()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true); GC.SuppressFinalize(this);
            }

            #endregion
        }
    }
}
