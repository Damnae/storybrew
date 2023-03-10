using BrewLib.Data;
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
using System.Xml;

namespace StorybrewEditor.Scripting
{
    public class ScriptManager<TScript> : IDisposable where TScript : Script
    {
        readonly ResourceContainer resourceContainer;
        readonly string scriptsNamespace, commonScriptsPath, scriptsLibraryPath, compiledScriptsPath;

        List<string> referencedAssemblies = new List<string>();
        public IEnumerable<string> ReferencedAssemblies
        {
            get => referencedAssemblies;
            set
            {
                referencedAssemblies = new List<string>(value);
                foreach (var scriptContainer in scriptContainers.Values) scriptContainer.ReferencedAssemblies = referencedAssemblies;
                updateSolutionFiles();
            }
        }

        FileSystemWatcher scriptWatcher;
        readonly FileSystemWatcher libraryWatcher;
        ThrottledActionScheduler scheduler = new ThrottledActionScheduler();
        Dictionary<string, ScriptContainer<TScript>> scriptContainers = new Dictionary<string, ScriptContainer<TScript>>();

        public string ScriptsPath { get; }

        public ScriptManager(ResourceContainer resourceContainer, string scriptsNamespace, string scriptsSourcePath, string commonScriptsPath, string scriptsLibraryPath, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
        {
            this.resourceContainer = resourceContainer;
            this.scriptsNamespace = scriptsNamespace;
            ScriptsPath = scriptsSourcePath;
            this.commonScriptsPath = commonScriptsPath;
            this.scriptsLibraryPath = scriptsLibraryPath;
            this.compiledScriptsPath = compiledScriptsPath;

            ReferencedAssemblies = referencedAssemblies;

            scriptWatcher = new FileSystemWatcher
            {
                Filter = "*.cs",
                Path = scriptsSourcePath,
                IncludeSubdirectories = false
            };

            scriptWatcher.Created += scriptWatcher_Changed;
            scriptWatcher.Changed += scriptWatcher_Changed;
            scriptWatcher.Renamed += scriptWatcher_Changed;
            scriptWatcher.Deleted += scriptWatcher_Changed;
            scriptWatcher.Error += (sender, e) => Trace.WriteLine($"Watcher error (script): {e.GetException()}");
            scriptWatcher.EnableRaisingEvents = true;
            Trace.WriteLine($"Watching (script): {scriptsSourcePath}");

            libraryWatcher = new FileSystemWatcher
            {
                Filter = "*.cs",
                Path = scriptsLibraryPath,
                IncludeSubdirectories = true
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
            if (scriptContainers.TryGetValue(scriptName, out ScriptContainer<TScript> scriptContainer)) return scriptContainer;

            var scriptTypeName = $"{scriptsNamespace}.{scriptName}";
            var sourcePath = Path.Combine(ScriptsPath, $"{scriptName}.cs");

            if (commonScriptsPath != null && !File.Exists(sourcePath))
            {
                var commonSourcePath = Path.Combine(commonScriptsPath, $"{scriptName}.cs");
                if (File.Exists(commonSourcePath))
                {
                    File.Copy(commonSourcePath, sourcePath);
                    File.SetAttributes(sourcePath, File.GetAttributes(sourcePath) & ~FileAttributes.ReadOnly);
                }
            }

            scriptContainer = new ScriptContainerAppDomain<TScript>(scriptTypeName, sourcePath, scriptsLibraryPath, compiledScriptsPath, referencedAssemblies);
            scriptContainers.Add(scriptName, scriptContainer);
            return scriptContainer;
        }
        public IEnumerable<string> GetScriptNames()
        {
            var projectScriptNames = new List<string>();
            foreach (var scriptPath in Directory.GetFiles(ScriptsPath, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(scriptPath);
                projectScriptNames.Add(name);
                yield return name;
            }
            foreach (var scriptPath in Directory.GetFiles(commonScriptsPath, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(scriptPath);
                if (!projectScriptNames.Contains(name)) yield return name;
            }
        }
        void scriptWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var change = e.ChangeType.ToString().ToLowerInvariant();
            Trace.WriteLine($"Watched script file {change}: {e.FullPath}");

            if (e.ChangeType != WatcherChangeTypes.Changed) scheduleSolutionUpdate();
            if (e.ChangeType != WatcherChangeTypes.Deleted) scheduler?.Schedule(e.FullPath, key =>
            {
                if (disposedValue) return;
                var scriptName = Path.GetFileNameWithoutExtension(e.Name);

                if (scriptContainers.TryGetValue(scriptName, out ScriptContainer<TScript> container)) container.ReloadScript();
            });
        }
        void libraryWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var change = e.ChangeType.ToString().ToLowerInvariant();
            Trace.WriteLine($"Watched library file {change}: {e.FullPath}");

            if (e.ChangeType != WatcherChangeTypes.Changed) scheduleSolutionUpdate();
            if (e.ChangeType != WatcherChangeTypes.Deleted) scheduler?.Schedule(e.FullPath, key =>
            {
                if (disposedValue) return;
                foreach (var container in scriptContainers.Values) container.ReloadScript();
            });
        }
        void scheduleSolutionUpdate() => scheduler?.Schedule($"*{nameof(updateSolutionFiles)}", key =>
        {
            if (disposedValue) return;
            updateSolutionFiles();
        });
        void updateSolutionFiles()
        {
            Trace.WriteLine($"Updating solution files");

            var slnPath = Path.Combine(ScriptsPath, "storyboard.sln");
            File.WriteAllBytes(slnPath, resourceContainer.GetBytes("project/storyboard.sln", ResourceSource.Embedded | ResourceSource.Relative));

            var csProjPath = Path.Combine(ScriptsPath, "scripts.csproj");
            var document = new XmlDocument { PreserveWhitespace = false };
            try
            {
                using (var stream = resourceContainer.GetStream("project/scripts.csproj", ResourceSource.Embedded | ResourceSource.Relative))
                    document.Load(stream);

                var xmlns = document.DocumentElement.GetAttribute("xmlns");

                var referencedAssembliesGroup = document.CreateElement("ItemGroup", xmlns);
                document.DocumentElement.AppendChild(referencedAssembliesGroup);
                var importedAssemblies = referencedAssemblies.Where(e => !Project.DefaultAssemblies.Contains(e));
                foreach (var path in importedAssemblies)
                {
                    var relativePath = PathHelper.GetRelativePath(ScriptsPath, path);

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

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    scriptWatcher.Dispose();
                    libraryWatcher.Dispose();
                    foreach (var entry in scriptContainers) entry.Value.Dispose();
                }
                scheduler = null;
                scriptWatcher = null;
                scriptContainers = null;

                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}