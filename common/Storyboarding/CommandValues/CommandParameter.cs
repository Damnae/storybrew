using System;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    ///<summary> Base struct for parameter commands. </summary>
    [Serializable]
    public struct CommandParameter : CommandValue
    {
#pragma warning disable CS1591
        public static readonly CommandParameter None = new CommandParameter(ParameterType.None);
        public static readonly CommandParameter FlipHorizontal = new CommandParameter(ParameterType.FlipHorizontal);
        public static readonly CommandParameter FlipVertical = new CommandParameter(ParameterType.FlipVertical);
        public static readonly CommandParameter AdditiveBlending = new CommandParameter(ParameterType.AdditiveBlending);

        public readonly ParameterType Type;

        CommandParameter(ParameterType type) => Type = type;
        public string ToOsbString(ExportSettings exportSettings)
        {
            switch (Type)
            {
                case ParameterType.FlipHorizontal: return "H";
                case ParameterType.FlipVertical: return "V";
                case ParameterType.AdditiveBlending: return "A";
                default: throw new InvalidOperationException($"Parameter command cannot be None.");
            }
        }
        public override string ToString() => ToOsbString(ExportSettings.Default);

        public float DistanceFrom(object obj)
        {
            var other = (CommandParameter)obj;
            return other.Type != Type ? 1 : 0;
        }
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is CommandParameter parameter && Equals(parameter);
        }
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(CommandParameter left, CommandParameter right) => left.Equals(right);
        public static bool operator !=(CommandParameter left, CommandParameter right) => !left.Equals(right);
        public static implicit operator bool(CommandParameter obj) => obj.Type != ParameterType.None;
    }
}