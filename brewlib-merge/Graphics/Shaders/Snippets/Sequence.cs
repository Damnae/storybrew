using System.Collections.Generic;
using System.Text;

namespace BrewLib.Graphics.Shaders.Snippets
{
    public class Sequence : ShaderSnippet
    {
        readonly ShaderSnippet[] snippets;

        public override IEnumerable<string> RequiredExtensions
        {
            get
            {
                foreach (var snippet in snippets) foreach (var requiredExtension in snippet.RequiredExtensions)
                        yield return requiredExtension;
            }
        }
        public override int MinVersion
        {
            get
            {
                var minVersion = base.MinVersion;
                foreach (var snippet in snippets) if (snippet.MinVersion > minVersion)
                        minVersion = snippet.MinVersion;

                return minVersion;
            }
        }
        public Sequence(params ShaderSnippet[] snippets) => this.snippets = snippets;

        public override void GenerateFunctions(StringBuilder code)
        {
            foreach (var snippet in snippets) snippet.GenerateFunctions(code);
        }
        public override void Generate(ShaderContext context)
        {
            foreach (var snippet in snippets) snippet.Generate(context);
        }
    }
}