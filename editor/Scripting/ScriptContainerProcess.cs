using StorybrewCommon.Scripting;
using StorybrewEditor.Processes;
using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewEditor.Scripting
{
    public class ScriptContainerProcess<TScript> : ScriptContainerBase<TScript> where TScript : Script
    {
        RemoteProcessWorkerContainer workerProcess;

        public ScriptContainerProcess(string scriptTypeName, string mainSourcePath, string libraryFolder, string compiledScriptsPath, IEnumerable<string> referencedAssemblies)
            : base(scriptTypeName, mainSourcePath, libraryFolder, compiledScriptsPath, referencedAssemblies) { }

        protected override ScriptProvider<TScript> LoadScript()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(ScriptContainerAppDomain<TScript>));
            try
            {
                var assemblyPath = Path.Combine(CompiledScriptsPath, $"{Guid.NewGuid()}.dll");
                ScriptCompiler.Compile(SourcePaths, assemblyPath, ReferencedAssemblies);

                workerProcess?.Dispose();
                workerProcess = new RemoteProcessWorkerContainer();

                var scriptProvider = workerProcess.Worker.CreateScriptProvider<TScript>();
                scriptProvider.Initialize(assemblyPath, ScriptTypeName);

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

        bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) workerProcess?.Dispose();
                workerProcess = null;
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}