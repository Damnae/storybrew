using System.Globalization;

namespace StorybrewCommon.Storyboarding
{
    public class ExportSettings
    {
        public static ExportSettings Default = new ExportSettings();

        /// <summary>
        /// Not compatible with Fallback!
        /// </summary>
        public bool UseFloatForMove = true;

        public readonly NumberFormatInfo NumberFormat = new CultureInfo(@"en-US", false).NumberFormat;

        /// <summary>
        /// Enables optimisation for OsbSprites that have a MaxCommandCount > 0
        /// </summary>
        public bool OptimiseSprites = true;
    }
}
