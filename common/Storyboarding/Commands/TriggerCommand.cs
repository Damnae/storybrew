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

        public override bool IsFragmentableAt(double time) => false;

        protected override string GetCommandGroupHeader(ExportSettings exportSettings)
            => $"T,{TriggerName},{((int)StartTime).ToString(exportSettings.NumberFormat)},{((int)EndTime).ToString(exportSettings.NumberFormat)},{Group.ToString(exportSettings.NumberFormat)}";
    }
}
