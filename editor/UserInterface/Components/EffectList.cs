using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StorybrewEditor.UserInterface.Components
{
    public class EffectList : Widget
    {
        private LinearLayout layout;
        private LinearLayout effectsLayout;
        private Project project;
        private EffectConfigUi effectConfigUi;

        public override Vector2 MinSize => layout.MinSize;
        public override Vector2 MaxSize => layout.MaxSize;
        public override Vector2 PreferredSize => layout.PreferredSize;

        public event Action<Effect> OnEffectPreselect;
        public event Action<Effect> OnEffectSelected;

        public EffectList(WidgetManager manager, Project project, EffectConfigUi effectConfigUi) : base(manager)
        {
            this.project = project;
            this.effectConfigUi = effectConfigUi;

            Button addEffectButton, newScriptButton;
            Add(layout = new LinearLayout(manager)
            {
                StyleName = "panel",
                Padding = new FourSide(16),
                FitChildren = true,
                Fill = true,
                Children = new Widget[]
                {
                    new Label(manager)
                    {
                        Text = "Effects",
                        CanGrow = false,
                    },
                    new ScrollArea(manager, effectsLayout = new LinearLayout(manager)
                    {
                        FitChildren = true,
                    }),
                    new LinearLayout(manager)
                    {
                        Fill = true,
                        FitChildren = true,
                        Horizontal = true,
                        CanGrow = false,
                        Children = new Widget[]
                        {
                            addEffectButton = new Button(Manager)
                            {
                                StyleName = "small",
                                Text = "Add effect",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                            },
                            newScriptButton = new Button(Manager)
                            {
                                StyleName = "small",
                                Text = "New script",
                                AnchorFrom = BoxAlignment.Centre,
                                AnchorTo = BoxAlignment.Centre,
                            },
                        },
                    },
                },
            });

            addEffectButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowContextMenu("Select an effect", (effectName) => project.AddEffect(effectName), project.GetEffectNames());
            newScriptButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowPrompt("Script name", name => createScript(name));

            project.OnEffectsChanged += project_OnEffectsChanged;
            refreshEffects();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                project.OnEffectsChanged -= project_OnEffectsChanged;
            }
            project = null;
            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            layout.Size = Size;
        }

        private void project_OnEffectsChanged(object sender, EventArgs e)
            => refreshEffects();

        private void refreshEffects()
        {
            effectsLayout.ClearWidgets();
            foreach (var effect in project.Effects.OrderBy(e => e.Name))
            {
                Widget effectRoot;
                Label nameLabel;
                Button renameButton, statusButton, configButton, editButton, removeButton;
                effectsLayout.Add(effectRoot = new LinearLayout(Manager)
                {
                    AnchorFrom = BoxAlignment.Centre,
                    AnchorTo = BoxAlignment.Centre,
                    Horizontal = true,
                    FitChildren = true,
                    Fill = true,
                    Children = new Widget[]
                    {
                        renameButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.Pencil,
                            Tooltip = "Rename",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                        },
                        new LinearLayout(Manager)
                        {
                            StyleName = "condensed",
                            Children = new Widget[]
                            {
                                nameLabel = new Label(Manager)
                                {
                                    StyleName = "listItem",
                                    Text = effect.Name,
                                    AnchorFrom = BoxAlignment.Left,
                                    AnchorTo = BoxAlignment.Left,
                                },
                                new Label(Manager)
                                {
                                    StyleName = "listItemSecondary",
                                    Text = $"using {effect.BaseName}",
                                    AnchorFrom = BoxAlignment.Left,
                                    AnchorTo = BoxAlignment.Left,
                                },
                            },
                        },
                        statusButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                            Displayed = false,
                        },
                        configButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.Gear,
                            Tooltip = "Configure",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                        },
                        editButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.PencilSquare,
                            Tooltip = "Edit script",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                            Disabled = effect.Path == null,
                        },
                        removeButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.Times,
                            Tooltip = "Remove",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                        },
                    },
                });

                updateStatusButton(statusButton, effect);

                var ef = effect;

                EventHandler changedHandler;
                effect.OnChanged += changedHandler = (sender, e) =>
                {
                    nameLabel.Text = ef.Name;
                    updateStatusButton(statusButton, ef);
                };
                effectRoot.OnHovered += (sender, e) =>
                {
                    ef.Highlight = e.Hovered;
                    OnEffectPreselect?.Invoke(e.Hovered ? ef : null);
                };
                effectRoot.OnClickDown += (sender, e) =>
                {
                    OnEffectSelected?.Invoke(ef);
                    return true;
                };
                effectRoot.OnDisposed += (sender, e) =>
                {
                    ef.Highlight = false;
                    ef.OnChanged -= changedHandler;
                };

                statusButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowMessage($"Status: {ef.Status}\n\n{ef.StatusMessage}");
                renameButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowPrompt("Effect name", $"Pick a new name for {ef.Name}", (newName) =>
                {
                    ef.Name = newName;
                    refreshEffects();
                });
                editButton.OnClick += (sender, e) => openEffectEditor(ef);
                configButton.OnClick += (sender, e) =>
                {
                    if (!effectConfigUi.Displayed || effectConfigUi.Effect != ef)
                    {
                        effectConfigUi.Effect = ef;
                        effectConfigUi.Displayed = true;
                    }
                    else effectConfigUi.Displayed = false;
                };
                removeButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowMessage($"Remove {ef.Name}?", () => project.Remove(ef), true);
            }
        }

        private static void updateStatusButton(Button button, Effect effect)
        {
            button.Disabled = string.IsNullOrWhiteSpace(effect.StatusMessage);
            button.Displayed = effect.Status != EffectStatus.Ready || !button.Disabled;
            button.Tooltip = effect.Status.ToString();
            switch (effect.Status)
            {
                case EffectStatus.Loading:
                case EffectStatus.Configuring:
                case EffectStatus.Updating:
                    button.Icon = IconFont.Spinner;
                    button.Disabled = true;
                    break;
                case EffectStatus.ReloadPending:
                    button.Icon = IconFont.ChainBroken;
                    button.Disabled = true;
                    break;
                case EffectStatus.CompilationFailed:
                case EffectStatus.LoadingFailed:
                case EffectStatus.ExecutionFailed:
                    button.Icon = IconFont.Bug;
                    break;
                case EffectStatus.Ready:
                    button.Icon = IconFont.Leaf;
                    button.Tooltip = "Open log";
                    break;
            }
        }

        private void createScript(string name)
        {
            name = Regex.Replace(name, @"([A-Z])", " $1");
            name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
            name = Regex.Replace(name, @"[^0-9a-zA-Z]", "");
            name = Regex.Replace(name, @"^[\d-]*", "");
            if (name.Length == 0) name = "EffectScript";

            var path = Path.Combine(project.ScriptsPath, $"{name}.cs");
            var script = Encoding.UTF8.GetString(Resources.scripttemplate).StripUtf8Bom();
            script = script.Replace("%CLASSNAME%", name);

            if (File.Exists(path))
            {
                Manager.ScreenLayerManager.ShowMessage($"There is already a script named {name}");
                return;
            }

            File.WriteAllText(path, script);
            openEffectEditor(project.AddEffect(name));
        }

        private void openEffectEditor(Effect effect)
        {
            var editorPath = Path.GetDirectoryName(Path.GetFullPath("."));

            var root = Path.GetPathRoot(effect.Path);
            var solutionFolder = Path.GetDirectoryName(effect.Path);
            while (solutionFolder != root)
            {
                if (solutionFolder == editorPath)
                    break;

                var isSolution = false;
                foreach (var file in Directory.GetFiles(solutionFolder, "*.sln"))
                {
                    isSolution = true;
                    break;
                }
                if (isSolution)
                    break;

                solutionFolder = Directory.GetParent(solutionFolder).FullName;
            }

            if (solutionFolder == root)
                solutionFolder = Path.GetDirectoryName(effect.Path);

            var paths = new List<string>()
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft VS Code", "bin", "code"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft VS Code", "bin", "code"),
            };
            foreach (var path in Environment.GetEnvironmentVariable("path").Split(';'))
                if (PathHelper.IsValidPath(path))
                    paths.Add(Path.Combine(path, "code"));
                else Trace.WriteLine($"Invalid path in environment variables: {path}");

            var arguments = $"\"{solutionFolder}\" \"{effect.Path}\" -r";
            if (Program.Settings.VerboseVsCode)
                arguments += " --verbose";

            foreach (var path in paths)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        Trace.WriteLine($"vscode not found at \"{path}\"");
                        continue;
                    }

                    Trace.WriteLine($"Opening vscode with \"{path} {arguments}\"");
                    var process = Process.Start(new ProcessStartInfo()
                    {
                        FileName = path,
                        Arguments = arguments,
                        WindowStyle = Program.Settings.VerboseVsCode ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    });
                    return;
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Could not open vscode:\n{e}");
                }
            }
            Manager.ScreenLayerManager.ShowMessage($"Visual Studio Code could not be found, do you want to install it?\n(You may have to restart after installing)",
                    () => Process.Start("https://code.visualstudio.com/"), true);
        }
    }
}