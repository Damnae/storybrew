using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Storyboarding.Display
{
    public class CompositeCommand<TValue> : AnimatedValue<TValue>, ITypedCommand<TValue>
        where TValue : CommandValue
    {
        public OsbEasing Easing { get { throw new InvalidOperationException(); } }
        public bool Enabled => true;

        public string ToOsbString(ExportSettings exportSettings)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => $"composite ({StartTime}s - {EndTime}s) : {StartValue} to {EndValue}";
    }
}