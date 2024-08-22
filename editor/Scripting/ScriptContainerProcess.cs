using StorybrewCommon.Scripting;
using StorybrewEditor.Processes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace StorybrewEditor.Scripting
{
    public class ScriptContainerProcess<TScript> : ScriptContainerBase<TScript>
        where TScript : Script
    {
        private RemoteProcessWorkerContainer workerProcess;

        public ScriptContainerProcess(ScriptManager<TScript> manager, string scriptTypeName, string mainSourcePath, string libraryFolder, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
            : base(manager, scriptTypeName, mainSourcePath, libraryFolder, compiledScriptsPath, referencedAssemblies)
        {
        }

        protected override ScriptProvider<TScript> LoadScript()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptContainerAppLoadContext<TScript>));

            try
            {
                var assemblyPath = Path.Combine(CompiledScriptsPath, $"{Guid.NewGuid().ToString()}.dll");
                ScriptCompiler.Compile(SourcePaths, assemblyPath, ReferencedAssemblies);

                workerProcess?.Dispose();
                workerProcess = new RemoteProcessWorkerContainer();

                return workerProcess.Worker.CreateScriptProvider<TScript>(Assembly.LoadFrom(assemblyPath).GetType(ScriptTypeName));
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
                    workerProcess?.Dispose();

                workerProcess = null;
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
