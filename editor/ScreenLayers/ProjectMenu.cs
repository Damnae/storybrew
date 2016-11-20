using BrewLib.Audio;
using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using OpenTK.Input;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface;
using StorybrewEditor.UserInterface.Components;
using StorybrewEditor.UserInterface.Drawables;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace StorybrewEditor.ScreenLayers
{
    public class ProjectMenu : UiScreenLayer
    {
        private Project project;

        private DrawableContainer mainStoryboardContainer;
        private StoryboardDrawable mainStoryboardDrawable;

        private DrawableContainer previewContainer;
        private StoryboardDrawable previewDrawable;

        private LinearLayout statusLayout;
        private Label statusIcon;
        private Label statusMessage;

        private LinearLayout bottomLeftLayout;
        private LinearLayout bottomRightLayout;
        private Button timeButton;
        private Button divisorButton;
        private Button audioTimeFactorButton;
        private TimelineSlider timeline;
        private Button playPauseButton;
        private Button fitButton;
        private Button projectFolderButton;
        private Button mapsetFolderButton;
        private Button saveButton;
        private Button exportButton;

        private Button helpButton;
        private Button effectsButton;
        private Button layersButton;

        private EffectConfigUi effectConfigUi;
        private EffectList effectsList;
        private LayerList layersList;

        private AudioStream audio;

        private int snapDivisor = 4;

        public ProjectMenu(Project project)
        {
            this.project = project;
        }

        public override void Load()
        {
            base.Load();

            audio = Program.AudioManager.LoadStream(project.AudioPath);

            WidgetManager.Root.Add(mainStoryboardContainer = new DrawableContainer(WidgetManager)
            {
                Drawable = mainStoryboardDrawable = new StoryboardDrawable(project),
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
            });

            WidgetManager.Root.Add(bottomLeftLayout = new LinearLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.BottomLeft,
                AnchorTo = BoxAlignment.BottomLeft,
                Padding = new FourSide(16, 8, 16, 16),
                Horizontal = true,
                Fill = true,
                Children = new Widget[]
                {
                    timeButton = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        AnchorFrom = BoxAlignment.Centre,
                        Text = "--:--:---",
                        Tooltip = "Current time\nCtrl-C to copy",
                        CanGrow = false,
                    },
                    divisorButton = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = $"1/{snapDivisor}",
                        Tooltip = "Snap divisor",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    audioTimeFactorButton = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = $"{audio.TimeFactor:P0}",
                        Tooltip = "Audio speed",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    timeline = new TimelineSlider(WidgetManager, project)
                    {
                        AnchorFrom = BoxAlignment.Centre,
                        SnapDivisor = snapDivisor,
                    },
                    playPauseButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.Play,
                        Tooltip = "Play/Pause\nShortcut: Space",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    fitButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.Desktop,
                        Tooltip = "Fit/Fill",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                        Checkable = true,
                    },
                },
            });

            WidgetManager.Root.Add(bottomRightLayout = new LinearLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.BottomRight,
                Padding = new FourSide(16, 16, 16, 8),
                Horizontal = true,
                Fill = true,
                Children = new Widget[]
                {
                    helpButton = new Button(WidgetManager)
                    {
                        Text = "Help!",
                    },
                    effectsButton = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = "Effects",
                    },
                    layersButton = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = "Layers",
                    },
                    projectFolderButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.FolderOpen,
                        Tooltip = "Open project folder",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    mapsetFolderButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.FolderOpen,
                        Tooltip = "Open mapset folder",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    saveButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.Save,
                        Tooltip = "Save project\nShortcut: Ctrl-S",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    exportButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.PuzzlePiece,
                        Tooltip = "Export to .osb",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                },
            });

            WidgetManager.Root.Add(effectConfigUi = new EffectConfigUi(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.TopLeft,
                AnchorTo = BoxAlignment.TopLeft,
                Offset = new Vector2(16, 16),
                Displayed = false,
            });
            effectConfigUi.OnDisplayedChanged += (sender, e) => resizeStoryboard();

            WidgetManager.Root.Add(effectsList = new EffectList(WidgetManager, project, effectConfigUi)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0),
            });

            WidgetManager.Root.Add(layersList = new LayerList(WidgetManager, project.LayerManager)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0),
            });

            WidgetManager.Root.Add(statusLayout = new LinearLayout(WidgetManager)
            {
                StyleName = "tooltip",
                AnchorTarget = bottomLeftLayout,
                AnchorFrom = BoxAlignment.BottomLeft,
                AnchorTo = BoxAlignment.TopLeft,
                Offset = new Vector2(16, 0),
                Horizontal = true,
                Hoverable = false,
                Displayed = false,
                Children = new Widget[]
                {
                    statusIcon = new Label(WidgetManager)
                    {
                        StyleName = "icon",
                        AnchorFrom = BoxAlignment.Left,
                        CanGrow = false,
                    },
                    statusMessage = new Label(WidgetManager)
                    {
                        AnchorFrom = BoxAlignment.Left,
                    },
                },
            });

            WidgetManager.Root.Add(previewContainer = new DrawableContainer(WidgetManager)
            {
                StyleName = "storyboardPreview",
                Drawable = previewDrawable = new StoryboardDrawable(project),
                AnchorTarget = timeline,
                AnchorFrom = BoxAlignment.Bottom,
                AnchorTo = BoxAlignment.Top,
                Hoverable = false,
                Displayed = false,
                Size = new Vector2(16, 9) * 16,
            });

            timeButton.OnClick += (sender, e) => Manager.ShowPrompt("Skip to...", value =>
            {
                var time = 0.0f;
                if (float.TryParse(value, out time)) timeline.Value = time / 1000;
            });

            timeline.MaxValue = (float)audio.Duration;
            timeline.OnValueChanged += (sender, e) => audio.Time = timeline.Value;
            timeline.OnValueCommited += (sender, e) => timeline.Snap();
            timeline.OnHovered += (sender, e) => previewContainer.Displayed = e.Hovered;
            timeline.OnClickDown += (sender, e) =>
            {
                if (e.Button != MouseButton.Right) return false;
                project.SwitchMainBeatmap();
                return true;
            };
            playPauseButton.OnClick += (sender, e) => audio.Playing = !audio.Playing;
            Program.Settings.FitStoryboard.Bind(fitButton, () => resizeStoryboard());

            divisorButton.OnClick += (sender, e) =>
            {
                snapDivisor++;
                if (snapDivisor == 5 || snapDivisor == 7) snapDivisor++;
                if (snapDivisor == 9) snapDivisor = 16;
                if (snapDivisor > 16) snapDivisor = 1;
                timeline.SnapDivisor = snapDivisor;
                divisorButton.Text = $"1/{snapDivisor}";
            };
            audioTimeFactorButton.OnClick += (sender, e) =>
            {
                var speed = audio.TimeFactor * 0.5;
                if (speed < 0.2) speed = 1;
                audio.TimeFactor = speed;
                audioTimeFactorButton.Text = $"{audio.TimeFactor:P0}";
            };

            helpButton.OnClick += (sender, e) => Process.Start($"https://github.com/{Program.Repository}/wiki");
            MakeTabs(
                new Button[] { effectsButton, layersButton },
                new Widget[] { effectsList, layersList });
            projectFolderButton.OnClick += (sender, e) =>
            {
                var path = Path.GetFullPath(project.ProjectFolderPath);
                if (Directory.Exists(path))
                    Process.Start(path);
            };
            mapsetFolderButton.OnClick += (sender, e) =>
            {
                var path = Path.GetFullPath(project.MapsetPath);
                if (Directory.Exists(path))
                    Process.Start(path);
            };
            saveButton.OnClick += (sender, e) => saveProject();
            exportButton.OnClick += (sender, e) => exportProject();

            project.OnEffectsStatusChanged += project_OnEffectsStatusChanged;
        }

        public override bool OnKeyDown(KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    if (e.Control)
                    {
                        var nextBookmark = project.MainBeatmap.Bookmarks.FirstOrDefault(bookmark => bookmark > Math.Round(timeline.Value * 1000) + 50);
                        if (nextBookmark != 0) timeline.Value = nextBookmark * 0.001f;
                    }
                    else timeline.Scroll(e.Shift ? 4 : 1);
                    return true;
                case Key.Left:
                    if (e.Control)
                    {
                        var prevBookmark = project.MainBeatmap.Bookmarks.LastOrDefault(bookmark => bookmark < Math.Round(timeline.Value * 1000) - 500);
                        if (prevBookmark != 0) timeline.Value = prevBookmark * 0.001f;
                    }
                    else timeline.Scroll(e.Shift ? -4 : -1);
                    return true;
            }

            if (!e.IsRepeat)
            {
                switch (e.Key)
                {
                    case Key.Space:
                        playPauseButton.Click();
                        return true;
                    case Key.S:
                        if (e.Control)
                        {
                            saveProject();
                            return true;
                        }
                        break;
                    case Key.C:
                        if (e.Control)
                        {
                            System.Windows.Forms.Clipboard.SetText(((int)(audio.Time * 1000)).ToString());
                            return true;
                        }
                        break;
                }
            }
            return base.OnKeyDown(e);
        }

        public override bool OnMouseWheel(MouseWheelEventArgs e)
        {
            timeline.Scroll(-e.DeltaPrecise);
            return true;
        }

        private void saveProject()
            => Manager.AsyncLoading("Saving...", () => project.Save());

        private void exportProject()
            => Manager.AsyncLoading("Exporting...", () => project.ExportToOsb());

        public override void Update(bool isTop, bool isCovered)
        {
            base.Update(isTop, isCovered);

            playPauseButton.Icon = audio.Playing ? IconFont.Pause : IconFont.Play;
            audio.Volume = WidgetManager.Root.Opacity;

            var time = (float)audio.Time;
            timeline.SetValueSilent(time);
            if (Manager.GetContext<Editor>().IsFixedRateUpdate)
                timeButton.Text = $"{(int)time / 60:00}:{(int)time % 60:00}:{(int)(time * 1000) % 1000:000}";

            mainStoryboardDrawable.Time = time;
            if (previewContainer.Visible)
                previewDrawable.Time = timeline.GetValueForPosition(Manager.GetContext<Editor>().InputManager.MousePosition);
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);

            bottomLeftLayout.Pack(650);
            bottomRightLayout.Pack(1024 - bottomLeftLayout.Width);

            effectsList.Pack(bottomRightLayout.Width - 24, WidgetManager.Root.Height - bottomRightLayout.Height - 16);
            layersList.Pack(bottomRightLayout.Width - 24, WidgetManager.Root.Height - bottomRightLayout.Height - 16);

            effectConfigUi.Pack(bottomRightLayout.Width, WidgetManager.Root.Height - bottomLeftLayout.Height - 16);

            resizeStoryboard();
        }

        private void resizeStoryboard()
        {
            var parentSize = WidgetManager.Size;
            if (effectConfigUi.Displayed)
            {
                mainStoryboardContainer.Offset = new Vector2(effectConfigUi.Bounds.Right / 2, 0);
                parentSize.X -= effectConfigUi.Bounds.Right;
            }
            else mainStoryboardContainer.Offset = Vector2.Zero;
            mainStoryboardContainer.Size = fitButton.Checked ? new Vector2(parentSize.X, (parentSize.X * 9) / 16) : parentSize;
        }

        public override void Close()
        {
            Manager.ShowMessage("Do you wish to save the project?", () => Manager.AsyncLoading("Saving...", () =>
            {
                project.Save();
                Program.Schedule(() => Manager.GetContext<Editor>().Restart());
            }),
            () => Manager.GetContext<Editor>().Restart(), true);
        }

        private void project_OnEffectsStatusChanged(object sender, EventArgs e)
        {
            switch (project.EffectsStatus)
            {
                case EffectStatus.ExecutionFailed:
                    statusIcon.Icon = IconFont.Bug;
                    statusMessage.Text = "An effect failed to execute.\nClick the Effects tabs, then the bug icon to see its error message.";
                    statusLayout.Pack(bottomLeftLayout.Width - 24);
                    statusLayout.Displayed = true;
                    break;
                case EffectStatus.Updating:
                    statusIcon.Icon = IconFont.Spinner;
                    statusMessage.Text = "Updating effects...";
                    statusLayout.Pack(bottomLeftLayout.Width - 24);
                    statusLayout.Displayed = true;
                    break;
                default:
                    statusLayout.Displayed = false;
                    break;
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposedValue)
            {
                if (disposing)
                {
                    project.OnEffectsStatusChanged -= project_OnEffectsStatusChanged;
                    project.Dispose();
                    audio.Dispose();
                }
                project = null;
                audio = null;
                disposedValue = true;
            }
        }

        #endregion
    }
}
