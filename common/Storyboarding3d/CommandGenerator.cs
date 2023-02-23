using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StorybrewCommon.Storyboarding
{
#pragma warning disable CS1591
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

        ///<summary> Gets the <see cref="CommandGenerator"/>'s starting state. </summary>
        public State StartState => states.Count == 0 ? null : states[0];

        ///<summary> Gets the <see cref="CommandGenerator"/>'s end state. </summary>
        public State EndState => states.Count == 0 ? null : states[states.Count - 1];

        ///<summary> The tolerance threshold for position keyframe simplification. </summary>
        public double PositionTolerance = 1;

        ///<summary> The tolerance threshold for scaling keyframe simplification. </summary>
        public double ScaleTolerance = .5;

        ///<summary> The tolerance threshold for rotation keyframe simplification. </summary>
        public double RotationTolerance = .25;

        ///<summary> The tolerance threshold for coloring keyframe simplification. </summary>
        public double ColorTolerance = 2;

        ///<summary> The tolerance threshold for opacity keyframe simplification. </summary>
        public double OpacityTolerance = .1;

        ///<summary> The amount of decimal digits for position keyframes. </summary>
        public int PositionDecimals = 1;

        ///<summary> The amount of decimal digits for scaling keyframes. </summary>
        public int ScaleDecimals = 3;

        ///<summary> The amount of decimal digits for rotation keyframes. </summary>
        public int RotationDecimals = 4;

        ///<summary> The amount of decimal digits for opacity keyframes. </summary>
        public int OpacityDecimals = 1;

        public Action<State> PostProcess;

        public void Add(State state, bool before = false)
        {
            if (states.Count == 0 || states[states.Count - 1].Time < state.Time) states.Add(state);
            else
            {
                var index = states.BinarySearch(state);
                if (index >= 0)
                {
                    if (before) while (index > 0 && states[index].Time >= state.Time) index--;
                    else while (index < states.Count && states[index].Time <= state.Time) index++;
                }
                else index = ~index;
                states.Insert(index, state);
            }
        }
        public void ClearStates() => states.Clear();

        public bool GenerateCommands(OsbSprite sprite, Action<Action, OsbSprite> action = null, double? startTime = null, double? endTime = null, double timeOffset = 0, bool loopable = false) 
            => GenerateCommands(sprite, OsuHitObject.WidescreenStoryboardBounds, action, startTime, endTime, timeOffset, loopable);

        public bool GenerateCommands(OsbSprite sprite, Box2 bounds, Action<Action, OsbSprite> action = null, double? startTime = null, double? endTime = null, double timeOffset = 0, bool loopable = false)
        {
            State previousState = null;
            var wasVisible = false;
            var everVisible = false;
            var stateAdded = false;
            var imageSize = Vector2.One;
            var distFade = false;

            states.ForEach(state =>
            {
                var time = state.Time + timeOffset;
                var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(sprite.TexturePath);
                imageSize = new Vector2(bitmap.Width, bitmap.Height);

                PostProcess?.Invoke(state);
                var isVisible = state.IsVisible(bitmap.Width, bitmap.Height, sprite.Origin, bounds);

                if (isVisible) everVisible = true;
                if (!wasVisible && isVisible)
                {
                    if (!stateAdded && previousState != null) addKeyframes(previousState, loopable ? time : previousState.Time + timeOffset);
                    addKeyframes(state, time);
                    stateAdded = true;
                }
                else if (wasVisible && !isVisible)
                {
                    addKeyframes(state, time);
                    commitKeyframes(imageSize);
                    stateAdded = true;
                }
                else if (isVisible)
                {
                    addKeyframes(state, time);
                    stateAdded = true;
                }
                else stateAdded = false;

                previousState = state;
                distFade = state.UseDistanceFade;
                wasVisible = isVisible;
            });

            if (wasVisible) commitKeyframes(imageSize);
            if (everVisible)
            {
                if (action != null) action(() => convertToCommands(sprite, startTime, endTime, timeOffset, loopable, distFade), sprite);
                else convertToCommands(sprite, startTime, endTime, timeOffset, loopable, distFade);
            }

            clearFinalKeyframes();
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

            var vec = finalScales.Any(k => Math.Round(k.Value.X, ScaleDecimals > 5 ? 5 : ScaleDecimals) != Math.Round(k.Value.Y, ScaleDecimals > 5 ? 5 : ScaleDecimals));
            finalScales.ForEachPair((s, e) =>
            {
                if (vec) sprite.ScaleVec(s.Time, e.Time, s.Value, e.Value);
                else sprite.Scale(s.Time, e.Time, s.Value.X, e.Value.X);
            }, Vector2.One, s => new Vector2((float)Math.Round(s.X, ScaleDecimals), (float)Math.Round(s.Y, ScaleDecimals)), startState, loopable: loopable);

            finalRotations.ForEachPair((s, e) => sprite.Rotate(s.Time, e.Time, s.Value, e.Value), 0,
                r => (float)Math.Round(r, RotationDecimals), startState, loopable: loopable);

            finalColors.ForEachPair((s, e) => sprite.Color(s.Time, e.Time, s.Value, e.Value), CommandColor.White,
                c => CommandColor.FromRgb(c.R, c.G, c.B), startState, loopable: loopable);

            finalfades.ForEachPair((s, e) =>
            {
                if (!((s.Time == sprite.CommandsStartTime && s.Time == e.Time && e.Value == 1 && distFade) ||
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
        void clearFinalKeyframes()
        {
            finalPositions.Clear();
            finalScales.Clear();
            finalRotations.Clear();
            finalColors.Clear();
            finalfades.Clear();
            flipH.Clear();
            flipV.Clear();
            additive.Clear();
        }
        public class State : IComparable<State>
        {
            public double Time;
            public Vector2 Position = new Vector2(320, 240);
            public Vector2 Scale = Vector2.One;
            public double Rotation = 0;
            public CommandColor Color = CommandColor.White;
            public double Opacity = 1;
            public bool FlipH, FlipV, Additive, UseDistanceFade;

            public bool IsVisible(int width, int height, OsbOrigin origin, Box2 bounds)
            {
                if (Additive && Color == CommandColor.Black || Opacity <= 0 || Scale.X == 0 || Scale.Y == 0) return false;
                if (!bounds.Contains(Position))
                {
                    var w = width * Scale.X;
                    var h = height * Scale.Y;
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
            public int CompareTo(State other) => Math.Sign(Time - other.Time);
        }
    }
}