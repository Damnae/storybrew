using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace StorybrewEditor.Scripting
{
    public class ScriptCompiler : MarshalByRefObject
    {
        static readonly string[] environmentDirectories =
        {
            Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "WPF"),
            Path.GetDirectoryName(typeof(object).Assembly.Location),
            Environment.CurrentDirectory
        };
        static int nextId;

        public static void Compile(string[] sourcePaths, string outputPath, IEnumerable<string> referencedAssemblies)
        {
            var setup = new AppDomainSetup
            {
                ApplicationName = $"ScriptCompiler {nextId++}",
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
            };

            Debug.Print($"{nameof(Scripting)}: Compiling {string.Join(", ", sourcePaths)}");
            var compilerDomain = AppDomain.CreateDomain(setup.ApplicationName, null, setup);
            try
            {
                var compiler = (ScriptCompiler)compilerDomain.CreateInstanceFromAndUnwrap(
                    typeof(ScriptCompiler).Assembly.ManifestModule.FullyQualifiedName,
                    typeof(ScriptCompiler).FullName);

                compiler.compile(sourcePaths, outputPath, referencedAssemblies);
            }
            finally
            {
                AppDomain.Unload(compilerDomain);
            }
        }
        void compile(string[] sourcePaths, string outputPath, IEnumerable<string> referencedAssemblies)
        {
            var symbolPath = Path.ChangeExtension(outputPath, "pdb");
            var trees = new Dictionary<SyntaxTree, KeyValuePair<string, SourceText>>();
            foreach (var sourcePath in sourcePaths) using (var sourceStream = File.OpenRead(sourcePath))
            {
                var sourceText = SourceText.From(sourceStream, canBeEmbedded: true);
                var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

                var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText, parseOptions);
                trees.Add(syntaxTree, new KeyValuePair<string, SourceText>(sourcePath, sourceText));
            }
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            foreach (var referencedAssembly in referencedAssemblies)
            {
                string asmPath = referencedAssembly;
                try
                {
                    if (Path.IsPathRooted(asmPath)) references.Add(MetadataReference.CreateFromFile(asmPath));
                    else
                    {
                        var isExist = false;
                        foreach (var environmentDir in environmentDirectories)
                        {
                            var actualAsmPath = Path.Combine(environmentDir, referencedAssembly);
                            if (!File.Exists(actualAsmPath)) continue;
                            isExist = true;
                            asmPath = actualAsmPath;
                            break;
                        }

                        if (isExist) references.Add(MetadataReference.CreateFromFile(asmPath));
                        else throw new Exception($"Could not resolve dependency: \"{referencedAssembly}\". " +
                            $"Searched directories: {string.Join(";", environmentDirectories.Select(k => $"\"{k}\""))}");
                    }
                }
                catch (Exception e)
                {
                    var message = new StringBuilder("Compilation error\n\n");
                    message.AppendLine(e.ToString());
                    throw new ScriptCompilationException(message.ToString());
                }
            }

            var compilation = CSharpCompilation.Create(
                Path.GetFileName(outputPath),
                trees.Keys,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: true, platform: Platform.AnyCpu, optimizationLevel: 
                    OptimizationLevel.Release, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            using (var assemblyStream = File.Create(outputPath)) using (var symbolsStream = File.Create(symbolPath))
            {
                var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: symbolPath);
                var embeddedTexts = trees.Values.Select(k => EmbeddedText.FromSource(k.Key, k.Value));
                var result = compilation.Emit(assemblyStream, symbolsStream, embeddedTexts: embeddedTexts, options: emitOptions);

                if (result.Success) return;

                var failures = result.Diagnostics.Where(diagnostic => 
                    diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error).ToList();

                failures.Reverse();
                var failureGroup = failures.GroupBy(k =>
                {
                    if (k.Location.SourceTree == null) return "";
                    if (trees.TryGetValue(k.Location.SourceTree, out var path)) return path.Key;
                    return "";
                }).ToDictionary(k => k.Key, k => k.ToHashSet());

                var message = new StringBuilder("Compilation error\n\n");
                foreach (var kvp in failureGroup)
                {
                    var file = kvp.Key;
                    var diagnostics = kvp.Value;
                    message.AppendLine($"{Path.GetFileName(file)}:");
                    foreach (var diagnostic in diagnostics) message.AppendLine($"--{diagnostic}");
                }

                throw new ScriptCompilationException(message.ToString());
            }
        }
    }
}