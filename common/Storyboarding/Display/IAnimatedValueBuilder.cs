using StorybrewCommon.Storyboarding.Commands;

namespace StorybrewCommon.Storyboarding.Display
{
#pragma warning disable CS1591
    public interface IAnimatedValueBuilder
    {
        void Add(ICommand command);
        void StartDisplayLoop(LoopCommand loopCommand);
        void StartDisplayTrigger(TriggerCommand triggerCommand);
        void EndDisplayComposite();
    }
}