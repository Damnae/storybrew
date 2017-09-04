using BrewLib.UserInterface;
using BrewLib.Util;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StorybrewEditor.ScreenLayers
{
    public class ReferencedAssemblyConfig : UiScreenLayer
    {
        private LinearLayout layout;
        private LinearLayout assembliesLayout;
        private LinearLayout buttonsLayout;

        private Button okButton;
        private Button cancelButton;

        public override bool IsPopup => true;

        private Project project;
        private List<string> selectedAssemblies;

        public ReferencedAssemblyConfig(Project project)
        {
            this.project = project;
            selectedAssemblies = new List<string>(project.ImportedAssemblies);
        }

        public override void Load()
        {
            base.Load();

            Button addAssemblyButton, addSystemAssemblyButton;

            WidgetManager.Root.Add(layout = new LinearLayout(WidgetManager)
            {
                StyleName = "panel",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = BoxAlignment.Centre,
                AnchorTo = BoxAlignment.Centre,
                Padding = new FourSide(16),
                FitChildren = true,
                Fill = true,
                Children = new Widget[]
                {
                    new Label(WidgetManager)
                    {
                        Text = "Imported Referenced Assemblies",
                        CanGrow = false,
                    },
                    new ScrollArea(WidgetManager, assembliesLayout = new LinearLayout(WidgetManager)
                    {
                        FitChildren = true,
                    }),
                    addAssemblyButton = new Button(WidgetManager)
                    {
                        Text = "Add assembly file",
                        AnchorFrom = BoxAlignment.Centre,
                        AnchorTo = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    addSystemAssemblyButton = new Button(WidgetManager)
                    {
                        Text = "Add system assembly",
                        AnchorFrom = BoxAlignment.Centre,
                        AnchorTo = BoxAlignment.Centre,
                        CanGrow = false,
                    },
                    buttonsLayout = new LinearLayout(WidgetManager)
                    {
                        Horizontal = true,
                        Fill = true,
                        AnchorFrom = BoxAlignment.Centre,
                        CanGrow = false,
                        Children = new Widget[]
                        {
                            okButton = new Button(WidgetManager)
                            {
                                Text = "Ok",
                                AnchorFrom = BoxAlignment.Centre,
                            },
                            cancelButton = new Button(WidgetManager)
                            {
                                Text = "Cancel",
                                AnchorFrom = BoxAlignment.Centre,
                            },
                        },
                    },
                },
            });

            addAssemblyButton.OnClick += (sender, e) => WidgetManager.ScreenLayerManager.OpenFilePicker("", "", project.ProjectFolderPath, ".NET Assemblies (*.dll)|*.dll",
                path =>
                {
                    if (!isValidAssembly(path))
                    {
                        WidgetManager.ScreenLayerManager.ShowMessage("Invalid assembly file. Are you sure that the file is intended for .NET?");
                        return;
                    }

                    if (validateAssembly(path))
                    {
                        var assembly = isSystemAssembly(path) ?
                            Path.GetFileName(path) :
                            PathHelper.FolderContainsPath(project.ProjectFolderPath, path) ? path : copyReferencedAssembly(path);

                        addReferencedAssembly(assembly);
                    }
                });

            addSystemAssemblyButton.OnClick += (sender, e) =>
            {
                tryCatchSystemAssemblies(() =>
                {
                    var systemAssemblies = getAvailableSystemAssemblies();
                    WidgetManager.ScreenLayerManager.ShowContextMenu<string>("Select Assembly",
                        result =>
                        {
                            var path = $"{result}.dll";
                            if (validateAssembly(path))
                                addReferencedAssembly(path);
                        }, systemAssemblies);
                });
            };

            okButton.OnClick += (sender, e) =>
            {
                project.ImportedAssemblies = selectedAssemblies;
                Exit();
            };
            cancelButton.OnClick += (sender, e) => Exit();

            refreshAssemblies();
        }

        public override void Resize(int width, int height)
        {
            base.Resize(width, height);
            layout.Pack(Math.Min(400, width), Math.Min(600, height));
        }

        private void refreshAssemblies()
        {
            assembliesLayout.ClearWidgets();
            foreach (var assembly in selectedAssemblies.OrderBy(id => isSystemAssembly(id) ? $"_{id}" : getAssemblyName(id)))
            {
                Widget assemblyRoot;
                Label nameLabel;
                Button statusButton, editButton, removeButton;

                assembliesLayout.Add(assemblyRoot = new LinearLayout(WidgetManager)
                {
                    AnchorFrom = BoxAlignment.Centre,
                    AnchorTo = BoxAlignment.Centre,
                    Horizontal = true,
                    FitChildren = true,
                    Fill = true,
                    Children = new Widget[]
                    {
                        new LinearLayout(WidgetManager)
                        {
                            StyleName = "condensed",
                            Children = new Widget[]
                            {
                                nameLabel = new Label(WidgetManager)
                                {
                                    StyleName = "listItem",
                                    Text = getAssemblyName(assembly),
                                    AnchorFrom = BoxAlignment.Left,
                                    AnchorTo = BoxAlignment.Left,
                                },
                            },
                        },
                        statusButton = new Button(WidgetManager)
                        {
                            StyleName = "icon",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                            Displayed = false,
                        },
                        editButton = new Button(WidgetManager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.PencilSquare,
                            Tooltip = "Change file",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                        },
                        removeButton = new Button(WidgetManager)
                        {
                            StyleName = "icon",
                            Icon = IconFont.Times,
                            Tooltip = "Remove",
                            AnchorFrom = BoxAlignment.Centre,
                            AnchorTo = BoxAlignment.Centre,
                            CanGrow = false,
                        },
                    }
                });

                var ass = assembly;

                editButton.OnClick += (sender, e) => changeReferencedAssembly(ass);
                removeButton.OnClick += (sender, e) => WidgetManager.ScreenLayerManager.ShowMessage($"Remove {getAssemblyName(ass)}?", () => removeReferencedAssembly(ass), true);
            }
        }

        private string getAssemblyName(string assemblyPath)
        {
            try
            {
                return AssemblyName.GetAssemblyName(assemblyPath).Name;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(assemblyPath);
            }
        }

        private string getRelativePath(string assembly)
            => PathHelper.FolderContainsPath(project.ProjectFolderPath, assembly) ? assembly : Path.Combine(project.ProjectFolderPath, Path.GetFileName(assembly));

        private bool isValidAssembly(string assembly)
        {
            try
            {
                AssemblyName.GetAssemblyName(assembly);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool assemblyImported(string assembly, IEnumerable<string> assemblies)
            => assemblies
                .Select(ass => getAssemblyName(ass))
                .Contains(getAssemblyName(assembly));

        private bool assemblyImported(string assembly)
            => assemblyImported(assembly, selectedAssemblies);

        private bool isDefaultAssembly(string assembly)
            => Project.DefaultAssemblies
                .Any(ass => getAssemblyName(ass) == getAssemblyName(assembly));

        private bool isSystemAssembly(string assemblyId)
            => getAssemblyName(assemblyId).StartsWith("System.");

        private bool validateAssembly(string assembly, IEnumerable<string> assemblies)
            => !(isDefaultAssembly(assembly) || assemblyImported(assembly, assemblies));

        private bool validateAssembly(string assembly)
            => validateAssembly(assembly, selectedAssemblies);

        private IEnumerable<string> getAvailableSystemAssemblies()
        {
            var assemblyDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET\\assembly");
            var badSystemAssemblySuffixes = new[] { "resources", "Resources", "Printing", "Speech", "VisualStudio.11.0" };

            var systemAssemblies = new List<string>();
            foreach (var gac in Directory.GetDirectories(assemblyDirectory))
            {
                foreach (var assemblyFolder in Directory.GetDirectories(gac))
                {
                    var assembly = PathHelper.GetRelativePath(gac, assemblyFolder);

                    if (!assembly.StartsWith("System.")) continue;
                    if (badSystemAssemblySuffixes.Any(suffix => assembly.EndsWith(suffix))) continue;

                    var filename = $"{assembly}.dll";
                    if (Project.DefaultAssemblies.Contains(filename)) continue;
                    if (selectedAssemblies.Contains(filename)) continue;

                    systemAssemblies.Add(assembly);
                }
            }
            return systemAssemblies.Distinct().OrderBy(e => e);
        }

        private string copyReferencedAssembly(string assembly)
        {
            var newPath = getRelativePath(assembly);
            File.Copy(assembly, newPath, true);
            return newPath;
        }

        private void addReferencedAssembly(string assembly)
        {
            selectedAssemblies.Add(assembly);
            refreshAssemblies();
        }

        private void removeReferencedAssembly(string assembly)
        {
            selectedAssemblies.Remove(assembly);
            refreshAssemblies();
        }

        private void changeReferencedAssembly(string assembly)
        {
            if (isSystemAssembly(assembly))
            {
                tryCatchSystemAssemblies(() =>
                {
                    var systemAssemblies = getAvailableSystemAssemblies();
                    WidgetManager.ScreenLayerManager.ShowContextMenu<string>("Select Assembly",
                        result =>
                        {
                            var newPath = $"{result}.dll";
                            var assemblies = selectedAssemblies.Where(ass => ass != assembly).ToList();
                            if (validateAssembly(newPath, assemblies))
                            {
                                selectedAssemblies.Remove(assembly);
                                selectedAssemblies.Add(newPath);
                                refreshAssemblies();
                            }
                        }, systemAssemblies);
                });
            }
            else
            {
                WidgetManager.ScreenLayerManager.OpenFilePicker("", "", Path.GetDirectoryName(assembly), ".NET Assemblies (*.dll)|*.dll",
                    path =>
                    {
                        if (!isValidAssembly(path))
                        {
                            WidgetManager.ScreenLayerManager.ShowMessage("Invalid assembly file. Are you sure that the file is intended for .NET?");
                            return;
                        }

                        var assemblies = selectedAssemblies.Where(ass => ass != assembly).ToList();

                        if (validateAssembly(path, assemblies))
                        {
                            var newPath = PathHelper.FolderContainsPath(project.ProjectFolderPath, path) ? path : copyReferencedAssembly(path);

                            if (path == assembly)
                                return;

                            selectedAssemblies.Remove(assembly);
                            selectedAssemblies.Add(newPath);
                            refreshAssemblies();
                        }
                    });
            }
        }

        private void tryCatchSystemAssemblies(Action action)
        {
            try
            {
                action();
            }
            catch (DirectoryNotFoundException)
            {
                WidgetManager.ScreenLayerManager.ShowMessage("Cannot find Global Assembly Cache folders. Consider your installation of the .NET framework.");
            }
            catch (Exception exception)
            {
                WidgetManager.ScreenLayerManager.ShowMessage($"An error occurred. Check your .NET Framework installation.\nException:\n{exception}");
            }
        }
    }
}
