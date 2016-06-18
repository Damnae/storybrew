using StorybrewCommon.Storyboarding;

namespace StorybrewCommon.Storyboarding3d
{
    public abstract class OsbContainer3d : StoryboardObject3d
    {
        public abstract OsbContainer3d CreateContainer3d();
        public abstract OsbSprite3d CreateSprite3d(string path, OsbOrigin origin = OsbOrigin.Centre);
    }
}
