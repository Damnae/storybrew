﻿using OpenTK;
using StorybrewCommon.Storyboarding3d;
using System;

namespace StorybrewCommon.Storyboarding
{
    public abstract class StoryboardLayer : MarshalByRefObject
    {
        private string identifier;
        public string Identifier => identifier;

        public StoryboardLayer(string identifier)
        {
            this.identifier = identifier;
        }

        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbSprite CreateSprite(string path, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbAnimation CreateAnimation(string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);
        public abstract OsbAnimation CreateAnimation(string path, int frameCount, int frameDelay, OsbLoopType loopType, OsbOrigin origin = OsbOrigin.Centre);

        public abstract OsbVideo CreateVideo(string path, int starttime, Vector2 initialPosition);
        public abstract OsbVideo CreateVideo(string path, int starttime);

#if DEBUG
        public abstract OsbScene3d CreateScene3d();
#endif

        public abstract OsbSample CreateSample(string path, double time, double volume = 100);
    }
}
