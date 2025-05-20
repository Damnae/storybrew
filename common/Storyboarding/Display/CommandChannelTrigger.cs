using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Display
{
    public class CommandChannelTrigger<TValue> : CommandChannel<TValue>
        where TValue : struct, CommandValue
    {
        public bool Active = false;
        public double TriggerTime = 0;

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
