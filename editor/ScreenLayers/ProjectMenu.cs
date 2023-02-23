using BrewLib.Audio;
using BrewLib.Time;
using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using OpenTK.Input;
using StorybrewCommon.Mapset;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface;
using StorybrewEditor.UserInterface.Components;
using StorybrewEditor.UserInterface.Drawables;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace StorybrewEditor.ScreenLayers
{
    public class ProjectMenu : UiScreenLayer
    {
        Project proj;

        DrawableContainer storyboardContainer, previewContainer;
        StoryboardDrawable storyboardDrawable, previewDrawable;

        LinearLayout statusLayout, bottomLeftLayout, bottomRightLayout;
        Label statusIcon, statusMessage, warningsLabel;

        Button timeB, divisorB, audioTimeB, mapB, fitB, playB, projFolderB, mapFolderB, saveB, exportB, settingB, effectB, layerB;

        TimelineSlider timeline;

        EffectList effects;
        LayerList layers;
        SettingsMenu settings;

        EffectConfigUi effectUI;

        AudioStream audio;
        TimeSourceExtender timeSource;
        double? pendingSeek;

        int defaultDiv = 4;
        Vector2 storyboardPosition;

        public ProjectMenu(Project project) => proj = project;

        public override void Load()
        {
            base.Load();
            refreshAudio();

            WidgetManager.Root.Add(storyboardContainer = new DrawableContainer(WidgetManager)
            {
                Drawable = storyboardDrawable = new StoryboardDrawable(proj)
                {
                    UpdateFrameStats = true
                },
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre
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
                    timeB = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        AnchorFrom = BoxAlignment.Centre,
                        Text = "--:--:---",
                        Tooltip = "Current time\nCtrl-C to copy",
                        CanGrow = false
                    },
                    divisorB = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = $"1/{defaultDiv}",
                        Tooltip = "Snap divisor",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    },
                    audioTimeB = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = $"{timeSource.TimeFactor:P0}",
                        Tooltip = "Audio speed",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    },
                    timeline = new TimelineSlider(WidgetManager, proj)
                    {
                        AnchorFrom = BoxAlignment.Centre,
                        SnapDivisor = defaultDiv
                    },
                    mapB = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.FilesO,
                        Tooltip = "Change beatmap",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    },
                    fitB = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.Desktop,
                        Tooltip = "Fit/Fill",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                        Checkable = true
                    },
                    playB = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.Play,
                        Tooltip = "Play/Pause\nShortcut: Space",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    }
                }
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
                    effectB = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = "Effects"
                    },
                    layerB = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = "Layers"
                    },
                    settingB = new Button(WidgetManager)
                    {
                        StyleName = "small",
                        Text = "Settings"
                    },
                    projFolderB = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.FolderOpen,
                        Tooltip = "Open project folder",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    },
                    mapFolderB = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.FolderOpen,
                        Tooltip = "Open mapset folder\n(Right click to change)",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    },
                    saveB = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.Save,
                        Tooltip = "Save project\nShortcut: Ctrl-S",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    },
                    exportB = new Button(WidgetManager)
                    {
                        StyleName = "icon",
                        Icon = IconFont.PuzzlePiece,
                        Tooltip = "Export to .osb\n(Right click to export once for each diff)",
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false
                    }
                }
            });
            WidgetManager.Root.Add(effectUI = new EffectConfigUi(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.TopLeft,
                AnchorTo = BoxAlignment.TopLeft,
                Offset = new Vector2(16, 16),
                Displayed = false
            });
            effectUI.OnDisplayedChanged += (sender, e) => resizeStoryboard();

            WidgetManager.Root.Add(effects = new EffectList(WidgetManager, proj, effectUI)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0)
            });
            effects.OnEffectPreselect += effect =>
            {
                if (effect != null) timeline.Highlight(effect.StartTime, effect.EndTime);
                else timeline.ClearHighlight();
            };
            effects.OnEffectSelected += effect => timeline.Value = (float)effect.StartTime / 1000;

            WidgetManager.Root.Add(layers = new LayerList(WidgetManager, proj.LayerManager)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0)
            });
            layers.OnLayerPreselect += layer =>
            {
                if (layer != null) timeline.Highlight(layer.StartTime, layer.EndTime);
                else timeline.ClearHighlight();
            };
            layers.OnLayerSelected += layer => timeline.Value = (float)layer.StartTime / 1000;

            WidgetManager.Root.Add(settings = new SettingsMenu(WidgetManager, proj)
            {
                AnchorTarget = bottomRightLayout,
                AnchorFrom = BoxAlignment.BottomRight,
                AnchorTo = BoxAlignment.TopRight,
                Offset = new Vector2(-16, 0)
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
                        CanGrow = false
                    },
                    statusMessage = new Label(WidgetManager)
                    {
                        AnchorFrom = BoxAlignment.Left
                    }
                }
            });
            WidgetManager.Root.Add(warningsLabel = new Label(WidgetManager)
            {
                StyleName = "tooltip",
                AnchorTarget = timeline,
                AnchorFrom = BoxAlignment.BottomLeft,
                AnchorTo = BoxAlignment.TopLeft,
                Offset = new Vector2(0, -8)
            });
            WidgetManager.Root.Add(previewContainer = new DrawableContainer(WidgetManager)
            {
                StyleName = "storyboardPreview",
                Drawable = previewDrawable = new StoryboardDrawable(proj),
                AnchorTarget = timeline,
                AnchorFrom = BoxAlignment.Bottom,
                AnchorTo = BoxAlignment.Top,
                Hoverable = false,
                Displayed = false,
                Size = new Vector2(16, 9) * 16
            });
            timeB.OnClick += (sender, e) => Manager.ShowPrompt("Skip to...", value =>
            {
                if (float.TryParse(value, out float time)) timeline.Value = time / 1000;
            });

            resizeTimeline();
            timeline.OnValueChanged += (sender, e) => pendingSeek = timeline.Value;
            timeline.OnValueCommited += (sender, e) => timeline.Snap();
            timeline.OnHovered += (sender, e) => previewContainer.Displayed = e.Hovered;

            mapB.OnClick += (sender, e) =>
            {
                if (proj.MapsetManager.BeatmapCount > 2) Manager.ShowContextMenu("Select a beatmap", beatmap =>
                    proj.SelectBeatmap(beatmap.Id, beatmap.Name), proj.MapsetManager.Beatmaps);
                else proj.SwitchMainBeatmap();
            };
            Program.Settings.FitStoryboard.Bind(fitB, () => resizeStoryboard());
            playB.OnClick += (sender, e) => timeSource.Playing = !timeSource.Playing;

            divisorB.OnClick += (sender, e) =>
            {
                defaultDiv++;
                if (defaultDiv == 5 || defaultDiv == 7) defaultDiv++;
                if (defaultDiv == 9) defaultDiv = 12;
                if (defaultDiv == 13) defaultDiv = 16;
                if (defaultDiv > 16) defaultDiv = 1;
                timeline.SnapDivisor = defaultDiv;
                divisorB.Text = $"1/{defaultDiv}";
            };
            audioTimeB.OnClick += (sender, e) =>
            {
                if (e == MouseButton.Left)
                {
                    var speed = timeSource.TimeFactor;
                    if (speed > 1) speed = 2;
                    speed /= 2;
                    if (speed < .2) speed = 1;
                    timeSource.TimeFactor = speed;
                }
                else if (e == MouseButton.Right)
                {
                    var speed = timeSource.TimeFactor;
                    if (speed < 1) speed = 1;
                    speed += speed >= 2 ? 1 : .5;
                    if (speed > 8) speed = 1;
                    timeSource.TimeFactor = speed;
                }
                else if (e == MouseButton.Middle) timeSource.TimeFactor = timeSource.TimeFactor == 8 ? 1 : 8;

                audioTimeB.Text = $"{timeSource.TimeFactor:P0}";
            };

            MakeTabs(new Button[] { settingB, effectB, layerB }, new Widget[] { settings, effects, layers });
            projFolderB.OnClick += (sender, e) =>
            {
                var path = Path.GetFullPath(proj.ProjectFolderPath);
                if (Directory.Exists(path)) Process.Start(path);
            };
            mapFolderB.OnClick += (sender, e) =>
            {
                var path = Path.GetFullPath(proj.MapsetPath);
                if (e == MouseButton.Right || !Directory.Exists(path)) changeMapsetFolder();
                else Process.Start(path);
            };
            saveB.OnClick += (sender, e) => saveProject();
            exportB.OnClick += (sender, e) =>
            {
                if (e == MouseButton.Right) exportProjectAll();
                else exportProject();
            };

            proj.OnMapsetPathChanged += project_OnMapsetPathChanged;
            proj.OnEffectsContentChanged += project_OnEffectsContentChanged;
            proj.OnEffectsStatusChanged += project_OnEffectsStatusChanged;

            if (!proj.MapsetPathIsValid) Manager.ShowMessage(
                $"The mapset folder cannot be found.\n{proj.MapsetPath}\n\nPlease select a new one.", () => changeMapsetFolder(), true);
        }
        public override bool OnKeyDown(KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    if (e.Control)
                    {
                        var nextBookmark = proj.MainBeatmap.Bookmarks.FirstOrDefault(bookmark => bookmark > Math.Round(timeline.Value * 1000) + 50);
                        if (nextBookmark != 0) timeline.Value = nextBookmark * .001f;
                    }
                    else timeline.Scroll(e.Shift ? 4 : 1);
                    return true;

                case Key.Left:
                    if (e.Control)
                    {
                        var prevBookmark = proj.MainBeatmap.Bookmarks.LastOrDefault(bookmark => bookmark < Math.Round(timeline.Value * 1000) - 500);
                        if (prevBookmark != 0) timeline.Value = prevBookmark * .001f;
                    }
                    else timeline.Scroll(e.Shift ? -4 : -1);
                    return true;
            }

            if (!e.IsRepeat)
            {
                switch (e.Key)
                {
                    case Key.Space: playB.Click(); return true;
                    case Key.O: withSavePrompt(() => Manager.ShowOpenProject()); return true;
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
                            if (e.Shift) ClipboardHelper.SetText(new TimeSpan(0, 0, 0, 0, (int)(timeSource.Current * 1000)).ToString(Program.Settings.TimeCopyFormat));
                            else if (e.Alt) ClipboardHelper.SetText($"{storyboardPosition.X:###}, {storyboardPosition.Y:###}");
                            else ClipboardHelper.SetText(((int)(timeSource.Current * 1000)).ToString());
                            return true;
                        }
                        break;
                }
            }
            return base.OnKeyDown(e);
        }
        public override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            var bounds = storyboardContainer.Bounds;
            var scale = OsuHitObject.StoryboardSize.Y / bounds.Height;

            storyboardPosition = (WidgetManager.MousePosition - new Vector2(bounds.Left, bounds.Top)) * scale;
            storyboardPosition.X -= (bounds.Width * scale - OsuHitObject.StoryboardSize.X) / 2;
        }
        public override bool OnMouseWheel(MouseWheelEventArgs e)
        {
            var inputManager = Manager.GetContext<Editor>().InputManager;
            timeline.Scroll(-e.DeltaPrecise * (inputManager.Shift ? 4 : 1));
            return true;
        }
        void changeMapsetFolder()
        {
            var initialDirectory = Path.GetFullPath(proj.MapsetPath);
            if (!Directory.Exists(initialDirectory)) initialDirectory = OsuHelper.GetOsuSongFolder();

            Manager.OpenFilePicker("Pick a new mapset location", "", initialDirectory, ".osu files (*.osu)|*.osu", (newPath) =>
            {
                if (!Directory.Exists(newPath) && File.Exists(newPath)) proj.MapsetPath = Path.GetDirectoryName(newPath);
                else Manager.ShowMessage("Invalid mapset path.");
            });
        }
        void saveProject() => Manager.AsyncLoading("Saving", () => Program.RunMainThread(() => proj.Save()));
        void exportProject() => Manager.AsyncLoading("Exporting", () => proj.ExportToOsb());
        void exportProjectAll()
        {
            Manager.AsyncLoading("Exporting", () =>
            {
                var first = true;
                foreach (var beatmap in proj.MapsetManager.Beatmaps)
                {
                    Program.RunMainThread(() => proj.MainBeatmap = beatmap);

                    while (proj.EffectsStatus != EffectStatus.Ready)
                    {
                        switch (proj.EffectsStatus)
                        {
                            case EffectStatus.CompilationFailed:
                            case EffectStatus.ExecutionFailed:
                            case EffectStatus.LoadingFailed: throw new Exception($"An effect failed to execute ({proj.EffectsStatus})\nCheck its log for the actual error.");
                        }
                        Thread.Sleep(200);
                    }

                    proj.ExportToOsb(first);
                    first = false;
                }
            });
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (pendingSeek.HasValue)
            {
                timeSource.Seek(pendingSeek.Value);
                pendingSeek = null;
            }
        }
        public override void Update(bool isTop, bool isCovered)
        {
            base.Update(isTop, isCovered);

            timeSource.Update();
            var time = (float)(pendingSeek ?? timeSource.Current);

            mapB.Disabled = proj.MapsetManager.BeatmapCount < 2;
            playB.Icon = timeSource.Playing ? IconFont.Pause : IconFont.Play;
            saveB.Disabled = !proj.Changed;
            exportB.Disabled = !proj.MapsetPathIsValid;
            audio.Volume = WidgetManager.Root.Opacity;

            if (timeSource.Playing)
            {
                if (timeline.RepeatStart != timeline.RepeatEnd && (time < timeline.RepeatStart - 0.005 || timeline.RepeatEnd < time))
                    pendingSeek = time = timeline.RepeatStart;
                else if (timeSource.Current > timeline.MaxValue)
                {
                    timeSource.Playing = false;
                    pendingSeek = timeline.MaxValue;
                }
            }

            timeline.SetValueSilent(time);
            if (Manager.GetContext<Editor>().IsFixedRateUpdate)
            {
                timeB.Text = Manager.GetContext<Editor>().InputManager.Alt ?
                    $"{storyboardPosition.X:000}, {storyboardPosition.Y:000}" :
                    $"{(time < 0 ? "-" : "")}{(int)Math.Abs(time / 60):00}:{(int)Math.Abs(time % 60):00}:{(int)Math.Abs(time * 1000) % 1000:000}";

                warningsLabel.Text = buildWarningMessage();
                warningsLabel.Displayed = warningsLabel.Text.Length > 0;
                warningsLabel.Pack(width: 600);
                warningsLabel.Pack();
            }

            if (timeSource.Playing && storyboardDrawable.Time < time) proj.TriggerEvents(storyboardDrawable.Time, time);

            storyboardDrawable.Time = time;
            storyboardDrawable.Clip = !Manager.GetContext<Editor>().InputManager.Alt;
            if (previewContainer.Visible) previewDrawable.Time = timeline.GetValueForPosition(Manager.GetContext<Editor>().InputManager.MousePosition);
        }
        string buildWarningMessage()
        {
            var warnings = "";

            var activeSpriteCount = proj.FrameStats.SpriteCount;
            if (activeSpriteCount > 0 && activeSpriteCount < 1500 && proj.DisplayDebugWarning) warnings += $"{activeSpriteCount:n0} Sprites\n";
            else if (activeSpriteCount >= 1500) warnings += $"⚠ {activeSpriteCount:n0} Sprites\n";

            var commandCount = proj.FrameStats.CommandCount;
            if (commandCount > 0 && commandCount < 15000 && proj.DisplayDebugWarning) warnings += $"{commandCount:n0} Commands\n";
            else if (commandCount >= 15000) warnings += $"⚠ {commandCount:n0} Commands\n";

            var effectiveCommandCount = proj.FrameStats.EffectiveCommandCount;
            var unusedCommandCount = commandCount - effectiveCommandCount;
            var unusedCommandFactor = (double)unusedCommandCount / commandCount;
            if ((unusedCommandCount >= 5000 && unusedCommandFactor > .5) ||
                (unusedCommandCount >= 10000 && unusedCommandFactor > .2) ||
                unusedCommandCount >= 15000)
                warnings += $"⚠ {unusedCommandCount:n0} ({unusedCommandFactor:0%}) Commands on Hidden Sprites\n";

            if (proj.FrameStats.OverlappedCommands) warnings += $"⚠ Overlapped Commands\n";
            if (proj.FrameStats.IncompatibleCommands) warnings += $"⚠ Incompatible Commands\n";

            var screenFill = proj.FrameStats.ScreenFill;
            if (screenFill > 0 && screenFill < 5 && proj.DisplayDebugWarning) warnings += $"{(int)screenFill}x Screen Fill\n";
            else if (screenFill >= 5) warnings += $"⚠ {(int)screenFill}x Screen Fill\n";

            return warnings.TrimEnd('\n');
        }
        public override void Resize(int width, int height)
        {
            base.Resize(width, height);

            bottomRightLayout.Pack(374);
            bottomLeftLayout.Pack(WidgetManager.Size.X - bottomRightLayout.Width);

            settings.Pack(bottomRightLayout.Width - 24, WidgetManager.Root.Height - bottomRightLayout.Height - 16);
            effects.Pack(bottomRightLayout.Width - 24, WidgetManager.Root.Height - bottomRightLayout.Height - 16);
            layers.Pack(bottomRightLayout.Width - 24, WidgetManager.Root.Height - bottomRightLayout.Height - 16);

            effectUI.Pack(bottomRightLayout.Width, WidgetManager.Root.Height - bottomLeftLayout.Height - 16);
            resizeStoryboard();
        }
        void resizeStoryboard()
        {
            var parentSize = WidgetManager.Size;
            if (effectUI.Displayed)
            {
                storyboardContainer.Offset = new Vector2(effectUI.Bounds.Right / 2, 0);
                parentSize.X -= effectUI.Bounds.Right;
            }
            else storyboardContainer.Offset = Vector2.Zero;
            storyboardContainer.Size = fitB.Checked ? new Vector2(parentSize.X, parentSize.X * 9 / 16) : parentSize;
        }
        void resizeTimeline()
        {
            timeline.MinValue = (float)Math.Min(0, proj.StartTime * .001);
            timeline.MaxValue = (float)Math.Max(audio.Duration, proj.EndTime * .001);
        }
        public override void Close() => withSavePrompt(() =>
        {
            proj.StopEffectUpdates();
            Manager.AsyncLoading("Stopping effect updates", () =>
            {
                proj.CancelEffectUpdates(true);
                Program.Schedule(() => Manager.GetContext<Editor>().Restart());
            });
        });
        void withSavePrompt(Action action)
        {
            if (proj.Changed)
            {
                Manager.ShowMessage("Do you wish to save the project?", () => Manager.AsyncLoading("Saving", () =>
                {
                    proj.Save();
                    Program.Schedule(() => action());
                }), action, true);
            }
            else action();
        }
        void refreshAudio()
        {
            audio = Program.AudioManager.LoadStream(proj.AudioPath, Manager.GetContext<Editor>().ResourceContainer);
            timeSource = new TimeSourceExtender(new AudioChannelTimeSource(audio));
        }
        void project_OnMapsetPathChanged(object sender, EventArgs e)
        {
            var previousAudio = audio;
            var previousTimeSource = timeSource;

            refreshAudio();
            resizeTimeline();

            if (previousAudio != null)
            {
                pendingSeek = previousTimeSource.Current;
                timeSource.TimeFactor = previousTimeSource.TimeFactor;
                timeSource.Playing = previousTimeSource.Playing;
                previousAudio.Dispose();
            }
        }
        void project_OnEffectsContentChanged(object sender, EventArgs e) => resizeTimeline();
        void project_OnEffectsStatusChanged(object sender, EventArgs e)
        {
            switch (proj.EffectsStatus)
            {
                case EffectStatus.ExecutionFailed:
                    statusIcon.Icon = IconFont.Bug;
                    statusMessage.Text = "An effect failed to execute.\nClick the Effects tab and the bug icon to see the error message.";
                    statusLayout.Pack(1024 - bottomRightLayout.Width - 24);
                    statusLayout.Displayed = true;
                    break;

                case EffectStatus.Updating:
                    statusIcon.Icon = IconFont.Spinner;
                    statusMessage.Text = "Updating effects...";
                    statusLayout.Pack(1024 - bottomRightLayout.Width - 24);
                    statusLayout.Displayed = true;
                    break;

                default: statusLayout.Displayed = false; break;
            }
        }

        #region IDisposable Support

        bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposed)
            {
                if (disposing)
                {
                    proj.OnEffectsContentChanged -= project_OnEffectsContentChanged;
                    proj.OnEffectsStatusChanged -= project_OnEffectsStatusChanged;
                    proj.Dispose();
                    audio.Dispose();
                }
                proj = null;
                audio = null;
                timeSource = null;
                disposed = true;
            }
        }

        #endregion
    }
}