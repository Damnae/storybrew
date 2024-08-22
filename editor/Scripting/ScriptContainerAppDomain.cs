using StorybrewCommon.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace StorybrewEditor.Scripting
{
    public class ScriptContainerAppDomain<TScript> : ScriptContainerBase<TScript>
        where TScript : Script
    {
        private AssemblyLoadContext assemblyLoadContext;
        private Assembly scriptAssembly;

        public ScriptContainerAppDomain(ScriptManager<TScript> manager, string scriptTypeName, string mainSourcePath, string libraryFolder, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
            : base(manager, scriptTypeName, mainSourcePath, libraryFolder, compiledScriptsPath, referencedAssemblies)
        {
        }

        protected override ScriptProvider<TScript> LoadScript()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptContainerAppDomain<TScript>));

            try
            {
                var assemblyPath = Path.Combine(CompiledScriptsPath, $"{Guid.NewGuid()}.dll");
                ScriptCompiler.Compile(SourcePaths, assemblyPath, ReferencedAssemblies);

                // Create a new AssemblyLoadContext for isolation
                var contextName = $"{Name} {Id}";
                Debug.Print($"{nameof(Scripting)}: Creating AssemblyLoadContext {contextName}");
                var assemblyLoadContext = new AssemblyLoadContext(contextName, isCollectible: true);

                try
                {
                    // Load the assembly into the new context
                    var scriptAssembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

                    // Create an instance of ScriptProvider<TScript> within the new context
                    ScriptProvider<TScript> provider = new ScriptProvider<TScript>();
                    provider.Initialize(assemblyPath, ScriptTypeName);

                    // Unload the previous context
                    if (this.assemblyLoadContext != null)
                    {
                        Debug.Print($"{nameof(Scripting)}: Unloading AssemblyLoadContext {this.assemblyLoadContext.Name}");
                        this.assemblyLoadContext.Unload();
                    }

                    this.assemblyLoadContext = assemblyLoadContext;
                    this.scriptAssembly = scriptAssembly;

                    return provider;
                }
                catch
                {
                    assemblyLoadContext.Unload();
                    throw;
                }
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
                    // Dispose of managed resources if necessary
                }

                // Unload the AssemblyLoadContext
                if (assemblyLoadContext != null)
                {
                    Debug.Print($"{nameof(Scripting)}: Unloading AssemblyLoadContext {assemblyLoadContext.Name}");
                    assemblyLoadContext.Unload();
                }

                assemblyLoadContext = null;
                scriptAssembly = null;
                disposedValue = true;

                base.Dispose(disposing);
            }
        }

        #endregion
    }
}
