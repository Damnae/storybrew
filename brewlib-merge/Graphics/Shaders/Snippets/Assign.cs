using System;

namespace BrewLib.Graphics.Shaders.Snippets
{
    public class Assign : ShaderSnippet
    {
        readonly ShaderVariable result;
        readonly Func<string> expression;
        readonly string components;

        public Assign(ShaderVariable result, Func<string> expression, string components = null)
        {
            this.result = result;
            this.expression = expression;
            this.components = components;
        }
        public Assign(ShaderVariable result, ShaderVariable value, string components = null) : this(result, () => value.Ref.ToString(), components) { }
        public Assign(ShaderVariable result, VertexAttribute value, string components = null) : this(result, () => value.Name, components) { }

        public override void Generate(ShaderContext context)
        {
            base.Generate(context);
            result?.Assign(expression, components);
        }
    }
}