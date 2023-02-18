using System.Collections.Generic;
using System.Text;

namespace BrewLib.Graphics.Shaders
{
    public class ShaderPartScope
    {
        int lastId;
        string nextGenericName => $"_{variablePrefix}_{lastId++:000}";

        readonly string variablePrefix;
        readonly List<ShaderVariable> variables = new List<ShaderVariable>();

        public ShaderPartScope(string variablePrefix) => this.variablePrefix = variablePrefix;

        public ShaderVariable AddVariable(ShaderContext context, string shaderTypeName)
        {
            var variable = new ShaderVariable(context, nextGenericName, shaderTypeName);
            variables.Add(variable);
            return variable;
        }
        public void DeclareVariables(StringBuilder code)
        {
            foreach (var variable in variables)
            {
                code.Append($"{variable.ShaderTypeName} {variable.Name}");
                if (variable.ArrayCount != -1) code.Append($"[{variable.ArrayCount}]");
                code.AppendLine(";");
            }
        }
    }
}