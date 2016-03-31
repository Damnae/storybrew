using System;

namespace StorybrewCommon.Storyboarding
{
    public abstract class GeneratorContext : MarshalByRefObject
    {
        public abstract StoryboardLayer GetLayer(string identifier);
    }
}
