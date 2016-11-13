using System;

namespace BrewLib.Graphics.Shaders.Snippets
{
    public class Assign : ShaderSnippet
    {
        private ShaderVariable result;
        private Func<string> expression;

        public Assign(ShaderVariable result, Func<string> expression)
        {
            this.result = result;
            this.expression = expression;
        }
        public Assign(ShaderVariable result, ShaderVariable value) : this(result, () => value.Ref.ToString()) { }
        public Assign(ShaderVariable result, VertexAttribute value) : this(result, () => value.Name) { }

        public override void Generate(ShaderContext context)
        {
            base.Generate(context);
            result?.Assign(expression);
        }
    }
}
