using System;
using System.Globalization;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    [Serializable]
    public struct CommandDecimal : CommandValue, IEquatable<CommandDecimal>
    {
        private readonly double value;

        public CommandDecimal(double value)
        {
            this.value = value;
        }

        public bool Equals(CommandDecimal other)
            => value.Equals(other.value);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is CommandDecimal && Equals((CommandDecimal)obj);
        }

        public override int GetHashCode()
            => value.GetHashCode();

        public override string ToString() => ToOsbString(ExportSettings.Default);

        public float DistanceFrom(object obj)
            => (float)Math.Abs(value - ((CommandDecimal)obj).value);

        public string ToOsbString(ExportSettings exportSettings)
            => ((float)value).ToString(exportSettings.NumberFormat);

        public static CommandDecimal operator -(CommandDecimal left, CommandDecimal right)
            => new CommandDecimal(left.value - right.value);

        public static CommandDecimal operator +(CommandDecimal left, CommandDecimal right)
            => new CommandDecimal(left.value + right.value);

        public static bool operator ==(CommandDecimal left, CommandDecimal right)
            => left.Equals(right);

        public static bool operator !=(CommandDecimal left, CommandDecimal right)
            => !left.Equals(right);

        public static implicit operator CommandDecimal(double value)
            => new CommandDecimal(value);

        public static implicit operator double(CommandDecimal obj)
            => obj.value;

        public static implicit operator float(CommandDecimal obj)
            => (float)obj.value;
    }
}