using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using System;

namespace StorybrewCommon.Storyboarding.Display
{
#pragma warning disable CS1591
    public class AnimatedValueBuilder<TValue> : IAnimatedValueBuilder where TValue : CommandValue
    {
        readonly AnimatedValue<TValue> value;
        CompositeCommand<TValue> composite;
        Func<ITypedCommand<TValue>, ITypedCommand<TValue>> decorate;

        public AnimatedValueBuilder(AnimatedValue<TValue> value) => this.value = value;

        public void Add(ICommand command) => Add(command as Command<TValue>);
        public void Add(Command<TValue> command)
        {
            if (command == null) return;
            (composite ?? value).Add(command);
        }
        public void StartDisplayLoop(LoopCommand loop)
        {
            if (composite != null) throw new InvalidOperationException("Cannot start loop: already inside a loop or trigger");

            decorate = command =>
            {
                if (loop.CommandsStartTime != 0) throw new InvalidOperationException($"Commands in a loop must start at 0ms, but start at {loop.CommandsStartTime}ms");
                return new LoopDecorator<TValue>(command, loop.StartTime, loop.CommandsDuration, loop.LoopCount);
            };
            composite = new CompositeCommand<TValue>();
        }
        public void StartDisplayTrigger(TriggerCommand triggerCommand)
        {
            if (composite != null) throw new InvalidOperationException("Cannot start trigger: already inside a loop or trigger");

            decorate = (command) => new TriggerDecorator<TValue>(command);
            composite = new CompositeCommand<TValue>();
        }
        public void EndDisplayComposite()
        {
            if (composite == null) throw new InvalidOperationException("Cannot complete loop or trigger: Not inside one");
            if (composite.HasCommands) value.Add(decorate(composite));

            composite = null;
            decorate = null;
        }
    }
}