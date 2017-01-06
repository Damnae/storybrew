using StorybrewCommon.Scripting;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Permissions;

namespace StorybrewEditor.Scripting
{
    public class ScriptContainerAppDomain<TScript> : ScriptContainerBase<TScript>
        where TScript : Script
    {
        private AppDomain appDomain;

        public ScriptContainerAppDomain(ScriptManager<TScript> manager, string scriptTypeName, string mainSourcePath, string libraryFolder, string compiledScriptsPath, params string[] referencedAssemblies)
            : base(manager, scriptTypeName, mainSourcePath, libraryFolder, compiledScriptsPath, referencedAssemblies)
        {
        }

        protected override ScriptProvider<TScript> LoadScript()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptContainerAppDomain<TScript>));

            try
            {
                var assemblyPath = Path.Combine(CompiledScriptsPath, $"{Guid.NewGuid().ToString()}.dll");
                ScriptCompiler.Compile(SourcePaths, assemblyPath, ReferencedAssemblies);

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

                ScriptProvider<TScript> scriptProvider;
                try
                {
                    var scriptProviderHandle = Activator.CreateInstanceFrom(scriptDomain,
                        typeof(ScriptProvider<TScript>).Assembly.ManifestModule.FullyQualifiedName,
                        typeof(ScriptProvider<TScript>).FullName);
                    scriptProvider = (ScriptProvider<TScript>)scriptProviderHandle.Unwrap();
                    scriptProvider.Initialize(assemblyPath, ScriptTypeName);
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

                return scriptProvider;
            }
            catch (ScriptCompilationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw CreateScriptLoadingException(e);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                if (appDomain != null) AppDomain.Unload(appDomain);
                appDomain = null;
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
