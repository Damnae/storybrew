using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;
using System.IO;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    [Serializable]
    public struct CommandColor : CommandValue, IEquatable<CommandColor>
    {
        public static readonly CommandColor White = new CommandColor(1, 1, 1);

        private readonly double r;
        private readonly double g;
        private readonly double b;

        public byte R => toByte(r);
        public byte G => toByte(g);
        public byte B => toByte(b);

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

        public CommandColor(Vector3 vector)
            : this(vector.X, vector.Y, vector.Z)
        {
        }

        public float DistanceFrom(object obj)
        {
            CommandColor other = (CommandColor)obj;
            float diffR = R - other.R;
            float diffG = G - other.G;
            float diffB = B - other.B;
            return (float)Math.Sqrt((diffR * diffR) + (diffG * diffG) + (diffB * diffB));
        }

        public bool Equals(CommandColor other)
            => r == other.r && g == other.g && b == other.b;

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other))
                return false;

            return other is CommandColor && Equals((CommandColor)other);
        }

        public override int GetHashCode()
        {
            var hashCode = r.GetHashCode();
            hashCode = (hashCode * 397) ^ g.GetHashCode();
            hashCode = (hashCode * 397) ^ b.GetHashCode();
            return hashCode;
        }

        public string ToOsbString(ExportSettings exportSettings)
            => $"{R},{G},{B}";

        public override string ToString() => ToOsbString(ExportSettings.Default);

        public static bool operator ==(CommandColor left, CommandColor right)
            => left.Equals(right);

        public static bool operator !=(CommandColor left, CommandColor right)
            => !left.Equals(right);

        public static CommandColor operator +(CommandColor left, CommandColor right)
            => new CommandColor(left.r + right.r, left.g + right.g, left.b + right.b);

        public static CommandColor operator -(CommandColor left, CommandColor right)
            => new CommandColor(left.r - right.r, left.g - right.g, left.b - right.b);

        public static CommandColor operator *(CommandColor left, CommandColor right)
            => new CommandColor(left.r * right.r, left.g * right.g, left.b * right.b);

        public static CommandColor operator *(CommandColor left, double right)
            => new CommandColor(left.r * right, left.g * right, left.b * right);

        public static CommandColor operator *(double left, CommandColor right)
            => right * left;

        public static CommandColor operator /(CommandColor left, double right)
            => new CommandColor(left.r / right, left.g / right, left.b / right);

        public static implicit operator Color4(CommandColor obj)
            => new Color4(obj.R, obj.G, obj.B, 255);

        public static implicit operator CommandColor(Color4 obj)
            => new CommandColor(obj.R, obj.G, obj.B);

        public static implicit operator CommandColor(Color obj)
            => new CommandColor(obj.R / 255.0, obj.G / 255.0, obj.B / 255.0);

        public static implicit operator CommandColor(string hexCode)
            => FromHtml(hexCode);

        public static CommandColor FromRgb(byte r, byte g, byte b)
            => new CommandColor(r / 255.0, g / 255.0, b / 255.0);

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

        public static CommandColor FromHtml(string htmlColor)
        {
            if (!htmlColor.StartsWith("#"))
                htmlColor = "#" + htmlColor;

            var color = ColorTranslator.FromHtml(htmlColor);
            return new CommandColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        }

        private static byte toByte(double x)
        {
            x *= 255;
            if (x > 255) return 255;
            if (x < 0) return 0;
            return (byte)x;
        }
    }
}