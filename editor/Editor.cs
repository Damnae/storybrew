using OpenTK;
using OpenTK.Graphics.OpenGL;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.Input;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.UserInterface;
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

            var inputDispatcher = new InputDispatcher();
            InputManager = new InputManager(window, inputDispatcher);

            ScreenLayerManager = new ScreenLayerManager(this);
            inputDispatcher.Add(createOverlay(ScreenLayerManager));
            inputDispatcher.Add(ScreenLayerManager.InputHandler);

            initializeVsCode();
            Restart(initialLayer);

            window.Resize += window_Resize;
            window.Closing += window_Closing;

            resizeToWindow();
        }

        public void Restart(ScreenLayer initialLayer = null, string message = null)
        {
            initializeOverlay();
            ScreenLayerManager.Set(initialLayer ?? new StartMenu());
            if (message != null) ScreenLayerManager.ShowMessage(message);
        }

        #region Overlay

        private WidgetManager overlay;
        private CameraOrtho overlayCamera;
        private LinearLayout overlayTop;
        private Slider volumeSlider;

        private WidgetManager createOverlay(ScreenLayerManager screenLayerManager)
        {
            return overlay = new WidgetManager(screenLayerManager)
            {
                Camera = overlayCamera = new CameraOrtho(),
            };
        }

        private void initializeOverlay()
        {
            overlay.Root.ClearWidgets();

            overlay.Root.Add(overlayTop = new LinearLayout(overlay)
            {
                AnchorTarget = overlay.Root,
                AnchorFrom = UiAlignment.Top,
                AnchorTo = UiAlignment.Top,
                Horizontal = true,
                Opacity = 0,
                Displayed = false,
                Children = new Widget[]
                {
                    new Label(overlay)
                    {
                        StyleName = "icon",
                        Icon = IconFont.VolumeUp,
                        AnchorTo = UiAlignment.Centre,
                    },
                    volumeSlider = new Slider(overlay)
                    {
                        MinValue = 0,
                        MaxValue = 1,
                        Step = 0.01f,
                        Value = Program.AudioManager.Volume,
                        Tooltip = $"Volume: {Program.AudioManager.Volume:P0}",
                        AnchorTo = UiAlignment.Centre,
                    }
                }
            });
            overlayTop.Pack(0, 0, 1024);

            overlay.Root.OnMouseWheel += (sender, e) =>
            {
                if (!InputManager.AltOnly)
                    return false;

                volumeSlider.Value += e.DeltaPrecise * 0.1f;
                return true;
            };
            volumeSlider.OnValueChanged += (sender, e) =>
            {
                Program.AudioManager.Volume = volumeSlider.Value;
                volumeSlider.Tooltip = $"Volume: {volumeSlider.Value:P0}";
            };
        }

        private void updateOverlay()
        {
            if (IsFixedRateUpdate)
            {
                var mousePosition = overlay.MousePosition;
                var bounds = overlayTop.Bounds;

                var showOverlayTop = InputManager.AltOnly || (overlayTop.Displayed && bounds.Top < mousePosition.Y && mousePosition.Y < bounds.Bottom);

                var opacity = overlayTop.Opacity;
                var targetOpacity = showOverlayTop ? 1f : 0f;
                if (Math.Abs(opacity - targetOpacity) <= 0.07f) opacity = targetOpacity;
                else opacity = MathHelper.Clamp(opacity + (opacity < targetOpacity ? 0.07f : -0.07f), 0, 1);

                overlayTop.Opacity = opacity;
                overlayTop.Displayed = opacity > 0;
            }
        }

        #endregion

        public void Dispose()
        {
            window.Resize -= window_Resize;
            window.Closing -= window_Closing;

            ScreenLayerManager.Dispose();
            overlay.Dispose();
            overlayCamera.Dispose();

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

            updateOverlay();
            ScreenLayerManager.Update();
        }

        public void Draw()
        {
            GL.ClearColor(ScreenLayerManager.BackgroundColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenLayerManager.Draw(drawContext);
            overlay.Draw(drawContext);
            DrawState.CompleteFrame();
        }

        public string GetStats()
        {
            var spriteRenderer = drawContext.SpriteRenderer;

            return string.Format("Sprite - t:{0}k f:{1:0.0}k b:{2} w:{3} lb:{4}",
                spriteRenderer.RenderedSpriteCount / 1000, spriteRenderer.FlushedBufferCount / 1000f, spriteRenderer.DiscardedBufferCount, spriteRenderer.BufferWaitCount, spriteRenderer.LargestBatch);
        }

        private void window_Resize(object sender, EventArgs e)
            => resizeToWindow();

        private void window_Closing(object sender, CancelEventArgs e)
            => e.Cancel = ScreenLayerManager.Close();

        private void initializeVsCode()
        {
            var vscodePath = Path.GetFullPath(".vscode");
            if (!Directory.Exists(vscodePath))
                Directory.CreateDirectory(vscodePath);

            File.WriteAllText(Path.Combine(vscodePath, "settings.json"), Encoding.UTF8.GetString(Resources.vscode_settings_json).StripUtf8Bom());
        }

        private void resizeToWindow()
        {
            var width = window.Width;
            var height = window.Height;

            DrawState.Viewport = new Rectangle(0, 0, width, height);
            if (width == 0 || height == 0) return;

            overlayCamera.VirtualHeight = (int)(height * Math.Max(1024f / width, 768f / height));
            overlayCamera.VirtualWidth = width * overlayCamera.VirtualHeight / height;
            overlay.Size = new Vector2(overlayCamera.VirtualWidth, overlayCamera.VirtualHeight);
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
