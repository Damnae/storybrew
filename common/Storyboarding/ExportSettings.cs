using System.Globalization;

namespace StorybrewCommon.Storyboarding
{
    public class ExportSettings
    {
        public static readonly ExportSettings Default = new ExportSettings();

        /// <summary>
        /// Not compatible with Fallback!
        /// </summary>
        public bool UseFloatForMove = true;
        
        /// <summary>
        /// Not compatible with Stable!
        /// </summary>
        public bool UseFloatForTime = false;

        /// <summary>
        /// Enables optimisation for OsbSprites that have a MaxCommandCount > 0
        /// </summary>
        public bool OptimiseSprites = true;

        public readonly NumberFormatInfo NumberFormat = new CultureInfo(@"en-US", false).NumberFormat;
    }
}
