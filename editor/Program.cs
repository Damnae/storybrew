using BrewLib.Audio;
using BrewLib.Util;
using Microsoft.Win32;
using OpenTK;
using OpenTK.Graphics;
using StorybrewEditor.Processes;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StorybrewEditor
{
    class Program
    {
        public const string Name = "storybrew editor";
        public const string Repository = "Damnae/storybrew";
        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static string FullName => $"{Name} {Version} ({Repository})";
        public static string DiscordUrl = $"https://discord.gg/0qfFOucX93QDNVN7";

        public static AudioManager AudioManager { get; set; }
        public static Settings Settings { get; set; }

        static int mainThreadId;
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;
        public static void CheckMainThread([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = -1, [CallerMemberName] string callerName = "")
        {
            if (IsMainThread) return;
            throw new InvalidOperationException($"{callerPath}:L{callerLine} {callerName} called from the thread '{Thread.CurrentThread.Name}', must be called from the main thread");
        }

        [STAThread] static void Main(string[] args)
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            if (args.Length != 0 && handleArguments(args)) return;
            setupLogging(checkFrozen: false);
            startEditor();
        }
        static bool handleArguments(string[] args)
        {
            switch (args[0])
            {
                case "update":
                    if (args.Length < 3) return false;
                    setupLogging(Path.Combine(args[1], DefaultLogPath), "update.log");
                    Updater.Update(args[1], new Version(args[2]));
                    return true;

                case "build":
                    setupLogging(null, "build.log");
                    Builder.Build();
                    return true;

                case "worker":
                    if (args.Length < 2) return false;
                    setupLogging(null, $"worker-{DateTime.UtcNow:yyyyMMddHHmmssfff}.log");
                    enableScheduling();
                    ProcessWorker.Run(args[1]);
                    return true;
            }
            return false;
        }

        #region Editor

        public static string Stats { get; set; }

        static void startEditor()
        {
            enableScheduling();

            Settings = new Settings();
            Updater.NotifyEditorRun();

            var displayDevice = findDisplayDevice();

            using (var window = createWindow(displayDevice)) using (AudioManager = createAudioManager(window))
            using (var editor = new Editor(window))
            {
                Trace.WriteLine($"{getOSVersion()} / {window.WindowInfo}");
                Trace.WriteLine($"graphics mode: {window.Context.GraphicsMode}");

                window.Icon = new Icon(typeof(Program), "icon.ico");
                window.Resize += (sender, e) =>
                {
                    editor.Draw(1);
                    window.SwapBuffers();
                };

                editor.Initialize();
                runMainLoop(window, editor, 1d / Settings.UpdateRate, 1d / (Settings.FrameRate > 0 ? Settings.FrameRate : displayDevice.RefreshRate));

                Settings.Save();
            }
        }
        static string getOSVersion()
        {
            try
            {
                using (var registryKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion"))
                    return (string)registryKey.GetValue("ProductName");
            }
            catch { }
            return Environment.OSVersion.ToString();
        }
        static DisplayDevice findDisplayDevice()
        {
            try
            {
                return DisplayDevice.GetDisplay(DisplayIndex.Default);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to use the default display device: {e}");

                var deviceIndex = 0;
                while (deviceIndex <= (int)DisplayIndex.Sixth) try
                {
                    return DisplayDevice.GetDisplay((DisplayIndex)deviceIndex);
                }
                catch (Exception e2)
                {
                    Trace.WriteLine($"Failed to use display device #{deviceIndex}: {e2}");
                    deviceIndex++;
                }
            }
            throw new InvalidOperationException("Failed to find a display device");
        }
        static GameWindow createWindow(DisplayDevice displayDevice)
        {
            var graphicsMode = new GraphicsMode(new ColorFormat(32), 24, 8, 0, ColorFormat.Empty, 2, false);
            var contextFlags = GraphicsContextFlags.ForwardCompatible;
            var primaryScreenArea = Screen.PrimaryScreen.WorkingArea;

            int windowWidth = 1366, windowHeight = 768;
            if (windowHeight >= primaryScreenArea.Height)
            {
                windowWidth = 1024;
                windowHeight = 600;
                if (windowWidth >= primaryScreenArea.Width) windowWidth = 800;
            }
            var window = new GameWindow(windowWidth, windowHeight, graphicsMode, Name, GameWindowFlags.Default, displayDevice, 2, 0, contextFlags);
            Trace.WriteLine($"Window dpi scale: {window.Height / (float)windowHeight}");

            window.Location = new Point(
                (int)(primaryScreenArea.Left + (primaryScreenArea.Width - window.Size.Width) / 2f),
                (int)(primaryScreenArea.Top + (primaryScreenArea.Height - window.Size.Height) / 2f)
            );
            if (window.Location.X < 0 || window.Location.Y < 0)
            {
                window.Location = primaryScreenArea.Location;
                window.Size = primaryScreenArea.Size;
                window.WindowState = WindowState.Maximized;
            }

            return window;
        }
        static AudioManager createAudioManager(GameWindow window)
        {
            var audioManager = new AudioManager(window.GetWindowHandle())
            {
                Volume = Settings.Volume
            };
            Settings.Volume.OnValueChanged += (sender, e) => audioManager.Volume = Settings.Volume;

            return audioManager;
        }
        static void runMainLoop(GameWindow window, Editor editor, double fixedRateUpdateDuration, double targetFrameDuration)
        {
            double previousTime = 0, fixedRateTime = 0, averageFrameTime = 0, averageActiveTime = 0, longestFrameTime = 0, lastStatTime = 0;
            var windowDisplayed = false;
            var watch = new Stopwatch();

            watch.Start();
            while (window.Exists && !window.IsExiting)
            {
                var focused = window.Focused;
                var currentTime = watch.Elapsed.TotalSeconds;
                var fixedUpdates = 0;

                AudioManager.Update();
                window.ProcessEvents();

                while (currentTime - fixedRateTime >= fixedRateUpdateDuration && fixedUpdates < 2)
                {
                    fixedRateTime += fixedRateUpdateDuration;
                    fixedUpdates++;

                    editor.Update(fixedRateTime, true);
                }
                if (focused && fixedUpdates == 0 && fixedRateTime < currentTime && currentTime < fixedRateTime + fixedRateUpdateDuration)
                    editor.Update(currentTime, false);

                if (!window.Exists || window.IsExiting) return;

                window.VSync = focused ? VSyncMode.Off : VSyncMode.On;
                if (window.WindowState != WindowState.Minimized)
                {
                    var tween = Math.Min((currentTime - fixedRateTime) / fixedRateUpdateDuration, 1);
                    editor.Draw(tween);
                    window.SwapBuffers();
                }

                if (!windowDisplayed)
                {
                    window.Visible = true;
                    windowDisplayed = true;
                }

                RunScheduledTasks();

                var activeDuration = watch.Elapsed.TotalSeconds - currentTime;
                var sleepMs = Math.Max(0, (int)(((focused ? targetFrameDuration : fixedRateUpdateDuration) - activeDuration) * 1000));
                Thread.Sleep(sleepMs);

                var frameTime = currentTime - previousTime;
                previousTime = currentTime;

                averageFrameTime = (frameTime + averageFrameTime) / 2;
                averageActiveTime = (activeDuration + averageActiveTime) / 2;
                longestFrameTime = Math.Max(frameTime, longestFrameTime);

                if (lastStatTime + 1 < currentTime)
                {
                    Stats = $"fps:{1 / averageFrameTime:0}/{1 / averageActiveTime:0} (act:{averageActiveTime * 1000:0} avg:{averageFrameTime * 1000:0} hi:{longestFrameTime * 1000:0})";

                    longestFrameTime = 0;
                    lastStatTime = currentTime;
                }
            }
        }

        #endregion

        #region Scheduling

        public static bool SchedulingEnabled { get; set; }

        static readonly Queue<Action> scheduledActions = new Queue<Action>();

        public static void enableScheduling() => SchedulingEnabled = true;

        /// <summary>
        /// Schedule the action to run in the main thread.
        /// Exceptions will be logged.
        /// </summary>
        public static void Schedule(Action action)
        {
            if (SchedulingEnabled) lock (scheduledActions) scheduledActions.Enqueue(action);
            else throw new InvalidOperationException("Scheduling isn't enabled");
        }

        /// <summary>
        /// Schedule the action to run in the main thread after a delay (in milliseconds).
        /// Exceptions will be logged.
        /// </summary>
        public static void Schedule(Action action, int delay) => Task.Run(async () =>
        {
            await Task.Delay(delay);
            Schedule(action);
        });

        /// <summary>
        /// Run the action synchronously in the main thread.
        /// Exceptions will be thrown to the calling thread.
        /// </summary>
        public static void RunMainThread(Action action)
        {
            if (IsMainThread)
            {
                action();
                return;
            }
            using (var completed = new ManualResetEvent(false))
            {
                Exception exception = null;
                Schedule(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                    completed.Set();
                });
                completed.WaitOne();

                if (exception != null) throw exception;
            }
        }
        public static void RunScheduledTasks()
        {
            CheckMainThread();

            Action[] actionsToRun;
            lock (scheduledActions)
            {
                actionsToRun = new Action[scheduledActions.Count];
                scheduledActions.CopyTo(actionsToRun, 0);
                scheduledActions.Clear();
            }

            foreach (var action in actionsToRun)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Scheduled task {action.Method} failed:\n{e}");
                }
            }
        }

        #endregion

        #region Error Handling

        public const string DefaultLogPath = "logs";

        static readonly object errorHandlerLock = new object();
        static volatile bool insideErrorHandler;

        static void setupLogging(string logsPath = null, string commonLogFilename = null, bool checkFrozen = false)
        {
            logsPath = logsPath ?? DefaultLogPath;
            var tracePath = Path.Combine(logsPath, commonLogFilename ?? "trace.log");
            var exceptionPath = Path.Combine(logsPath, commonLogFilename ?? "exception.log");
            var crashPath = Path.Combine(logsPath, commonLogFilename ?? "crash.log");
            var freezePath = Path.Combine(logsPath, commonLogFilename ?? "freeze.log");

            if (!Directory.Exists(logsPath)) Directory.CreateDirectory(logsPath);
            else
            {
                if (File.Exists(tracePath)) File.Delete(tracePath);
                if (File.Exists(exceptionPath)) File.Delete(exceptionPath);
            }

            Trace.WriteLine($"{FullName}\n");

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) => logError(e.Exception, exceptionPath, null, false);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => logError((Exception)e.ExceptionObject, crashPath, "crash", true);

            if (checkFrozen) setupFreezeCheck(e => logError(e, freezePath, null, false));
        }
        static void logError(Exception e, string filename, string reportType, bool show)
        {
            lock (errorHandlerLock)
            {
                if (insideErrorHandler) return;
                insideErrorHandler = true;

                try
                {
                    var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
                    using (StreamWriter w = new StreamWriter(logPath, true))
                    {
                        w.Write(DateTime.Now + " - ");
                        w.WriteLine(e);
                        w.WriteLine();
                    }

                    if (reportType != null) Report(reportType, e);

                    if (show)
                    {
                        var result = MessageBox.Show($"An error occured:\n\n{e.Message} ({e.GetType().Name})\n\nClick Ok if you want to receive and invitation to a Discord server where you can get help with this problem.", FullName, MessageBoxButtons.OKCancel);
                        if (result == DialogResult.OK) Process.Start(DiscordUrl);
                    }
                }
                catch (Exception e2)
                {
                    Trace.WriteLine(e2.Message);
                }
                finally
                {
                    insideErrorHandler = false;
                }
            }
        }
        public static void Report(string type, Exception e) => NetHelper.BlockingPost("http://a-damnae.rhcloud.com/storybrew/report.php", new NameValueCollection
        {
            ["reporttype"] = type,
            ["source"] = Settings?.Id ?? "-",
            ["version"] = Version.ToString(),
            ["content"] = e.ToString()
        }, (r, ex) => { });
        static void setupFreezeCheck(Action<Exception> action)
        {
            var mainThread = Thread.CurrentThread;
            var thread = new Thread(() =>
            {
                var answered = false;
                var frozen = 0;

                while (!SchedulingEnabled) Thread.Sleep(1000);
                while (true)
                {
                    answered = false;
                    Schedule(() => answered = true);

                    Thread.Sleep(1000);

                    if (!answered) frozen++;
                    if (frozen >= 3)
                    {
                        frozen = 0;

                        var wait = new AutoResetEvent(false);
                        wait.WaitOne();

                        StackTrace trace = null;
                        try
                        {
                            trace = new StackTrace(true);
                            action(new Exception(trace.ToString()));
                        }
                        catch (ThreadStateException e)
                        {
                            action(e);
                        }
                        try
                        {
                            wait.Set();
                        }
                        catch (ThreadStateException e)
                        {
                            action(e);
                        }
                    }
                }
            })
            {
                Name = "Freeze Checker",
                IsBackground = true
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        #endregion
    }
}