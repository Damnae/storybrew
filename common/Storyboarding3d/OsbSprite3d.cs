using StorybrewCommon.Storyboarding;

namespace StorybrewCommon.Storyboarding3d
{
    public abstract class OsbSprite3d : StoryboardObject3d
    {
        public string TexturePath = "";
        public OsbOrigin Origin = OsbOrigin.Centre;

        public string GetTexturePathAt(double time)
            => TexturePath;
    }
}
