using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

namespace StorybrewEditor.Processes
{
    public static class ProcessWorker
    {
        static bool exit;

        public static void Run(string identifier)
        {
            Trace.WriteLine($"channel: {identifier}");
            try
            {
                var name = $"sbrew-worker-{identifier}";
                var pipeServer = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                try
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    while (!exit)
                    {
                        pipeServer.WaitForConnection();
                        var remoteProcessWorker = new RemoteProcessWorker();
                        var stream = pipeServer;
                        remoteProcessWorker = (RemoteProcessWorker)formatter.Deserialize(stream);

                        stream.Position = 0;
                        formatter.Serialize(stream, remoteProcessWorker);

                        pipeServer.Disconnect();
                        Program.RunScheduledTasks();
                        Thread.Sleep(100);
                    }
                }
                finally
                {
                    Trace.WriteLine($"closing pipe server");
                    pipeServer.Close();
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"ProcessWorker failed: {e}");
            }
        }
        public static void Exit()
        {
            Trace.WriteLine($"exiting");
            exit = true;
        }
    }
}