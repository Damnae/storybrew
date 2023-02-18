using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;
using System.IO;

namespace StorybrewCommon.Storyboarding
{
#pragma warning disable CS1591
    public class OsbWriterFactory
    {
        public static OsbSpriteWriter CreateWriter(OsbSprite sprite,
            AnimatedValue<CommandPosition> move, AnimatedValue<CommandDecimal> moveX, AnimatedValue<CommandDecimal> moveY,
            AnimatedValue<CommandDecimal> scale, AnimatedValue<CommandScale> scaleVec,
            AnimatedValue<CommandDecimal> rotate,
            AnimatedValue<CommandDecimal> fade, AnimatedValue<CommandColor> color,
            TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            if (sprite is OsbAnimation animation) return new OsbAnimationWriter(animation,
                move, moveX, moveY,
                scale, scaleVec,
                rotate,
                fade,
                color,
                writer, exportSettings, layer);

            else return new OsbSpriteWriter(sprite,
                move, moveX, moveY,
                scale, scaleVec,
                rotate,
                fade, color,
                writer, exportSettings, layer);
        }
    }
}