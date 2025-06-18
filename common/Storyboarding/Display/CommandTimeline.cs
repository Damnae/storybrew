using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Display
{
    public interface CommandTimeline
    {
        bool HasCommands { get; }
        bool HasOverlap { get; }

        void Add(ICommand command);
        void StartGroup(LoopCommand loop);
        void StartGroup(TriggerCommand trigger);
        void EndGroup();
    }

    public class CommandTimeline<TValue> : CommandTimeline
        where TValue : struct, CommandValue
    {
        public TValue DefaultValue;
        public bool HasCommands => channels.Count > 0;
        public bool HasOverlap => channels.Any(c => c.HasOverlap);

        private readonly List<CommandChannel<TValue>> channels = [];

        private CommandChannel<TValue> defaultChannel;
        private CommandChannel<TValue> currentChannel;
        private Action groupEndAction;

        public CommandTimeline()
        {
        }
        public CommandTimeline(TValue defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public void Add(ICommand command)
            => Add(command as Command<TValue>);

        public void Add(ITypedCommand<TValue> command)
        {
            if (command == null)
                return;

            if (currentChannel == null)
                channels.Add(currentChannel = defaultChannel = new CommandChannel<TValue>());

            currentChannel.Add(command);
        }

        public void StartGroup(LoopCommand loop)
        {
            if (groupEndAction != null)
                EndGroup();

            var loopChannel = new CommandChannelLoop<TValue>();
            currentChannel = loopChannel;
            groupEndAction = () =>
            {
                loopChannel.LoopCount = loop.LoopCount;
                loopChannel.LoopStartTime = loop.StartTime;
                loopChannel.LoopDuration = loop.CommandsDuration;
            };
        }

        public void StartGroup(TriggerCommand trigger)
        {
            if (groupEndAction != null)
                EndGroup();

            var triggerChannel = new CommandChannelTrigger<TValue>();
            currentChannel = triggerChannel;
            groupEndAction = () =>
            {
                triggerChannel.ListenStartTime = trigger.StartTime;
                triggerChannel.ListenEndTime = trigger.EndTime;
            };
        }

        public void EndGroup()
        {
            if (groupEndAction == null)
                return;

            if (currentChannel.Commands.Count > 0)
            {
                groupEndAction();
                channels.Add(currentChannel);
            }
            currentChannel = defaultChannel;
            groupEndAction = null;
        }

        public TValue ValueAtTime(double time)
        {
            var currentState = ResultState.NoCommand;
            var currentResult = default(CommandResult<TValue>);

            foreach (var channel in channels)
            {
                if (!channel.ResultAtTime(time, out var channelResult))
                    continue;

                var channelState = ResultState.CommandInPresent;
                if (time < channelResult.StartTime)
                    channelState = ResultState.CommandInFuture;
                else if (channelResult.EndTime < time)
                    channelState = ResultState.CommandInPast;

                switch (currentState)
                {
                    case ResultState.NoCommand:
                        currentResult = channelResult;
                        currentState = channelState;
                        break;

                    case ResultState.CommandInPresent:
                        // Only a present command starting earlier can take over (the state doesn't change)
                        if (channelState == ResultState.CommandInPresent && channelResult.IsBefore(currentResult))
                            currentResult = channelResult;
                        break;

                    case ResultState.CommandInFuture:
                        // Any previous or current command can take over; a future command that starts earlier can also take over
                        if (channelState != ResultState.CommandInFuture || channelState == ResultState.CommandInFuture && channelResult.IsBefore(currentResult))
                        {
                            currentResult = channelResult;
                            currentState = channelState;
                        }
                        break;

                    case ResultState.CommandInPast:
                        // An active command or a command ending later can take over
                        if (channelState == ResultState.CommandInPresent || channelState == ResultState.CommandInPast && currentResult.IsBefore(channelResult))
                        {
                            currentResult = channelResult;
                            currentState = channelState;
                        }
                        break;
                }
            }

            return currentState switch
            {
                ResultState.NoCommand => DefaultValue,
                _ => currentResult.ValueAtTime(time),
            };
        }

        public bool FindStartEdge(Func<TValue, bool> isZero, Func<TValue, TValue, bool> isNoOp, out double startEdge)
        {
            startEdge = double.MaxValue;
            foreach (var result in channels.SelectMany(c => c.CommandResults).OrderBy(c => c.StartTime))
            {
                if (isNoOp(result.StartValue, result.EndValue))
                    continue;

                if (isZero(result.StartValue))
                {
                    startEdge = double.Min(startEdge, result.StartTime);
                    return true;
                }
                break;
            }
            return false;
        }

        public bool FindEndEdge(Func<TValue, bool> isZero, Func<TValue, TValue, bool> isNoOp, out double endEdge)
        {
            endEdge = double.MinValue;
            foreach (var result in channels.SelectMany(c => c.CommandResults).OrderByDescending(c => c.StartTime))
            {
                if (endEdge == double.MinValue)
                {
                    if (isNoOp(result.StartValue, result.EndValue))
                        continue;

                    if (isZero(result.EndValue))
                        endEdge = double.Max(endEdge, result.EndTime);
                    else return false;
                }
                else
                {
                    // While we've found an end edge, an ealier overlapping command may delay it
                    endEdge = double.Max(endEdge, result.EndTime);
                }
            }
            return endEdge != double.MinValue;
        }

        private enum ResultState
        {
            NoCommand,
            CommandInPresent,
            CommandInFuture,
            CommandInPast,
        }
    }
}
