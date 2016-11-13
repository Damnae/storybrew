using System;
using System.Collections.Generic;
using System.Text;

namespace BrewLib.Graphics.Shaders
{
    public abstract class ShaderSnippet
    {
        public readonly static ShaderSnippet Empty = new EmptySnippet();

        private static int lastId;
        protected static string NextGenericFunctionName => $"_func{lastId++:000}";

        public virtual IEnumerable<string> RequiredExtensions { get { yield break; } }
        public virtual int MinVersion => 110;

        public virtual void GenerateFunctions(StringBuilder code) { }
        public virtual void Generate(ShaderContext context)
            => context.Comment(GetType().Name);

        public static explicit operator ShaderSnippet(Action<ShaderContext> action) => new Snippets.CustomSnippet(action);

        private class EmptySnippet : ShaderSnippet
        {
            public EmptySnippet() { }
            public override void Generate(ShaderContext context) { }
        }
    }
}
