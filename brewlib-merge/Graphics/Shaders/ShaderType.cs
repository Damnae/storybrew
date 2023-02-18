using System;
using System.Collections.Generic;

namespace BrewLib.Graphics.Shaders
{
    public class ShaderType
    {
        readonly List<Field> fields = new List<Field>();

        public readonly string Name;
        public IEnumerable<Field> Fields => fields;

        public ShaderType(string name) => Name = name;

        public Field AddField(string name, string shaderTypeName)
        {
            var field = new Field(name, shaderTypeName);
            fields.Add(field);
            return field;
        }
        public ShaderVariable FieldAsVariable(ShaderVariable variable, Field field)
        {
            if (variable == null) return null;

            if (variable.ShaderTypeName != Name) throw new InvalidOperationException();
            if (!fields.Contains(field)) throw new InvalidOperationException();

            return new ShaderFieldVariable(variable.Context, variable, field);
        }
        public class Field
        {
            public readonly string Name;
            public readonly string ShaderTypeName;

            public Field(string name, string shaderTypeName)
            {
                Name = name;
                ShaderTypeName = shaderTypeName;
            }
        }
    }
}