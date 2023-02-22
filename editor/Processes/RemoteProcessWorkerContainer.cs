using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

namespace StorybrewEditor.Processes
{
    public class RemoteProcessWorkerContainer : IDisposable
    {
        IChannel channel;
        Process process;

        public RemoteProcessWorker Worker { get; private set; }

        public RemoteProcessWorkerContainer()
        {
            var identifier = $"{Guid.NewGuid()}";
            var workerUrl = $"ipc://sbrew-worker-{identifier}/worker";

            channel = new IpcChannel(new Hashtable
            {
                ["name"] = $"sbrew-{identifier}",
                ["portName"] = $"sbrew-{identifier}"
            }, new BinaryClientFormatterSinkProvider(), null);

            ChannelServices.RegisterChannel(channel, false);
            startProcess(identifier);
            Worker = retrieveWorker(workerUrl);
        }
        void startProcess(string identifier)
        {
            var executablePath = Assembly.GetExecutingAssembly().Location;
            var workingDirectory = Path.GetDirectoryName(executablePath);
            process = new Process()
            {
                StartInfo = new ProcessStartInfo(executablePath, $"worker \"{identifier}\"")
                {
                    WorkingDirectory = workingDirectory
                },
            };
            process.Start();
        }
        RemoteProcessWorker retrieveWorker(string workerUrl)
        {
            while (true)
            {
                Thread.Sleep(250);
                try
                {
                    Trace.WriteLine($"Retrieving {workerUrl}");
                    var worker = (RemoteProcessWorker)Activator.GetObject(typeof(RemoteProcessWorker), workerUrl);
                    worker.CheckIpc();
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
                    if (!process.WaitForExit(3000)) process.Kill();
                    ChannelServices.UnregisterChannel(channel);
                }
                Worker = null;
                process = null;
                channel = null;
                disposed = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}