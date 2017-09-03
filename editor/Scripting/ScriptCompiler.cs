using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace StorybrewEditor.Scripting
{
    public class ScriptCompiler : MarshalByRefObject
    {
        private static int nextId;

        public static void Compile(string[] sourcePaths, string outputPath, IEnumerable<string> referencedAssemblies)
        {
            var setup = new AppDomainSetup()
            {
                ApplicationName = $"ScriptCompiler {nextId++}",
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
            };

            Debug.Print($"{nameof(Scripting)}: Compiling {string.Join(", ", sourcePaths)}");
            var compilerDomain = AppDomain.CreateDomain(setup.ApplicationName, null, setup);
            try
            {
                var compiler = (ScriptCompiler)compilerDomain.CreateInstanceFromAndUnwrap(
                    typeof(ScriptCompiler).Assembly.ManifestModule.FullyQualifiedName,
                    typeof(ScriptCompiler).FullName);

                compiler.compile(sourcePaths, outputPath, Program.Settings.UseRoslyn, referencedAssemblies);
            }
            finally
            {
                AppDomain.Unload(compilerDomain);
            }
        }

        private void compile(string[] sourcePaths, string outputPath, bool useRoslyn, IEnumerable<string> referencedAssemblies)
        {
            var parameters = new CompilerParameters()
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                OutputAssembly = outputPath,
                IncludeDebugInformation = true,
            };

            foreach (var referencedAssembly in referencedAssemblies)
                parameters.ReferencedAssemblies.Add(referencedAssembly);

            using (var codeProvider = useRoslyn ? new CSharpCodeProvider() : CodeDomProvider.CreateProvider("csharp"))
            {
                var results = codeProvider.CompileAssemblyFromFile(parameters, sourcePaths);

                var errors = results.Errors;
                if (errors.Count > 0)
                {
                    var sourceLines = new Dictionary<string, string[]>();
                    foreach (var sourcePath in sourcePaths)
                        sourceLines[sourcePath.ToLowerInvariant()] = File.ReadAllText(sourcePath).Split('\n');

                    var message = new StringBuilder("Compilation error\n\n");
                    for (var i = 0; i < errors.Count; i++)
                    {
                        var error = errors[i];
                        if (!string.IsNullOrWhiteSpace(error.FileName))
                        {
                            message.AppendLine($"{error.FileName}, line {error.Line}: {error.ErrorText}");
                            if (i == errors.Count - 1 || error.Line != errors[i + 1].Line)
                                message.AppendLine(sourceLines[error.FileName.ToLowerInvariant()][error.Line - 1]);
                        }
                        else message.AppendLine(error.ErrorText);
                    }
                    throw new ScriptCompilationException(message.ToString());
                }
            }
        }
    }
}
