using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace StorybrewEditor.Scripting
{
    public class ScriptCompiler : MarshalByRefObject
    {
        private static int nextId;

        public static void Compile(string sourcePath, string outputPath, params string[] referencedAssemblies)
        {
            var setup = new AppDomainSetup()
            {
                ApplicationName = $"ScriptCompiler {nextId++}",
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
            };

            Debug.Print($"{nameof(Scripting)}: Compiling {sourcePath}");
            var compilerDomain = AppDomain.CreateDomain(setup.ApplicationName, null, setup);
            try
            {
                var compiler = (ScriptCompiler)compilerDomain.CreateInstanceFromAndUnwrap(
                    typeof(ScriptCompiler).Assembly.ManifestModule.FullyQualifiedName,
                    typeof(ScriptCompiler).FullName);

                compiler.compile(sourcePath, outputPath, referencedAssemblies);
            }
            finally
            {
                AppDomain.Unload(compilerDomain);
            }
        }

        private void compile(string sourcePath, string outputPath, params string[] referencedAssemblies)
        {
            var parameters = new CompilerParameters()
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                OutputAssembly = outputPath,
#if DEBUG
                IncludeDebugInformation = true,
#endif
            };

            foreach (var referencedAssembly in referencedAssemblies)
                parameters.ReferencedAssemblies.Add(referencedAssembly);

            using (var codeProvider = CodeDomProvider.CreateProvider("csharp"))
            {
                var results = codeProvider.CompileAssemblyFromFile(parameters, sourcePath);

                var errors = results.Errors;
                if (errors.Count > 0)
                {
                    var sourceLines = File.ReadAllText(sourcePath).Split('\n');
                    var message = new StringBuilder();
                    for (var i = 0; i < errors.Count; i++)
                    {
                        var error = errors[i];
                        message.AppendLine($"(Line {error.Line}) {error.ErrorText}");
                        if (i == errors.Count - 1 || error.Line != errors[i + 1].Line)
                            message.AppendLine(sourceLines[error.Line - 1]);
                    }
                    throw new ScriptCompilationException(message.ToString());
                }
            }
        }
    }
}
