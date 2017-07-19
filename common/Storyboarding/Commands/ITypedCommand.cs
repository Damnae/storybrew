
namespace StorybrewCommon.Storyboarding.Commands
{
    public interface ITypedCommand<TValue> : ICommand
    {
        OsbEasing Easing { get; }
        TValue StartValue { get; }
        TValue EndValue { get; }
        double Duration { get; }

        TValue ValueAtTime(double time);
    }
}