﻿using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;

namespace StorybrewCommon.Storyboarding.Display
{
    public class AnimatedValueBuilder<TValue> : IAnimatedValueBuilder
        where TValue : CommandValue
    {
        private readonly AnimatedValue<TValue> value;
        private CompositeCommand<TValue> composite;
        private Func<ITypedCommand<TValue>, ITypedCommand<TValue>> decorate;

        public AnimatedValueBuilder(AnimatedValue<TValue> value)
        {
            this.value = value;
        }

        public void Add(ICommand command)
            => Add(command as Command<TValue>);

        public void Add(Command<TValue> command)
        {
            if (command == null) return;
            (composite ?? value).Add(command);
        }

        public void StartDisplayLoop(LoopCommand loopCommand)
        {
            if (composite != null) throw new InvalidOperationException("Cannot start loop: already inside a loop or trigger");

            decorate = (command) =>
            {
                if (loopCommand.CommandsStartTime != 0) throw new InvalidOperationException($"Commands in a loop must start at 0ms, but start at {loopCommand.CommandsStartTime}ms");
                return new LoopDecorator<TValue>(command, loopCommand.StartTime, loopCommand.CommandsDuration, loopCommand.LoopCount);
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

            if (composite.HasCommands)
                value.Add(decorate(composite));

            composite = null;
            decorate = null;
        }
    }
}
