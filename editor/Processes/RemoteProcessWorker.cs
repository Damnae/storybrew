using StorybrewCommon.Scripting;
using StorybrewEditor.Scripting;
using System;
using System.Diagnostics;

namespace StorybrewEditor.Processes
{
    public class RemoteProcessWorker : MarshalByRefObject, IDisposable
    {
        public void CheckIpc() => Trace.WriteLine("CheckIpc");
        public ScriptProvider<TScript> CreateScriptProvider<TScript>() where TScript : Script
        {
            Trace.WriteLine("GetScriptProvider");
            return new ScriptProvider<TScript>();
        }

        #region IDisposable Support

        bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) ProcessWorker.Exit();
                disposed = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}