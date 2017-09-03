using BrewLib.Audio;
using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using OpenTK.Input;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface;
using StorybrewEditor.UserInterface.Components;
using StorybrewEditor.UserInterface.Drawables;
using StorybrewEditor.Util;
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
        private Button changeMapButton;
        private Button fitButton;
        private Button playPauseButton;
        private Button projectFolderButton;
        private Button mapsetFolderButton;
        private Button saveButton;
        private Button exportButton;

        private Button settingsButton;
        private Button effectsButton;
        private Button layersButton;

        private EffectConfigUi effectConfigUi;

        private SettingsMenu settingsMenu;

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
                    changeMapButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.FilesO,
                        Tooltip = "Change beatmap",
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
                    playPauseButton = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.Play,
                        Tooltip = "Play/Pause\nShortcut: Space",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
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
                    settingsButton = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = "Settings",
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
                        Tooltip = "Open mapset folder\n(Right click to change)",
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

            WidgetManager.Root.Add(settingsMenu = new SettingsMenu(WidgetManager, project)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0),
            });

            WidgetManager.Root.Add(effectsList = new EffectList(WidgetManager, project, effectConfigUi)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0),
            });
            effectsList.OnEffectPreselect += effect =>
            {
                if (effect != null)
                    timeline.Highlight(effect.StartTime, effect.EndTime);
                else timeline.ClearHighlight();
            };
            effectsList.OnEffectSelected += effect => timeline.Value = (float)effect.StartTime / 1000;

            WidgetManager.Root.Add(layersList = new LayerList(WidgetManager, project.LayerManager)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0),
            });
            layersList.OnLayerPreselect += layer =>
            {
                if (layer != null)
                    timeline.Highlight(layer.StartTime, layer.EndTime);
                else timeline.ClearHighlight();
            };
            layersList.OnLayerSelected += layer => timeline.Value = (float)layer.StartTime / 1000;

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
            changeMapButton.OnClick += (sender, e) =>
            {
                if (project.MapsetManager.BeatmapCount > 2)
                    Manager.ShowContextMenu("Select a beatmap", beatmap => project.SelectBeatmap(beatmap.Id, beatmap.Name), project.MapsetManager.Beatmaps);
                else project.SwitchMainBeatmap();
            };
            Program.Settings.FitStoryboard.Bind(fitButton, () => resizeStoryboard());
            playPauseButton.OnClick += (sender, e) => audio.Playing = !audio.Playing;

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
                if (e == MouseButton.Left)
                {
                    var speed = audio.TimeFactor;
                    if (speed > 1) speed = 2;
                    speed *= 0.5;
                    if (speed < 0.2) speed = 1;
                    audio.TimeFactor = speed;
                }
                else if (e == MouseButton.Right)
                {
                    var speed = audio.TimeFactor;
                    if (speed < 1) speed = 1;
                    speed += speed >= 2 ? 1 : 0.5;
                    if (speed > 8) speed = 1;
                    audio.TimeFactor = speed;
                }
                else if (e == MouseButton.Middle)
                    audio.TimeFactor = audio.TimeFactor == 8 ? 1 : 8;

                audioTimeFactorButton.Text = $"{audio.TimeFactor:P0}";
            };

            MakeTabs(
                new Button[] { settingsButton, effectsButton, layersButton },
                new Widget[] { settingsMenu, effectsList, layersList });
            projectFolderButton.OnClick += (sender, e) =>
            {
                var path = Path.GetFullPath(project.ProjectFolderPath);
                if (Directory.Exists(path))
                    Process.Start(path);
            };
            mapsetFolderButton.OnClick += (sender, e) =>
            {
                var path = Path.GetFullPath(project.MapsetPath);
                if (e == MouseButton.Right || !Directory.Exists(path))
                    changeMapsetFolder();
                else Process.Start(path);
            };
            saveButton.OnClick += (sender, e) => saveProject();
            exportButton.OnClick += (sender, e) => exportProject();

            project.OnMapsetPathChanged += project_OnMapsetPathChanged;
            project.OnEffectsStatusChanged += project_OnEffectsStatusChanged;

            if (!Directory.Exists(project.MapsetPath))
                Manager.ShowMessage($"The mapset folder cannot be found.\n{project.MapsetPath}\n\nPlease select a new one.", () => changeMapsetFolder(), true);
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
                    case Key.O:
                        withSavePrompt(() => Manager.ShowOpenProject());
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
                            if (e.Shift)
                                ClipboardHelper.SetText(new TimeSpan(0, 0, 0, 0, (int)(audio.Time * 1000)).ToString(Program.Settings.TimeCopyFormat));
                            else ClipboardHelper.SetText(((int)(audio.Time * 1000)).ToString());
                            return true;
                        }
                        break;
                }
            }
            return base.OnKeyDown(e);
        }

        public override bool OnMouseWheel(MouseWheelEventArgs e)
        {
            var inputManager = Manager.GetContext<Editor>().InputManager;
            timeline.Scroll(-e.DeltaPrecise * (inputManager.Shift ? 4 : 1));
            return true;
        }

        private void changeMapsetFolder()
        {
            var initialDirectory = Path.GetFullPath(project.MapsetPath);
            if (!Directory.Exists(initialDirectory))
                initialDirectory = OsuHelper.GetOsuSongFolder();

            Manager.OpenFilePicker("Pick a new mapset location", "", initialDirectory, ".osu files (*.osu)|*.osu", (newPath) =>
            {
                if (!Directory.Exists(newPath) && File.Exists(newPath))
                    project.MapsetPath = Path.GetDirectoryName(newPath);
                else Manager.ShowMessage("Invalid mapset path.");
            });
        }

        private void saveProject()
            => Manager.AsyncLoading("Saving", () => project.Save());

        private void exportProject()
            => Manager.AsyncLoading("Exporting", () => project.ExportToOsb());

        public override void Update(bool isTop, bool isCovered)
        {
            base.Update(isTop, isCovered);

            changeMapButton.Disabled = project.MapsetManager.BeatmapCount < 2;
            playPauseButton.Icon = audio.Playing ? IconFont.Pause : IconFont.Play;
            saveButton.Disabled = !project.Changed;
            audio.Volume = WidgetManager.Root.Opacity;

            var time = (float)audio.Time;

            if (audio.Playing &&
                timeline.RepeatStart != timeline.RepeatEnd &&
                (time < timeline.RepeatStart - 0.005 || timeline.RepeatEnd < time))
            {
                audio.Time = time = timeline.RepeatStart;
            }

            timeline.SetValueSilent(time);
            if (Manager.GetContext<Editor>().IsFixedRateUpdate)
                timeButton.Text = $"{(int)time / 60:00}:{(int)time % 60:00}:{(int)(time * 1000) % 1000:000}";

            if (audio.Playing && mainStoryboardDrawable.Time < time)
                project.TriggerEvents(mainStoryboardDrawable.Time, time);

            mainStoryboardDrawable.Time = time;
            mainStoryboardDrawable.Clip = !Manager.GetContext<Editor>().InputManager.Alt;
            if (previewContainer.Visible)
                previewDrawable.Time = timeline.GetValueForPosition(Manager.GetContext<Editor>().InputManager.MousePosition);
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);

            bottomRightLayout.Pack(374);
            bottomLeftLayout.Pack(WidgetManager.Size.X - bottomRightLayout.Width);

            settingsMenu.Pack(bottomRightLayout.Width - 24, WidgetManager.Root.Height - bottomRightLayout.Height - 16);
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
            => withSavePrompt(() => Manager.GetContext<Editor>().Restart());

        private void withSavePrompt(Action action)
        {
            if (project.Changed)
            {
                Manager.ShowMessage("Do you wish to save the project?", () => Manager.AsyncLoading("Saving", () =>
                {
                    project.Save();
                    Program.Schedule(() => action());
                }), action, true);
            }
            else action();
        }

        private void project_OnMapsetPathChanged(object sender, EventArgs e)
        {
            var previousAudio = audio;

            audio = Program.AudioManager.LoadStream(project.AudioPath);
            timeline.MaxValue = (float)audio.Duration;

            if (previousAudio != null)
            {
                audio.Time = previousAudio.Time;
                audio.TimeFactor = previousAudio.TimeFactor;
                audio.Playing = previousAudio.Playing;
                previousAudio.Dispose();
            }
        }

        private void project_OnEffectsStatusChanged(object sender, EventArgs e)
        {
            switch (project.EffectsStatus)
            {
                case EffectStatus.ExecutionFailed:
                    statusIcon.Icon = IconFont.Bug;
                    statusMessage.Text = "An effect failed to execute.\nClick the Effects tabs, then the bug icon to see its error message.";
                    statusLayout.Pack(1024 - bottomRightLayout.Width - 24);
                    statusLayout.Displayed = true;
                    break;
                case EffectStatus.Updating:
                    statusIcon.Icon = IconFont.Spinner;
                    statusMessage.Text = "Updating effects...";
                    statusLayout.Pack(1024 - bottomRightLayout.Width - 24);
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
