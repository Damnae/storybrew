using StorybrewCommon.Scripting;
using StorybrewEditor.Scripting;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace StorybrewEditor.Processes
{
    public class RemoteProcessWorker : IDisposable
    {
        private readonly NamedPipeClientStream _pipeClient;
        private bool _disposed;

        public RemoteProcessWorker(NamedPipeClientStream pipeClient)
        {
            _pipeClient = pipeClient;
        }

        public void CheckIpc()
        {
            Trace.WriteLine("CheckIpc");
            // Add code to check the pipe connection if needed
        }

        public ScriptProvider<TScript> CreateScriptProvider<TScript>()
            where TScript : Script
        {
            Trace.WriteLine("GetScriptProvider");
            return new ScriptProvider<TScript>();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _pipeClient?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
