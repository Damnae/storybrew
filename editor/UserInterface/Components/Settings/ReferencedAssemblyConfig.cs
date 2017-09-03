using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using StorybrewEditor.ScreenLayers;
using StorybrewEditor.Storyboarding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StorybrewEditor.UserInterface.Components
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
        private List<String> currentAssemblies;

        public ReferencedAssemblyConfig(Project project)
        {
            this.project = project;
            this.currentAssemblies = new List<string>(project.ImportedAssemblies);
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
                (path) =>
                {
                    if (!isValidAssembly(path))
                    {
                        WidgetManager.ScreenLayerManager.ShowMessage("Invalid assembly file. Are you sure that the file is intended for .NET?");
                        return;
                    }

                    if (validateAssembly(path))
                    {
                        string assembly;

                        if (isSystemAssembly(path))
                            assembly = Path.GetFileName(path);
                        else
                            assembly = PathHelper.FolderContainsPath(project.ProjectFolderPath, path) ? path : copyReferencedAssembly(path);

                        addReferencedAssembly(assembly);
                    }
                });

            addSystemAssemblyButton.OnClick += (sender, e) =>
            {
                WidgetManager.ScreenLayerManager.ShowContextMenu<string>("Select Assembly",
                    (result) =>
                    {
                        var path = $"{result}.dll";

                        if (validateAssembly(path))
                            addReferencedAssembly(path);
                        
                    }, getSystemAssemblies());
            };

            okButton.OnClick += (sender, e) =>
            {
                project.SetImportedAssemblies(currentAssemblies);
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

        private string getAssemblyName(string assembly)
        {
            try
            {
                return AssemblyName.GetAssemblyName(assembly).Name;
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(assembly);
            }
        } 

        private string getRelativePath(string assembly) => PathHelper.FolderContainsPath(project.ProjectFolderPath, assembly) ? assembly : Path.Combine(project.ProjectFolderPath, Path.GetFileName(assembly));

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

        private bool assemblyImported(string assembly, List<String> assemblies) =>
            assemblies.Select(ass => getAssemblyName(ass))
            .Contains(getAssemblyName(assembly));

        private bool assemblyImported(string assembly) => assemblyImported(assembly, currentAssemblies);

        private bool isDefaultAssembly(string assembly) =>
            Project.DefaultReferencedAssemblies.Any(
                ass => getAssemblyName(ass) == getAssemblyName(assembly));

        private bool isSystemAssembly(string assembly) => getAssemblyName(assembly).StartsWith("System");

        private bool validateAssembly(string assembly, List<String> assemblies)
        {
            if (isDefaultAssembly(assembly))
                return false;

            if (assemblyImported(assembly, assemblies))
            {
                WidgetManager.ScreenLayerManager.ShowMessage("Cannot import assembly file. An assembly of the same name already exists.");
                return false;
            }
            return true;
        }

        private bool validateAssembly(string assembly) => validateAssembly(assembly, currentAssemblies);

        private List<String> getSystemAssemblies()
        {
            var assemblyDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET\\assembly");
            List<String> systemAssemblies = new List<String>();
            foreach (var gac in Directory.GetDirectories(assemblyDirectory))
            {
                foreach (var assemblyFolder in Directory.GetDirectories(gac))
                {
                    var assembly = PathHelper.GetRelativePath(gac, assemblyFolder);
                    if (assembly.StartsWith("System"))
                        systemAssemblies.Add(assembly);
                }
            }
            return systemAssemblies;
        }
        
        private string copyReferencedAssembly(string assembly)
        {
            var newPath = getRelativePath(assembly);
            File.Copy(assembly, newPath, true);
            return newPath;
        }

        private void addReferencedAssembly(string assembly)
        {
            currentAssemblies.Add(assembly);
            refreshAssemblies();
        }

        private void removeReferencedAssembly(string assembly)
        {
            currentAssemblies.Remove(assembly);
            refreshAssemblies();
        }

        private void changeReferencedAssembly(string assembly)
        {
            if (isSystemAssembly(assembly))
            {
                WidgetManager.ScreenLayerManager.ShowContextMenu<string>("Select Assembly",
                    (result) =>
                    {
                        var newPath = $"{result}.dll";
                        var assemblies = currentAssemblies.Where(ass => ass != assembly).ToList();

                        if (validateAssembly(newPath, assemblies))
                        {
                            currentAssemblies.Remove(assembly);
                            currentAssemblies.Add(newPath);
                            refreshAssemblies();
                        }                        
                    }, getSystemAssemblies());
            }
            else
            {
                WidgetManager.ScreenLayerManager.OpenFilePicker("", "", Path.GetDirectoryName(assembly), ".NET Assemblies (*.dll)|*.dll",
                    (path) =>
                    {
                        if (!isValidAssembly(path))
                        {
                            WidgetManager.ScreenLayerManager.ShowMessage("Invalid assembly file. Are you sure that the file is intended for .NET?");
                            return;
                        }

                        var assemblies = currentAssemblies.Where(ass => ass != assembly).ToList();

                        if (validateAssembly(path, assemblies))
                        {
                            var newPath = PathHelper.FolderContainsPath(project.ProjectFolderPath, path) ? path : copyReferencedAssembly(path);

                            if (path == assembly)
                                return;

                            currentAssemblies.Remove(assembly);
                            currentAssemblies.Add(newPath);
                            refreshAssemblies();
                        }
                    });
            }
        }

        private void refreshAssemblies()
        {
            assembliesLayout.ClearWidgets();
            var assemblies = currentAssemblies.OrderBy(e => isSystemAssembly(e) ? $"_{e}" : getAssemblyName(e));
            foreach (var assembly in assemblies)
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
    }
}
