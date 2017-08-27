using OpenTK;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;
using System;
using System.Collections.Generic;
using System.IO;

namespace StorybrewCommon.Storyboarding
{
    public class OsbSprite : StoryboardObject
    {
        public static readonly Vector2 DefaultPosition = new Vector2(320, 240);

        private List<ICommand> commands = new List<ICommand>();
        private CommandGroup currentCommandGroup;

        public string texturePath = "";
        public string TexturePath
        {
            get { return texturePath; }
            set
            {
                if (texturePath == value) return;
                new FileInfo(value);
                texturePath = value;
            }
        }
        public virtual string GetTexturePathAt(double time)
            => TexturePath;

        public OsbOrigin Origin = OsbOrigin.Centre;

        private Vector2 initialPosition;
        public Vector2 InitialPosition
        {
            get { return initialPosition; }
            set
            {
                if (initialPosition == value) return;
                initialPosition = value;
                moveTimeline.DefaultValue = initialPosition;
                moveXTimeline.DefaultValue = initialPosition.X;
                moveYTimeline.DefaultValue = initialPosition.Y;
            }
        }

        public IEnumerable<ICommand> Commands => commands;
        public int CommandCount => commands.Count;

        private double commandsStartTime = double.MaxValue;
        public double CommandsStartTime
        {
            get
            {
                if (commandsStartTime == double.MaxValue)
                    refreshStartEndTimes();
                return commandsStartTime;
            }
        }

        private double commandsEndTime = double.MinValue;
        public double CommandsEndTime
        {
            get
            {
                if (commandsEndTime == double.MinValue)
                    refreshStartEndTimes();
                return commandsEndTime;
            }
        }

        private void refreshStartEndTimes()
        {
            clearStartEndTimes();
            foreach (var command in commands)
            {
                if (!command.Active) continue;
                commandsStartTime = Math.Min(commandsStartTime, command.StartTime);
                commandsEndTime = Math.Max(commandsEndTime, command.EndTime);
            }
        }

        private void clearStartEndTimes()
        {
            commandsStartTime = double.MaxValue;
            commandsEndTime = double.MinValue;
        }

        public OsbSprite()
        {
            initializeDisplayValueBuilders();
            InitialPosition = DefaultPosition;
        }

        public void Move(OsbEasing easing, double startTime, double endTime, CommandPosition startPosition, CommandPosition endPosition) => addCommand(new MoveCommand(easing, startTime, endTime, startPosition, endPosition));
        public void Move(OsbEasing easing, double startTime, double endTime, CommandPosition startPosition, double endX, double endY) => Move(easing, startTime, endTime, startPosition, new CommandPosition(endX, endY));
        public void Move(OsbEasing easing, double startTime, double endTime, double startX, double startY, double endX, double endY) => Move(easing, startTime, endTime, new CommandPosition(startX, startY), new CommandPosition(endX, endY));
        public void Move(double startTime, double endTime, CommandPosition startPosition, CommandPosition endPosition) => Move(OsbEasing.None, startTime, endTime, startPosition, endPosition);
        public void Move(double startTime, double endTime, CommandPosition startPosition, double endX, double endY) => Move(OsbEasing.None, startTime, endTime, startPosition, endX, endY);
        public void Move(double startTime, double endTime, double startX, double startY, double endX, double endY) => Move(OsbEasing.None, startTime, endTime, startX, startY, endX, endY);
        public void Move(double time, CommandPosition position) => Move(OsbEasing.None, time, time, position, position);
        public void Move(double time, double x, double y) => Move(OsbEasing.None, time, time, x, y, x, y);

        public void MoveX(OsbEasing easing, double startTime, double endTime, CommandDecimal startX, CommandDecimal endX) => addCommand(new MoveXCommand(easing, startTime, endTime, startX, endX));
        public void MoveX(double startTime, double endTime, CommandDecimal startX, CommandDecimal endX) => MoveX(OsbEasing.None, startTime, endTime, startX, endX);
        public void MoveX(double time, CommandDecimal x) => MoveX(OsbEasing.None, time, time, x, x);

        public void MoveY(OsbEasing easing, double startTime, double endTime, CommandDecimal startY, CommandDecimal endY) => addCommand(new MoveYCommand(easing, startTime, endTime, startY, endY));
        public void MoveY(double startTime, double endTime, CommandDecimal startY, CommandDecimal endY) => MoveY(OsbEasing.None, startTime, endTime, startY, endY);
        public void MoveY(double time, CommandDecimal y) => MoveY(OsbEasing.None, time, time, y, y);

        public void Scale(OsbEasing easing, double startTime, double endTime, CommandDecimal startScale, CommandDecimal endScale) => addCommand(new ScaleCommand(easing, startTime, endTime, startScale, endScale));
        public void Scale(double startTime, double endTime, CommandDecimal startScale, CommandDecimal endScale) => Scale(OsbEasing.None, startTime, endTime, startScale, endScale);
        public void Scale(double time, CommandDecimal scale) => Scale(OsbEasing.None, time, time, scale, scale);

        public void ScaleVec(OsbEasing easing, double startTime, double endTime, CommandScale startScale, CommandScale endScale) => addCommand(new VScaleCommand(easing, startTime, endTime, startScale, endScale));
        public void ScaleVec(OsbEasing easing, double startTime, double endTime, CommandScale startScale, double endX, double endY) => ScaleVec(easing, startTime, endTime, startScale, new CommandScale(endX, endY));
        public void ScaleVec(OsbEasing easing, double startTime, double endTime, double startX, double startY, double endX, double endY) => ScaleVec(easing, startTime, endTime, new CommandScale(startX, startY), new CommandScale(endX, endY));
        public void ScaleVec(double startTime, double endTime, CommandScale startScale, CommandScale endScale) => ScaleVec(OsbEasing.None, startTime, endTime, startScale, endScale);
        public void ScaleVec(double startTime, double endTime, CommandScale startScale, double endX, double endY) => ScaleVec(OsbEasing.None, startTime, endTime, startScale, endX, endY);
        public void ScaleVec(double startTime, double endTime, double startX, double startY, double endX, double endY) => ScaleVec(OsbEasing.None, startTime, endTime, startX, startY, endX, endY);
        public void ScaleVec(double time, CommandScale scale) => ScaleVec(OsbEasing.None, time, time, scale, scale);
        public void ScaleVec(double time, double x, double y) => ScaleVec(OsbEasing.None, time, time, x, y, x, y);

        public void Rotate(OsbEasing easing, double startTime, double endTime, CommandDecimal startRotation, CommandDecimal endRotation) => addCommand(new RotateCommand(easing, startTime, endTime, startRotation, endRotation));
        public void Rotate(double startTime, double endTime, CommandDecimal startRotation, CommandDecimal endRotation) => Rotate(OsbEasing.None, startTime, endTime, startRotation, endRotation);
        public void Rotate(double time, CommandDecimal rotation) => Rotate(OsbEasing.None, time, time, rotation, rotation);

        public void Fade(OsbEasing easing, double startTime, double endTime, CommandDecimal startOpacity, CommandDecimal endOpacity) => addCommand(new FadeCommand(easing, startTime, endTime, startOpacity, endOpacity));
        public void Fade(double startTime, double endTime, CommandDecimal startOpacity, CommandDecimal endOpacity) => Fade(OsbEasing.None, startTime, endTime, startOpacity, endOpacity);
        public void Fade(double time, CommandDecimal opacity) => Fade(OsbEasing.None, time, time, opacity, opacity);

        public void Color(OsbEasing easing, double startTime, double endTime, CommandColor startColor, CommandColor endColor) => addCommand(new ColorCommand(easing, startTime, endTime, startColor, endColor));
        public void Color(OsbEasing easing, double startTime, double endTime, CommandColor startColor, double endRed, double endGreen, double endBlue) => Color(easing, startTime, endTime, startColor, new CommandColor(endRed, endGreen, endBlue));
        public void Color(OsbEasing easing, double startTime, double endTime, double startRed, double startGreen, double startBlue, double endRed, double endGreen, double endBlue) => Color(easing, startTime, endTime, new CommandColor(startRed, startGreen, startBlue), new CommandColor(endRed, endGreen, endBlue));
        public void Color(double startTime, double endTime, CommandColor startColor, CommandColor endColor) => Color(OsbEasing.None, startTime, endTime, startColor, endColor);
        public void Color(double startTime, double endTime, CommandColor startColor, double endRed, double endGreen, double endBlue) => Color(OsbEasing.None, startTime, endTime, startColor, endRed, endGreen, endBlue);
        public void Color(double startTime, double endTime, double startRed, double startGreen, double startBlue, double endRed, double endGreen, double endBlue) => Color(OsbEasing.None, startTime, endTime, startRed, startGreen, startBlue, endRed, endGreen, endBlue);
        public void Color(double time, CommandColor color) => Color(OsbEasing.None, time, time, color, color);
        public void Color(double time, double red, double green, double blue) => Color(OsbEasing.None, time, time, red, green, blue, red, green, blue);

        public void ColorHsb(OsbEasing easing, double startTime, double endTime, CommandColor startColor, double endHue, double endSaturation, double endBrightness) => Color(easing, startTime, endTime, startColor, CommandColor.FromHsb(endHue, endSaturation, endBrightness));
        public void ColorHsb(OsbEasing easing, double startTime, double endTime, double startHue, double startSaturation, double startBrightness, double endHue, double endSaturation, double endBrightness) => Color(easing, startTime, endTime, CommandColor.FromHsb(startHue, startSaturation, startBrightness), CommandColor.FromHsb(endHue, endSaturation, endBrightness));
        public void ColorHsb(double startTime, double endTime, CommandColor startColor, double endHue, double endSaturation, double endBrightness) => ColorHsb(OsbEasing.None, startTime, endTime, startColor, endHue, endSaturation, endBrightness);
        public void ColorHsb(double startTime, double endTime, double startHue, double startSaturation, double startBrightness, double endHue, double endSaturation, double endBrightness) => ColorHsb(OsbEasing.None, startTime, endTime, startHue, startSaturation, startBrightness, endHue, endSaturation, endBrightness);
        public void ColorHsb(double time, double hue, double saturation, double brightness) => ColorHsb(OsbEasing.None, time, time, hue, saturation, brightness, hue, saturation, brightness);

        public void Parameter(OsbEasing easing, double startTime, double endTime, CommandParameter parameter) => addCommand(new ParameterCommand(easing, startTime, endTime, parameter));
        public void FlipH(double startTime, double endTime) => Parameter(OsbEasing.None, startTime, endTime, CommandParameter.FlipHorizontal);
        public void FlipV(double startTime, double endTime) => Parameter(OsbEasing.None, startTime, endTime, CommandParameter.FlipVertical);
        public void Additive(double startTime, double endTime) => Parameter(OsbEasing.None, startTime, endTime, CommandParameter.AdditiveBlending);

        public LoopCommand StartLoopGroup(double startTime, int loopCount)
        {
            var loopCommand = new LoopCommand(startTime, loopCount);
            addCommand(loopCommand);
            startDisplayLoop(loopCommand);
            return loopCommand;
        }

        public TriggerCommand StartTriggerGroup(string triggerName, double startTime, double endTime, int group = 0)
        {
            var triggerCommand = new TriggerCommand(triggerName, startTime, endTime, group);
            addCommand(triggerCommand);
            startDisplayTrigger(triggerCommand);
            return triggerCommand;
        }

        public void EndGroup()
        {
            currentCommandGroup.EndGroup();
            currentCommandGroup = null;

            endDisplayComposites();
        }

        private void addCommand(ICommand command)
        {
            var commandGroup = command as CommandGroup;
            if (commandGroup != null)
            {
                currentCommandGroup = commandGroup;
                commands.Add(commandGroup);
            }
            else
            {
                if (currentCommandGroup != null)
                    currentCommandGroup.Add(command);
                else
                    commands.Add(command);
                addDisplayCommand(command);
            }
            clearStartEndTimes();
        }

        #region Display 

        private List<KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>> displayValueBuilders = new List<KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>>();

        private AnimatedValue<CommandPosition> moveTimeline = new AnimatedValue<CommandPosition>();
        private AnimatedValue<CommandDecimal> moveXTimeline = new AnimatedValue<CommandDecimal>();
        private AnimatedValue<CommandDecimal> moveYTimeline = new AnimatedValue<CommandDecimal>();
        private AnimatedValue<CommandDecimal> scaleTimeline = new AnimatedValue<CommandDecimal>(1);
        private AnimatedValue<CommandScale> scaleVecTimeline = new AnimatedValue<CommandScale>(Vector2.One);
        private AnimatedValue<CommandDecimal> rotateTimeline = new AnimatedValue<CommandDecimal>();
        private AnimatedValue<CommandDecimal> fadeTimeline = new AnimatedValue<CommandDecimal>(1);
        private AnimatedValue<CommandColor> colorTimeline = new AnimatedValue<CommandColor>(CommandColor.FromRgb(255, 255, 255));
        private AnimatedValue<CommandParameter> additiveTimeline = new AnimatedValue<CommandParameter>(CommandParameter.None, true);
        private AnimatedValue<CommandParameter> flipHTimeline = new AnimatedValue<CommandParameter>(CommandParameter.None, true);
        private AnimatedValue<CommandParameter> flipVTimeline = new AnimatedValue<CommandParameter>(CommandParameter.None, true);

        public CommandPosition PositionAt(double time) => moveTimeline.HasCommands ? moveTimeline.ValueAtTime(time) : new CommandPosition(moveXTimeline.ValueAtTime(time), moveYTimeline.ValueAtTime(time));
        public CommandScale ScaleAt(double time) => scaleVecTimeline.HasCommands ? scaleVecTimeline.ValueAtTime(time) : new CommandScale(scaleTimeline.ValueAtTime(time));
        public CommandDecimal RotationAt(double time) => rotateTimeline.ValueAtTime(time);
        public CommandDecimal OpacityAt(double time) => fadeTimeline.ValueAtTime(time);
        public CommandColor ColorAt(double time) => colorTimeline.ValueAtTime(time);
        public CommandParameter AdditiveAt(double time) => additiveTimeline.ValueAtTime(time);
        public CommandParameter FlipHAt(double time) => flipHTimeline.ValueAtTime(time);
        public CommandParameter FlipVAt(double time) => flipVTimeline.ValueAtTime(time);

        private void initializeDisplayValueBuilders()
        {
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is MoveCommand, new AnimatedValueBuilder<CommandPosition>(moveTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is MoveXCommand, new AnimatedValueBuilder<CommandDecimal>(moveXTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is MoveYCommand, new AnimatedValueBuilder<CommandDecimal>(moveYTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is ScaleCommand, new AnimatedValueBuilder<CommandDecimal>(scaleTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is VScaleCommand, new AnimatedValueBuilder<CommandScale>(scaleVecTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is RotateCommand, new AnimatedValueBuilder<CommandDecimal>(rotateTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is FadeCommand, new AnimatedValueBuilder<CommandDecimal>(fadeTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => c is ColorCommand, new AnimatedValueBuilder<CommandColor>(colorTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => (c as ParameterCommand)?.StartValue.Type == ParameterType.AdditiveBlending, new AnimatedValueBuilder<CommandParameter>(additiveTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => (c as ParameterCommand)?.StartValue.Type == ParameterType.FlipHorizontal, new AnimatedValueBuilder<CommandParameter>(flipHTimeline)));
            displayValueBuilders.Add(new KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>((c) => (c as ParameterCommand)?.StartValue.Type == ParameterType.FlipVertical, new AnimatedValueBuilder<CommandParameter>(flipVTimeline)));
        }

        private void addDisplayCommand(ICommand command)
        {
            foreach (var builders in displayValueBuilders)
                if (builders.Key(command))
                    builders.Value.Add(command);
        }

        private void startDisplayLoop(LoopCommand loopCommand)
        {
            foreach (var builders in displayValueBuilders)
                builders.Value.StartDisplayLoop(loopCommand);
        }

        private void startDisplayTrigger(TriggerCommand triggerCommand)
        {
            foreach (var builders in displayValueBuilders)
                builders.Value.StartDisplayTrigger(triggerCommand);
        }

        private void endDisplayComposites()
        {
            foreach (var builders in displayValueBuilders)
                builders.Value.EndDisplayComposite();
        }

        #endregion

        public bool IsActive(double time)
            => CommandsStartTime <= time && time <= CommandsEndTime;

        protected virtual void WriteHeader(TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            writer.Write($"Sprite,{layer},{Origin.ToString()},\"{TexturePath}\"");
            if (!moveTimeline.HasCommands && !moveXTimeline.HasCommands)
                writer.Write($",{ InitialPosition.X.ToString(exportSettings.NumberFormat)}");
            else writer.Write($",0");
            if (!moveTimeline.HasCommands && !moveYTimeline.HasCommands)
                writer.WriteLine($",{ InitialPosition.Y.ToString(exportSettings.NumberFormat)}");
            else writer.WriteLine($",0");
        }

        public override double StartTime => CommandsStartTime;
        public override double EndTime => CommandsEndTime;

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            if (commands.Count == 0)
                return;

            WriteHeader(writer, exportSettings, layer);
            foreach (var command in commands)
                command.WriteOsb(writer, exportSettings, 1);
        }
    }

    public enum OsbLayer
    {
        Background,
        Fail,
        Pass,
        Foreground,
    }

    public enum OsbOrigin
    {
        TopLeft,
        TopCentre,
        TopRight,
        CentreLeft,
        Centre,
        CentreRight,
        BottomLeft,
        BottomCentre,
        BottomRight,
    }

    public enum OsbLoopType
    {
        LoopForever,
        LoopOnce,
    }

    public enum OsbEasing
    {
        None,
        Out,
        In,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InSine,
        OutSine,
        InOutSine,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InElastic,
        OutElastic,
        OutElasticHalf,
        OutElasticQuarter,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce,
    }

    public enum ParameterType
    {
        None,
        FlipHorizontal,
        FlipVertical,
        AdditiveBlending,
    }
}
