using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

namespace StorybrewEditor.Processes
{
    public class RemoteProcessWorkerContainer : IDisposable
    {
        readonly NamedPipeServerStream pipeServer;
        Process process;

        public RemoteProcessWorker Worker { get; private set; }
        public RemoteProcessWorkerContainer()
        {
            pipeServer = new NamedPipeServerStream($"sbrew-{Guid.NewGuid()}");
            pipeServer.WaitForConnection();

            Worker = retrieveWorker(pipeServer);
        }

        RemoteProcessWorker retrieveWorker(NamedPipeServerStream pipeServer)
        {
            while (true)
            {
                Thread.Sleep(250);
                try
                {
                    Trace.WriteLine("Waiting for connection...");
                    pipeServer.WaitForConnection();
                    Trace.WriteLine("Connection established.");

                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    var worker = (RemoteProcessWorker)formatter.Deserialize(pipeServer);
                    Trace.WriteLine("Worker received.");

                    return worker;
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Couldn't start ipc: {e}");
                }
            }
        }

        #region IDisposable Support

        bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Worker.Dispose();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Failed to dispose the worker: {e}");
                    }
                    if (!process.WaitForExit(2000)) process.Kill();
                }
                Worker = null;
                process = null;
                disposed = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}