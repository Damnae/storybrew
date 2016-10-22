using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Threading;

namespace StorybrewEditor.Processes
{
    public static class ProcessWorker
    {
        private static bool exit;

        public static void Run(string identifier)
        {
            //if (!Debugger.IsAttached) Debugger.Launch();

            Trace.WriteLine($"channel: {identifier}");
            try
            {
                var name = $"sbrew-worker-{identifier}";
                var channel = new IpcServerChannel(name, name, new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });

                ChannelServices.RegisterChannel(channel, false);
                try
                {
                    RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteProcessWorker), "worker", WellKnownObjectMode.Singleton);
                    Trace.WriteLine($"ready\n");

                    while (!exit)
                    {
                        Program.RunScheduledTasks();
                        Thread.Sleep(100);
                    }
                }
                finally
                {
                    Trace.WriteLine($"unregistering channel");
                    ChannelServices.UnregisterChannel(channel);
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
