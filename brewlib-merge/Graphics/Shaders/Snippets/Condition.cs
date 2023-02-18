using System;
using System.Collections.Generic;
using System.Text;

namespace BrewLib.Graphics.Shaders.Snippets
{
    public class Condition : ShaderSnippet
    {
        readonly Func<string> expression;
        readonly ShaderSnippet trueSnippet;
        readonly ShaderSnippet falseSnippet;

        public override IEnumerable<string> RequiredExtensions
        {
            get
            {
                foreach (var requiredExtension in trueSnippet.RequiredExtensions)
                    yield return requiredExtension;

                if (falseSnippet != null) foreach (var requiredExtension in falseSnippet.RequiredExtensions)
                        yield return requiredExtension;
            }
        }

        public override int MinVersion => falseSnippet != null ?
            Math.Max(trueSnippet.MinVersion, falseSnippet.MinVersion) : trueSnippet.MinVersion;

        public Condition(Func<string> expression, ShaderSnippet trueSnippet, ShaderSnippet falseSnippet = null)
        {
            this.expression = expression;
            this.trueSnippet = trueSnippet;
            this.falseSnippet = falseSnippet;
        }
        public override void GenerateFunctions(StringBuilder code)
        {
            trueSnippet.GenerateFunctions(code);
            falseSnippet?.GenerateFunctions(code);
        }
        public override void Generate(ShaderContext context) => context.Condition(expression, trueSnippet, falseSnippet);
    }
}