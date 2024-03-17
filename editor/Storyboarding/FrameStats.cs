using System.Collections.Generic;

namespace StorybrewEditor.Storyboarding
{
    public class FrameStats
    {
        public int SpriteCount;
        public int CommandCount;
        public int EffectiveCommandCount;
        public bool IncompatibleCommands;
        public bool OverlappedCommands;
        public float ScreenFill;

        public double GpuMemoryFrameMb => GpuPixelsFrame / 1024.0 / 1024.0;
        public ulong GpuPixelsFrame;
        public HashSet<string> LoadedPaths = new HashSet<string>();
    }
}
