using System.Collections.Generic;

namespace StorybrewEditor.Storyboarding
{
    public class FrameStats
    {
        public string LastTexture;
        public bool LastBlendingMode;
        public HashSet<string> LoadedPaths = new();

        public int SpriteCount;
        public int Batches;

        public int CommandCount;
        public int EffectiveCommandCount;
        public int ProlongedCommands;
        public bool IncompatibleCommands;
        public bool OverlappedCommands;

        public float ScreenFill;
        public ulong GpuPixelsFrame;
        public double GpuMemoryFrameMb => GpuPixelsFrame / 1024.0 / 1024.0 * 4.0;
    }
}
