using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;

namespace StorybrewCommon.Storyboarding.Commands
{
    public interface ITypedCommand<TValue> : ICommand
        where TValue : struct, CommandValue
    {
        OsbEasing Easing { get; }
        TValue StartValue { get; }
        TValue EndValue { get; }
        double Duration { get; }

        CommandResult<TValue> AsResult(double timeOffset = 0);
        TValue ValueAtTime(double time);
    }
}