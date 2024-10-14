namespace StorybrewCommon.Storyboarding
{
    public class OsbAnimation : OsbSprite
    {
        public int FrameCount;
        public double FrameDelay;
        public OsbLoopType LoopType;
        public double LoopDuration => FrameCount * FrameDelay;
        public double AnimationEndTime => (LoopType == OsbLoopType.LoopOnce) ? StartTime + LoopDuration : EndTime;

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
    }
}
