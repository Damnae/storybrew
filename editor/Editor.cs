using BrewLib.Data;
using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.Graphics.Drawables;
using BrewLib.Graphics.Renderers;
using BrewLib.Graphics.Textures;
using BrewLib.Input;
using BrewLib.ScreenLayers;
using BrewLib.Time;
using BrewLib.UserInterface;
using BrewLib.UserInterface.Skinning;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using StorybrewEditor.ScreenLayers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace StorybrewEditor
{
    public class Editor : IDisposable
    {
        public GameWindow Window { get; }
        public readonly FormsWindow FormsWindow;

        readonly FrameClock clock = new FrameClock();
        public FrameTimeSource TimeSource => clock;

        public bool IsFixedRateUpdate { get; set; }

        DrawContext drawContext;
        public ResourceContainer ResourceContainer;
        public Skin Skin;
        public ScreenLayerManager ScreenLayerManager;
        public InputManager InputManager;

        public Editor(GameWindow window)
        {
            Window = window;
            FormsWindow = new FormsWindow(window.GetWindowHandle());
        }

        public void Initialize(ScreenLayer initialLayer = null)
        {
            ResourceContainer = new AssemblyResourceContainer(Assembly.GetEntryAssembly(), $"{nameof(StorybrewEditor)}.Resources", "resources");
            DrawState.Initialize(ResourceContainer, Window.Width, Window.Height);

            drawContext = new DrawContext();
            drawContext.Register(this);
            drawContext.Register<TextureContainer>(new TextureContainerAtlas(ResourceContainer), true);
            drawContext.Register<QuadRenderer>(new QuadRendererBuffered(), true);
            drawContext.Register<LineRenderer>(new LineRendererBuffered(), true);

            try
            {
                var brewLibAssembly = Assembly.GetAssembly(typeof(Drawable));
                Skin = new Skin(drawContext.Get<TextureContainer>())
                {
                    ResolveDrawableType = (drawableTypeName) => brewLibAssembly.GetType(
                        $"{nameof(BrewLib)}.{nameof(BrewLib.Graphics)}.{nameof(BrewLib.Graphics.Drawables)}.{drawableTypeName}", true, true),

                    ResolveWidgetType = (widgetTypeName) => Type.GetType(
                        $"{nameof(StorybrewEditor)}.{nameof(UserInterface)}.{widgetTypeName}", false, true) ??
                        brewLibAssembly.GetType($"{nameof(BrewLib)}.{nameof(UserInterface)}.{widgetTypeName}", true, true),

                    ResolveStyleType = (styleTypeName) => Type.GetType(
                        $"{nameof(StorybrewEditor)}.{nameof(UserInterface)}.{nameof(UserInterface.Skinning)}.{nameof(UserInterface.Skinning.Styles)}.{styleTypeName}", false, true) ??
                        brewLibAssembly.GetType($"{nameof(BrewLib)}.{nameof(UserInterface)}.{nameof(UserInterface.Skinning)}.{nameof(UserInterface.Skinning.Styles)}.{styleTypeName}", true, true),
                };
                Skin.Load("skin.json", ResourceContainer);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to load skin: {e}");
                Skin = new Skin(drawContext.Get<TextureContainer>());
            }

            var inputDispatcher = new InputDispatcher();
            InputManager = new InputManager(Window, inputDispatcher);

            ScreenLayerManager = new ScreenLayerManager(Window, clock, this);
            inputDispatcher.Add(createOverlay(ScreenLayerManager));
            inputDispatcher.Add(ScreenLayerManager.InputHandler);

            Restart(initialLayer);

            Window.Resize += window_Resize;
            Window.Closing += window_Closing;

            resizeToWindow();
        }
        public void Restart(ScreenLayer initialLayer = null, string message = null)
        {
            initializeOverlay();
            ScreenLayerManager.Set(initialLayer ?? new StartMenu());
            if (message != null) ScreenLayerManager.ShowMessage(message);
        }

        #region Overlay

        WidgetManager overlay;
        CameraOrtho overlayCamera;
        LinearLayout overlayTop, altOverlayTop;
        Slider volumeSlider;
        Label statsLabel;

        WidgetManager createOverlay(ScreenLayerManager screenLayerManager) => overlay = new WidgetManager(screenLayerManager, InputManager, Skin)
        {
            Camera = overlayCamera = new CameraOrtho()
        };

        void initializeOverlay()
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
                        Displayed = Program.Settings.ShowStats
                    }
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
                        AnchorTo = BoxAlignment.Centre
                    },
                    volumeSlider = new Slider(overlay)
                    {
                        Step = .01f,
                        AnchorTo = BoxAlignment.Centre
                    }
                }
            });
            altOverlayTop.Pack(0, 0, 1024);

            Program.Settings.Volume.Bind(volumeSlider, () => volumeSlider.Tooltip = $"Volume: {volumeSlider.Value:P0}");
            overlay.Root.OnMouseWheel += (sender, e) =>
            {
                if (!InputManager.AltOnly) return false;

                volumeSlider.Value += e.DeltaPrecise * 0.05f;
                return true;
            };
        }
        void updateOverlay()
        {
            if (IsFixedRateUpdate)
            {
                var mousePosition = overlay.MousePosition;
                var bounds = altOverlayTop.Bounds;

                var showAltOverlayTop = InputManager.AltOnly || (altOverlayTop.Displayed && bounds.Top < mousePosition.Y && mousePosition.Y < bounds.Bottom);

                var altOpacity = altOverlayTop.Opacity;
                var targetOpacity = showAltOverlayTop ? 1f : 0;
                if (Math.Abs(altOpacity - targetOpacity) <= .07) altOpacity = targetOpacity;
                else altOpacity = MathHelper.Clamp(altOpacity + (altOpacity < targetOpacity ? .07f : -.07f), 0, 1);

                overlayTop.Opacity = 1 - altOpacity;
                overlayTop.Displayed = altOpacity < 1;

                altOverlayTop.Opacity = altOpacity;
                altOverlayTop.Displayed = altOpacity > 0;

                if (statsLabel.Visible) statsLabel.Text = Program.Stats;
            }
        }

        #endregion

        public void Dispose()
        {
            Window.Resize -= window_Resize;
            Window.Closing -= window_Closing;

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
            IsFixedRateUpdate = isFixedRateUpdate;
            clock.AdvanceFrameTo(time);

            updateOverlay();
            ScreenLayerManager.Update(IsFixedRateUpdate);
        }
        public void Draw(double tween)
        {
            GL.ClearColor(ScreenLayerManager.BackgroundColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ScreenLayerManager.Draw(drawContext, tween);
            overlay.Draw(drawContext);
            DrawState.CompleteFrame();
        }
        public string GetStats()
        {
            var spriteRenderer = drawContext.Get<QuadRenderer>();
            return string.Format("Sprite - t:{0}k f:{1:0.0}k b:{2} w:{3} lb:{4}",
                spriteRenderer.RenderedQuadCount / 1000, spriteRenderer.FlushedBufferCount / 1000f, spriteRenderer.DiscardedBufferCount, spriteRenderer.BufferWaitCount, spriteRenderer.LargestBatch);
        }

        void window_Resize(object sender, EventArgs e) => resizeToWindow();
        void window_Closing(object sender, CancelEventArgs e) => e.Cancel = ScreenLayerManager.Close();
        void resizeToWindow()
        {
            var width = Window.Width;
            var height = Window.Height;
            if (width == 0 || height == 0) return;

            DrawState.Viewport = new Rectangle(0, 0, width, height);

            overlayCamera.VirtualHeight = (int)(height * Math.Max(1024d / width, 768d / height));
            overlayCamera.VirtualWidth = width * overlayCamera.VirtualHeight / height;
            overlay.Size = new Vector2(overlayCamera.VirtualWidth, overlayCamera.VirtualHeight);
        }
    }
    public class FormsWindow : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; }
        public FormsWindow(IntPtr handle) => Handle = handle;
    }
}