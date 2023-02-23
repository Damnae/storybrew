using StorybrewCommon.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorybrewEditor.Scripting
{
    public abstract class ScriptContainerBase<TScript> : ScriptContainer<TScript> where TScript : Script
    {
        static int nextId;
        public readonly int Id = nextId++;

        public string CompiledScriptsPath { get; }

        ScriptProvider<TScript> scriptProvider;

        volatile int currentVersion = 0, targetVersion = 1;

        public string Name
        {
            get
            {
                var name = ScriptTypeName;
                if (name.Contains(".")) name = name.Substring(name.LastIndexOf('.') + 1);
                return name;
            }
        }
        public string ScriptTypeName { get; }
        public string MainSourcePath { get; }
        public string LibraryFolder { get; }
        public string[] SourcePaths
        {
            get
            {
                if (LibraryFolder == null || !Directory.Exists(LibraryFolder)) return new[] { MainSourcePath };
                return Directory.GetFiles(LibraryFolder, "*.cs", SearchOption.AllDirectories).Concat(new[] { MainSourcePath }).ToArray();
            }
        }

        List<string> referencedAssemblies = new List<string>();
        public IEnumerable<string> ReferencedAssemblies
        {
            get => referencedAssemblies;
            set
            {
                var newReferencedAssemblies = new List<string>(value);
                if (newReferencedAssemblies.Count == referencedAssemblies.Count &&
                    newReferencedAssemblies.All(ass => referencedAssemblies.Contains(ass))) return;

                referencedAssemblies = newReferencedAssemblies;
                ReloadScript();
            }
        }

        ///<summary> Returns false when Script would return null. </summary>
        public bool HasScript => scriptProvider != null || currentVersion != targetVersion;
        public event EventHandler OnScriptChanged;

        public ScriptContainerBase(string scriptTypeName, string mainSourcePath, string libraryFolder, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
        {
            ScriptTypeName = scriptTypeName;
            MainSourcePath = mainSourcePath;
            LibraryFolder = libraryFolder;
            CompiledScriptsPath = compiledScriptsPath;

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
                if (targetVersion <= localCurrentVersion) targetVersion = localCurrentVersion + 1;
            }
            while (currentVersion != localCurrentVersion);

            if (targetVersion > initialTargetVersion) OnScriptChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract ScriptProvider<TScript> LoadScript();
        protected ScriptLoadingException CreateScriptLoadingException(Exception e)
        {
            var details = "";
            if (e is TypeLoadException) details = "Make sure the script's class name is the same as the file name.\n";
            return new ScriptLoadingException($"{ScriptTypeName} failed to load.\n{details}\n{e}");
        }

        #region IDisposable Support

        bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                scriptProvider = null;
                disposedValue = true;
            }
        }
        public void Dispose() => Dispose(true);

        #endregion
    }
}