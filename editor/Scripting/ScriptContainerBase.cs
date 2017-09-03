using StorybrewCommon.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorybrewEditor.Scripting
{
    public abstract class ScriptContainerBase<TScript> : ScriptContainer<TScript>
        where TScript : Script
    {
        private static int nextId;
        public readonly int Id = nextId++;

        private ScriptManager<TScript> manager;

        private string compiledScriptsPath;
        public string CompiledScriptsPath => compiledScriptsPath;

        private ScriptProvider<TScript> scriptProvider;

        private volatile int currentVersion = 0;
        private volatile int targetVersion = 1;

        public string Name
        {
            get
            {
                var name = scriptTypeName;
                if (name.Contains("."))
                    name = name.Substring(name.LastIndexOf('.') + 1);
                return name;
            }
        }

        private string scriptTypeName;
        public string ScriptTypeName => scriptTypeName;

        private string mainSourcePath;
        public string MainSourcePath => mainSourcePath;

        private string libraryFolder;
        public string LibraryFolder => libraryFolder;

        public string[] SourcePaths
        {
            get
            {
                if (libraryFolder == null || !Directory.Exists(libraryFolder))
                    return new[] { MainSourcePath };

                return Directory.GetFiles(libraryFolder, "*.cs", SearchOption.AllDirectories)
                    .Concat(new[] { MainSourcePath }).ToArray();
            }
        }

        private List<string> referencedAssemblies = new List<string>();
        public IEnumerable<string> ReferencedAssemblies
        {
            get { return referencedAssemblies; }
            set
            {
                var newReferencedAssemblies = new List<string>(value);
                if (newReferencedAssemblies.Count == referencedAssemblies.Count && newReferencedAssemblies.All(ass => referencedAssemblies.Contains(ass)))
                    return;

                referencedAssemblies = newReferencedAssemblies;
                ReloadScript();
            }
        }

        /// <summary>
        /// Returns false when Script would return null.
        /// </summary>
        public bool HasScript => scriptProvider != null || currentVersion != targetVersion;

        public event EventHandler OnScriptChanged;

        public ScriptContainerBase(ScriptManager<TScript> manager, string scriptTypeName, string mainSourcePath, string libraryFolder, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
        {
            this.manager = manager;
            this.scriptTypeName = scriptTypeName;
            this.mainSourcePath = mainSourcePath;
            this.libraryFolder = libraryFolder;
            this.compiledScriptsPath = compiledScriptsPath;

            ReferencedAssemblies = referencedAssemblies;
        }

        public TScript CreateScript()
        {
            var localTargetVersion = targetVersion;
            if (currentVersion < localTargetVersion)
            {
                currentVersion = localTargetVersion;
                scriptProvider = LoadScript();
            }
            return scriptProvider.CreateScript();
        }

        public void ReloadScript()
        {
            var initialTargetVersion = targetVersion;

            int localCurrentVersion;
            do
            {
                localCurrentVersion = currentVersion;
                if (targetVersion <= localCurrentVersion)
                    targetVersion = localCurrentVersion + 1;
            }
            while (currentVersion != localCurrentVersion);

            if (targetVersion > initialTargetVersion)
                OnScriptChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract ScriptProvider<TScript> LoadScript();

        protected ScriptLoadingException CreateScriptLoadingException(Exception e)
        {
            var details = "";
            if (e is TypeLoadException)
                details = "Make sure the script's class name is the same as the file name.\n";

            return new ScriptLoadingException($"{ScriptTypeName} failed to load.\n{details}\n{e}");
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                scriptProvider = null;
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
