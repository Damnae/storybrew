using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace StorybrewEditor.Scripting
{
    public class ScriptCompiler
    {
        private static int nextId;

        public static void Compile(string[] sourcePaths, string outputPath, IEnumerable<string> referencedAssemblies)
        {
            Debug.Print($"{nameof(Scripting)}: Compiling {string.Join(", ", sourcePaths)}");

            // Create a new AssemblyLoadContext for isolation
            var assemblyLoadContext = new AssemblyLoadContext($"ScriptCompiler {nextId++}", isCollectible: true);

            try
            {
                // Load the current assembly into the new context
                var assembly = assemblyLoadContext.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);

                // Create an instance of ScriptCompiler in the new context
                var compilerType = assembly.GetType(typeof(ScriptCompiler).FullName);
                var compilerInstance = Activator.CreateInstance(compilerType);

                // Call the compile method
                var compileMethod = compilerType.GetMethod("compile", BindingFlags.Instance | BindingFlags.NonPublic);
                compileMethod.Invoke(compilerInstance, new object[] { sourcePaths, outputPath, referencedAssemblies });
            }
            finally
            {
                // Unload the AssemblyLoadContext to free resources
                assemblyLoadContext.Unload();
            }
        }

        private void compile(string[] sourcePaths, string outputPath, IEnumerable<string> referencedAssemblies)
        {
            var syntaxTrees = sourcePaths.Select(path => SyntaxFactory.ParseSyntaxTree(File.ReadAllText(path), path: path)).ToArray();

            var references = new List<MetadataReference>();

            foreach (var assembly in referencedAssemblies)
            {
                if (File.Exists(assembly))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly));
                }
            }

            var compilation = CSharpCompilation.Create(
                assemblyName: Path.GetFileNameWithoutExtension(outputPath),
                syntaxTrees: syntaxTrees,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            EmitResult result;
            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                result = compilation.Emit(stream);
            }

            if (!result.Success)
            {
                var diagnostics = result.Diagnostics
                    .Where(diag => diag.Severity == DiagnosticSeverity.Error);

                var sourceLines = new Dictionary<string, string[]>();
                foreach (var sourcePath in sourcePaths)
                {
                    try
                    {
                        sourceLines[Path.GetFullPath(sourcePath)] = File.ReadAllText(sourcePath).Split('\n');
                    }
                    catch { }
                }

                var message = new StringBuilder("Compilation error\n\n");
                foreach (var diagnostic in diagnostics)
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();
                    var filePath = lineSpan.Path;
                    var linePosition = lineSpan.StartLinePosition.Line + 1;
                    var lineContent = sourceLines.ContainsKey(filePath) ? sourceLines[filePath].ElementAtOrDefault(linePosition - 1) : string.Empty;

                    message.AppendLine($"{filePath}, line {linePosition}: {diagnostic.GetMessage()}");
                    if (!string.IsNullOrWhiteSpace(lineContent))
                    {
                        message.AppendLine(lineContent);
                    }
                }

                throw new ScriptCompilationException(message.ToString());
            }
        }
    }
}
