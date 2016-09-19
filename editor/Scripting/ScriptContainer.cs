using StorybrewCommon.Scripting;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace StorybrewEditor.Scripting
{
    public class ScriptContainer<TScript> : IDisposable
        where TScript : Script
    {
        private static int nextId;
        public readonly int Id = nextId++;

        private ScriptManager<TScript> manager;
        private string scriptTypeName;
        public string ScriptTypeName => scriptTypeName;
        private string sourcePath;
        public string SourcePath => sourcePath;
        private string compiledScriptsPath;
        private string[] referencedAssemblies;

        private ScriptProvider<TScript> scriptProvider;
        private AppDomain appDomain;

        private volatile int currentVersion = 0;
        private volatile int targetVersion = 1;

        /// <summary>
        /// Returns false when Script would return null.
        /// </summary>
        public bool HasScript => scriptProvider != null || currentVersion != targetVersion;

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

        public event EventHandler OnScriptChanged;

        public ScriptContainer(ScriptManager<TScript> manager, string scriptTypeName, string sourcePath, string compiledScriptsPath, params string[] referencedAssemblies)
        {
            this.manager = manager;
            this.scriptTypeName = scriptTypeName;
            this.sourcePath = sourcePath;
            this.compiledScriptsPath = compiledScriptsPath;
            this.referencedAssemblies = referencedAssemblies;
        }

        public TScript CreateScript(out bool scriptChanged)
        {
            var localTargetVersion = targetVersion;
            if (currentVersion < localTargetVersion)
            {
                currentVersion = localTargetVersion;
                loadScript();
                scriptChanged = true;
            }
            else scriptChanged = false;
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

        private void loadScript()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptContainer<TScript>));

            try
            {
                var assemblyPath = Path.Combine(compiledScriptsPath, $"{Guid.NewGuid().ToString()}.dll");
                ScriptCompiler.Compile(sourcePath, assemblyPath, referencedAssemblies);

                var setup = new AppDomainSetup()
                {
                    ApplicationName = $"{Name} {Id}",
                    ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    DisallowCodeDownload = true,
                    DisallowPublisherPolicy = true,
                    DisallowBindingRedirects = true,
                };

                var permissions = new PermissionSet(PermissionState.Unrestricted);

                Debug.Print($"{nameof(Scripting)}: Loading domain {setup.ApplicationName}");
                var scriptDomain = AppDomain.CreateDomain(setup.ApplicationName, null, setup, permissions);
                try
                {
                    var scriptProviderHandle = Activator.CreateInstanceFrom(scriptDomain,
                        typeof(ScriptProvider<TScript>).Assembly.ManifestModule.FullyQualifiedName,
                        typeof(ScriptProvider<TScript>).FullName);
                    var scriptProvider = (ScriptProvider<TScript>)scriptProviderHandle.Unwrap();
                    scriptProvider.Initialize(assemblyPath, scriptTypeName);

                    this.scriptProvider = scriptProvider;
                }
                catch
                {
                    AppDomain.Unload(scriptDomain);
                    throw;
                }

                if (appDomain != null)
                {
                    Debug.Print($"{nameof(Scripting)}: Unloading domain {appDomain.FriendlyName}");
                    AppDomain.Unload(appDomain);
                }

                appDomain = scriptDomain;
            }
            catch (ScriptCompilationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ScriptLoadingException($"{scriptTypeName} failed to load", e);
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
                }

                if (appDomain != null) AppDomain.Unload(appDomain);
                appDomain = null;
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

    public class ScriptProvider<TScript> : MarshalByRefObject
    {
        private Type type;

        public void Initialize(string assemblyPath, string typeName)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            type = assembly.GetType(typeName, true, true);
        }

        public TScript CreateScript() => (TScript)Activator.CreateInstance(type);
    }
}
