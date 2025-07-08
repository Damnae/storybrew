using System.Globalization;

namespace StorybrewCommon.Storyboarding
{
    public class ExportSettings
    {
        public static readonly ExportSettings Default = new();
        public static readonly ExportSettings SizeCalculation = new() { OptimiseSprites = false, };

        /// <summary>
        /// Not compatible with Fallback!
        /// </summary>
        public bool UseFloatForMove = true;

        /// <summary>
        /// Not compatible with Stable!
        /// </summary>
        public bool UseFloatForTime = false;

        public bool OptimiseSprites = true;

        public readonly NumberFormatInfo NumberFormat = new CultureInfo(@"en-US", false).NumberFormat;

        public ExportSettings Clone() => new()
        {
            UseFloatForMove = UseFloatForMove,
            UseFloatForTime = UseFloatForTime,
            OptimiseSprites = OptimiseSprites,
        };
    }
}
