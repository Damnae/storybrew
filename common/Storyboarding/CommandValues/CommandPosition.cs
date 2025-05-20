using OpenTK;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    [Serializable]
    public readonly struct CommandPosition : CommandValue, IEquatable<CommandPosition>
    {
        private readonly CommandDecimal x;
        private readonly CommandDecimal y;

        public CommandDecimal X => x;
        public CommandDecimal Y => y;

        public CommandPosition(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public CommandPosition(Vector2 vector)
            : this(vector.X, vector.Y)
        {
        }

        public bool Equals(CommandPosition other)
            => x.Equals(other.x) && y.Equals(other.y);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is CommandPosition && Equals((CommandPosition)obj);
        }

        public override int GetHashCode()
            => (x.GetHashCode() * 397) ^ y.GetHashCode();

        public string ToOsbString(ExportSettings exportSettings)
            => exportSettings.UseFloatForMove ?
                $"{X.ToOsbString(exportSettings)},{Y.ToOsbString(exportSettings)}" :
                $"{(int)Math.Round(X)},{(int)Math.Round(Y)}";

        public override string ToString() => ToOsbString(ExportSettings.Default);

        public float DistanceFrom(object obj)
            => Distance(this, (CommandPosition)obj);

        public static float Distance(CommandPosition a, CommandPosition b)
        {
            var diffX = a.X - b.X;
            var diffY = a.Y - b.Y;
            return (float)Math.Sqrt((diffX * diffX) + (diffY * diffY));
        }

        public static CommandPosition operator +(CommandPosition left, CommandPosition right)
            => new CommandPosition(left.X + right.X, left.Y + right.Y);

        public static CommandPosition operator -(CommandPosition left, CommandPosition right)
            => new CommandPosition(left.X - right.X, left.Y - right.Y);

        public static CommandPosition operator *(CommandPosition left, CommandPosition right)
            => new CommandPosition(left.X * right.X, left.Y * right.Y);

        public static CommandPosition operator *(CommandPosition left, double right)
            => new CommandPosition(left.X * right, left.Y * right);

        public static CommandPosition operator *(double left, CommandPosition right)
            => right * left;

        public static CommandPosition operator /(CommandPosition left, double right)
            => new CommandPosition(left.X / right, left.Y / right);

        public static bool operator ==(CommandPosition left, CommandPosition right)
            => left.Equals(right);

        public static bool operator !=(CommandPosition left, CommandPosition right)
            => !left.Equals(right);

        public static implicit operator Vector2(CommandPosition position)
            => new Vector2(position.X, position.Y);

        public static implicit operator CommandPosition(Vector2 vector)
            => new CommandPosition(vector.X, vector.Y);
    }
}