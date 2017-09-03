using BrewLib.Util;
using StorybrewCommon.Scripting;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace StorybrewEditor.Scripting
{
    public class ScriptManager<TScript> : IDisposable
        where TScript : Script
    {
        private string scriptsNamespace;
        private string scriptsSourcePath;
        private string commonScriptsPath;
        private string scriptsLibraryPath;
        private string compiledScriptsPath;

        private List<string> referencedAssemblies = new List<string>();
        public IEnumerable<string> ReferencedAssemblies
        {
            get { return referencedAssemblies; }
            set
            {
                referencedAssemblies = new List<string>(value);
                foreach (var scriptContainer in scriptContainers.Values)
                    scriptContainer.ReferencedAssemblies = referencedAssemblies;
                updateSolutionFiles();
            }
        }

        private FileSystemWatcher scriptWatcher;
        private FileSystemWatcher libraryWatcher;
        private ThrottledActionScheduler scheduler = new ThrottledActionScheduler();
        private Dictionary<string, ScriptContainer<TScript>> scriptContainers = new Dictionary<string, ScriptContainer<TScript>>();

        public string ScriptsPath => scriptsSourcePath;

        public ScriptManager(string scriptsNamespace, string scriptsSourcePath, string commonScriptsPath, string scriptsLibraryPath, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
        {
            this.scriptsNamespace = scriptsNamespace;
            this.scriptsSourcePath = scriptsSourcePath;
            this.commonScriptsPath = commonScriptsPath;
            this.scriptsLibraryPath = scriptsLibraryPath;
            this.compiledScriptsPath = compiledScriptsPath;

            ReferencedAssemblies = referencedAssemblies;

            scriptWatcher = new FileSystemWatcher()
            {
                Filter = "*.cs",
                Path = scriptsSourcePath,
                IncludeSubdirectories = false,
            };
            scriptWatcher.Created += scriptWatcher_Changed;
            scriptWatcher.Changed += scriptWatcher_Changed;
            scriptWatcher.Renamed += scriptWatcher_Changed;
            scriptWatcher.Deleted += scriptWatcher_Changed;
            scriptWatcher.Error += (sender, e) => Trace.WriteLine($"Watcher error (script): {e.GetException()}");
            scriptWatcher.EnableRaisingEvents = true;
            Trace.WriteLine($"Watching (script): {scriptsSourcePath}");

            libraryWatcher = new FileSystemWatcher()
            {
                Filter = "*.cs",
                Path = scriptsLibraryPath,
                IncludeSubdirectories = true,
            };
            libraryWatcher.Created += libraryWatcher_Changed;
            libraryWatcher.Changed += libraryWatcher_Changed;
            libraryWatcher.Renamed += libraryWatcher_Changed;
            libraryWatcher.Deleted += libraryWatcher_Changed;
            libraryWatcher.Error += (sender, e) => Trace.WriteLine($"Watcher error (library): {e.GetException()}");
            libraryWatcher.EnableRaisingEvents = true;
            Trace.WriteLine($"Watching (library): {scriptsLibraryPath}");
        }

        public ScriptContainer<TScript> Get(string scriptName)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptManager<TScript>));

            ScriptContainer<TScript> scriptContainer;
            if (scriptContainers.TryGetValue(scriptName, out scriptContainer))
                return scriptContainer;

            var scriptTypeName = $"{scriptsNamespace}.{scriptName}";
            var sourcePath = Path.Combine(scriptsSourcePath, $"{scriptName}.cs");

            if (commonScriptsPath != null && !File.Exists(sourcePath))
            {
                var commonSourcePath = Path.Combine(commonScriptsPath, $"{scriptName}.cs");
                if (File.Exists(commonSourcePath))
                {
                    File.Copy(commonSourcePath, sourcePath);
                    File.SetAttributes(sourcePath, File.GetAttributes(sourcePath) & ~FileAttributes.ReadOnly);
                }
            }

            scriptContainer = new ScriptContainerAppDomain<TScript>(this, scriptTypeName, sourcePath, scriptsLibraryPath, compiledScriptsPath, referencedAssemblies);
            //scriptContainer = new ScriptContainerProcess<TScript>(this, scriptTypeName, sourcePath, scriptsLibraryPath, compiledScriptsPath, referencedAssemblies);
            scriptContainers.Add(scriptName, scriptContainer);
            return scriptContainer;
        }

        public IEnumerable<string> GetScriptNames()
        {
            var projectScriptNames = new List<string>();
            foreach (var scriptPath in Directory.GetFiles(scriptsSourcePath, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(scriptPath);
                projectScriptNames.Add(name);
                yield return name;
            }
            foreach (var scriptPath in Directory.GetFiles(commonScriptsPath, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(scriptPath);
                if (!projectScriptNames.Contains(name))
                    yield return name;
            }
        }

        private void scriptWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var change = e.ChangeType.ToString().ToLowerInvariant();
            Trace.WriteLine($"Watched script file {change}: {e.FullPath}");

            if (e.ChangeType != WatcherChangeTypes.Changed)
                scheduleSolutionUpdate();

            if (e.ChangeType != WatcherChangeTypes.Deleted)
                scheduler?.Schedule(e.FullPath, key =>
                {
                    if (disposedValue) return;
                    var scriptName = Path.GetFileNameWithoutExtension(e.Name);

                    ScriptContainer<TScript> container;
                    if (scriptContainers.TryGetValue(scriptName, out container))
                        container.ReloadScript();
                });
        }

        private void libraryWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var change = e.ChangeType.ToString().ToLowerInvariant();
            Trace.WriteLine($"Watched library file {change}: {e.FullPath}");

            if (e.ChangeType != WatcherChangeTypes.Changed)
                scheduleSolutionUpdate();

            if (e.ChangeType != WatcherChangeTypes.Deleted)
                scheduler?.Schedule(e.FullPath, key =>
                {
                    if (disposedValue) return;
                    foreach (var container in scriptContainers.Values)
                        container.ReloadScript();
                });
        }

        private void scheduleSolutionUpdate()
        {
            scheduler?.Schedule($"*{nameof(updateSolutionFiles)}", key =>
            {
                if (disposedValue) return;
                updateSolutionFiles();
            });
        }

        private void updateSolutionFiles()
        {
            Trace.WriteLine($"Updating solution files");

            var slnPath = Path.Combine(scriptsSourcePath, "storyboard.sln");
            File.WriteAllBytes(slnPath, Resources.project_storyboard_sln);

            var csProjPath = Path.Combine(scriptsSourcePath, "scripts.csproj");
            var document = new XmlDocument() { PreserveWhitespace = false, };
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.project_scripts_csproj)))
                    document.Load(stream);

                var xmlns = document.DocumentElement.GetAttribute("xmlns");
                var compileGroup = document.CreateElement("ItemGroup", xmlns);
                document.DocumentElement.AppendChild(compileGroup);
                foreach (var path in Directory.EnumerateFiles(scriptsSourcePath, "*.cs", SearchOption.AllDirectories))
                {
                    var relativePath = PathHelper.GetRelativePath(scriptsSourcePath, path);

                    var compileNode = document.CreateElement("Compile", xmlns);
                    compileNode.SetAttribute("Include", relativePath);
                    compileGroup.AppendChild(compileNode);
                }

                var referencedAssembliesGroup = document.CreateElement("ItemGroup", xmlns);
                document.DocumentElement.AppendChild(referencedAssembliesGroup);
                var importedAssemblies = referencedAssemblies.Where(e => !Project.DefaultAssemblies.Contains(e));
                foreach (var path in importedAssemblies)
                {
                    var relativePath = PathHelper.GetRelativePath(scriptsSourcePath, path);

                    var compileNode = document.CreateElement("Reference", xmlns);
                    compileNode.SetAttribute("Include", AssemblyName.GetAssemblyName(path).Name);
                    var hintPath = document.CreateElement("HintPath", xmlns);
                    hintPath.AppendChild(document.CreateTextNode(relativePath));
                    compileNode.AppendChild(hintPath);
                    referencedAssembliesGroup.AppendChild(compileNode);
                }
                document.Save(csProjPath);
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Failed to update scripts.csproj: {e}");
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    scriptWatcher.Dispose();
                    libraryWatcher.Dispose();
                    foreach (var entry in scriptContainers)
                        entry.Value.Dispose();
                }
                scheduler = null;
                scriptWatcher = null;
                scriptContainers = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
