using OpenTK;
using System;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    ///<summary> Base struct for movement commands. Alternative for <see cref="Vector2"/>. </summary>
    [Serializable]
    public struct CommandPosition : CommandValue, IEquatable<CommandPosition>
    {
        readonly CommandDecimal x, y;

#pragma warning disable CS1591

        ///<summary> Gets the X value of this instance. </summary>
        public CommandDecimal X => x;

        ///<summary> Gets the Y value of this instance. </summary>
        public CommandDecimal Y => y;

        ///<summary> Constructs a <see cref="CommandPosition"/> from X and Y values. </summary>
        public CommandPosition(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        ///<summary> Constructs a <see cref="CommandPosition"/> from a <see cref="Vector2"/>. </summary>
        public CommandPosition(Vector2 vector) : this(vector.X, vector.Y) {}

        ///<summary> Returns whether or not this instance and <paramref name="other"/> are equal to each other. </summary>
        public bool Equals(CommandPosition other) => x.Equals(other.x) && y.Equals(other.y);

        ///<inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is CommandPosition position && Equals(position);
        }

        ///<inheritdoc/>
        public override int GetHashCode() => (x.GetHashCode() * 397) ^ y.GetHashCode();

        ///<summary> Converts this instance to a .osb string. </summary>
        public string ToOsbString(ExportSettings exportSettings) => exportSettings.UseFloatForMove ? $"{X.ToOsbString(exportSettings)},{Y.ToOsbString(exportSettings)}" : $"{(int)Math.Round(X)},{(int)Math.Round(Y)}";
        
        ///<summary> Converts this instance to a string. </summary>
        public override string ToString() => ToOsbString(ExportSettings.Default);

        ///<summary> Returns the distance between this instance and point <paramref name="obj"/> on the Cartesian plane. </summary>
        public float DistanceFrom(object obj) => Distance(this, (CommandPosition)obj);

        ///<summary> Returns the distance between <paramref name="a"/> and <paramref name="b"/> on the Cartesian plane. </summary>
        public static float Distance(CommandPosition a, CommandPosition b)
        {
            var diffX = a.X - b.X;
            var diffY = a.Y - b.Y;
            return (float)Math.Sqrt((diffX * diffX) + (diffY * diffY));
        }

        public static CommandPosition operator +(CommandPosition left, CommandPosition right) => new CommandPosition(left.X + right.X, left.Y + right.Y);
        public static CommandPosition operator -(CommandPosition left, CommandPosition right) => new CommandPosition(left.X - right.X, left.Y - right.Y);
        public static CommandPosition operator *(CommandPosition left, CommandPosition right) => new CommandPosition(left.X * right.X, left.Y * right.Y);
        public static CommandPosition operator *(CommandPosition left, double right) => new CommandPosition(left.X * right, left.Y * right);
        public static CommandPosition operator *(double left, CommandPosition right) => right * left;
        public static CommandPosition operator /(CommandPosition left, double right) => new CommandPosition(left.X / right, left.Y / right);
        public static bool operator ==(CommandPosition left, CommandPosition right) => left.Equals(right);
        public static bool operator !=(CommandPosition left, CommandPosition right) => !left.Equals(right);
        public static implicit operator Vector2(CommandPosition position) => new Vector2(position.X, position.Y);
        public static implicit operator CommandPosition(Vector2 vector) => new CommandPosition(vector.X, vector.Y);
    }
}