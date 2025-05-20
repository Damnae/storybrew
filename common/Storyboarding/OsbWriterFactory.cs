using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;

namespace StorybrewCommon.Storyboarding
{
    public class OsbWriterFactory
    {
        public static OsbSpriteWriter CreateWriter(OsbSprite osbSprite, CommandTimeline<CommandPosition> moveTimeline,
                                                                        CommandTimeline<CommandDecimal> moveXTimeline,
                                                                        CommandTimeline<CommandDecimal> moveYTimeline,
                                                                        CommandTimeline<CommandDecimal> scaleTimeline,
                                                                        CommandTimeline<CommandScale> scaleVecTimeline,
                                                                        CommandTimeline<CommandDecimal> rotateTimeline,
                                                                        CommandTimeline<CommandDecimal> fadeTimeline,
                                                                        CommandTimeline<CommandColor> colorTimeline,
                                                                        TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            if (osbSprite is OsbAnimation osbAnimation)
            {
                return new OsbAnimationWriter(osbAnimation, moveTimeline,
                                                            moveXTimeline,
                                                            moveYTimeline,
                                                            scaleTimeline,
                                                            scaleVecTimeline,
                                                            rotateTimeline,
                                                            fadeTimeline,
                                                            colorTimeline,
                                                            writer, exportSettings, layer);
            }
            else
            {
                return new OsbSpriteWriter(osbSprite, moveTimeline,
                                                      moveXTimeline,
                                                      moveYTimeline,
                                                      scaleTimeline,
                                                      scaleVecTimeline,
                                                      rotateTimeline,
                                                      fadeTimeline,
                                                      colorTimeline,
                                                      writer, exportSettings, layer);
            }
        }
    }
}
