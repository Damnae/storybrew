using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Display
{
    public class CommandChannelLoop<TValue> : CommandChannel<TValue>
        where TValue : struct, CommandValue
    {
        public int LoopCount = 1;
        public double LoopStartTime = 0;
        public double LoopDuration = 0;

        public override bool ResultAtTime(double time, out CommandResult<TValue> result)
        {
            if (Commands.Count == 0)
            {
                result = default;
                return false;
            }
            
            if (time < LoopStartTime)
            {
                result = StartCommand.AsResult(LoopStartTime);
                return true;
            }

            var loopTime = time - LoopStartTime;
            if (loopTime >= LoopCount * LoopDuration)
            {
                result = EndCommand.AsResult(LoopStartTime + (LoopCount - 1) * LoopDuration);
                return true;
            }

            if (loopTime < LoopDuration)
            {
                // First iteration
                result = CommandAtTime(loopTime).AsResult(LoopStartTime);
                return true;
            }

            var loopNumber = (int)(loopTime / LoopDuration);
            loopTime %= LoopDuration;

            if (loopTime < StartCommand.StartTime)
            {
                // Before the first command in the loop, the last command from the previous loop takes effect
                result = EndCommand.AsResult(LoopStartTime + (loopNumber - 1) * LoopDuration);
                return true;
            }

            result = CommandAtTime(loopTime).AsResult(LoopStartTime + loopNumber * LoopDuration);
            return true;
        }
    }
}
