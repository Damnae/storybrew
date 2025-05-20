using OpenTK;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    [Serializable]
    public readonly struct CommandScale : CommandValue, IEquatable<CommandScale>
    {
        public readonly static CommandScale One = new CommandScale(1, 1);

        private readonly CommandDecimal x;
        private readonly CommandDecimal y;

        public CommandDecimal X => x;
        public CommandDecimal Y => y;

        public CommandScale(CommandDecimal x, CommandDecimal y)
        {
            this.x = x;
            this.y = y;
        }

        public CommandScale(CommandDecimal value)
            : this(value, value)
        {
        }

        public CommandScale(Vector2 vector)
            : this(vector.X, vector.Y)
        {
        }

        public bool Equals(CommandScale other)
            => x.Equals(other.x) && y.Equals(other.y);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is CommandScale && Equals((CommandScale)obj);
        }

        public override int GetHashCode()
            => (x.GetHashCode() * 397) ^ y.GetHashCode();

        public string ToOsbString(ExportSettings exportSettings)
            => $"{X.ToOsbString(exportSettings)},{Y.ToOsbString(exportSettings)}";

        public override string ToString() => ToOsbString(ExportSettings.Default);

        public float DistanceFrom(object obj)
        {
            var other = (CommandScale)obj;
            var diffX = X.DistanceFrom(other.X);
            var diffY = Y.DistanceFrom(other.Y);
            return (float)Math.Sqrt((diffX * diffX) + (diffY * diffY));
        }

        public static CommandScale operator +(CommandScale left, CommandScale right)
            => new CommandScale(left.X + right.X, left.Y + right.Y);

        public static CommandScale operator -(CommandScale left, CommandScale right)
            => new CommandScale(left.X - right.X, left.Y - right.Y);

        public static CommandScale operator *(CommandScale left, CommandScale right)
            => new CommandScale(left.X * right.X, left.Y * right.Y);

        public static CommandScale operator *(CommandScale left, double right)
            => new CommandScale(left.X * right, left.Y * right);

        public static CommandScale operator *(double left, CommandScale right)
            => right * left;

        public static CommandScale operator /(CommandScale left, double right)
            => new CommandScale(left.X / right, left.Y / right);

        public static bool operator ==(CommandScale left, CommandScale right)
            => left.Equals(right);

        public static bool operator !=(CommandScale left, CommandScale right)
            => !left.Equals(right);

        public static implicit operator CommandScale(Vector2 vector)
            => new CommandScale(vector);

        public static implicit operator Vector2(CommandScale obj)
            => new Vector2(obj.x, obj.y);
    }
}