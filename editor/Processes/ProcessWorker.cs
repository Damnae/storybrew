using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;

namespace StorybrewEditor.Processes
{
    public static class ProcessWorker
    {
        private static bool exit;

        public static void Run(string identifier)
        {
            //if (!Debugger.IsAttached) Debugger.Launch();

            Trace.WriteLine($"pipe name: {identifier}");
            try
            {
                var pipeName = $"sbrew-worker-{identifier}";
                var serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

                Trace.WriteLine($"ready\n");

                while (!exit)
                {
                    // Simulate running scheduled tasks
                    Program.RunScheduledTasks();
                    Thread.Sleep(100);
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
