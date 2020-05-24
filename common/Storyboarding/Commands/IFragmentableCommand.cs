using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorybrewCommon.Storyboarding.Commands
{
    public interface IFragmentableCommand : ICommand
    {
        bool IsFragmentable { get; }
        IFragmentableCommand GetFragment(double startTime, double endTime);
    }
}
