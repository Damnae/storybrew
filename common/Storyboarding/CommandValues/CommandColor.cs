using OpenTK;
using OpenTK.Graphics;
using System;
using System.Drawing;

namespace StorybrewCommon.Storyboarding.CommandValues
{
    [Serializable]
    public struct CommandColor : CommandValue, IEquatable<CommandColor>
    {
        private readonly double b;
        private readonly double g;
        private readonly double r;

        public int B => clamp((int)(b * 255));
        public int G => clamp((int)(g * 255));
        public int R => clamp((int)(r * 255));

        public CommandColor(double r, double g, double b)
        {
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
            => new Color4((float)obj.r, (float)obj.g, (float)obj.b, 1f);

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
            var color = ColorTranslator.FromHtml(htmlColor);
            return new CommandColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        }

        private static int clamp(int x)
        {
            if (x > 255) return 255;
            if (x < 0) return 0;
            return x;
        }
    }
}