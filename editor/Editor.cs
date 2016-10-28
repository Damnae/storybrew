using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Input;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.UserInterface;
using StorybrewEditor.UserInterface.Skinning;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

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
            FormsWindow = new FormsWindow(window.GetWindowHandle());
        }

        public void Initialize(ScreenLayer initialLayer = null)
        {
            DrawState.Initialize(Resources.ResourceManager, window.Width, window.Height);
            drawContext = new DrawContext(Resources.ResourceManager);

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
        private LinearLayout altOverlayTop;
        private Slider volumeSlider;
        private Label statsLabel;

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
                AnchorFrom = BoxAlignment.Top,
                AnchorTo = BoxAlignment.Top,
                Horizontal = true,
                Opacity = 0,
                Displayed = false,
                Children = new Widget[]
                {
                    statsLabel = new Label(overlay)
                    {
                        StyleName = "small",
                        AnchorTo = BoxAlignment.Centre,
                        Displayed = Program.Settings.ShowStats,
                    },
                }
            });
            overlayTop.Pack(1024, 16);

            overlay.Root.Add(altOverlayTop = new LinearLayout(overlay)
            {
                AnchorTarget = overlay.Root,
                AnchorFrom = BoxAlignment.Top,
                AnchorTo = BoxAlignment.Top,
                Horizontal = true,
                Opacity = 0,
                Displayed = false,
                Children = new Widget[]
                {
                    new Label(overlay)
                    {
                        StyleName = "icon",
                        Icon = IconFont.VolumeUp,
                        AnchorTo = BoxAlignment.Centre,
                    },
                    volumeSlider = new Slider(overlay)
                    {
                        Step = 0.01f,
                        AnchorTo = BoxAlignment.Centre,
                    },
                }
            });
            altOverlayTop.Pack(0, 0, 1024);

            Program.Settings.Volume.Bind(volumeSlider, () => volumeSlider.Tooltip = $"Volume: {volumeSlider.Value:P0}");
            overlay.Root.OnMouseWheel += (sender, e) =>
            {
                if (!InputManager.AltOnly)
                    return false;

                volumeSlider.Value += e.DeltaPrecise * 0.05f;
                return true;
            };
        }

        private void updateOverlay()
        {
            if (IsFixedRateUpdate)
            {
                var mousePosition = overlay.MousePosition;
                var bounds = altOverlayTop.Bounds;

                var showAltOverlayTop = InputManager.AltOnly || (altOverlayTop.Displayed && bounds.Top < mousePosition.Y && mousePosition.Y < bounds.Bottom);

                var altOpacity = altOverlayTop.Opacity;
                var targetOpacity = showAltOverlayTop ? 1f : 0f;
                if (Math.Abs(altOpacity - targetOpacity) <= 0.07f) altOpacity = targetOpacity;
                else altOpacity = MathHelper.Clamp(altOpacity + (altOpacity < targetOpacity ? 0.07f : -0.07f), 0, 1);

                overlayTop.Opacity = 1 - altOpacity;
                overlayTop.Displayed = altOpacity < 1;

                altOverlayTop.Opacity = altOpacity;
                altOverlayTop.Displayed = altOpacity > 0;

                if (statsLabel.Visible)
                    statsLabel.Text = Program.Stats;
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

        private void resizeToWindow()
        {
            var width = window.Width;
            var height = window.Height;
            if (width == 0 || height == 0) return;

            DrawState.Viewport = new Rectangle(0, 0, width, height);

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
