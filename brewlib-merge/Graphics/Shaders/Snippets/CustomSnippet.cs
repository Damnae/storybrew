using System;

namespace BrewLib.Graphics.Shaders.Snippets
{
    public class CustomSnippet : ShaderSnippet
    {
        readonly Action<ShaderContext> action;

        public CustomSnippet(Action<ShaderContext> action) => this.action = action;
        public override void Generate(ShaderContext context)
        {
            base.Generate(context);
            action(context);
        }
    }
}