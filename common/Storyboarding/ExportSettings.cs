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

        public readonly NumberFormatInfo NumberFormat = new CultureInfo(@"en-US", false).NumberFormat;
    }
}
