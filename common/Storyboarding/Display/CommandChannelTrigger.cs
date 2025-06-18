using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Display
{
    public class CommandChannelTrigger<TValue> : CommandChannel<TValue>
        where TValue : struct, CommandValue
    {
        public double ListenStartTime = 0;
        public double ListenEndTime = 0;

        public bool Active = false;
        public double TriggerTime = 0;

        public override IEnumerable<CommandResult<TValue>> CommandResults
        {
            get
            {
                if (!Active)
                    yield break;

                foreach (var command in Commands)
                    yield return command.AsResult(TriggerTime);
            }
        }

        public override bool ResultAtTime(double time, out CommandResult<TValue> result)
        {
            if (!Active)
            {
                result = default;
                return false;
            }

            result = CommandAtTime(time - TriggerTime).AsResult(TriggerTime);
            return true;
        }
    }
}
