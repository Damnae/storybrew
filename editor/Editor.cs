using OpenTK;
using OpenTK.Graphics.OpenGL;
using StorybrewEditor.Graphics;
using StorybrewEditor.Input;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.UserInterface.Skinning;
using StorybrewEditor.Util;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace StorybrewEditor
{
    public class Editor : IDisposable
    {
        private GameWindow window;
        public GameWindow Window => window;
        public readonly FormsWindow FormsWindow;

        private double time;
        private double delta;
        private bool isFixedRateUpdate;

        public double Time => time;
        public double TimeDelta => delta;
        public bool IsFixedRateUpdate => isFixedRateUpdate;

        private DrawContext drawContext;

        public Skin Skin;
        public ScreenLayerManager ScreenLayerManager;
        public InputManager InputManager;

        public Editor(GameWindow window)
        {
            this.window = window;
            FormsWindow = new FormsWindow(window.WindowInfo.Handle);
        }

        public void Initialize(ScreenLayer initialLayer = null)
        {
            DrawState.Initialize(window.Width, window.Height);
            drawContext = new DrawContext();

            try
            {
                Skin = Skin.Load("skin.json", drawContext.TextureContainer);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to load skin: {e}");
                Skin = new Skin(drawContext.TextureContainer);
            }
            ScreenLayerManager = new ScreenLayerManager(this);
            InputManager = new InputManager(window, ScreenLayerManager.InputHandler);

            initializeVsCode();
            Restart(initialLayer);

            window.Resize += window_Resize;
            window.Closing += window_Closing;
        }

        public void Restart(ScreenLayer initialLayer = null, string message = null)
        {
            ScreenLayerManager.Set(initialLayer ?? new StartMenu());
            if (message != null) ScreenLayerManager.ShowMessage(message);
        }

        public void Dispose()
        {
            window.Resize -= window_Resize;
            window.Closing -= window_Closing;

            ScreenLayerManager.Dispose();
            InputManager.Dispose();
            drawContext.Dispose();

            Skin.Dispose();

            DrawState.Cleanup();
        }

        public void Update(double time, bool isFixedRateUpdate)
        {
            delta = time - this.time;
            this.time = time;
            this.isFixedRateUpdate = isFixedRateUpdate;

            ScreenLayerManager.Update();
        }

        public void Draw()
        {
            GL.ClearColor(ScreenLayerManager.BackgroundColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenLayerManager.Draw(drawContext);
            DrawState.CompleteFrame();
        }

        public string GetStats()
        {
            var spriteRenderer = drawContext.SpriteRenderer;

            return string.Format("Sprite - t:{0}k f:{1:0.0}k b:{2} w:{3} lb:{4}",
                spriteRenderer.RenderedSpriteCount / 1000, spriteRenderer.FlushedBufferCount / 1000f, spriteRenderer.DiscardedBufferCount, spriteRenderer.BufferWaitCount, spriteRenderer.LargestBatch);
        }

        private void window_Resize(object sender, EventArgs e)
        {
            DrawState.Viewport = new Rectangle(0, 0, window.Width, window.Height);
        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = ScreenLayerManager.Close();
        }

        private void initializeVsCode()
        {
            var vscodePath = Path.GetFullPath(".vscode");
            if (!Directory.Exists(vscodePath))
                Directory.CreateDirectory(vscodePath);

            File.WriteAllText(Path.Combine(vscodePath, "settings.json"), Encoding.UTF8.GetString(Resources.vscode_settings_json).StripUtf8Bom());
        }
    }

    public class FormsWindow : System.Windows.Forms.IWin32Window
    {
        private IntPtr handle;
        public IntPtr Handle => handle;

        public FormsWindow(IntPtr handle)
        {
            this.handle = handle;
        }
    }
}
