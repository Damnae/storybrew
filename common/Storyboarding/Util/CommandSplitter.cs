﻿using OpenTK;
using StorybrewCommon.Storyboarding.Commands;
using System.Diagnostics;

namespace StorybrewCommon.Storyboarding.Util
{
    public static class CommandSplitter
    {
        public static void Split(OsbSprite sprite, StoryboardSegment segment, int commandSplitThreshold, CancellationToken token)
        {
            var sprites = Split(sprite, segment.CreateSprite, segment.CreateAnimation, commandSplitThreshold, token).ToArray();
            if (!sprites.Contains(sprite))
                segment.Discard(sprite);
        }

        public static IEnumerable<OsbSprite> Split(OsbSprite sprite, int commandSplitThreshold, CancellationToken token) =>
            Split(sprite,
                (path, origin, initialPosition) => new OsbSprite
                {
                    TexturePath = path,
                    Origin = origin,
                    InitialPosition = initialPosition,
                },
                (path, frameCount, frameDelay, loopType, origin, initialPosition) => new OsbAnimation
                {
                    TexturePath = path,
                    Origin = origin,
                    FrameCount = frameCount,
                    FrameDelay = frameDelay,
                    LoopType = loopType,
                    InitialPosition = initialPosition,
                },
            commandSplitThreshold, token);

        public delegate OsbSprite CreateSpriteDelegate(string path, OsbOrigin origin, Vector2 initialPosition);
        public delegate OsbSprite CreateAnimationDelegate(string path, int frameCount, double frameDelay, OsbLoopType loopType, OsbOrigin origin, Vector2 initialPosition);

        public static IEnumerable<OsbSprite> Split(OsbSprite sprite, CreateSpriteDelegate createSprite, CreateAnimationDelegate createAnimation, int commandSplitThreshold, CancellationToken token)
        {
            if (sprite.HasIncompatibleCommands || sprite.HasTrigger || sprite.CommandCost < commandSplitThreshold)
            {
                yield return sprite;
                yield break;
            }

            var candidateFragmentTimes = getFragmentationTimes(sprite)
                .Where(t => canFragmentCommandsAt(sprite, t, token))
                .Order()
                .ToArray();

            var idealSegmentCount = Math.Min((int)Math.Ceiling((float)sprite.CommandCost / commandSplitThreshold), candidateFragmentTimes.Length + 1);
            if (idealSegmentCount < 2)
            {
                yield return sprite;
                yield break;
            }

            var segmentTimes = pickFragmentTimes(candidateFragmentTimes, idealSegmentCount, sprite.CommandsStartTime, sprite.CommandsEndTime)
                .Prepend(sprite.CommandsStartTime).Append(sprite.CommandsEndTime)
                .ToArray();

            for (var i = 1; i < segmentTimes.Length; i++)
            {
                token.ThrowIfCancellationRequested();

                var segmentStart = segmentTimes[i - 1];
                var segmentEnd = segmentTimes[i];

                var segmentSprite = sprite switch
                {
                    OsbAnimation animation when animation.LoopType == OsbLoopType.LoopOnce && i == 1 || animation.LoopType == OsbLoopType.LoopForever
                        => createAnimation(animation.TexturePath, animation.FrameCount, animation.FrameDelay, animation.LoopType, animation.Origin, animation.InitialPosition),
                    OsbAnimation animation when animation.LoopType == OsbLoopType.LoopOnce && i > 1
                        => createSprite(animation.GetTexturePathAt(animation.CommandsStartTime + animation.FrameDelay * animation.FrameCount), animation.Origin, animation.InitialPosition),
                    _ => createSprite(sprite.TexturePath, sprite.Origin, sprite.InitialPosition),
                };
                transferCommands(sprite, segmentSprite, segmentStart, segmentEnd);
                transferStartState(sprite, segmentSprite, segmentStart);

                yield return segmentSprite;
            }
        }

        private static IEnumerable<double> pickFragmentTimes(double[] candidates, int idealSegmentCount, double startTime, double endTime)
        {
            var times = new HashSet<double>();
            var idealSegmentDuration = (endTime - startTime) / idealSegmentCount;
            for (var i = 1; i < idealSegmentCount; i++)
            {
                var idealTime = startTime + i * idealSegmentDuration;
                var time = candidates.MinBy(candidate => Math.Abs(candidate - idealTime));
                if (times.Add(time))
                    yield return time;
            }
        }

        private static void transferCommands(OsbSprite sprite, OsbSprite segmentSprite, double segmentStart, double segmentEnd)
        {
            foreach (var command in sprite.Commands)
            {
                if (segmentEnd <= command.StartTime || command.EndTime <= segmentStart)
                    continue;

                var startTime = Math.Clamp(command.StartTime, segmentStart, segmentEnd);
                var endTime = Math.Clamp(command.EndTime, segmentStart, segmentEnd);

                switch (command)
                {
                    case MoveCommand moveCommand:
                        segmentSprite.Move(moveCommand.Easing, startTime, endTime, moveCommand.ValueAtTime(startTime), moveCommand.ValueAtTime(endTime));
                        break;
                    case MoveXCommand moveXCommand:
                        segmentSprite.MoveX(moveXCommand.Easing, startTime, endTime, moveXCommand.ValueAtTime(startTime), moveXCommand.ValueAtTime(endTime));
                        break;
                    case MoveYCommand moveYCommand:
                        segmentSprite.MoveY(moveYCommand.Easing, startTime, endTime, moveYCommand.ValueAtTime(startTime), moveYCommand.ValueAtTime(endTime));
                        break;
                    case ScaleCommand scaleCommand:
                        segmentSprite.Scale(scaleCommand.Easing, startTime, endTime, scaleCommand.ValueAtTime(startTime), scaleCommand.ValueAtTime(endTime));
                        break;
                    case VScaleCommand scaleVecCommand:
                        segmentSprite.ScaleVec(scaleVecCommand.Easing, startTime, endTime, scaleVecCommand.ValueAtTime(startTime), scaleVecCommand.ValueAtTime(endTime));
                        break;
                    case RotateCommand rotateCommand:
                        segmentSprite.Rotate(rotateCommand.Easing, startTime, endTime, rotateCommand.ValueAtTime(startTime), rotateCommand.ValueAtTime(endTime));
                        break;
                    case FadeCommand fadeCommand:
                        segmentSprite.Fade(fadeCommand.Easing, startTime, endTime, fadeCommand.ValueAtTime(startTime), fadeCommand.ValueAtTime(endTime));
                        break;
                    case ColorCommand colorCommand:
                        segmentSprite.Color(colorCommand.Easing, startTime, endTime, colorCommand.ValueAtTime(startTime), colorCommand.ValueAtTime(endTime));
                        break;
                    case ParameterCommand parameterCommand:
                        segmentSprite.Parameter(parameterCommand.Easing, startTime, endTime, parameterCommand.StartValue);
                        break;
                    case LoopCommand loopCommand:
                        if (loopCommand.StartTime < startTime || endTime < loopCommand.EndTime)
                        {
                            var loopStartIndex = (startTime - loopCommand.StartTime) / loopCommand.CommandsDuration;
                            var loopEndIndex = (endTime - loopCommand.StartTime) / loopCommand.CommandsDuration;
                            var loopCount = (int)Math.Round(loopEndIndex - loopStartIndex);

                            var loopSegmentStartTime = loopCommand.StartTime + loopCommand.CommandsDuration * loopStartIndex;

                            Debug.Assert(loopCount > 0);
                            if (loopCount == 1)
                            {
                                foreach (var c in loopCommand.Commands)
                                    segmentSprite.AddCommand(c, loopSegmentStartTime);
                            }
                            else
                            {
                                segmentSprite.StartLoopGroup(loopSegmentStartTime, loopCount);
                                foreach (var c in loopCommand.Commands)
                                    segmentSprite.AddCommand(c);
                                segmentSprite.EndGroup();
                            }
                        }
                        else
                        {
                            segmentSprite.StartLoopGroup(loopCommand.StartTime, loopCommand.LoopCount);
                            foreach (var c in loopCommand.Commands)
                                segmentSprite.AddCommand(c);
                            segmentSprite.EndGroup();
                        }
                        break;
                    default:
                        throw new NotImplementedException(command.GetType().FullName);
                }
            }
        }

        private static void transferStartState(OsbSprite sprite, OsbSprite segmentSprite, double segmentStart)
        {
            if (sprite.HasMoveXYCommands)
            {
                var spriteStartPosition = sprite.PositionAt(segmentStart);
                var segmentStartPosition = segmentSprite.PositionAt(segmentStart);
                if (segmentStartPosition.X != spriteStartPosition.X)
                    segmentSprite.MoveX(segmentStart, spriteStartPosition.X);
                if (segmentStartPosition.Y != spriteStartPosition.Y)
                    segmentSprite.MoveY(segmentStart, spriteStartPosition.Y);
            }
            else
            {
                var spriteStartPosition = sprite.PositionAt(segmentStart);
                if (segmentSprite.PositionAt(segmentStart) != spriteStartPosition)
                    segmentSprite.Move(segmentStart, spriteStartPosition);
            }

            if (sprite.HasScaleVecCommands)
            {
                var spriteStartScale = sprite.ScaleAt(segmentStart);
                if (segmentSprite.ScaleAt(segmentStart) != spriteStartScale)
                    segmentSprite.ScaleVec(segmentStart, spriteStartScale.X, spriteStartScale.Y);
            }
            else
            {
                var spriteStartScale = sprite.ScaleAt(segmentStart);
                Debug.Assert(spriteStartScale.X == spriteStartScale.Y);
                if (segmentSprite.ScaleAt(segmentStart) != spriteStartScale)
                    segmentSprite.Scale(segmentStart, spriteStartScale.X);
            }

            var spriteStartRotation = sprite.RotationAt(segmentStart);
            if (segmentSprite.RotationAt(segmentStart) != spriteStartRotation)
                segmentSprite.Rotate(segmentStart, spriteStartRotation);

            var spriteStartFade = sprite.OpacityAt(segmentStart);
            if (segmentSprite.OpacityAt(segmentStart) != spriteStartFade)
                segmentSprite.Fade(segmentStart, spriteStartFade);

            var spriteStartColor = sprite.ColorAt(segmentStart);
            if (segmentSprite.ColorAt(segmentStart) != spriteStartColor)
                segmentSprite.Color(segmentStart, spriteStartColor);

            if (segmentSprite.AdditiveAt(segmentStart) != sprite.AdditiveAt(segmentStart))
                segmentSprite.Additive(segmentStart);

            if (segmentSprite.FlipHAt(segmentStart) != sprite.FlipHAt(segmentStart))
                segmentSprite.FlipH(segmentStart);

            if (segmentSprite.FlipVAt(segmentStart) != sprite.FlipVAt(segmentStart))
                segmentSprite.FlipV(segmentStart);
        }

        private static bool canFragmentCommandsAt(OsbSprite sprite, double time, CancellationToken token)
        {
            var duringCommandTypes = new HashSet<Type>();
            foreach (var command in sprite.Commands)
            {
                token.ThrowIfCancellationRequested();

                if (time <= command.StartTime || command.EndTime <= time)
                    continue;

                if (!command.IsFragmentableAt(time))
                    return false;

                // Overlapping commands cannot be fragmented
                if (command is LoopCommand loop)
                {
                    var loopTime = (time - loop.StartTime) % loop.CommandsDuration;
                    foreach (var loopCommand in loop.Commands)
                    {
                        if (loopTime <= loopCommand.StartTime || loopCommand.EndTime <= loopTime)
                            continue;

                        if (!duringCommandTypes.Add(loopCommand.GetType()))
                            return false;
                    }
                }
                else if (!duringCommandTypes.Add(command.GetType()))
                    return false;
            }
            return true;
        }

        private static IEnumerable<double> getFragmentationTimes(OsbSprite sprite)
        {
            if (sprite is OsbAnimation animation)
                return animation.LoopType switch
                {
                    OsbLoopType.LoopOnce => getCommandFragmentationTimes(sprite)
                        .Where(t => animation.CommandsStartTime + animation.FrameDelay * animation.FrameCount <= t)
                        .Except(getStartEnd(sprite))
                        .Distinct(),
                    OsbLoopType.LoopForever => animation.GetAnimationRepeatTimes(),
                    _ => throw new NotImplementedException(animation.LoopType.ToString()),
                };

            return getCommandFragmentationTimes(sprite)
                .Except(getStartEnd(sprite))
                .Distinct();
        }

        private static IEnumerable<double> getCommandFragmentationTimes(OsbSprite sprite)
        {
            foreach (var command in sprite.Commands)
            {
                yield return command.StartTime;
                if (command is LoopCommand loopCommand)
                {
                    Debug.Assert(loopCommand.CommandsStartTime == 0);
                    for (var i = 1; i < loopCommand.LoopCount - 1; i++)
                        yield return command.StartTime + i * loopCommand.CommandsEndTime;
                }
                yield return command.EndTime;
            }
        }

        private static IEnumerable<double> getStartEnd(OsbSprite sprite)
        {
            yield return sprite.CommandsStartTime;
            yield return sprite.CommandsEndTime;
        }
    }
}
