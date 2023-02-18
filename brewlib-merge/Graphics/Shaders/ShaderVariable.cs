using System;

namespace BrewLib.Graphics.Shaders
{
    public class ShaderVariable
    {
        public readonly ShaderContext Context;
        public readonly string Name;
        public readonly string ShaderTypeName;
        public readonly int ArrayCount;

        readonly Reference reference;
        public virtual Reference Ref
        {
            get
            {
                RecordDependency();
                return reference;
            }
        }

        public ShaderVariable(ShaderContext context, string name, string shaderTypeName = null, int count = -1)
        {
            Context = context;
            Name = name;
            ShaderTypeName = shaderTypeName;
            ArrayCount = count;

            reference = new Reference(this);
        }

        public void Assign(ShaderVariable value, string components = null) => Context.Assign(this, value, components);
        public void Assign(Func<string> expression, string components = null) => Context.Assign(this, expression, components);

        protected void RecordDependency() => Context.RecordDependency(this);
        public override string ToString()
        {
            var arrayTag = ArrayCount != -1 ? $"[{ArrayCount}]" : string.Empty;
            return $"{ShaderTypeName} {Name}{arrayTag}";
        }
        public class Reference
        {
            readonly ShaderVariable variable;
            public Reference(ShaderVariable variable) => this.variable = variable;

            public string this[int index] => this[index.ToString()];
            public virtual string this[string index] => $"{variable.Name}[{index}]";
            public override string ToString() => variable.Name;
        }
    }
}