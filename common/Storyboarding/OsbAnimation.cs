using System;
using System.IO;

namespace StorybrewCommon.Storyboarding
{
    public class OsbAnimation : OsbSprite
    {
        public int FrameCount;
        public double FrameDelay;
        public OsbLoopType LoopType;

        public override string GetTexturePathAt(double time)
        {
            var dotIndex = TexturePath.LastIndexOf('.');
            if (dotIndex < 0) return TexturePath + GetFrameAt(time);

            return TexturePath.Substring(0, dotIndex) + GetFrameAt(time) + TexturePath.Substring(dotIndex, TexturePath.Length - dotIndex);
        }

        public int GetFrameAt(double time)
        {
            var frame = (time - CommandsStartTime) / FrameDelay;
            switch (LoopType)
            {
                case OsbLoopType.LoopForever:
                    frame %= FrameCount;
                    break;
                case OsbLoopType.LoopOnce:
                    frame = Math.Min(frame, FrameCount - 1);
                    break;
            }
            return Math.Max(0, (int)frame);
        }

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            OsbAnimationWriter osbSpriteWriter = new OsbAnimationWriter(this, moveTimeline,
                                                                        moveXTimeline,
                                                                        moveYTimeline,
                                                                        scaleTimeline,
                                                                        scaleVecTimeline,
                                                                        rotateTimeline,
                                                                        fadeTimeline,
                                                                        colorTimeline,
                                                                        writer, exportSettings, layer);
            osbSpriteWriter.WriteOsb();
        }
    }
}
