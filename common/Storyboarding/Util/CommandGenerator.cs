
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

        public bool GenerateCommands(OsbSprite sprite, Action<Action, OsbSprite> action = null)
            => GenerateCommands(sprite, OsuHitObject.WidescreenStoryboardBounds, action);

        public bool GenerateCommands(OsbSprite sprite, Box2 bounds, Action<Action, OsbSprite> action = null)
        {
            var previousState = (State)null;
            var wasVisible = false;
            var everVisible = false;
            var stateAdded = false;

            foreach (var state in states)
            {
                var bitmap = StoryboardObjectGenerator.Current.GetMapsetBitmap(sprite.GetTexturePathAt(state.Time));

                var isVisible = state.IsVisible(bitmap.Width, bitmap.Height, sprite.Origin, bounds);
                if (isVisible) everVisible = true;

                if (!wasVisible && isVisible)
                {
                    if (!stateAdded && previousState != null)
                        addKeyframes(previousState);
                    addKeyframes(state);
                    stateAdded = true;
                }
                else if (wasVisible && !isVisible)
                {
                    addKeyframes(state);
                    commitKeyframes();
                    stateAdded = true;
                }
                else if (isVisible)
                {
                    addKeyframes(state);
                    stateAdded = true;
                }
                else stateAdded = false;

                previousState = state;
                wasVisible = isVisible;
            }

            if (wasVisible)
                commitKeyframes();

            if (everVisible)
            {
                if (action != null)
                    action(() => convertToCommands(sprite), sprite);
                else convertToCommands(sprite);
            }

            clearFinalKeyframes();
            return everVisible;
        }

        private void commitKeyframes()
        {
            positions.Simplify2dKeyframes(PositionTolerance, p => p);
            positions.TransferKeyframes(finalPositions);

            scales.Simplify2dKeyframes(ScaleTolerance, s => s);
            scales.TransferKeyframes(finalScales);

            rotations.Simplify1dKeyframes(RotationTolerance, a => a);
            rotations.TransferKeyframes(finalRotations);

            colors.Simplify3dKeyframes(ColorTolerance, c => new Vector3(c.R, c.G, c.B));
            colors.TransferKeyframes(finalColors);

            if (opacities.StartValue > 0) opacities.Add(opacities.StartTime, 0, before: true);
            opacities.Simplify1dKeyframes(OpacityTolerance, o => o);
            if (opacities.EndValue > 0) opacities.Add(opacities.EndTime, 0);
            opacities.TransferKeyframes(finalOpacities);
        }

        private void convertToCommands(OsbSprite sprite)
        {
            finalPositions.ForEachPair((startKeyframe, endKeyframe) => sprite.Move(startKeyframe.Time, endKeyframe.Time, startKeyframe.Value, endKeyframe.Value), new Vector2(320, 240));
            finalScales.ForEachPair((startKeyframe, endKeyframe) => sprite.ScaleVec(startKeyframe.Time, endKeyframe.Time, startKeyframe.Value, endKeyframe.Value), Vector2.One);
            finalRotations.ForEachPair((startKeyframe, endKeyframe) => sprite.Rotate(startKeyframe.Time, endKeyframe.Time, startKeyframe.Value, endKeyframe.Value), 0);
            finalColors.ForEachPair((startKeyframe, endKeyframe) => sprite.Color(startKeyframe.Time, endKeyframe.Time, startKeyframe.Value, endKeyframe.Value), CommandColor.White);
            finalOpacities.ForEachPair((startKeyframe, endKeyframe) => sprite.Fade(startKeyframe.Time, endKeyframe.Time, startKeyframe.Value, endKeyframe.Value), -1);
        }

        private void addKeyframes(State state)
        {
            var time = state.Time;
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
                    Vector2 originVector;
                    switch (origin)
                    {
                        default:
                        case OsbOrigin.TopLeft: originVector = Vector2.Zero; break;
                        case OsbOrigin.TopCentre: originVector = new Vector2(width * Scale.X * 0.5f, 0); break;
                        case OsbOrigin.TopRight: originVector = new Vector2(width * Scale.X, 0); break;
                        case OsbOrigin.CentreLeft: originVector = new Vector2(0, height * 0.5f); break;
                        case OsbOrigin.Centre: originVector = new Vector2(width * Scale.X * 0.5f, height * Scale.Y * 0.5f); break;
                        case OsbOrigin.CentreRight: originVector = new Vector2(width * Scale.X, height * Scale.Y * 0.5f); break;
                        case OsbOrigin.BottomLeft: originVector = new Vector2(0, height); break;
                        case OsbOrigin.BottomCentre: originVector = new Vector2(width * Scale.X * 0.5f, height * Scale.Y); break;
                        case OsbOrigin.BottomRight: originVector = new Vector2(width * Scale.X, height * Scale.Y); break;
                    }
                    var obb = new OrientedBoundingBox(Position, originVector, width * Scale.X, height * Scale.Y, Rotation);
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
