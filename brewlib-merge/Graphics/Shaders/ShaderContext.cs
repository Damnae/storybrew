using System;
using System.Collections.Generic;
using System.Text;

namespace BrewLib.Graphics.Shaders
{
    public class ShaderContext
    {
        int lastId;
        string nextGenericName => $"_tmp_{lastId++:000}";

        readonly Dictionary<ShaderVariable, HashSet<ShaderVariable>> dependencies = new Dictionary<ShaderVariable, HashSet<ShaderVariable>>();
        readonly HashSet<ShaderVariable> usedVariables = new HashSet<ShaderVariable>();
        readonly HashSet<ShaderVariable> flowVariables = new HashSet<ShaderVariable>();
        ShaderVariable[] dependantVariables;
        bool flowDependant;
        StringBuilder code;
        bool canReceiveCommands;

        public VertexDeclaration VertexDeclaration;

        public void RecordDependency(ShaderVariable referencedVariable)
        {
            if (dependantVariables == null && !flowDependant) throw new InvalidOperationException("Cannot reference variables while dependencies aren't defined");
            if (flowDependant) flowVariables.Add(referencedVariable);

            if (dependantVariables != null) foreach (var dependentVariable in dependantVariables)
                {
                    if (referencedVariable == dependentVariable) continue;

                    if (!dependencies.TryGetValue(dependentVariable, out HashSet<ShaderVariable> existingDependencies))
                        existingDependencies = dependencies[dependentVariable] = new HashSet<ShaderVariable>();

                    existingDependencies.Add(referencedVariable);
                }
        }
        public void MarkUsedVariables(Action action, params ShaderVariable[] outputVariables)
        {
            if (canReceiveCommands) throw new InvalidOperationException(code == null ? "Already marking used variables" : "Can't mark used variables while generate code");

            canReceiveCommands = true;
            action();
            canReceiveCommands = false;

            foreach (var flowVariable in flowVariables) markUsed(flowVariable);
            foreach (var outputVariable in outputVariables) markUsed(outputVariable);
        }
        public void GenerateCode(StringBuilder code, Action action)
        {
            if (canReceiveCommands) throw new InvalidOperationException(this.code != null ? "Already generating code" : "Can't generate code while mark used variables");

            this.code = code;
            canReceiveCommands = true;
            action();
            this.code = null;
            canReceiveCommands = false;
        }

        public bool Uses(ShaderVariable variable) => usedVariables.Contains(variable);
        public bool UsesAny(params ShaderVariable[] variables)
        {
            foreach (var variable in variables) if (Uses(variable)) return true;
            return false;
        }

        public ShaderVariable Declare(string shaderTypeName, Func<string> expression = null)
        {
            checkCanReceiveCommands();

            var variable = new ShaderVariable(this, nextGenericName, shaderTypeName);
            assign(variable, expression, true, null);
            return variable;
        }
        public ShaderVariable Declare(string shaderTypeName, ShaderVariable value) => Declare(shaderTypeName, () => value.Ref.ToString());
        public ShaderVariable Declare(string shaderTypeName, VertexAttribute value) => Declare(shaderTypeName, () => value.Name);

        public void Assign(ShaderVariable result, Func<string> expression, string components = null) => assign(result, expression, false, components);
        public void Assign(ShaderVariable result, ShaderVariable value, string components = null) => assign(result, () => value.Ref.ToString(), false, components);
        public void Assign(ShaderVariable result, VertexAttribute value, string components = null) => assign(result, () => value.Name, false, components);

        public void Condition(Func<string> expression, ShaderSnippet trueSnippet, ShaderSnippet falseSnippet)
        {
            checkCanReceiveCommands();

            if (code != null)
            {
                FlowDependant(() => $"if ({expression()})\n{{");
                trueSnippet.Generate(this);
                if (falseSnippet != null)
                {
                    code.AppendLine("}\nelse\n{");
                    falseSnippet.Generate(this);
                }
                code.AppendLine("}");
            }
            else
            {
                FlowDependant(expression);
                trueSnippet.Generate(this);
                falseSnippet?.Generate(this);
            }
        }
        public void Dependant(Func<string> expression, params ShaderVariable[] dependantVariables)
        {
            checkCanReceiveCommands();

            var previousDependentVariables = this.dependantVariables;
            this.dependantVariables = dependantVariables;

            if (code != null) code.AppendLine($"{expression()};");
            else expression();

            this.dependantVariables = previousDependentVariables;
        }
        public void FlowDependant(Func<string> expression)
        {
            checkCanReceiveCommands();

            var previousFlowDependant = flowDependant;
            flowDependant = true;

            if (code != null) code.AppendLine($"{expression()}");
            else expression();

            flowDependant = previousFlowDependant;
        }
        public void Comment(string line)
        {
            checkCanReceiveCommands();
            line = string.Join("\n// ", line.Split('\n'));
            code?.AppendLine($"\n// {line}\n");
        }
        public void Preprocessor(string line)
        {
            checkCanReceiveCommands();
            code?.AppendLine($"#{line}");
        }
        void assign(ShaderVariable result, Func<string> expression, bool declare, string components = null)
        {
            checkCanReceiveCommands();

            if (result == null) throw new ArgumentNullException(nameof(result));
            if (declare && components != null) throw new InvalidOperationException("Cannot set components when declaring a variable");
            if (expression != null)
            {
                Dependant(() => declare ? $"{result.ShaderTypeName} {result.Ref} = {expression()}" :
                    components != null ?
                    $"{result.Ref}.{components} = {expression()}" : $"{result.Ref} = {expression()}", result);
            }
            else if (declare) code?.AppendLine($"{result.ShaderTypeName} {result.Name};");
            else throw new ArgumentNullException(nameof(expression));
        }
        void markUsed(ShaderVariable variable)
        {
            if (usedVariables.Add(variable)) if (dependencies.TryGetValue(variable, out HashSet<ShaderVariable> variableDependencies))
                    foreach (var variableDependency in variableDependencies) markUsed(variableDependency);
        }
        void checkCanReceiveCommands()
        {
            if (!canReceiveCommands) throw new InvalidOperationException($"Cannot receive commands outside of {nameof(MarkUsedVariables)} or {nameof(GenerateCode)}");
        }
    }
}