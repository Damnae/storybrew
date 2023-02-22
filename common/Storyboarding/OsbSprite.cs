using OpenTK;
using StorybrewCommon.Mapset;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Storyboarding.Display;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Base sprite in storyboards. </summary>
    public class OsbSprite : StoryboardObject
    {
        ///<summary> Default position of sprites, unless modified elsewhere. </summary>
        public static readonly Vector2 DefaultPosition = new Vector2(320, 240);

        readonly List<ICommand> commands = new List<ICommand>();
        CommandGroup currentCommandGroup;

        ///<returns> True if the sprite is in a command group, else returns false. </returns>
        public bool InGroup => currentCommandGroup != null;

        ///<summary> If the sprite has more commands than <see cref="CommandSplitThreshold"/>, they will be split between multiple sprites. </summary>
        ///<remarks> Does not apply when the sprite has triggers. </remarks>
        public int CommandSplitThreshold = 0;

        string texturePath = "";
        ///<returns> The path to the image of the <see cref="OsbSprite"/>. </returns>
        public string TexturePath
        {
            get => texturePath;
            set
            {
                if (texturePath == value) return;
                new FileInfo(value);
                texturePath = value;
            }
        }
        ///<returns> Image of the sprite at <paramref name="time"/>. </returns>
        public virtual string GetTexturePathAt(double time) => TexturePath;

        ///<summary> Origin of this sprite. </summary>
        public OsbOrigin Origin = OsbOrigin.Centre;

        Vector2 initialPosition;

        ///<returns> The initial position of the <see cref="OsbSprite"/>. </returns>
        public Vector2 InitialPosition
        {
            get => initialPosition;
            set
            {
                if (initialPosition == value) return;
                initialPosition = value;
                moveTimeline.DefaultValue = initialPosition;
                moveXTimeline.DefaultValue = initialPosition.X;
                moveYTimeline.DefaultValue = initialPosition.Y;
            }
        }

        ///<summary> Gets the list of commands on this sprite. </summary>
        public IEnumerable<ICommand> Commands => commands;

        ///<returns> The total amount of commands being run on this instance of the <see cref="OsbSprite"/>. </returns>
        public int CommandCount => commands.Count;

        ///<returns> The sum of commands being run on this instance of the <see cref="OsbSprite"/>. </returns>
        public int CommandCost => commands.Sum(c => c.Cost);

        ///<returns> True if the <see cref="OsbSprite"/> has incompatible commands, else returns false. </returns>
        public bool HasIncompatibleCommands =>
            (moveTimeline.HasCommands && (moveXTimeline.HasCommands || moveYTimeline.HasCommands)) ||
            (scaleTimeline.HasCommands && scaleVecTimeline.HasCommands);

        ///<returns> True if the <see cref="OsbSprite"/> has overlapping commands, else returns false. </returns>
        public bool HasOverlappedCommands =>
            moveTimeline.HasOverlap || moveXTimeline.HasOverlap || moveYTimeline.HasOverlap ||
            scaleTimeline.HasOverlap || scaleVecTimeline.HasOverlap ||
            rotateTimeline.HasOverlap ||
            fadeTimeline.HasOverlap ||
            colorTimeline.HasOverlap ||
            additiveTimeline.HasOverlap || flipHTimeline.HasOverlap || flipVTimeline.HasOverlap;

        double commandsStartTime = double.MaxValue;
        double commandsEndTime = double.MinValue;

        ///<returns> The start time, in milliseconds, of this instance of the <see cref="OsbSprite"/>. </returns>
        public double CommandsStartTime
        {
            get
            {
                if (commandsStartTime == double.MaxValue) refreshStartEndTimes();
                return commandsStartTime;
            }
        }
        ///<returns> The end time, in milliseconds, of this instance of the <see cref="OsbSprite"/>. </returns>
        public double CommandsEndTime
        {
            get
            {
                if (commandsEndTime == double.MinValue) refreshStartEndTimes();
                return commandsEndTime;
            }
        }
        void refreshStartEndTimes()
        {
            clearStartEndTimes();
            foreach (var command in commands)
            {
                if (!command.Active) continue;
                commandsStartTime = Math.Min(commandsStartTime, command.StartTime);
                commandsEndTime = Math.Max(commandsEndTime, command.EndTime);
            }
        }
        void clearStartEndTimes()
        {
            commandsStartTime = double.MaxValue;
            commandsEndTime = double.MinValue;
        }

        ///<summary> Constructs a new abstract sprite. </summary>
        public OsbSprite()
        {
            initializeDisplayValueBuilders();
            InitialPosition = DefaultPosition;
        }

        //==========M==========//
        ///<summary> Change the position of an <see cref="OsbSprite"/> over time. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startPosition"> Start <see cref="CommandPosition"/> value of the command. </param>
        ///<param name="endPosition"> End <see cref="CommandPosition"/> value of the command. </param>
        public void Move(OsbEasing easing, double startTime, double endTime, CommandPosition startPosition, CommandPosition endPosition) => addCommand(new MoveCommand(easing, startTime, endTime, startPosition, endPosition));

        ///<summary> Change the position of an <see cref="OsbSprite"/> over time. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startPosition"> Start <see cref="CommandPosition"/> value of the command. </param>
        ///<param name="endX"> End-X value of the command. </param>
        ///<param name="endY"> End-Y value of the command. </param>
        public void Move(OsbEasing easing, double startTime, double endTime, CommandPosition startPosition, double endX, double endY) => Move(easing, startTime, endTime, startPosition, new CommandPosition(endX, endY));

        ///<summary> Change the position of an <see cref="OsbSprite"/> over time. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startX"> Start-X value of the command. </param>
        ///<param name="startY"> Start-Y value of the command. </param>
        ///<param name="endX"> End-X value of the command. </param>
        ///<param name="endY"> End-Y value of the command. </param>
        public void Move(OsbEasing easing, double startTime, double endTime, double startX, double startY, double endX, double endY) => Move(easing, startTime, endTime, new CommandPosition(startX, startY), new CommandPosition(endX, endY));

        ///<summary> Change the position of an <see cref="OsbSprite"/> over time. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startPosition"> Start <see cref="CommandPosition"/> value of the command. </param>
        ///<param name="endPosition"> End <see cref="CommandPosition"/> value of the command. </param>
        public void Move(double startTime, double endTime, CommandPosition startPosition, CommandPosition endPosition) => Move(OsbEasing.None, startTime, endTime, startPosition, endPosition);

        ///<summary> Change the position of an <see cref="OsbSprite"/> over time. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startPosition"> Start <see cref="CommandPosition"/> value of the command. </param>
        ///<param name="endX"> End-X value of the command. </param>
        ///<param name="endY"> End-Y value of the command. </param>
        public void Move(double startTime, double endTime, CommandPosition startPosition, double endX, double endY) => Move(OsbEasing.None, startTime, endTime, startPosition, endX, endY);

        ///<summary> Change the position of an <see cref="OsbSprite"/> over time. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startX"> Start-X value of the command. </param>
        ///<param name="startY"> Start-Y value of the command. </param>
        ///<param name="endX"> End-X value of the command. </param>
        ///<param name="endY"> End-Y value of the command. </param>
        public void Move(double startTime, double endTime, double startX, double startY, double endX, double endY) => Move(OsbEasing.None, startTime, endTime, startX, startY, endX, endY);

        ///<summary> Sets the position of an <see cref="OsbSprite"/>. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="time"> Time of the command. </param>
        ///<param name="position"> <see cref="CommandPosition"/> value of the command. </param>
        public void Move(double time, CommandPosition position) => Move(time, time, position, position);

        ///<summary> Sets the position of an <see cref="OsbSprite"/>. Commands similar to MoveX are available for MoveY. </summary>
        ///<remarks> Cannot be used with <see cref="MoveXCommand"/> or <see cref="MoveYCommand"/>. </remarks>
        ///<param name="time"> Time of the command. </param>
        ///<param name="x"> X value of the command. </param>
        ///<param name="y"> Y value of the command. </param>
        public void Move(double time, double x, double y) => Move(time, time, x, y, x, y);

        //==========MX==========//
        ///<summary> Change the x-position of a <see cref="OsbSprite"/> over time. Commands are also available for MoveY.</summary>
        ///<remarks> Cannot be used with <see cref="MoveCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startX"> Start-X value of the command. </param>
        ///<param name="endX"> End-X value of the command. </param>
        public void MoveX(OsbEasing easing, double startTime, double endTime, CommandDecimal startX, CommandDecimal endX) => addCommand(new MoveXCommand(easing, startTime, endTime, startX, endX));

        ///<summary> Change the x-position of a <see cref="OsbSprite"/> over time. Commands are also available for MoveY.</summary>
        ///<remarks> Cannot be used with <see cref="MoveCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startX"> Start-X value of the command. </param>
        ///<param name="endX"> End-X value of the command. </param>
        public void MoveX(double startTime, double endTime, CommandDecimal startX, CommandDecimal endX) => MoveX(OsbEasing.None, startTime, endTime, startX, endX);

        ///<summary> Sets the X-Position of an <see cref="OsbSprite"/>. Commands are also available for MoveY.</summary>
        ///<remarks> Cannot be used with <see cref="MoveCommand"/>. </remarks>
        ///<param name="time"> Time of the command. </param>
        ///<param name="x"> X value of the command. </param>
        public void MoveX(double time, CommandDecimal x) => MoveX(time, time, x, x);

        //==========MY==========//
        ///<summary> Change the Y-Position of an <see cref="OsbSprite"/> over time. Commands are also available for MoveX. </summary>
        ///<remarks> Cannot be used with <see cref="MoveCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startY"> Start-Y value of the command. </param>
        ///<param name="endY"> End-Y value of the command. </param>
        public void MoveY(OsbEasing easing, double startTime, double endTime, CommandDecimal startY, CommandDecimal endY) => addCommand(new MoveYCommand(easing, startTime, endTime, startY, endY));

        ///<summary> Change the Y-Position of an <see cref="OsbSprite"/> over time. Commands are also available for MoveX. </summary>
        ///<remarks> Cannot be used with <see cref="MoveCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startY"> Start-Y value of the command. </param>
        ///<param name="endY"> End-Y value of the command. </param>
        public void MoveY(double startTime, double endTime, CommandDecimal startY, CommandDecimal endY) => MoveY(OsbEasing.None, startTime, endTime, startY, endY);

        ///<summary> Sets the Y-Position of an <see cref="OsbSprite"/>. Commands are also available for MoveX. </summary>
        ///<remarks> Cannot be used with <see cref="MoveCommand"/>. </remarks>
        ///<param name="time"> Time of the command. </param>
        ///<param name="y"> Y value of the command. </param>
        public void MoveY(double time, CommandDecimal y) => MoveY(time, time, y, y);

        //==========S==========//
        ///<summary> Change the size of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="VScaleCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startScale"> Start scale of the command. </param>
        ///<param name="endScale"> End scale of the command. </param>
        public void Scale(OsbEasing easing, double startTime, double endTime, CommandDecimal startScale, CommandDecimal endScale) => addCommand(new ScaleCommand(easing, startTime, endTime, startScale, endScale));

        ///<summary> Change the size of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="VScaleCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startScale"> Start scale of the command. </param>
        ///<param name="endScale"> End scale of the command. </param>
        public void Scale(double startTime, double endTime, CommandDecimal startScale, CommandDecimal endScale) => Scale(OsbEasing.None, startTime, endTime, startScale, endScale);

        ///<summary> Sets the size of a sprite. </summary>
        ///<remarks> Cannot be used with <see cref="VScaleCommand"/>. </remarks>
        ///<param name="time"> Time of the command. </param>
        ///<param name="scale"> Scale of the command. </param>
        public void Scale(double time, CommandDecimal scale) => Scale(time, time, scale, scale);

        //==========V==========//
        ///<summary> Change the vector scale of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startScale"> Start <see cref="CommandScale"/> value of the command. </param>
        ///<param name="endScale"> End <see cref="CommandScale"/> value of the command. </param>
        public void ScaleVec(OsbEasing easing, double startTime, double endTime, CommandScale startScale, CommandScale endScale) => addCommand(new VScaleCommand(easing, startTime, endTime, startScale, endScale));

        ///<summary> Change the vector scale of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startScale"> Start <see cref="CommandScale"/> value of the command. </param>
        ///<param name="endX"> End X-Scale value of the command. </param>
        ///<param name="endY"> End Y-Scale value of the command. </param>
        public void ScaleVec(OsbEasing easing, double startTime, double endTime, CommandScale startScale, double endX, double endY) => ScaleVec(easing, startTime, endTime, startScale, new CommandScale(endX, endY));

        ///<summary> Change the vector scale of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startX"> Start X-Scale value of the command. </param>
        ///<param name="startY"> Start Y-Scale value of the command. </param>
        ///<param name="endX"> End X-Scale value of the command. </param>
        ///<param name="endY"> End Y-Scale value of the command. </param>
        public void ScaleVec(OsbEasing easing, double startTime, double endTime, double startX, double startY, double endX, double endY) => ScaleVec(easing, startTime, endTime, new CommandScale(startX, startY), new CommandScale(endX, endY));

        ///<summary> Change the vector scale of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startScale"> Start <see cref="CommandScale"/> value of the command. </param>
        ///<param name="endScale"> End <see cref="CommandScale"/> value of the command. </param>
        public void ScaleVec(double startTime, double endTime, CommandScale startScale, CommandScale endScale) => ScaleVec(OsbEasing.None, startTime, endTime, startScale, endScale);

        ///<summary> Change the vector scale of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startScale"> Start <see cref="CommandScale"/> value of the command. </param>
        ///<param name="endX"> End X-Scale value of the command. </param>
        ///<param name="endY"> End Y-Scale value of the command. </param>
        public void ScaleVec(double startTime, double endTime, CommandScale startScale, double endX, double endY) => ScaleVec(OsbEasing.None, startTime, endTime, startScale, endX, endY);

        ///<summary> Change the vector scale of a sprite over time. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startX"> Start X-Scale value of the command. </param>
        ///<param name="startY"> Start Y-Scale value of the command. </param>
        ///<param name="endX"> End X-Scale value of the command. </param>
        ///<param name="endY"> End Y-Scale value of the command. </param>
        public void ScaleVec(double startTime, double endTime, double startX, double startY, double endX, double endY) => ScaleVec(OsbEasing.None, startTime, endTime, startX, startY, endX, endY);

        ///<summary> Sets the vector scale of a sprite. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="time"> Time of the command. </param>
        ///<param name="scale"> <see cref="CommandScale"/> value of the command. </param>
        public void ScaleVec(double time, CommandScale scale) => ScaleVec(time, time, scale, scale);

        ///<summary> Sets the vector scale of a sprite. </summary>
        ///<remarks> Cannot be used with <see cref="ScaleCommand"/>. </remarks>
        ///<param name="time"> Time of the command. </param>
        ///<param name="x"> Scale-X value of the command. </param>
        ///<param name="y"> Scale-Y value of the command. </param>
        public void ScaleVec(double time, double x, double y) => ScaleVec(time, time, x, y, x, y);

        //==========R==========//
        ///<summary> Change the rotation of an <see cref="OsbSprite"/> over time. Angles are in radians. </summary>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startRotation"> Start radians of the command. </param>
        ///<param name="endRotation"> End radians of the command. </param>
        public void Rotate(OsbEasing easing, double startTime, double endTime, CommandDecimal startRotation, CommandDecimal endRotation) => addCommand(new RotateCommand(easing, startTime, endTime, startRotation, endRotation));

        ///<summary> Change the rotation of an <see cref="OsbSprite"/> over time. Angles are in radians. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startRotation"> Start radians of the command. </param>
        ///<param name="endRotation"> End radians of the command. </param>
        public void Rotate(double startTime, double endTime, CommandDecimal startRotation, CommandDecimal endRotation) => Rotate(OsbEasing.None, startTime, endTime, startRotation, endRotation);

        ///<summary> Sets the rotation of an <see cref="OsbSprite"/>. Angles are in radians. </summary>
        ///<param name="time"> Time of the command. </param>
        ///<param name="rotation"> Radians of the command. </param>
        public void Rotate(double time, CommandDecimal rotation) => Rotate(time, time, rotation, rotation);

        //==========F==========//
        ///<summary> Change the opacity of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startFade"> Start fade value of the command. </param>
        ///<param name="endFade"> End fade value of the command. </param>
        public void Fade(OsbEasing easing, double startTime, double endTime, CommandDecimal startFade, CommandDecimal endFade) => addCommand(new FadeCommand(easing, startTime, endTime, startFade, endFade));

        ///<summary> Change the opacity of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startFade"> Start fade value of the command. </param>
        ///<param name="endFade"> End fade value of the command. </param>
        public void Fade(double startTime, double endTime, CommandDecimal startFade, CommandDecimal endFade) => Fade(OsbEasing.None, startTime, endTime, startFade, endFade);

        ///<summary> Sets the opacity of an <see cref="OsbSprite"/>. </summary>
        ///<param name="time"> Time of the command. </param>
        ///<param name="fade"> Fade value of the command. </param>
        public void Fade(double time, CommandDecimal fade) => Fade(time, time, fade, fade);

        //==========C==========//
        ///<summary> Change the RGB color of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startColor"> Start <see cref="CommandColor"/> value of the command. </param>
        ///<param name="endColor"> End <see cref="CommandColor"/> value of the command. </param>
        public void Color(OsbEasing easing, double startTime, double endTime, CommandColor startColor, CommandColor endColor) => addCommand(new ColorCommand(easing, startTime, endTime, startColor, endColor));

        ///<summary> Change the RGB color of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startColor"> Start <see cref="CommandColor"/> value of the command. </param>
        ///<param name="endR"> End red value of the command. </param>
        ///<param name="endG"> End green value of the command. </param>
        ///<param name="endB"> End blue value of the command. </param>
        public void Color(OsbEasing easing, double startTime, double endTime, CommandColor startColor, double endR, double endG, double endB) => Color(easing, startTime, endTime, startColor, new CommandColor(endR, endG, endB));

        ///<summary> Change the RGB color of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startR"> Start red value of the command. </param>
        ///<param name="startG"> Start green value of the command. </param>
        ///<param name="startB"> Start blue value of the command. </param>
        ///<param name="endR"> End red value of the command. </param>
        ///<param name="endG"> End green value of the command. </param>
        ///<param name="endB"> End blue value of the command. </param>
        public void Color(OsbEasing easing, double startTime, double endTime, double startR, double startG, double startB, double endR, double endG, double endB) => Color(easing, startTime, endTime, new CommandColor(startR, startG, startB), new CommandColor(endR, endG, endB));

        ///<summary> Change the RGB color of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startColor"> Start <see cref="CommandColor"/> value of the command. </param>
        ///<param name="endColor"> End <see cref="CommandColor"/> value of the command. </param>
        public void Color(double startTime, double endTime, CommandColor startColor, CommandColor endColor) => Color(OsbEasing.None, startTime, endTime, startColor, endColor);

        ///<summary> Change the RGB color of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startColor"> Start <see cref="CommandColor"/> value of the command. </param>
        ///<param name="endR"> End red value of the command. </param>
        ///<param name="endG"> End green value of the command. </param>
        ///<param name="endB"> End blue value of the command. </param>
        public void Color(double startTime, double endTime, CommandColor startColor, double endR, double endG, double endB) => Color(OsbEasing.None, startTime, endTime, startColor, endR, endG, endB);

        ///<summary> Change the RGB color of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startR"> Start red value of the command. </param>
        ///<param name="startG"> Start green value of the command. </param>
        ///<param name="startB"> Start blue value of the command. </param>
        ///<param name="endR"> End red value of the command. </param>
        ///<param name="endG"> End green value of the command. </param>
        ///<param name="endB"> End blue value of the command. </param>
        public void Color(double startTime, double endTime, double startR, double startG, double startB, double endR, double endG, double endB) => Color(OsbEasing.None, startTime, endTime, startR, startG, startB, endR, endG, endB);

        ///<summary> Sets the RGB color of an <see cref="OsbSprite"/>. </summary>
        ///<param name="time"> Time of the command. </param>
        ///<param name="color"> The <see cref="CommandColor"/> value of the command. </param>
        public void Color(double time, CommandColor color) => Color(time, time, color, color);

        ///<summary> Sets the RGB color of an <see cref="OsbSprite"/>. </summary>
        ///<param name="time"> Time of the command. </param>
        ///<param name="r"> Red value of the command. </param>
        ///<param name="g"> Green value of the command. </param>
        ///<param name="b"> Blue value of the command. </param>
        public void Color(double time, double r, double g, double b) => Color(time, time, r, g, b, r, g, b);

        ///<summary> Changes the hue, saturation, and brightness of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startColor"> Start <see cref="CommandColor"/> value of the command. </param>
        ///<param name="endH"> End hue value (in degrees) of the command. </param>
        ///<param name="endS"> End saturation value (from 0 to 1) of the command. </param>
        ///<param name="endB"> End brightness level (from 0 to 1) of the command. </param>
        public void ColorHsb(OsbEasing easing, double startTime, double endTime, CommandColor startColor, double endH, double endS, double endB) => Color(easing, startTime, endTime, startColor, CommandColor.FromHsb(endH, endS, endB));

        ///<summary> Changes the hue, saturation, and brightness of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="easing"> <see cref="OsbEasing"/> to be applied to the command. </param>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startH"> Start hue value (in degrees) of the command. </param>
        ///<param name="startS"> Start saturation value (from 0 to 1) of the command. </param>
        ///<param name="startB"> Start brightness level (from 0 to 1) of the command. </param>
        ///<param name="endH"> End hue value (in degrees) of the command. </param>
        ///<param name="endS"> End saturation value (from 0 to 1) of the command. </param>
        ///<param name="endB"> End brightness level (from 0 to 1) of the command. </param>
        public void ColorHsb(OsbEasing easing, double startTime, double endTime, double startH, double startS, double startB, double endH, double endS, double endB) => Color(easing, startTime, endTime, CommandColor.FromHsb(startH, startS, startB), CommandColor.FromHsb(endH, endS, endB));

        ///<summary> Changes the hue, saturation, and brightness of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startColor"> Start <see cref="CommandColor"/> value of the command. </param>
        ///<param name="endH"> End hue value (in degrees) of the command. </param>
        ///<param name="endS"> End saturation value (from 0 to 1) of the command. </param>
        ///<param name="endB"> End brightness level (from 0 to 1) of the command. </param>
        public void ColorHsb(double startTime, double endTime, CommandColor startColor, double endH, double endS, double endB) => ColorHsb(OsbEasing.None, startTime, endTime, startColor, endH, endS, endB);

        ///<summary> Changes the hue, saturation, and brightness of an <see cref="OsbSprite"/> over time. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="startH"> Start hue value (in degrees) of the command. </param>
        ///<param name="startS"> Start saturation value (from 0 to 1) of the command. </param>
        ///<param name="startB"> Start brightness level (from 0 to 1) of the command. </param>
        ///<param name="endH"> End hue value (in degrees) of the command. </param>
        ///<param name="endS"> End saturation value (from 0 to 1) of the command. </param>
        ///<param name="endB"> End brightness level (from 0 to 1) of the command. </param>
        public void ColorHsb(double startTime, double endTime, double startH, double startS, double startB, double endH, double endS, double endB) => ColorHsb(OsbEasing.None, startTime, endTime, startH, startS, startB, endH, endS, endB);

        ///<summary> Sets the hue, saturation, and brightness of an <see cref="OsbSprite"/>. </summary>
        ///<param name="time"> Time of the command. </param>
        ///<param name="h"> Hue value (in degrees) of the command. </param>
        ///<param name="s"> Saturation value (from 0 to 1) of the command. </param>
        ///<param name="b"> Brightness level (from 0 to 1) of the command. </param>
        public void ColorHsb(double time, double h, double s, double b) => ColorHsb(time, time, h, s, b, h, s, b);

        //==========P==========//
        ///<summary> Applies a parameter to an <see cref="OsbSprite"/> over a given duration. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        ///<param name="param"> The <see cref="CommandParameter"/> type to be applied. </param>
        public void Parameter(double startTime, double endTime, CommandParameter param) => addCommand(new ParameterCommand(startTime, endTime, param));

        ///<summary> Flips an <see cref="OsbSprite"/> horizontally over a given duration. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        public void FlipH(double startTime, double endTime) => Parameter(startTime, endTime, CommandParameter.FlipHorizontal);

        ///<summary> Flips an <see cref="OsbSprite"/> horizontally. </summary>
        ///<param name="time"> Time of the command. </param>
        public void FlipH(double time) => FlipH(time, time);

        ///<summary> Flips an <see cref="OsbSprite"/> vertically over a given duration. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        public void FlipV(double startTime, double endTime) => Parameter(startTime, endTime, CommandParameter.FlipVertical);

        ///<summary> Flips an <see cref="OsbSprite"/> horizontally. </summary>
        ///<param name="time"> Time of the command. </param>
        public void FlipV(double time) => FlipV(time, time);

        ///<summary> Applies additive blending to an <see cref="OsbSprite"/> over a given duration. </summary>
        ///<param name="startTime"> Start time of the command. </param>
        ///<param name="endTime"> End time of the command. </param>
        public void Additive(double startTime, double endTime) => Parameter(startTime, endTime, CommandParameter.AdditiveBlending);

        ///<summary> Applies additive blending to an <see cref="OsbSprite"/>. </summary>
        ///<param name="time"> Time of the command. </param>
        public void Additive(double time) => Additive(time, time);

        ///<summary> Repeat commands <paramref name="loopCount"/> times until <see cref="EndGroup"/> is called. </summary>
        ///<remarks> Command times inside the loop are relative to the <paramref name="startTime"/> of the loop. </remarks>
        ///<param name="startTime"> Start time of the loop. </param>
        ///<param name="loopCount"> How many times the loop should repeat. </param>
        public LoopCommand StartLoopGroup(double startTime, int loopCount)
        {
            var loopCommand = new LoopCommand(startTime, loopCount);
            addCommand(loopCommand);
            startDisplayLoop(loopCommand);
            return loopCommand;
        }

        ///<summary> Commands on the <see cref="OsbSprite"/> until <see cref="EndGroup"/> is called will be active when the <paramref name="triggerName"/> event happens until <paramref name="endTime"/>. </summary>
        ///<remarks> Command times inside the loop are relative to the <paramref name="startTime"/> of the trigger loop. </remarks>
        ///<param name="triggerName"> Trigger type of the loop </param>
        ///<param name="startTime"> Start time of the loop. </param>
        ///<param name="endTime"> End time of the loop. </param>
        ///<param name="group"> Group number of the loop. </param>
        public TriggerCommand StartTriggerGroup(string triggerName, double startTime, double endTime, int group = 0)
        {
            var triggerCommand = new TriggerCommand(triggerName, startTime, endTime, group);
            addCommand(triggerCommand);
            startDisplayTrigger(triggerCommand);
            return triggerCommand;
        }

        ///<summary> Calls the end of a loop. </summary>
        public void EndGroup()
        {
            currentCommandGroup.EndGroup();
            currentCommandGroup = null;

            endDisplayComposites();
        }

        void addCommand(ICommand command)
        {
            var commandGroup = command as CommandGroup;
            if (commandGroup != null)
            {
                currentCommandGroup = commandGroup;
                commands.Add(commandGroup);
            }
            else
            {
                if (currentCommandGroup != null) currentCommandGroup.Add(command);
                else commands.Add(command);
                addDisplayCommand(command);
            }
            clearStartEndTimes();
        }

        ///<summary> Adds a command to be run on the sprite. </summary>
        ///<param name="command"> The command type to be run. </param>
        public void AddCommand(ICommand command)
        {
            if (command is ColorCommand color) Color(color.Easing, color.StartTime, color.EndTime, color.StartValue, color.EndValue);
            else if (command is FadeCommand fade) Fade(fade.Easing, fade.StartTime, fade.EndTime, fade.StartValue, fade.EndValue);
            else if (command is ScaleCommand scale) Scale(scale.Easing, scale.StartTime, scale.EndTime, scale.StartValue, scale.EndValue);
            else if (command is VScaleCommand vScale) ScaleVec(vScale.Easing, vScale.StartTime, vScale.EndTime, vScale.StartValue, vScale.EndValue);
            else if (command is ParameterCommand param) Parameter(param.StartTime, param.EndTime, param.StartValue);
            else if (command is MoveCommand move) Move(move.Easing, move.StartTime, move.EndTime, move.StartValue, move.EndValue);
            else if (command is MoveXCommand moveX) MoveX(moveX.Easing, moveX.StartTime, moveX.EndTime, moveX.StartValue, moveX.EndValue);
            else if (command is MoveYCommand moveY) MoveY(moveY.Easing, moveY.StartTime, moveY.EndTime, moveY.StartValue, moveY.EndValue);
            else if (command is RotateCommand rotate) Rotate(rotate.Easing, rotate.StartTime, rotate.EndTime, rotate.StartValue, rotate.EndValue);
            else if (command is LoopCommand loop)
            {
                StartLoopGroup(loop.StartTime, loop.LoopCount);
                foreach (var cmd in loop.Commands) AddCommand(cmd);
                EndGroup();
            }
            else if (command is TriggerCommand trigger)
            {
                StartTriggerGroup(trigger.TriggerName, trigger.StartTime, trigger.EndTime, trigger.Group);
                foreach (var cmd in trigger.Commands) AddCommand(cmd);
                EndGroup();
            }
            else throw new NotSupportedException($"Failed to add command: No support for adding command of type {command.GetType().FullName}");
        }

        #region Display 

        readonly List<KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>> displayValueBuilders = new List<KeyValuePair<Predicate<ICommand>, IAnimatedValueBuilder>>();

        readonly AnimatedValue<CommandPosition> moveTimeline = new AnimatedValue<CommandPosition>();
        readonly AnimatedValue<CommandDecimal> moveXTimeline = new AnimatedValue<CommandDecimal>();
        readonly AnimatedValue<CommandDecimal> moveYTimeline = new AnimatedValue<CommandDecimal>();
        readonly AnimatedValue<CommandDecimal> scaleTimeline = new AnimatedValue<CommandDecimal>(1);
        readonly AnimatedValue<CommandScale> scaleVecTimeline = new AnimatedValue<CommandScale>(Vector2.One);
        readonly AnimatedValue<CommandDecimal> rotateTimeline = new AnimatedValue<CommandDecimal>();
        readonly AnimatedValue<CommandDecimal> fadeTimeline = new AnimatedValue<CommandDecimal>(1);
        readonly AnimatedValue<CommandColor> colorTimeline = new AnimatedValue<CommandColor>(CommandColor.White);
        readonly AnimatedValue<CommandParameter> additiveTimeline = new AnimatedValue<CommandParameter>(CommandParameter.None);
        readonly AnimatedValue<CommandParameter> flipHTimeline = new AnimatedValue<CommandParameter>(CommandParameter.None);
        readonly AnimatedValue<CommandParameter> flipVTimeline = new AnimatedValue<CommandParameter>(CommandParameter.None);

        ///<summary> Retrieves the <see cref="CommandPosition"/> of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandPosition PositionAt(double time) => moveTimeline.HasCommands ? moveTimeline.ValueAtTime(time) : new CommandPosition(moveXTimeline.ValueAtTime(time), moveYTimeline.ValueAtTime(time));

        ///<summary> Retrieves the <see cref="CommandScale"/> of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandScale ScaleAt(double time) => scaleVecTimeline.HasCommands ? scaleVecTimeline.ValueAtTime(time) : new CommandScale(scaleTimeline.ValueAtTime(time));

        ///<summary> Retrieves the rotation, in radians, of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandDecimal RotationAt(double time) => rotateTimeline.ValueAtTime(time);

        ///<summary> Retrieves the opacity level of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandDecimal OpacityAt(double time) => fadeTimeline.ValueAtTime(time);

        ///<summary> Retrieves the <see cref="CommandColor"/> of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandColor ColorAt(double time) => colorTimeline.ValueAtTime(time);

        ///<summary> Retrieves the additive value of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandParameter AdditiveAt(double time) => additiveTimeline.ValueAtTime(time);

        ///<summary> Retrieves the horizontal flip of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandParameter FlipHAt(double time) => flipHTimeline.ValueAtTime(time);

        ///<summary> Retrieves the vertical flip of a sprite at a given time. </summary>
        ///<param name="time"> Time to retrieve the information at. </param>
        public CommandParameter FlipVAt(double time) => flipVTimeline.ValueAtTime(time);

        void initializeDisplayValueBuilders()
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
        void addDisplayCommand(ICommand command) => displayValueBuilders.ForEach(builders =>
        {
            if (builders.Key(command)) builders.Value.Add(command);
        });
        void startDisplayLoop(LoopCommand loopCommand) => displayValueBuilders.ForEach(builders => builders.Value.StartDisplayLoop(loopCommand));
        void startDisplayTrigger(TriggerCommand triggerCommand) => displayValueBuilders.ForEach(builders => builders.Value.StartDisplayTrigger(triggerCommand));
        void endDisplayComposites() => displayValueBuilders.ForEach(builders => builders.Value.EndDisplayComposite());

        #endregion

        ///<returns> True if the sprite is active at <paramref name="time"/>, else returns false. </returns>
        public bool IsActive(double time) => CommandsStartTime <= time && time <= CommandsEndTime;

        ///<summary> Gets the start time of the 1st command on this sprite. </summary>
        public override double StartTime => CommandsStartTime;

        ///<summary> Gets the end time of the last command on this sprite. </summary>
        public override double EndTime => CommandsEndTime;

#pragma warning disable CS1591
        public override void WriteOsb(TextWriter writer, ExportSettings exportSettings, OsbLayer layer)
        {
            if (CommandCount == 0) return;
            var osbSpriteWriter = OsbWriterFactory.CreateWriter(this,
                moveTimeline, moveXTimeline, moveYTimeline,
                scaleTimeline, scaleVecTimeline,
                rotateTimeline,
                fadeTimeline,
                colorTimeline,
                writer, exportSettings, layer);

            osbSpriteWriter.WriteOsb();
        }

        public static bool InScreenBounds(Vector2 position, Vector2 size, float rotation, Vector2 origin)
            => new OrientedBoundingBox(position, origin, size.X, size.Y, rotation).Intersects(OsuHitObject.WidescreenStoryboardBounds);

        ///<summary> Gets the <see cref="Vector2"/> origin of a sprite based on its <see cref="OsbOrigin"/> </summary>
        ///<param name="origin"> The <see cref="OsbOrigin"/> to be taken into account. </param>
        ///<param name="width"> The width of the sprite. </param>
        ///<param name="height"> The height of the sprite. </param>
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
    ///<summary> Used mainly by export functions </summary>
    public enum OsbLayer
    {
        Background, Fail, Pass, Foreground, Overlay
    }

    ///<summary> Enumeration values determining the origin of a sprite/image. </summary>
    public enum OsbOrigin
    {
        TopLeft, TopCentre, TopRight, CentreLeft, Centre, CentreRight, BottomLeft, BottomCentre, BottomRight
    }

    ///<summary> Apply an easing to a command. Contains enumeration values unlike .osb syntax. </summary>
    ///<remarks> Visit <see href="http://easings.net/"/> for more information. </remarks>
    public enum OsbEasing
    {
        None, Out, In,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        InQuart, OutQuart, InOutQuart,
        InQuint, OutQuint, InOutQuint,
        InSine, OutSine, InOutSine,
        InExpo, OutExpo, InOutExpo,
        InCirc, OutCirc, InOutCirc,
        InElastic, OutElastic, OutElasticHalf, OutElasticQuarter, InOutElastic,
        InBack, OutBack, InOutBack,
        InBounce, OutBounce, InOutBounce
    }

    ///<summary> Define the loop type for an animation. </summary>
    public enum OsbLoopType
    {
        LoopForever, LoopOnce
    }

    ///<summary> Define the parameter type for a parameter command. </summary>
    public enum ParameterType
    {
        None, FlipHorizontal, FlipVertical, AdditiveBlending
    }
}