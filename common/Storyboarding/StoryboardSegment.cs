using OpenTK;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Storyboarding segments for storyboard objects. </summary>
    public abstract class StoryboardSegment : StoryboardObject
    {
        ///<summary> Reverses the order of sprites, from newer sprites being placed at the bottom of the list. </summary>
        public abstract bool ReverseDepth { get; set; }

        ///<summary> Creates a new storyboard segment. </summary>
        public abstract StoryboardSegment CreateSegment();

        ///<summary> Creates an <see cref="OsbSprite"/>. </summary>
        ///<param name="path"> Path to the image of this sprite. </param>
        ///<param name="origin"> <see cref="OsbOrigin"/> of this sprite. </param>
        ///<param name="initialPosition"> The initial <see cref="Vector2"/> value of this sprite. </param>
        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition);

        ///<summary> Creates an <see cref="OsbSprite"/>. </summary>
        ///<param name="path"> Path to the image of this sprite. </param>
        ///<param name="origin"> <see cref="OsbOrigin"/> of this sprite. </param>
        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin = OsbOrigin.Centre);

        ///<summary> Creates an <see cref="OsbAnimation"/>. </summary>
        ///<param name="path"> Path to the image of this animation. </param>
        ///<param name="frameCount"> Amount of frames to loop through in this animation. </param>
        ///<param name="frameDelay"> Delay between frames in this animation. </param>
        ///<param name="loopType"> <see cref="OsbLoopType"/> of this animation. </param>
        ///<param name="origin"> <see cref="OsbOrigin"/> of this animation. </param>
        ///<param name="initialPosition"> The initial <see cref="Vector2"/> value of this animation. </param>
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);

        ///<summary> Creates an <see cref="OsbAnimation"/>. </summary>
        ///<param name="path"> Path to the image of this animation. </param>
        ///<param name="frameCount"> Amount of frames to loop through in this animation. </param>
        ///<param name="frameDelay"> Delay between frames in this animation. </param>
        ///<param name="loopType"> <see cref="OsbLoopType"/> of this animation. </param>
        ///<param name="origin"> <see cref="OsbOrigin"/> of this animation. </param>
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre);

        ///<summary> Creates an <see cref="OsbSample"/>. </summary>
        ///<param name="path"> Path to the audio file of this sample. </param>
        ///<param name="time"> Time for the audio to be played. </param>
        ///<param name="volume"> Volume of the audio sample. </param>
        public abstract OsbSample CreateSample(string path, double time, double volume = 100);

        ///<summary> Removes a storyboard object from the segment. </summary>
        ///<param name="storyboardObject"> The storyboard object to be discarded. </param>
        public abstract void Discard(StoryboardObject storyboardObject);
    }
}