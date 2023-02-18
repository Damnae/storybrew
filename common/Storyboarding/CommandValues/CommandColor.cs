using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;
using System.IO;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    ///<summary> Base struct for coloring commands. </summary>
    [Serializable]
    public struct CommandColor : CommandValue, IEquatable<CommandColor>
    {
#pragma warning disable CS1591
        public static readonly CommandColor Black = new CommandColor(0, 0, 0);
        public static readonly CommandColor White = new CommandColor(1, 1, 1);
        public static readonly CommandColor Red = new CommandColor(1, 0, 0);
        public static readonly CommandColor Green = new CommandColor(0, 1, 0);
        public static readonly CommandColor Blue = new CommandColor(0, 0, 1);

        readonly double r, g, b;

        ///<summary> Gets the red value of this instance. </summary>
        public byte R => toByte(r);

        ///<summary> Gets the green value of this instance. </summary>
        public byte G => toByte(g);

        ///<summary> Gets the blue value of this instance. </summary>
        public byte B => toByte(b);

        ///<summary> Constructs a new <see cref="CommandColor"/> from red, green, and blue values from 0.0 to 1.0. </summary>
        public CommandColor(double r, double g, double b)
        {
            if (double.IsNaN(r) || double.IsInfinity(r) ||
                double.IsNaN(g) || double.IsInfinity(g) ||
                double.IsNaN(b) || double.IsInfinity(b))
                throw new InvalidDataException($"Invalid command color {r},{g},{b}");

            this.r = r;
            this.g = g;
            this.b = b;
        }

        ///<summary> Constructs a new <see cref="CommandColor"/> from a <see cref="Vector3"/> containing red, green, and blue values from 0.0 to 1.0. </summary>
        public CommandColor(Vector3 vector) : this(vector.X, vector.Y, vector.Z) { }

        ///<summary> Returns the combined distance from each color value. </summary>
        public float DistanceFrom(object obj)
        {
            CommandColor other = (CommandColor)obj;
            float diffR = R - other.R;
            float diffG = G - other.G;
            float diffB = B - other.B;
            return (float)Math.Sqrt((diffR * diffR) + (diffG * diffG) + (diffB * diffB));
        }

        ///<summary> Returns whether or not this instance and <paramref name="other"/> are equal to each other. </summary>
        public bool Equals(CommandColor other) => r == other.r && g == other.g && b == other.b;

        ///<summary> Returns whether or not this instance and <paramref name="other"/> are equal to each other. </summary>
        public override bool Equals(object other)
        {
            if (other is null) return false;
            return other is CommandColor color && Equals(color);
        }

        ///<inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = r.GetHashCode();
            hashCode = (hashCode * 397) ^ g.GetHashCode();
            hashCode = (hashCode * 397) ^ b.GetHashCode();
            return hashCode;
        }

        ///<summary> Converts this instance into a .osb string, formatted as "R, G, B". </summary>
        public string ToOsbString(ExportSettings exportSettings) => $"{R},{G},{B}";

        ///<summary> Converts this instance into a string, formatted as "R, G, B". </summary>
        public override string ToString() => ToOsbString(ExportSettings.Default);

        ///<summary> Returns whether or not the left and right colors are equal to each other. </summary>
        public static bool operator ==(CommandColor left, CommandColor right) => left.Equals(right);

        ///<summary> Returns whether or not the left and right colors are unequal to each other. </summary>
        public static bool operator !=(CommandColor left, CommandColor right) => !left.Equals(right);

        ///<summary> Adds the color values of the left and right values together. </summary>
        public static CommandColor operator +(CommandColor left, CommandColor right) => new CommandColor(left.r + right.r, left.g + right.g, left.b + right.b);

        ///<summary> Subtracts the color values of the right value from the left value. </summary>
        public static CommandColor operator -(CommandColor left, CommandColor right) => new CommandColor(left.r - right.r, left.g - right.g, left.b - right.b);

        ///<summary> Multiplies the color values of the left and right values together. </summary>
        public static CommandColor operator *(CommandColor left, CommandColor right) => new CommandColor(left.r * right.r, left.g * right.g, left.b * right.b);

        ///<summary> Multiplies the right value together with each of the left color values. </summary>
        public static CommandColor operator *(CommandColor left, double right) => new CommandColor(left.r * right, left.g * right, left.b * right);

        ///<summary> Multiplies the left value together with each of the right color values. </summary>
        public static CommandColor operator *(double left, CommandColor right) => right * left;

        ///<summary> Divides each left value by the right value. </summary>
        public static CommandColor operator /(CommandColor left, double right) => new CommandColor(left.r / right, left.g / right, left.b / right);

        public static implicit operator Color4(CommandColor obj) => new Color4(obj.R, obj.G, obj.B, 255);
        public static implicit operator CommandColor(Color4 obj) => new CommandColor(obj.R, obj.G, obj.B);
        public static implicit operator CommandColor(Color obj) => new CommandColor(obj.R / 255.0, obj.G / 255.0, obj.B / 255.0);
        public static implicit operator CommandColor(string hexCode) => FromHtml(hexCode);

        ///<summary> Creates a <see cref="CommandColor"/> from RGB byte values. </summary>
        public static CommandColor FromRgb(byte r, byte g, byte b) => new CommandColor(r / 255.0, g / 255.0, b / 255.0);

        ///<summary> Creates a <see cref="CommandColor"/> from HSB values. <para>Hue: 0 - 180.0 | Saturation: 0 - 1.0 | Brightness: 0 - 1.0</para></summary>
        public static CommandColor FromHsb(double hue, double saturation, double brightness)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = (hue / 60) - Math.Floor(hue / 60);

            var v = brightness;
            var p = brightness * (1 - saturation);
            var q = brightness * (1 - (f * saturation));
            var t = brightness * (1 - ((1 - f) * saturation));

            if (hi == 0) return new CommandColor(v, t, p);
            if (hi == 1) return new CommandColor(q, v, p);
            if (hi == 2) return new CommandColor(p, v, t);
            if (hi == 3) return new CommandColor(p, q, v);
            if (hi == 4) return new CommandColor(t, p, v);
            return new CommandColor(v, p, q);
        }

        ///<summary> Creates a <see cref="CommandColor"/> from a hex-code color. </summary>
        public static CommandColor FromHtml(string htmlColor)
        {
            if (!htmlColor.StartsWith("#")) htmlColor = "#" + htmlColor;
            var color = ColorTranslator.FromHtml(htmlColor);
            return new CommandColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        }
        static byte toByte(double x)
        {
            x *= 255;
            if (x > 255) return 255;
            if (x < 0) return 0;
            return (byte)x;
        }
    }
}