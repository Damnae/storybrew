using System;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    [Serializable]
    public struct CommandParameter : CommandValue
    {
        public static readonly CommandParameter None = new CommandParameter(ParameterType.None);
        public static readonly CommandParameter FlipHorizontal = new CommandParameter(ParameterType.FlipHorizontal);
        public static readonly CommandParameter FlipVertical = new CommandParameter(ParameterType.FlipVertical);
        public static readonly CommandParameter AdditiveBlending = new CommandParameter(ParameterType.AdditiveBlending);

        public readonly ParameterType Type;

        private CommandParameter(ParameterType type)
        {
            Type = type;
        }

        public string ToOsbString(ExportSettings exportSettings)
        {
            switch (Type)
            {
                case ParameterType.FlipHorizontal: return "H";
                case ParameterType.FlipVertical: return "V";
                case ParameterType.AdditiveBlending: return "A";
                default: throw new InvalidOperationException(Type.ToString());
            }
        }

        public override string ToString() => ToOsbString(ExportSettings.Default);

        public float DistanceFrom(object obj)
        {
            var other = (CommandParameter)obj;
            return other.Type != Type ? 1 : 0;
        }

        public static bool operator ==(CommandParameter left, CommandParameter right)
            => left.Type == right.Type;

        public static bool operator !=(CommandParameter left, CommandParameter right)
            => left.Type != right.Type;

        public static implicit operator bool(CommandParameter obj)
            => obj.Type != ParameterType.None;
    }
}
