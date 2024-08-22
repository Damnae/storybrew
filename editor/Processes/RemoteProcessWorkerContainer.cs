using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;

namespace StorybrewEditor.Processes
{
    public class RemoteProcessWorkerContainer : IDisposable
    {
        private NamedPipeClientStream pipeClient;
        private Process process;

        public RemoteProcessWorker Worker { get; private set; }

        public RemoteProcessWorkerContainer()
        {
            var identifier = $"{Guid.NewGuid().ToString()}";
            var pipeName = $"sbrew-worker-{identifier}";

            pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            StartProcess(identifier);
            Worker = RetrieveWorker();
        }

        private void StartProcess(string identifier)
        {
            var executablePath = Assembly.GetExecutingAssembly().Location;
            var workingDirectory = Path.GetDirectoryName(executablePath);
            process = new Process()
            {
                StartInfo = new ProcessStartInfo(executablePath, $"worker \"{identifier}\"")
                {
                    WorkingDirectory = workingDirectory,
                },
            };
            process.Start();
        }

        private RemoteProcessWorker RetrieveWorker()
        {
            while (true)
            {
                try
                {
                    Trace.WriteLine($"Connecting to pipe");
                    pipeClient.Connect(5000); // Timeout in milliseconds
                    return new RemoteProcessWorker(pipeClient);
                }
                catch (TimeoutException)
                {
                    Trace.WriteLine($"Timed out connecting to pipe");
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Couldn't connect to pipe: {e}");
                }

                Thread.Sleep(250);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        Worker?.Dispose();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Failed to dispose the worker: {e}");
                    }
                    if (!process.WaitForExit(3000))
                        process.Kill();
                    pipeClient?.Dispose();
                }
                Worker = null;
                process = null;
                pipeClient = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
