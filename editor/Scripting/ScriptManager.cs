﻿using BrewLib.Data;
using StorybrewCommon.Scripting;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace StorybrewEditor.Scripting
{
    public class ScriptManager<TScript> : IDisposable
        where TScript : Script
    {
        private readonly ResourceContainer resourceContainer;
        private readonly string scriptsNamespace;
        private readonly string commonScriptsPath;
        private readonly string scriptsLibraryPath;
        private readonly string compiledScriptsPath;

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
        private readonly FileSystemWatcher libraryWatcher;
        private ThrottledActionScheduler scheduler = new ThrottledActionScheduler();
        private Dictionary<string, ScriptContainer<TScript>> scriptContainers = new Dictionary<string, ScriptContainer<TScript>>();

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

            if (scriptContainers.TryGetValue(scriptName, out ScriptContainer<TScript> scriptContainer))
                return scriptContainer;

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

            scriptContainer = new ScriptContainer<TScript>(this, scriptTypeName, sourcePath, scriptsLibraryPath, compiledScriptsPath, referencedAssemblies);
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
                if (!projectScriptNames.Contains(name))
                    yield return name;
            }
        }

        private void scriptWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var change = e.ChangeType.ToString().ToLowerInvariant();
            Trace.WriteLine($"Watched script file {change}: {e.FullPath}");

            if (e.ChangeType != WatcherChangeTypes.Changed && e.ChangeType != WatcherChangeTypes.Renamed)
                scheduleSolutionUpdate();

            if (e.ChangeType != WatcherChangeTypes.Deleted)
                scheduler?.Schedule(e.FullPath, key =>
                {
                    if (disposedValue) return;
                    var scriptName = Path.GetFileNameWithoutExtension(e.Name);

                    if (scriptContainers.TryGetValue(scriptName, out ScriptContainer<TScript> container))
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

            var slnPath = Path.Combine(ScriptsPath, "storyboard.sln");
            File.WriteAllBytes(slnPath, resourceContainer.GetBytes("project/storyboard.sln", ResourceSource.Embedded | ResourceSource.Relative));

            var vsCodePath = Path.Combine(ScriptsPath, ".vscode");
            if (!Directory.Exists(vsCodePath))
                Directory.CreateDirectory(vsCodePath);

            var csProjPath = Path.Combine(ScriptsPath, "scripts.csproj");
            File.WriteAllBytes(csProjPath, resourceContainer.GetBytes("project/scripts.csproj", ResourceSource.Embedded | ResourceSource.Relative));
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
