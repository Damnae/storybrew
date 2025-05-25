using BrewLib.Graphics.Drawables;
using OpenTK;
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;
using StorybrewCommon.Util;

namespace StorybrewCommon.Storyboarding
{
    public class OsbSprite : StoryboardObject
    {
        public static readonly Vector2 DefaultPosition = new Vector2(320, 240);

        private readonly List<ICommand> commands = new List<ICommand>();
        private CommandGroup currentCommandGroup;
        public bool InGroup => currentCommandGroup != null;
        public bool HasTrigger;

        /// <summary>
        /// If this sprite contains more than CommandSplitThreshold commands, they will be split between multiple sprites.
        /// Does not apply when the sprite has triggers. No currently implemented.
        /// </summary>
        public int CommandSplitThreshold = 0;

        private string texturePath = "";
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
        public int CommandCost => commands.Sum(c => c.Cost);

        public bool HasIncompatibleCommands =>
            (moveTimeline.HasCommands && (moveXTimeline.HasCommands || moveYTimeline.HasCommands)) ||
            (scaleTimeline.HasCommands && scaleVecTimeline.HasCommands);

        public bool HasOverlappedCommands =>
            moveTimeline.HasOverlap ||
            moveXTimeline.HasOverlap ||
            moveYTimeline.HasOverlap ||
            scaleTimeline.HasOverlap ||
            scaleVecTimeline.HasOverlap ||
            rotateTimeline.HasOverlap ||
            fadeTimeline.HasOverlap ||
            colorTimeline.HasOverlap ||
            additiveTimeline.HasOverlap ||
            flipHTimeline.HasOverlap ||
            flipVTimeline.HasOverlap;

        public bool HasRotateCommands => rotateTimeline.HasCommands;
        public bool HasScalingCommands => scaleTimeline.HasCommands || scaleVecTimeline.HasCommands;
        public bool HasMoveXYCommands => moveXTimeline.HasCommands || moveYTimeline.HasCommands;

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

        private double displayStartTime = double.MinValue;
        public double DisplayStartTime
        {
            get
            {
                if (displayStartTime == double.MinValue)
                    refreshStartEndTimes();
                return displayStartTime;
            }
        }

        private double displayEndTime = double.MaxValue;
        public double DisplayEndTime
        {
            get
            {
                if (displayEndTime == double.MaxValue)
                    refreshStartEndTimes();
                return displayEndTime;
            }
        }

        private void refreshStartEndTimes()
        {
            clearStartEndTimes();

            foreach (var command in commands)
            {
                commandsStartTime = Math.Min(commandsStartTime, command.StartTime);
                commandsEndTime = Math.Max(commandsEndTime, command.EndTime);
            }

            if (!HasTrigger)
            {
                if (fadeTimeline.HasCommands)
                {
                    var start = fadeTimeline.StartResult;
                    if (start.StartValue == 0)
                        displayStartTime = Math.Max(displayStartTime, start.StartTime);

                    var end = fadeTimeline.EndResult;
                    if (end.EndValue == 0)
                        displayEndTime = Math.Min(displayEndTime, end.EndTime);
                }
                if (scaleTimeline.HasCommands)
                {
                    var start = scaleTimeline.StartResult;
                    if (start.StartValue == 0)
                        displayStartTime = Math.Max(displayStartTime, start.StartTime);

                    var end = scaleTimeline.EndResult;
                    if (end.EndValue == 0)
                        displayEndTime = Math.Min(displayEndTime, end.EndTime);
                }
                if (scaleVecTimeline.HasCommands)
                {
                    var start = scaleVecTimeline.StartResult;
                    if (start.StartValue.X <= 0 || start.StartValue.Y <= 0)
                        displayStartTime = Math.Max(displayStartTime, start.StartTime);

                    var end = scaleVecTimeline.EndResult;
                    if (end.EndValue.X <= 0 || end.EndValue.Y <= 0)
                        displayEndTime = Math.Min(displayEndTime, end.EndTime);
                }
            }
            displayStartTime = Math.Max(displayStartTime, commandsStartTime);
            displayEndTime = Math.Min(displayEndTime, commandsEndTime);
        }

        private void clearStartEndTimes()
        {
            commandsStartTime = double.MaxValue;
            commandsEndTime = double.MinValue;
            displayStartTime = double.MinValue;
            displayEndTime = double.MaxValue;
        }

        public OsbSprite()
        {
            initializeDisplayTimelines();
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
        public void FlipH(double time) => FlipH(time, time);
        public void FlipV(double startTime, double endTime) => Parameter(OsbEasing.None, startTime, endTime, CommandParameter.FlipVertical);
        public void FlipV(double time) => FlipV(time, time);
        public void Additive(double startTime, double endTime) => Parameter(OsbEasing.None, startTime, endTime, CommandParameter.AdditiveBlending);
        public void Additive(double time) => Additive(time, time);

        public LoopCommand StartLoopGroup(double startTime, int loopCount)
        {
            var loopCommand = new LoopCommand(startTime, loopCount);
            addCommand(loopCommand);
            startDisplayGroup(loopCommand);
            return loopCommand;
        }

        public TriggerCommand StartTriggerGroup(string triggerName, double startTime, double endTime, int group = 0)
        {
            var triggerCommand = new TriggerCommand(triggerName, startTime, endTime, group);
            addCommand(triggerCommand);
            startDisplayGroup(triggerCommand);
            HasTrigger = true;
            return triggerCommand;
        }

        public void EndGroup()
        {
            currentCommandGroup.EndGroup();
            currentCommandGroup = null;

            endDisplayGroup();
        }

        private void addCommand(ICommand command)
        {
            if (command is CommandGroup commandGroup)
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

        public void AddCommand(ICommand command)
        {
            if (command is ColorCommand colorCommand)
                Color(colorCommand.Easing, colorCommand.StartTime, colorCommand.EndTime, colorCommand.StartValue, colorCommand.EndValue);
            else if (command is FadeCommand fadeCommand)
                Fade(fadeCommand.Easing, fadeCommand.StartTime, fadeCommand.EndTime, fadeCommand.StartValue, fadeCommand.EndValue);
            else if (command is ScaleCommand scaleCommand)
                Scale(scaleCommand.Easing, scaleCommand.StartTime, scaleCommand.EndTime, scaleCommand.StartValue, scaleCommand.EndValue);
            else if (command is VScaleCommand vScaleCommand)
                ScaleVec(vScaleCommand.Easing, vScaleCommand.StartTime, vScaleCommand.EndTime, vScaleCommand.StartValue, vScaleCommand.EndValue);
            else if (command is ParameterCommand parameterCommand)
                Parameter(parameterCommand.Easing, parameterCommand.StartTime, parameterCommand.EndTime, parameterCommand.StartValue);
            else if (command is MoveCommand moveCommand)
                Move(moveCommand.Easing, moveCommand.StartTime, moveCommand.EndTime, moveCommand.StartValue, moveCommand.EndValue);
            else if (command is MoveXCommand moveXCommand)
                MoveX(moveXCommand.Easing, moveXCommand.StartTime, moveXCommand.EndTime, moveXCommand.StartValue, moveXCommand.EndValue);
            else if (command is MoveYCommand moveYCommand)
                MoveY(moveYCommand.Easing, moveYCommand.StartTime, moveYCommand.EndTime, moveYCommand.StartValue, moveYCommand.EndValue);
            else if (command is RotateCommand rotateCommand)
                Rotate(rotateCommand.Easing, rotateCommand.StartTime, rotateCommand.EndTime, rotateCommand.StartValue, rotateCommand.EndValue);
            else if (command is LoopCommand loopCommand)
            {
                StartLoopGroup(loopCommand.StartTime, loopCommand.LoopCount);
                foreach (var cmd in loopCommand.Commands)
                    AddCommand(cmd);
                EndGroup();
            }
            else if (command is TriggerCommand triggerCommand)
            {
                StartTriggerGroup(triggerCommand.TriggerName, triggerCommand.StartTime, triggerCommand.EndTime, triggerCommand.Group);
                foreach (var cmd in triggerCommand.Commands)
                    AddCommand(cmd);
                EndGroup();
            }
            else throw new NotSupportedException($"Failed to add command: No support for adding command of type {command.GetType().FullName}");
        }

        #region Display 

        private readonly List<(Predicate<ICommand> Condition, CommandTimeline Timeline)> displayTimelines = [];

        private readonly CommandTimeline<CommandPosition> moveTimeline = new();
        private readonly CommandTimeline<CommandDecimal> moveXTimeline = new();
        private readonly CommandTimeline<CommandDecimal> moveYTimeline = new();
        private readonly CommandTimeline<CommandDecimal> scaleTimeline = new(1);
        private readonly CommandTimeline<CommandScale> scaleVecTimeline = new(Vector2.One);
        private readonly CommandTimeline<CommandDecimal> rotateTimeline = new();
        private readonly CommandTimeline<CommandDecimal> fadeTimeline = new(1);
        private readonly CommandTimeline<CommandColor> colorTimeline = new(CommandColor.FromRgb(255, 255, 255));
        private readonly CommandTimeline<CommandParameter> additiveTimeline = new(CommandParameter.None);
        private readonly CommandTimeline<CommandParameter> flipHTimeline = new(CommandParameter.None);
        private readonly CommandTimeline<CommandParameter> flipVTimeline = new(CommandParameter.None);

        public CommandPosition PositionAt(double time) => moveTimeline.HasCommands ? moveTimeline.ValueAtTime(time) : new CommandPosition(moveXTimeline.ValueAtTime(time), moveYTimeline.ValueAtTime(time));
        public CommandScale ScaleAt(double time) => scaleVecTimeline.HasCommands ? scaleVecTimeline.ValueAtTime(time) : new CommandScale(scaleTimeline.ValueAtTime(time));
        public CommandDecimal RotationAt(double time) => rotateTimeline.ValueAtTime(time);
        public CommandDecimal OpacityAt(double time) => fadeTimeline.ValueAtTime(time);
        public CommandColor ColorAt(double time) => colorTimeline.ValueAtTime(time);
        public CommandParameter AdditiveAt(double time) => additiveTimeline.ValueAtTime(time);
        public CommandParameter FlipHAt(double time) => flipHTimeline.ValueAtTime(time);
        public CommandParameter FlipVAt(double time) => flipVTimeline.ValueAtTime(time);

        private void initializeDisplayTimelines()
        {
            displayTimelines.Add(new(c => c is MoveCommand, moveTimeline));
            displayTimelines.Add(new(c => c is MoveXCommand, moveXTimeline));
            displayTimelines.Add(new(c => c is MoveYCommand, moveYTimeline));
            displayTimelines.Add(new(c => c is ScaleCommand, scaleTimeline));
            displayTimelines.Add(new(c => c is VScaleCommand, scaleVecTimeline));
            displayTimelines.Add(new(c => c is RotateCommand, rotateTimeline));
            displayTimelines.Add(new(c => c is FadeCommand, fadeTimeline));
            displayTimelines.Add(new(c => c is ColorCommand, colorTimeline));
            displayTimelines.Add(new(c => c is ParameterCommand { StartValue.Type: ParameterType.AdditiveBlending }, additiveTimeline));
            displayTimelines.Add(new(c => c is ParameterCommand { StartValue.Type: ParameterType.FlipHorizontal }, flipHTimeline));
            displayTimelines.Add(new(c => c is ParameterCommand { StartValue.Type: ParameterType.FlipVertical }, flipVTimeline));
        }

        private void addDisplayCommand(ICommand command)
        {
            foreach (var (checkCondition, timeline) in displayTimelines)
                if (checkCondition(command))
                    timeline.Add(command);
        }

        private void startDisplayGroup(LoopCommand loopCommand)
        {
            foreach (var (_, timeline) in displayTimelines)
                timeline.StartGroup(loopCommand);
        }

        private void startDisplayGroup(TriggerCommand triggerCommand)
        {
            foreach (var (_, timeline) in displayTimelines)
                timeline.StartGroup(triggerCommand);
        }

        private void endDisplayGroup()
        {
            foreach (var (_, timeline) in displayTimelines)
                timeline.EndGroup();
        }

        #endregion

        public bool IsActive(double time) => CommandsStartTime <= time && time <= CommandsEndTime;
        public bool ShouldBeActive(double time) => DisplayStartTime <= time && time <= DisplayEndTime;

        public override double StartTime => CommandsStartTime;
        public override double EndTime => CommandsEndTime;

        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer, StoryboardTransform transform)
        {
            if (CommandCount == 0)
                return;

            WriteHeader(writer, exportSettings, layer, transform);
            foreach (var command in Commands)
                command.WriteOsb(writer, exportSettings, transform, 1);
        }

        protected virtual void WriteHeader(TextWriter writer, ExportSettings exportSettings, OsbLayer layer, StoryboardTransform transform)
        {
            writer.Write("Sprite,");
            WriteHeaderCommon(writer, exportSettings, layer, transform);
            writer.WriteLine();
        }

        protected virtual void WriteHeaderCommon(TextWriter writer, ExportSettings exportSettings, OsbLayer layer, StoryboardTransform transform)
        {
            var transformedInitialPosition = transform == null ? InitialPosition :
                HasMoveXYCommands ?
                    transform.ApplyToPositionXY(InitialPosition) :
                    transform.ApplyToPosition(InitialPosition);

            writer.Write($"{layer},{Origin},\"{TexturePath.Trim()}\"");
            if (!moveTimeline.HasCommands && !moveXTimeline.HasCommands)
                writer.Write($",{transformedInitialPosition.X.ToString(exportSettings.NumberFormat)}");
            else writer.Write($",0");
            if (!moveTimeline.HasCommands && !moveYTimeline.HasCommands)
                writer.Write($",{transformedInitialPosition.Y.ToString(exportSettings.NumberFormat)}");
            else writer.Write($",0");
        }

        public static bool InScreenBounds(Vector2 position, Vector2 size, float rotation, Vector2 origin)
            => new OrientedBoundingBox(position, origin, size.X, size.Y, rotation)
                .Intersects(OsuHitObject.WidescreenStoryboardBounds);

        public static Vector2 GetOriginVector(OsbOrigin origin, float width, float height)
        {
            switch (origin)
            {
                case OsbOrigin.TopLeft: return Vector2.Zero;
                case OsbOrigin.TopCentre: return new Vector2(width * 0.5f, 0);
                case OsbOrigin.TopRight: return new Vector2(width, 0);
                case OsbOrigin.CentreLeft: return new Vector2(0, height * 0.5f);
                case OsbOrigin.Centre: return new Vector2(width * 0.5f, height * 0.5f);
                case OsbOrigin.CentreRight: return new Vector2(width, height * 0.5f);
                case OsbOrigin.BottomLeft: return new Vector2(0, height);
                case OsbOrigin.BottomCentre: return new Vector2(width * 0.5f, height);
                case OsbOrigin.BottomRight: return new Vector2(width, height);
            }
            throw new NotSupportedException(origin.ToString());
        }
    }

    public enum OsbLayer
    {
        Background,
        Fail,
        Pass,
        Foreground,
        Overlay,
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
