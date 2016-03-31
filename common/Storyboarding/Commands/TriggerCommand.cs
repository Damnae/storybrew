using System.Globalization;

namespace StorybrewCommon.Storyboarding.Commands
{
    public class TriggerCommand : CommandGroup
    {
        public string TriggerName { get; set; }
        public int Group { get; set; }

        public TriggerCommand(string triggerName, double startTime, double endTime, int group = 0)
        {
            TriggerName = triggerName;
            StartTime = startTime;
            EndTime = endTime;
            Group = group;
        }

        protected override string GetCommandGroupHeader(ExportSettings exportSettings)
            => $"T,{TriggerName},{((int)StartTime).ToString(CultureInfo.InvariantCulture)},{((int)EndTime).ToString(CultureInfo.InvariantCulture)},{Group.ToString(CultureInfo.InvariantCulture)}";
    }
}
