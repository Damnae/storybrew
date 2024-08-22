using StorybrewCommon.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace StorybrewEditor.Scripting
{
    public class ScriptContainerAppLoadContext<TScript> : ScriptContainerBase<TScript>
        where TScript : Script
    {
        private AssemblyLoadContext assemblyLoadContext;

        public ScriptContainerAppLoadContext(ScriptManager<TScript> manager, string scriptTypeName, string mainSourcePath, string libraryFolder, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
            : base(manager, scriptTypeName, mainSourcePath, libraryFolder, compiledScriptsPath, referencedAssemblies)
        {
        }

        protected override ScriptProvider<TScript> LoadScript()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptContainerAppLoadContext<TScript>));

            try
            {
                if (assemblyLoadContext != null)
                {
                    Debug.Print($"{nameof(Scripting)}: Unloading AssemblyLoadContext {assemblyLoadContext.Name}");
                    assemblyLoadContext.Unload();
                }

                var assemblyPath = Path.Combine(CompiledScriptsPath, $"{Guid.NewGuid()}.dll");
                ScriptCompiler.Compile(SourcePaths, assemblyPath, ReferencedAssemblies);

                var contextName = $"{Name} {Id}";
                Debug.Print($"{nameof(Scripting)}: Creating AssemblyLoadContext {contextName}");
                assemblyLoadContext = new AssemblyLoadContext(contextName, isCollectible: true);

                try
                {
                    var assembly = assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
                    return new ScriptProvider<TScript>(assembly.GetType(ScriptTypeName));
                }
                catch
                {
                    Debug.Print($"{nameof(Scripting)}: Unloading AssemblyLoadContext {assemblyLoadContext.Name}");
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
                if (assemblyLoadContext != null)
                {
                    Debug.Print($"{nameof(Scripting)}: Unloading AssemblyLoadContext {assemblyLoadContext.Name}");
                    assemblyLoadContext.Unload();
                }

                assemblyLoadContext = null;
                disposedValue = true;

                base.Dispose(disposing);
            }
        }

        #endregion
    }
}
