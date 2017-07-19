#if DEBUG
using StorybrewCommon.Storyboarding;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding3d
{
    public interface HasOsbSprites
    {
        IEnumerable<OsbSprite> Sprites { get; }
    }
}
#endif