
#if DEBUG
using OpenTK;
using StorybrewCommon.Animations;
using StorybrewCommon.Mapset;
using StorybrewCommon.Scripting;
using StorybrewCommon.Storyboarding.CommandValues;
using StorybrewCommon.Util;
using System;
using System.Collections.Generic;

namespace StorybrewCommon.Storyboarding.Util
{
    public class CommandGenerator
    {
        private List<State> states = new List<State>();

        private readonly KeyframedValue<Vector2> positions = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2);
        private readonly KeyframedValue<Vector2> scales = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2);
        private readonly KeyframedValue<float> rotations = new KeyframedValue<float>(InterpolatingFunctions.FloatAngle);
        private readonly KeyframedValue<CommandColor> colors = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor);
        private readonly KeyframedValue<float> opacities = new KeyframedValue<float>(InterpolatingFunctions.Float);

        private readonly KeyframedValue<Vector2> finalPositions = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2);
        private readonly KeyframedValue<Vector2> finalScales = new KeyframedValue<Vector2>(InterpolatingFunctions.Vector2);
        private readonly KeyframedValue<float> finalRotations = new KeyframedValue<float>(InterpolatingFunctions.FloatAngle);
        private readonly KeyframedValue<CommandColor> finalColors = new KeyframedValue<CommandColor>(InterpolatingFunctions.CommandColor);
        private readonly KeyframedValue<float> finalOpacities = new KeyframedValue<float>(InterpolatingFunctions.Float);

        public State StartState => states.Count == 0 ? null : states[0];
        public State EndState => states.Count == 0 ? null : states[states.Count - 1];

        public double PositionTolerance = 1;
        public double ScaleTolerance = 0.1;
        public double RotationTolerance = 0.001;
        public double ColorTolerance = 1f / 255;
        public double OpacityTolerance = 0.01;

        public int PositionDecimals = 1;
        public int ScaleDecimals = 2;
        public int RotationDecimals = 3;
        public int OpacityDecimals = 2;

        public void Add(State state)
        {
            if (states.Count == 0 || states[states.Count - 1].Time < state.Time)
                states.Add(state);
            else
            {
                var index = states.BinarySearch(state);
                if (index < 0) index = ~index;
                states.Insert(index, state);
            }
        }

        public void ClearStates()
            => states.Clear();

        public bool GenerateCommands(OsbSprite sprite, Action<Action, OsbSprite> action = null, double timeOffset = 0)
            => GenerateCommands(sprite, OsuHitObject.WidescreenStoryboardBounds, action, timeOffset);

        public bool GenerateCommands(OsbSprite sprite, Box2 bounds, Action<Action, OsbSprite> action = null, double timeOffset = 0)
        {
            var previousState = (State)null;
            var wasVisible = false;
            var everVisible = false;
            var stateAdded = false;
            var imageSize = Vector2.One;

            foreach (var state in states)
            {
                var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(sprite.GetTexturePathAt(state.Time + timeOffset));
                imageSize = new Vector2(bitmap.Width, bitmap.Height);

                var isVisible = state.IsVisible(bitmap.Width, bitmap.Height, sprite.Origin, bounds);
                if (isVisible) everVisible = true;

                if (!wasVisible && isVisible)
                {
                    if (!stateAdded && previousState != null)
                        addKeyframes(previousState, timeOffset);
                    addKeyframes(state, timeOffset);
                    stateAdded = true;
                }
                else if (wasVisible && !isVisible)
                {
                    addKeyframes(state, timeOffset);
                    commitKeyframes(imageSize);
                    stateAdded = true;
                }
                else if (isVisible)
                {
                    addKeyframes(state, timeOffset);
                    stateAdded = true;
                }
                else stateAdded = false;

                previousState = state;
                wasVisible = isVisible;
            }

            if (wasVisible)
                commitKeyframes(imageSize);

            if (everVisible)
            {
                if (action != null)
                    action(() => convertToCommands(sprite), sprite);
                else convertToCommands(sprite);
            }

            clearFinalKeyframes();
            return everVisible;
        }

        private void commitKeyframes(Vector2 imageSize)
        {
            positions.Simplify2dKeyframes(PositionTolerance, p => p);
            positions.TransferKeyframes(finalPositions);

            scales.Simplify2dKeyframes(ScaleTolerance, s => new Vector2(s.X * imageSize.X, s.Y * imageSize.Y));
            scales.TransferKeyframes(finalScales);

            rotations.Simplify1dKeyframes(RotationTolerance, a => a);
            rotations.TransferKeyframes(finalRotations);

            colors.Simplify3dKeyframes(ColorTolerance, c => new Vector3(c.R, c.G, c.B));
            colors.TransferKeyframes(finalColors);

            opacities.Simplify1dKeyframes(OpacityTolerance, o => o);
            if (opacities.StartValue > 0) opacities.Add(opacities.StartTime, 0, before: true);
            if (opacities.EndValue > 0) opacities.Add(opacities.EndTime, 0);
            opacities.TransferKeyframes(finalOpacities);
        }

        private void convertToCommands(OsbSprite sprite)
        {
            finalPositions.ForEachPair((start, end) => sprite.Move(start.Time, end.Time, start.Value, end.Value), new Vector2(320, 240),
                p => new Vector2((float)Math.Round(p.X, PositionDecimals), (float)Math.Round(p.Y, PositionDecimals)));
            finalScales.ForEachPair((start, end) => sprite.ScaleVec(start.Time, end.Time, start.Value, end.Value), Vector2.One,
                s => new Vector2((float)Math.Round(s.X, ScaleDecimals), (float)Math.Round(s.Y, ScaleDecimals)));
            finalRotations.ForEachPair((start, end) => sprite.Rotate(start.Time, end.Time, start.Value, end.Value), 0,
                r => (float)Math.Round(r, RotationDecimals));
            finalColors.ForEachPair((start, end) => sprite.Color(start.Time, end.Time, start.Value, end.Value), CommandColor.White,
                c => CommandColor.FromRgb(c.R, c.G, c.B));
            finalOpacities.ForEachPair((start, end) => sprite.Fade(start.Time, end.Time, start.Value, end.Value), -1,
                o => (float)Math.Round(o, OpacityDecimals));
        }

        private void addKeyframes(State state, double timeOffset)
        {
            var time = state.Time + timeOffset;
            positions.Add(time, state.Position);
            scales.Add(time, state.Scale);
            rotations.Add(time, (float)state.Rotation);
            colors.Add(time, state.Color);
            opacities.Add(time, (float)state.Opacity);
        }

        private void clearFinalKeyframes()
        {
            finalPositions.Clear();
            finalScales.Clear();
            finalRotations.Clear();
            finalColors.Clear();
            finalOpacities.Clear();
        }

        public class State : IComparable<State>
        {
            public double Time;
            public Vector2 Position = new Vector2(320, 240);
            public Vector2 Scale = new Vector2(1, 1);
            public double Rotation = 0;
            public CommandColor Color = CommandColor.White;
            public double Opacity = 1;

            public bool IsVisible(int width, int height, OsbOrigin origin, Box2 bounds)
            {
                if (Opacity <= 0)
                    return false;

                if (Scale.X == 0 || Scale.Y == 0)
                    return false;

                if (!bounds.Contains(Position))
                {
                    var w = width * Scale.X;
                    var h = height * Scale.Y;
                    Vector2 originVector;
                    switch (origin)
                    {
                        default:
                        case OsbOrigin.TopLeft: originVector = Vector2.Zero; break;
                        case OsbOrigin.TopCentre: originVector = new Vector2(w * 0.5f, 0); break;
                        case OsbOrigin.TopRight: originVector = new Vector2(w, 0); break;
                        case OsbOrigin.CentreLeft: originVector = new Vector2(0, h * 0.5f); break;
                        case OsbOrigin.Centre: originVector = new Vector2(w * 0.5f, h * 0.5f); break;
                        case OsbOrigin.CentreRight: originVector = new Vector2(w, h * 0.5f); break;
                        case OsbOrigin.BottomLeft: originVector = new Vector2(0, h); break;
                        case OsbOrigin.BottomCentre: originVector = new Vector2(w * 0.5f, h); break;
                        case OsbOrigin.BottomRight: originVector = new Vector2(w, h); break;
                    }
                    var obb = new OrientedBoundingBox(Position, originVector, w, h, Rotation);
                    if (!obb.Intersects(bounds))
                        return false;
                }

                return true;
            }

            public int CompareTo(State other)
                => Math.Sign(Time - other.Time);
        }
    }
}
#endif
