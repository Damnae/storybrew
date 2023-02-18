namespace BrewLib.Graphics.Shaders
{
    internal class ShaderFieldVariable : ShaderVariable
    {
        readonly Reference reference;
        public override ShaderVariable.Reference Ref
        {
            get
            {
                RecordDependency();
                return reference;
            }
        }

        public ShaderFieldVariable(ShaderContext context, ShaderVariable baseVariable, ShaderType.Field field)
            : base(context, $"{baseVariable.Name}_field_{field.Name}", field.ShaderTypeName, baseVariable.ArrayCount)
            => reference = new Reference(baseVariable, field);

        public new class Reference : ShaderVariable.Reference
        {
            readonly ShaderType.Field field;
            public Reference(ShaderVariable variable, ShaderType.Field field) : base(variable) => this.field = field;

            public override string this[string index] => $"{base[index]}.{field.Name}";
            public override string ToString() => $"{base.ToString()}.{field.Name}";
        }
    }
}