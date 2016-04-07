using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using StorybrewEditor.Audio;
using StorybrewEditor.Graphics;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace StorybrewEditor
{
    class Program
    {
        public const string Name = "storybrew editor";
        public const string Repository = "Damnae/storybrew";
        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static string FullName => $"{Name} {Version} ({Repository})";

        private static int mainThreadId;
        public static AudioManager audioManager;
        public static Settings settings;

        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;
        public static AudioManager AudioManager => audioManager;
        public static Settings Settings => settings;

        [STAThread]
        public static void Main()
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            setupLogging();

            settings = new Settings();
            var displayDevice = DisplayDevice.GetDisplay(DisplayIndex.Default);

            using (var window = createWindow(displayDevice))
            using (audioManager = new AudioManager(window.WindowInfo.Handle))
            using (var editor = new Editor(window))
            {
                DrawState.CheckError("initializing openGL context");
                Trace.WriteLine("graphics mode: " + window.Context.GraphicsMode);

                setupOpenGLDebugOutput();

                window.Icon = new Icon(typeof(Program), "icon.ico");
                window.Resize += (sender, e) =>
                {
                    editor.Draw();
                    window.SwapBuffers();
                };

                editor.Initialize();
                runMainLoop(window, editor, 1 / 60.0, 1 / displayDevice.RefreshRate);

                settings.Save();
            }
        }

        private static GameWindow createWindow(DisplayDevice displayDevice)
        {
            var graphicsMode = new GraphicsMode(new ColorFormat(32), 24, 8, 4, ColorFormat.Empty, 2, false);
#if DEBUG
            var contextFlags = GraphicsContextFlags.Debug | GraphicsContextFlags.ForwardCompatible;
#else
            var contextFlags = GraphicsContextFlags.ForwardCompatible;
#endif
            int windowWidth = 1366, windowHeight = 768;
            if (windowHeight >= displayDevice.Height)
            {
                windowWidth = 1024;
                windowHeight = 600;
                if (windowWidth >= displayDevice.Width) windowWidth = 800;
            }
            return new GameWindow(windowWidth, windowHeight, graphicsMode, Name, GameWindowFlags.Default, DisplayDevice.Default, 1, 0, contextFlags);
        }

        private static void runMainLoop(GameWindow window, Editor editor, double fixedRateUpdateDuration, double targetFrameDuration)
        {
            var time = 0.0;
            var fixedRateTime = 0.0;
            var averageFrameTime = 0.0;
            var longestFrameTime = 0.0;
            var lastStatTime = 0.0;
            var windowDisplayed = false;
            var watch = new Stopwatch();

            watch.Start();
            while (window.Exists && !window.IsExiting)
            {
                var focused = window.Focused;
                var currentTime = watch.Elapsed.TotalSeconds;
                var fixedUpdates = 0;

                window.ProcessEvents();

                while (time - fixedRateTime >= fixedRateUpdateDuration && fixedUpdates < 2)
                {
                    fixedRateTime += fixedRateUpdateDuration;
                    fixedUpdates++;

                    editor.Update(fixedRateTime, true);
                }
                if (focused && fixedUpdates == 0 && fixedRateTime < currentTime && currentTime < fixedRateTime + fixedRateUpdateDuration)
                    editor.Update(currentTime, false);

                if (!window.Exists || window.IsExiting) return;

                editor.Draw();
                window.VSync = focused ? VSyncMode.Off : VSyncMode.On;
                window.SwapBuffers();

                if (!windowDisplayed)
                {
                    window.Visible = true;
                    windowDisplayed = true;
                }

                runScheduledTasks();

                var activeDuration = watch.Elapsed.TotalSeconds - currentTime;
                var sleepMs = Math.Max(0, (int)(((focused ? targetFrameDuration : fixedRateUpdateDuration) - activeDuration) * 1000));
                Thread.Sleep(sleepMs);

                var frameTime = currentTime - time;
                time = currentTime;

                // Stats

                averageFrameTime = (frameTime + averageFrameTime) / 2;
                longestFrameTime = Math.Max(frameTime, longestFrameTime);

                if (lastStatTime + 10 < time)
                {
                    Debug.WriteLine($"Frame - avg:{averageFrameTime * 1000:0} hi:{longestFrameTime * 1000:0} fps:{1 / averageFrameTime:0}, TexBinds - {DrawState.TextureBinds}, {editor.GetStats()}");
                    longestFrameTime = 0;
                    lastStatTime = time;
                }
            }
        }

        #region Scheduling

        private static readonly Queue<Action> scheduledActions = new Queue<Action>();

        /// <summary>
        /// Schedule the action to run in the main thread.
        /// Exceptions will be logged.
        /// </summary>
        public static void Schedule(Action action)
        {
            lock (scheduledActions)
                scheduledActions.Enqueue(action);
        }

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

                if (exception != null)
                    throw exception;
            }
        }

        private static void runScheduledTasks()
        {
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

        private static TraceLogger logger;
        private static void setupLogging()
        {
#if DEBUG
            return;
#endif

            var logsPath = "logs";
            var tracePath = Path.Combine(logsPath, "trace.log");
            var exceptionPath = Path.Combine(logsPath, "exception.log");
            var crashPath = Path.Combine(logsPath, "crash.log");

            if (!Directory.Exists(logsPath))
                Directory.CreateDirectory(logsPath);
            else
            {
                if (File.Exists(tracePath)) File.Delete(tracePath);
                if (File.Exists(exceptionPath)) File.Delete(exceptionPath);
                if (File.Exists(crashPath)) File.Delete(crashPath);
            }

            logger = new TraceLogger(tracePath);
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) => logError(exceptionPath, e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => logError(crashPath, (Exception)e.ExceptionObject);
        }

        private static void logError(string filename, Exception e)
        {
            if (!IsMainThread)
            {
                Schedule(() => logError(filename, e));
                return;
            }

            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
                using (StreamWriter w = new StreamWriter(logPath, true))
                {
                    w.Write(DateTime.Now + " - ");
                    w.WriteLine(e);
                    w.WriteLine();
                }
            }
            catch (Exception e2)
            {
                Trace.WriteLine(e2.Message);
            }
        }

        private static DebugProc openGLDebugDelegate;
        private static void setupOpenGLDebugOutput()
        {
#if !DEBUG
            return;
#endif
            if (!DrawState.HasCapabilities(4, 3, "GL_KHR_debug"))
            {
                Trace.WriteLine("openGL debug output is unavailable");
                return;
            }

            Trace.WriteLine("\nenabling openGL debug output");

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            DrawState.CheckError("enabling debug output");

            openGLDebugDelegate = new DebugProc(openGLDebugCallback);

            GL.DebugMessageCallback(openGLDebugDelegate, IntPtr.Zero);
            GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, new int[0], true);
            DrawState.CheckError("setting up debug output");

            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, -1, "Debug output enabled");
            DrawState.CheckError("testing debug output");
        }

        private static void openGLDebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            Trace.WriteLine(source == DebugSource.DebugSourceApplication ?
                $"openGL - {Marshal.PtrToStringAnsi(message, length)}" :
                $"openGL - {Marshal.PtrToStringAnsi(message, length)}\n\tid:{id} severity:{severity} type:{type} source:{source}\n");
        }

        #endregion
    }
}
