using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.IO;

namespace StorybrewCommon.Storyboarding.Display
{
    public class CompositeCommand<TValue> : AnimatedValue<TValue>, ITypedCommand<TValue>
        where TValue : CommandValue
    {
        public OsbEasing Easing { get { throw new InvalidOperationException(); } }
        public bool Enabled => true;

        public void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => $"composite ({StartTime}s - {EndTime}s) : {StartValue} to {EndValue}";
    }
}