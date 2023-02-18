using System;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> A type of <see cref="OsbSprite"/> that loops through given frames, or animates. </summary>
    public class OsbAnimation : OsbSprite
    {
        ///<summary> Amount of frames in the animation. </summary>
        public int FrameCount;

        ///<summary> Delay between frames in the animation. </summary>
        public double FrameDelay;

        ///<summary> The <see cref="OsbLoopType"/> of this animation. </summary>
        public OsbLoopType LoopType;

        ///<summary> How long the animation takes to loop through its frames once. </summary>
        public double LoopDuration => FrameCount * FrameDelay;

        ///<summary> The time of when the animation stops looping. </summary>
        public double AnimationEndTime => (LoopType == OsbLoopType.LoopOnce) ? StartTime + LoopDuration : EndTime;

        ///<summary> Gets the path of the frame at <paramref name="time"/>. </summary>
        public override string GetTexturePathAt(double time)
        {
            var dotIndex = TexturePath.LastIndexOf('.');
            if (dotIndex < 0) return TexturePath + GetFrameAt(time);

            return TexturePath.Substring(0, dotIndex) + GetFrameAt(time) + TexturePath.Substring(dotIndex, TexturePath.Length - dotIndex);
        }

        ///<summary> Gets the frame number at <paramref name="time"/>. </summary>
        public int GetFrameAt(double time)
        {
            var frame = (time - CommandsStartTime) / FrameDelay;
            switch (LoopType)
            {
                case OsbLoopType.LoopForever: frame %= FrameCount; break;
                case OsbLoopType.LoopOnce: frame = Math.Min(frame, FrameCount - 1); break;
            }
            return Math.Max(0, (int)frame);
        }
    }
}