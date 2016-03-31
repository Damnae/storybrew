﻿using OpenTK;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;

namespace StorybrewEditor.Storyboarding
{
    public interface DisplayableObject
    {
        OsbLayer Layer { get; set; }
        void Draw(DrawContext drawContext, Camera camera, Box2 bounds, float opacity, Project project);
    }
}
