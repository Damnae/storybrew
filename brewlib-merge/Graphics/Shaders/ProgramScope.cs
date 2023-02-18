using System.Collections.Generic;
using System.Text;

namespace BrewLib.Graphics.Shaders
{
    public class ProgramScope
    {
        readonly List<ShaderType> types = new List<ShaderType>();
        readonly List<ShaderVariable> varyings = new List<ShaderVariable>();
        readonly List<ShaderVariable> uniforms = new List<ShaderVariable>();

        int lastId;
        string nextGenericTypeName => $"t_{lastId++:000}";
        string nextGenericVaryingName => $"v_{lastId++:000}";

        public ShaderType AddStruct()
        {
            var type = new ShaderType(nextGenericTypeName);
            types.Add(type);
            return type;
        }
        public ShaderVariable AddUniform(ShaderContext context, string name, string shaderTypeName, int count = -1)
        {
            var uniform = new ShaderVariable(context, name, shaderTypeName, count);
            uniforms.Add(uniform);
            return uniform;
        }
        public ShaderVariable AddVarying(ShaderContext context, string shaderTypeName)
        {
            var varying = new ShaderVariable(context, nextGenericVaryingName, shaderTypeName);
            varyings.Add(varying);
            return varying;
        }
        public void DeclareTypes(StringBuilder code)
        {
            foreach (var type in types)
            {
                code.AppendLine($"struct {type.Name} {{");
                foreach (var field in type.Fields) code.AppendLine($"    {field.ShaderTypeName} {field.Name};");
                code.AppendLine("};");
            }
        }
        public void DeclareUniforms(StringBuilder code)
        {
            foreach (var uniform in uniforms)
            {
                code.Append($"uniform {uniform.ShaderTypeName} {uniform.Name}");
                if (uniform.ArrayCount != -1) code.Append($"[{uniform.ArrayCount}]");
                code.AppendLine(";");
            }
        }
        public void DeclareVaryings(StringBuilder code, ShaderContext context)
        {
            foreach (var varying in varyings) if (context.Uses(varying))
                {
                    code.Append($"varying {varying.ShaderTypeName} {varying.Name}");
                    if (varying.ArrayCount != -1) code.Append($"[{varying.ArrayCount}]");
                    code.AppendLine(";");
                }
        }
        public void DeclareUnusedVaryingsAsVariables(StringBuilder code, ShaderContext context)
        {
            foreach (var varying in varyings) if (!context.Uses(varying))
                {
                    code.Append($"{varying.ShaderTypeName} {varying.Name}");
                    if (varying.ArrayCount != -1) code.Append($"[{varying.ArrayCount}]");
                    code.AppendLine(";");
                }
        }
    }
}