﻿using OpenTK;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.Util;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
                    effectsLayout = new LinearLayout(manager)
                    {
                        FitChildren = true,
                    },
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
                                AnchorFrom = UiAlignment.Centre,
                                AnchorTo = UiAlignment.Centre,
                            },
                            newScriptButton = new Button(Manager)
                            {
                                StyleName = "small",
                                Text = "New script",
                                AnchorFrom = UiAlignment.Centre,
                                AnchorTo = UiAlignment.Centre,
                            },
                        },
                    },
                },
            });

            addEffectButton.OnClick += (sender, e) => Manager.ScreenLayerManager.Add(new EffectNameSelector(project, (effectName) => project.AddEffect(effectName)));
            newScriptButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowName("", name => createScript(name));

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
            foreach (var effect in project.Effects)
            {
                Widget effectRoot;
                Label nameLabel;
                Button renameButton, statusButton, configButton, editButton, removeButton;
                effectsLayout.Add(effectRoot = new LinearLayout(Manager)
                {
                    AnchorFrom = UiAlignment.Centre,
                    AnchorTo = UiAlignment.Centre,
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
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
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
                                    AnchorFrom = UiAlignment.Left,
                                    AnchorTo = UiAlignment.Left,
                                },
                                new Label(Manager)
                                {
                                    StyleName = "listItemSecondary",
                                    Text = $"using {effect.BaseName}",
                                    AnchorFrom = UiAlignment.Left,
                                    AnchorTo = UiAlignment.Left,
                                },
                            },
                        },
                        statusButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            CanGrow = false,
                            Displayed = false,
                        },
                        configButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.Gear,
                            Tooltip = "Configure",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            CanGrow = false,
#if !DEBUG
                            Disabled = true,
#endif
                        },
                        editButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.PencilSquare,
                            Tooltip = "Edit script",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
                            CanGrow = false,
                            Disabled = effect.Path == null,
                        },
                        removeButton = new Button(Manager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.Times,
                            Tooltip = "Remove",
                            AnchorFrom = UiAlignment.Centre,
                            AnchorTo = UiAlignment.Centre,
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
                effectRoot.OnDisposed += (sender, e) => ef.OnChanged -= changedHandler;

                statusButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowMessage($"Status: {ef.Status}\n\n{ef.StatusMessage}");
                renameButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowName(ef.Name, (newName) => ef.Name = newName);
                editButton.OnClick += (sender, e) => openEffectEditor(ef);
                configButton.OnClick += (sender, e) =>
                {
                    effectConfigUi.Effect = ef;
                    effectConfigUi.Displayed = true;
                };
                removeButton.OnClick += (sender, e) => Manager.ScreenLayerManager.ShowMessage($"Remove {ef.Name}?", () => project.Remove(ef), true);
            }
        }

        private static void updateStatusButton(Button button, Effect effect)
        {
            button.Displayed = effect.Status != EffectStatus.Ready;
            button.Disabled = string.IsNullOrWhiteSpace(effect.StatusMessage);
            switch (effect.Status)
            {
                case EffectStatus.Loading:
                case EffectStatus.Configuring:
                case EffectStatus.Updating:
                    button.Icon = IconFont.Spinner;
                    break;
                case EffectStatus.ReloadPending:
                    button.Icon = IconFont.ChainBroken;
                    break;
                case EffectStatus.CompilationFailed:
                case EffectStatus.LoadingFailed:
                case EffectStatus.ExecutionFailed:
                    button.Icon = IconFont.Bug;
                    break;
            }
            button.Tooltip = effect.Status.ToString();
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

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "code",
                    Arguments = $"\"{solutionFolder}\" \"{effect.Path}\" -r",
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }
            catch
            {
                Manager.ScreenLayerManager.ShowMessage($"Visual Studio Code could not be found, do you want to install it?\n(You may have to restart after installing it)",
                    () => Process.Start("https://code.visualstudio.com/"), true);
            }
        }
    }
}