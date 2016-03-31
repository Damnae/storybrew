using StorybrewCommon.Scripting;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace StorybrewEditor.Scripting
{
    public class ScriptManager<TScript> : IDisposable
        where TScript : Script
    {
        private string scriptsNamespace;
        private string scriptsSourcePath;
        private string compiledScriptsPath;
        private string[] referencedAssemblies;

        private FileSystemWatcher scriptWatcher;
        private Dictionary<string, ScriptContainer<TScript>> scriptContainers = new Dictionary<string, ScriptContainer<TScript>>();
        private Dictionary<string, byte[]> scriptHashes = new Dictionary<string, byte[]>();

        public string ScriptsPath => scriptsSourcePath;

        public ScriptManager(string scriptsNamespace, string scriptsSourcePath, string compiledScriptsPath, params string[] referencedAssemblies)
        {
            this.scriptsNamespace = scriptsNamespace;
            this.scriptsSourcePath = scriptsSourcePath;
            this.referencedAssemblies = referencedAssemblies;
            this.compiledScriptsPath = compiledScriptsPath;

            scriptWatcher = new FileSystemWatcher()
            {
                Filter = "*.cs",
                Path = scriptsSourcePath,
                IncludeSubdirectories = false,
            };
            scriptWatcher.Changed += scriptWatcher_Changed;
            scriptWatcher.Renamed += scriptWatcher_Changed;
            scriptWatcher.EnableRaisingEvents = true;
        }

        public ScriptContainer<TScript> Get(string scriptName)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptManager<TScript>));

            ScriptContainer<TScript> scriptContainer;
            if (scriptContainers.TryGetValue(scriptName, out scriptContainer))
                return scriptContainer;

            var scriptTypeName = $"{scriptsNamespace}.{scriptName}";
            var sourcePath = Path.Combine(scriptsSourcePath, $"{scriptName}.cs");

            scriptContainer = new ScriptContainer<TScript>(this, scriptTypeName, sourcePath, compiledScriptsPath, referencedAssemblies);
            scriptContainers.Add(scriptName, scriptContainer);
            return scriptContainer;
        }

        public IEnumerable<string> GetScriptNames()
        {
            foreach (var scriptPath in Directory.GetFiles(scriptsSourcePath, "*.cs", SearchOption.TopDirectoryOnly))
                yield return Path.GetFileNameWithoutExtension(scriptPath);
        }

        private void scriptWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var scriptName = Path.GetFileNameWithoutExtension(e.Name);

            Program.Schedule(() =>
            {
                ScriptContainer<TScript> container;
                if (scriptContainers.TryGetValue(scriptName, out container))
                {
                    try
                    {
                        var scriptHash = HashHelper.GetFileMd5Bytes(e.FullPath);

                        byte[] currentScriptHash;
                        if (scriptHashes.TryGetValue(scriptName, out currentScriptHash) && currentScriptHash.SequenceEqual(scriptHash))
                            return;

                        Debug.Print($"{nameof(Scripting)}: {e.FullPath} changed, reloading {scriptName}");

                        scriptHashes[scriptName] = scriptHash;
                        container.ReloadScript();
                    }
                    catch (IOException exception)
                    {
                        var type = exception.GetType();
                        Debug.Print($"{nameof(Scripting)}: Waiting for {e.Name} ({exception.Message})");
                    }
                }
            });
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
                    foreach (var entry in scriptContainers)
                        entry.Value.Dispose();
                }
                scriptWatcher = null;
                scriptContainers = null;
                scriptHashes = null;

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
