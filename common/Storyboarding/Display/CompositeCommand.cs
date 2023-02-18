using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;
using System.IO;

namespace StorybrewCommon.Storyboarding.Display
{
#pragma warning disable CS1591
    public class CompositeCommand<TValue> : AnimatedValue<TValue>, ITypedCommand<TValue> where TValue : CommandValue
    {
        public OsbEasing Easing { get { throw new InvalidOperationException(); } }
        public bool Active => true;

        public int Cost => throw new InvalidOperationException();
        public int CompareTo(ICommand other) => CommandComparer.CompareCommands(this, other);

        public void WriteOsb(TextWriter writer, ExportSettings exportSettings, int indentation) => throw new InvalidOperationException();
        public override string ToString() => $"composite ({StartTime}s - {EndTime}s) : {StartValue} to {EndValue}";
    }
}