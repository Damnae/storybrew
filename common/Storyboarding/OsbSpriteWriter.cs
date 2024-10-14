﻿using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;

namespace StorybrewCommon.Storyboarding
{
    public class OsbSpriteWriter
    {
        private readonly OsbSprite osbSprite;
        private readonly AnimatedValue<CommandPosition> moveTimeline;
        private readonly AnimatedValue<CommandDecimal> moveXTimeline;
        private readonly AnimatedValue<CommandDecimal> moveYTimeline;
        private readonly AnimatedValue<CommandDecimal> scaleTimeline;
        private readonly AnimatedValue<CommandScale> scaleVecTimeline;
        private readonly AnimatedValue<CommandDecimal> rotateTimeline;
        private readonly AnimatedValue<CommandDecimal> fadeTimeline;
        private readonly AnimatedValue<CommandColor> colorTimeline;
        protected readonly TextWriter TextWriter;
        protected readonly ExportSettings ExportSettings;
        protected readonly OsbLayer OsbLayer;

        public OsbSpriteWriter(OsbSprite osbSprite, AnimatedValue<CommandPosition> moveTimeline,
                                                    AnimatedValue<CommandDecimal> moveXTimeline,
                                                    AnimatedValue<CommandDecimal> moveYTimeline,
                                                    AnimatedValue<CommandDecimal> scaleTimeline,
                                                    AnimatedValue<CommandScale> scaleVecTimeline,
                                                    AnimatedValue<CommandDecimal> rotateTimeline,
                                                    AnimatedValue<CommandDecimal> fadeTimeline,
                                                    AnimatedValue<CommandColor> colorTimeline,
                                                    TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            this.osbSprite = osbSprite;
            this.moveTimeline = moveTimeline;
            this.moveXTimeline = moveXTimeline;
            this.moveYTimeline = moveYTimeline;
            this.scaleTimeline = scaleTimeline;
            this.scaleVecTimeline = scaleVecTimeline;
            this.rotateTimeline = rotateTimeline;
            this.fadeTimeline = fadeTimeline;
            this.colorTimeline = colorTimeline;
            TextWriter = writer;
            ExportSettings = exportSettings;
            OsbLayer = layer;
        }

        public void WriteOsb(StoryboardTransform transform)
        {
            if (ExportSettings.OptimiseSprites && osbSprite.CommandSplitThreshold > 0 && osbSprite.CommandCount > osbSprite.CommandSplitThreshold && IsFragmentable())
            {
                var commands = osbSprite.Commands.Select(c => (IFragmentableCommand)c).ToList();
                var fragmentationTimes = GetFragmentationTimes(commands);

                while (commands.Count > 0)
                {
                    var segment = getNextSegment(fragmentationTimes, commands);
                    var sprite = CreateSprite(segment);
                    writeOsbSprite(sprite, transform);
                }
            }
            else writeOsbSprite(osbSprite, transform);
        }

        protected virtual OsbSprite CreateSprite(List<IFragmentableCommand> segment)
        {
            var sprite = new OsbSprite()
            {
                TexturePath = osbSprite.TexturePath,
                InitialPosition = osbSprite.InitialPosition,
                Origin = osbSprite.Origin,
            };

            foreach (var command in segment)
                sprite.AddCommand(command);

            return sprite;
        }

        private void writeOsbSprite(OsbSprite sprite, StoryboardTransform transform)
        {
            WriteHeader(sprite, transform);
            foreach (var command in sprite.Commands)
                command.WriteOsb(TextWriter, ExportSettings, transform, 1);
        }

        protected virtual void WriteHeader(OsbSprite sprite, StoryboardTransform transform)
        {
            TextWriter.Write($"Sprite");
            WriteHeaderCommon(sprite, transform);
            TextWriter.WriteLine();
        }

        protected virtual void WriteHeaderCommon(OsbSprite sprite, StoryboardTransform transform)
        {
            TextWriter.Write($",{OsbLayer},{sprite.Origin},\"{sprite.TexturePath.Trim()}\"");
            
            var transformedInitialPosition = transform == null ?
                sprite.InitialPosition :
                sprite.HasMoveXYCommands ? 
                    transform.ApplyToPositionXY(sprite.InitialPosition) : 
                    transform.ApplyToPosition(sprite.InitialPosition);

            if (!moveTimeline.HasCommands && !moveXTimeline.HasCommands)
                TextWriter.Write($",{transformedInitialPosition.X.ToString(ExportSettings.NumberFormat)}");
            else TextWriter.Write($",0");
            if (!moveTimeline.HasCommands && !moveYTimeline.HasCommands)
                TextWriter.Write($",{transformedInitialPosition.Y.ToString(ExportSettings.NumberFormat)}");
            else TextWriter.Write($",0");
        }

        protected virtual bool IsFragmentable()
        {
            // if there are commands with non-deterministic results (aka triggercommands) the sprite can't reliably be split
            if (osbSprite.Commands.Any(c => !(c is IFragmentableCommand)))
                return false;

            return !(moveTimeline.HasOverlap ||
                     moveXTimeline.HasOverlap ||
                     moveYTimeline.HasOverlap ||
                     rotateTimeline.HasOverlap ||
                     scaleTimeline.HasOverlap ||
                     scaleVecTimeline.HasOverlap ||
                     fadeTimeline.HasOverlap ||
                     colorTimeline.HasOverlap);
        }

        protected virtual HashSet<int> GetFragmentationTimes(IEnumerable<IFragmentableCommand> fragmentableCommands)
        {
            var fragmentationTimes = new HashSet<int>(Enumerable.Range((int)osbSprite.StartTime, (int)(osbSprite.EndTime - osbSprite.StartTime) + 1));

            foreach (var command in fragmentableCommands)
                fragmentationTimes.ExceptWith(command.GetNonFragmentableTimes());

            return fragmentationTimes;
        }

        private List<IFragmentableCommand> getNextSegment(HashSet<int> fragmentationTimes, List<IFragmentableCommand> commands)
        {
            var segment = new List<IFragmentableCommand>();

            var startTime = fragmentationTimes.Min();
            var endTime = getSegmentEndTime(fragmentationTimes, commands);

            foreach (var cmd in commands.Where(c => c.StartTime < endTime))
            {
                var sTime = Math.Max(startTime, (int)Math.Round(cmd.StartTime));
                var eTime = Math.Min(endTime, (int)Math.Round(cmd.EndTime));

                IFragmentableCommand command;
                if (sTime != (int)Math.Round(cmd.StartTime) || eTime != (int)Math.Round(cmd.EndTime))
                    command = cmd.GetFragment(sTime, eTime);
                else command = cmd;

                segment.Add(command);
            }

            addStaticCommands(segment, startTime);

            fragmentationTimes.RemoveWhere(t => t < endTime);
            commands.RemoveAll(c => c.EndTime <= endTime);

            return segment;
        }

        private int getSegmentEndTime(HashSet<int> fragmentationTimes, List<IFragmentableCommand> commands)
        {
            var startTime = fragmentationTimes.Min();
            int endTime;
            var maxCommandCount = osbSprite.CommandSplitThreshold;

            //split the last 2 segments evenly so we don't have weird 5 command leftovers
            if (commands.Count < osbSprite.CommandSplitThreshold * 2 && commands.Count > osbSprite.CommandSplitThreshold)
                maxCommandCount = (int)Math.Ceiling(commands.Count / 2.0);

            if (commands.Count < maxCommandCount)
                endTime = fragmentationTimes.Max() + 1;
            else
            {
                var lastCommand = commands.OrderBy(c => c.StartTime).ElementAt(maxCommandCount - 1);
                if (fragmentationTimes.Contains((int)lastCommand.StartTime) && lastCommand.StartTime > startTime)
                    endTime = (int)lastCommand.StartTime;
                else
                {
                    if (fragmentationTimes.Any(t => t < (int)lastCommand.StartTime))
                    {
                        endTime = fragmentationTimes.Where(t => t < (int)lastCommand.StartTime).Max();
                        if (endTime == startTime) // segment can't be <= MaxCommandCount, so we use the smallest available
                            endTime = fragmentationTimes.First(t => t > startTime);
                    }
                    else endTime = fragmentationTimes.First(t => t > startTime);
                }
            }

            return endTime;
        }

        private void addStaticCommands(List<IFragmentableCommand> segment, int startTime)
        {
            if (moveTimeline.HasCommands && !segment.Any(c => c is MoveCommand && c.StartTime == startTime))
            {
                var value = moveTimeline.ValueAtTime(startTime);
                segment.Add(new MoveCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (moveXTimeline.HasCommands && !segment.Any(c => c is MoveXCommand && c.StartTime == startTime))
            {
                var value = moveXTimeline.ValueAtTime(startTime);
                segment.Add(new MoveXCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (moveYTimeline.HasCommands && !segment.Any(c => c is MoveYCommand && c.StartTime == startTime))
            {
                var value = moveYTimeline.ValueAtTime(startTime);
                segment.Add(new MoveYCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (rotateTimeline.HasCommands && !segment.Any(c => c is RotateCommand && c.StartTime == startTime))
            {
                var value = rotateTimeline.ValueAtTime(startTime);
                segment.Add(new RotateCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (scaleTimeline.HasCommands && !segment.Any(c => c is ScaleCommand && c.StartTime == startTime))
            {
                var value = scaleTimeline.ValueAtTime(startTime);
                segment.Add(new ScaleCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (scaleVecTimeline.HasCommands && !segment.Any(c => c is VScaleCommand && c.StartTime == startTime))
            {
                var value = scaleVecTimeline.ValueAtTime(startTime);
                segment.Add(new VScaleCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (colorTimeline.HasCommands && !segment.Any(c => c is ColorCommand && c.StartTime == startTime))
            {
                var value = colorTimeline.ValueAtTime(startTime);
                segment.Add(new ColorCommand(OsbEasing.None, startTime, startTime, value, value));
            }

            if (fadeTimeline.HasCommands && !segment.Any(c => c is FadeCommand && c.StartTime == startTime))
            {
                var value = fadeTimeline.ValueAtTime(startTime);
                segment.Add(new FadeCommand(OsbEasing.None, startTime, startTime, value, value));
            }
        }
    }
}
