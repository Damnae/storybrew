using StorybrewCommon.Storyboarding;

namespace StorybrewCommon.Storyboarding3d
{
    public abstract class OsbScene3d : StoryboardObject
    {
        public abstract StoryboardCamera Camera { get; }
        public abstract OsbContainer3d RootContainer { get; }
    }
}
