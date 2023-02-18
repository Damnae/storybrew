using System.Globalization;

namespace StorybrewCommon.Storyboarding
{
#pragma warning disable CS1591
    public class ExportSettings
    {
        public static readonly ExportSettings Default = new ExportSettings();

        public bool UseFloatForMove = true;
        public bool UseFloatForTime = false;

        ///<summary> Enables optimisation for OsbSprites that have a MaxCommandCount > 0 </summary>
        public bool OptimiseSprites = true;

        public readonly NumberFormatInfo NumberFormat = new CultureInfo(@"en-US", false).NumberFormat;
    }
}