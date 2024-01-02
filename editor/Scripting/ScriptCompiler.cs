using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

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
            if (!useRoslyn)
                throw new InvalidOperationException("Compilation without roslyn is no longer supported. Make sure to set \"UseRoslyn: True\" in the settings.cfg");

            EmitResult compilationResult;
            var syntaxTrees = sourcePaths.Select(sourcePath =>
                CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath), path: sourcePath, encoding: Encoding.UTF8));

            var newResults = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(outputPath), syntaxTrees)
                .WithReferences(referencedAssemblies.Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath)))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Release)
                    .WithSpecificDiagnosticOptions(new KeyValuePair<string, ReportDiagnostic>[] { new("CS1701", ReportDiagnostic.Suppress) })
                );
            using (var peStream = File.Create(outputPath))
            {
                compilationResult = newResults.Emit(peStream, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.Embedded));
            }

            if (!compilationResult.Success)
            {
                var message = new StringBuilder("Compilation error\n\n");
                foreach (Diagnostic diagnostic in compilationResult.Diagnostics)
                {
                    var line = diagnostic.Location.SourceTree!.GetText().Lines[diagnostic.Location.GetLineSpan().StartLinePosition.Line];
                    message.AppendLine(diagnostic.ToString());
                    message.AppendLine(line.ToString());
                }

                throw new ScriptCompilationException(message.ToString());
            }
        }
    }
}
