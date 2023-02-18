using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Commands
{
#pragma warning disable CS1591
    public interface IFragmentableCommand : ICommand
    {
        bool IsFragmentable { get; }
        IFragmentableCommand GetFragment(double startTime, double endTime);
        IEnumerable<int> GetNonFragmentableTimes();
    }
}
