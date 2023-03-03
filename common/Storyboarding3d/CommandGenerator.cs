using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding.Commands;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StorybrewCommon.Storyboarding
{
    ///<summary> Generates commands on an <see cref="OsbSprite"/> based on the states of that sprite. </summary>
    public class CommandGenerator
    {
        readonly KeyframedValue<Vector2> 
            positions = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2),
            scales = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2),
            finalPositions = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2),
            finalScales = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2);

        readonly KeyframedValue<float> 
            rotations = new KeyframedValue<float>(InterpolatingFunctions.FloatAngle),
            fades = new KeyframedValue<float>(InterpolatingFunctions.Float),
            finalRotations = new KeyframedValue<float>(InterpolatingFunctions.FloatAngle),
            finalfades = new KeyframedValue<float>(InterpolatingFunctions.Float);

        readonly KeyframedValue<CommandColor> 
            colors = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor),
            finalColors = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor);

        readonly KeyframedValue<bool> flipH = new KeyframedValue<bool>(InterpolatingFunctions.BoolFrom),
            flipV = new KeyframedValue<bool>(InterpolatingFunctions.BoolFrom),
            additive = new KeyframedValue<bool>(InterpolatingFunctions.BoolFrom);

        readonly List<State> states = new List<State>();

        ///<summary> Gets the <see cref="CommandGenerator"/>'s start state. </summary>
        public State StartState => states.Count == 0 ? null : states[0];

        ///<summary> Gets the <see cref="CommandGenerator"/>'s end state. </summary>
        public State EndState => states.Count == 0 ? null : states[states.Count - 1];

        ///<summary> The tolerance threshold for position keyframe simplification. </summary>
        public double PositionTolerance = 1;

        ///<summary> The tolerance threshold for scaling keyframe simplification. </summary>
        public double ScaleTolerance = 1;

        ///<summary> The tolerance threshold for rotation keyframe simplification. </summary>
        public double RotationTolerance = .005;

        ///<summary> The tolerance threshold for coloring keyframe simplification. </summary>
        public double ColorTolerance = 2;

        ///<summary> The tolerance threshold for opacity keyframe simplification. </summary>
        public double OpacityTolerance = .1;

        ///<summary> The amount of decimal digits for position keyframes. </summary>
        public int PositionDecimals = 1;

        ///<summary> The amount of decimal digits for scaling keyframes. </summary>
        public int ScaleDecimals = 3;

        ///<summary> The amount of decimal digits for rotation keyframes. </summary>
        public int RotationDecimals = 5;

        ///<summary> The amount of decimal digits for opacity keyframes. </summary>
        public int OpacityDecimals = 1;

        /// <summary> 
        /// Adds a <see cref="State"/> to this instance. If <paramref name="before"/> is <see langword="true"/>, adds the state at the beginning of the list.
        /// </summary>
        public void Add(State state, bool before = false)
        {
            var count = states.Count;
            if (count == 0 || states[count - 1].Time < state.Time)
            {
                states.Add(state);
                return;
            }

            var index = states.BinarySearch(state);
            if (index >= 0)
            {
                if (before) while (index > 0 && states[index - 1].Time >= state.Time) index--;
                else while (index < count - 1 && states[index + 1].Time <= state.Time) index++;
            }
            else index = ~index;

            states.Insert(index, state);
        }

        ///<summary> Generates commands on a sprite based on this generator's states. </summary>
        ///<param name="sprite"> The <see cref="OsbSprite"/> to have commands generated on. </param>
        ///<param name="action"> Encapsulates a group of commands to be generated on <paramref name="sprite"/>. </param>
        ///<param name="startTime"> The explicit start time of the command generation. Can be left <see langword="null"/> if <see cref="State.Time"/> is used. </param>
        ///<param name="endTime"> The explicit end time of the command generation. Can be left <see langword="null"/> if <see cref="State.Time"/> is used. </param>
        ///<param name="timeOffset"> The time offset of the command times. </param>
        ///<param name="loopable"> Whether the commands to be generated are contained within a <see cref="LoopCommand"/>. </param>
        ///<returns> <see langword="true"/> if any commands were generated, else returns <see langword="false"/>. </returns>
        public bool GenerateCommands(OsbSprite sprite, Action<Action, OsbSprite> action = null, double? startTime = null, double? endTime = null, double timeOffset = 0, bool loopable = false) 
            => GenerateCommands(sprite, OsuHitObject.WidescreenStoryboardBounds, action, startTime, endTime, timeOffset, loopable);

        ///<summary> Generates commands on a sprite based on this generator's states. </summary>
        ///<param name="sprite"> The <see cref="OsbSprite"/> to have commands generated on. </param>
        ///<param name="bounds"> The rectangular boundary for the sprite to be generated within. </param>
        ///<param name="action"> Encapsulates a group of commands to be generated on <paramref name="sprite"/>. </param>
        ///<param name="startTime"> The explicit start time of the command generation. Can be left <see langword="null"/> if <see cref="State.Time"/> is used. </param>
        ///<param name="endTime"> The explicit end time of the command generation. Can be left <see langword="null"/> if <see cref="State.Time"/> is used. </param>
        ///<param name="timeOffset"> The time offset of the command times. </param>
        ///<param name="loopable"> Whether the commands to be generated are contained within a <see cref="LoopCommand"/>. </param>
        ///<returns> <see langword="true"/> if any commands were generated, else returns <see langword="false"/>. </returns>
        public bool GenerateCommands(OsbSprite sprite, Box2 bounds, Action<Action, OsbSprite> action = null, double? startTime = null, double? endTime = null, double timeOffset = 0, bool loopable = false)
        {
            State previousState = null;
            bool wasVisible = false, everVisible = false, stateAdded = false, distFade = false;

            var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(sprite.TexturePath);
            var imageSize = new Vector2(bitmap.Width, bitmap.Height);

            states.ForEach(state =>
            {
                var time = state.Time + timeOffset;
                var isVisible = state.IsVisible(imageSize, sprite.Origin, bounds, this);

                if (isVisible && everVisible != true) everVisible = true;
                if (!wasVisible && isVisible)
                {
                    if (!stateAdded && previousState != null) addKeyframes(previousState, loopable ? time : previousState.Time + timeOffset);
                    addKeyframes(state, time);
                    if (stateAdded != true) stateAdded = true;
                }
                else if (wasVisible && !isVisible)
                {
                    addKeyframes(state, time);
                    commitKeyframes(imageSize);
                    if (stateAdded != true) stateAdded = true;
                }
                else if (isVisible)
                {
                    addKeyframes(state, time);
                    if (stateAdded != true) stateAdded = true;
                }
                else stateAdded = false;

                previousState = state;
                distFade = state.UseDistanceFade;
                wasVisible = isVisible;
            });

            if (wasVisible) commitKeyframes(imageSize);
            if (everVisible)
            {
                if (action is null) convertToCommands(sprite, startTime, endTime, timeOffset, loopable, distFade);
                else action(() => convertToCommands(sprite, startTime, endTime, timeOffset, loopable, distFade), sprite);
            }

            clearKeyframes();
            return everVisible;
        }
        void commitKeyframes(Vector2 imageSize)
        {
            positions.Simplify2dKeyframes(PositionTolerance, s => s);
            positions.TransferKeyframes(finalPositions);

            scales.Simplify2dKeyframes(ScaleTolerance, v => v * imageSize);
            scales.TransferKeyframes(finalScales);

            rotations.Simplify1dKeyframes(RotationTolerance, r => r);
            rotations.TransferKeyframes(finalRotations);

            colors.Simplify3dKeyframes(ColorTolerance, c => new Vector3(c.R, c.G, c.B));
            colors.TransferKeyframes(finalColors);

            fades.Simplify1dKeyframes(OpacityTolerance, f => f);
            if (fades.StartValue > 0) fades.Add(fades.StartTime, 0, before: true);
            if (fades.EndValue > 0) fades.Add(fades.EndTime, 0);
            fades.TransferKeyframes(finalfades);
        }
        void convertToCommands(OsbSprite sprite, double? startTime, double? endTime, double timeOffset, bool loopable, bool distFade)
        {
            var startState = loopable ? (startTime ?? StartState.Time) + timeOffset : (double?)null;
            var endState = loopable ? (endTime ?? EndState.Time) + timeOffset : (double?)null;

            var first = finalPositions.FirstOrDefault().Value;
            bool moveX = finalPositions.All(k => k.Value.Y == first.Y), moveY = finalPositions.All(k => k.Value.X == first.X);
            finalPositions.ForEachPair((s, e) =>
            {
                if (moveX && !moveY)
                {
                    sprite.MoveX(s.Time, e.Time, s.Value.X, e.Value.X);
                    sprite.InitialPosition = new Vector2(0, s.Value.Y);
                }
                else if (moveY && !moveX)
                {
                    sprite.MoveY(s.Time, e.Time, s.Value.Y, e.Value.Y);
                    sprite.InitialPosition = new Vector2(s.Value.X, 0);
                }
                else sprite.Move(s.Time, e.Time, s.Value, e.Value);
            }, new Vector2(320, 240), p => new Vector2((float)Math.Round(p.X, PositionDecimals), (float)Math.Round(p.Y, PositionDecimals)), startState, loopable: loopable);

            var checkScale = ScaleDecimals > 5 ? 5 : ScaleDecimals;
            var vec = finalScales.Any(k => Math.Round(k.Value.X, checkScale) != Math.Round(k.Value.Y, checkScale));
            finalScales.ForEachPair((s, e) =>
            {
                if (vec) sprite.ScaleVec(s.Time, e.Time, s.Value, e.Value);
                else sprite.Scale(s.Time, e.Time, s.Value.X, e.Value.X);
            }, Vector2.One, s => new Vector2((float)Math.Round(s.X, ScaleDecimals), (float)Math.Round(s.Y, ScaleDecimals)), startState, loopable: loopable);

            finalRotations.ForEachPair((s, e) => sprite.Rotate(s.Time, e.Time, s.Value, e.Value), 
                0, r => (float)Math.Round(r, RotationDecimals), startState, loopable: loopable);

            finalColors.ForEachPair((s, e) => sprite.Color(s.Time, e.Time, s.Value, e.Value), CommandColor.White,
                c => CommandColor.FromRgb(c.R, c.G, c.B), startState, loopable: loopable);

            finalfades.ForEachPair((s, e) =>
            {
                if (!((s.Time == sprite.CommandsStartTime && s.Time == e.Time && e.Value == 1) ||
                    (s.Time == sprite.CommandsEndTime && s.Time == e.Time && e.Value == 0)))
                sprite.Fade(s.Time, e.Time, s.Value, e.Value);
            }, -1, o => (float)Math.Round(o, OpacityDecimals), startState, endState, loopable: loopable);

            flipH.ForEachFlag((f, t) => sprite.FlipH(f, t));
            flipV.ForEachFlag((f, t) => sprite.FlipV(f, t));
            additive.ForEachFlag((f, t) => sprite.Additive(f, t));
        }
        void addKeyframes(State state, double time)
        {
            positions.Add(time, state.Position);
            scales.Add(time, state.Scale);
            rotations.Add(time, (float)state.Rotation);
            colors.Add(time, state.Color);
            fades.Add(time, (float)state.Opacity);
            flipH.Add(time, state.FlipH);
            flipV.Add(time, state.FlipV);
            additive.Add(time, state.Additive);
        }
        void clearKeyframes()
        {
            finalPositions.Clear();
            finalScales.Clear();
            finalRotations.Clear();
            finalColors.Clear();
            finalfades.Clear();
            flipH.Clear();
            flipV.Clear();
            additive.Clear();
            states.Clear();
            states.TrimExcess();
        }
    }

    ///<summary> Defines all of an <see cref="OsbSprite"/>'s states as a class. </summary>
    public class State : IComparable<State>
    {
        ///<summary> Represents the base time, in milliseconds, of this state. </summary>
        public double Time;

        ///<summary> Represents the rotation, in radians, of this state. </summary>
        public double Rotation = 0;

        ///<summary> Represents the opacity, from 0 to 1, of this state. </summary>
        public double Opacity = 0;

        ///<summary> Represents the position, in osu!pixels, of this state. </summary>
        public Vector2 Position = new Vector2(320, 240);

        ///<summary> Represents the scale, in osu!pixels, of this state. </summary>
        public Vector2 Scale = Vector2.One;

        ///<summary> Represents the color, in RGB values, of this state. </summary>
        public CommandColor Color = CommandColor.White;

        ///<summary> Represents the horizontal flip condition of this state. </summary>
        public bool FlipH;

        ///<summary> Represents the vertical flip condition of this state. </summary>
        public bool FlipV;

        ///<summary> Represents the additive toggle condition of this state. </summary>
        public bool Additive;

        internal bool UseDistanceFade;

        /// <summary> 
        /// Returns the visibility of the sprite in the current <see cref="State"/> based on its image size, <see cref="OsbOrigin"/>, and screen boundaries. 
        /// </summary>
        /// <returns> <see langword="true"/> if the sprite is within <paramref name="bounds"/>, else returns <see langword="false"/>. </returns>
        public bool IsVisible(Vector2 imageSize, OsbOrigin origin, Box2 bounds, CommandGenerator generator = null)
        {
            if (Additive && Color == CommandColor.Black || 
                (generator is null ? Opacity : Math.Round(Opacity, generator.OpacityDecimals)) <= 0 ||
                Scale.X == 0 || Scale.Y == 0
                return false;

            if (!bounds.Contains(Position))
            {
                var w = imageSize.X * Scale.X;
                var h = imageSize.Y * Scale.Y;
                Vector2 originVector;

                switch (origin)
                {
                    default:
                    case OsbOrigin.TopLeft: originVector = Vector2.Zero; break;
                    case OsbOrigin.TopCentre: originVector = new Vector2(w / 2, 0); break;
                    case OsbOrigin.TopRight: originVector = new Vector2(w, 0); break;
                    case OsbOrigin.CentreLeft: originVector = new Vector2(0, h / 2); break;
                    case OsbOrigin.Centre: originVector = new Vector2(w / 2, h / 2); break;
                    case OsbOrigin.CentreRight: originVector = new Vector2(w, h / 2); break;
                    case OsbOrigin.BottomLeft: originVector = new Vector2(0, h); break;
                    case OsbOrigin.BottomCentre: originVector = new Vector2(w / 2, h); break;
                    case OsbOrigin.BottomRight: originVector = new Vector2(w, h); break;
                }

                var obb = new OrientedBoundingBox(Position, originVector, w, h, Rotation);
                if (!obb.Intersects(bounds)) return false;
            }
            return true;
        }

        ///<inheritdoc/>
        public int CompareTo(State other) => Math.Sign(Time - other.Time);
    }
}
