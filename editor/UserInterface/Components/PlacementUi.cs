using BrewLib.Graphics;
using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using OpenTK.Input;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface.Drawables;
using System;
using System.Diagnostics;

namespace StorybrewEditor.UserInterface.Components
{
    public class PlacementUi : Widget
    {
        public StoryboardSegment Segment
        {
            get => placementDrawable.Segment;
            set
            {
                placementDrawable.Segment = value;
                if (placementDrawable.Segment != null)
                    placementDrawable.ParentTransform = placementDrawable.Segment.BuildCompleteParentTransform();
                else placementDrawable.ParentTransform = null;
            }
        }

        public override Vector2 MinSize => placementDrawable?.MinSize ?? Vector2.Zero;
        public override Vector2 PreferredSize => placementDrawable?.PreferredSize ?? Vector2.Zero;

        private PlacementDrawable placementDrawable;

        public PlacementUi(WidgetManager manager) : base(manager)
        {
            placementDrawable = new PlacementDrawable();

            OnClickDown += placementUi_OnClickDown;
            OnClickUp += placementUi_onClickUp;
            OnClickMove += placementUi_onClickMove;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                OnClickDown -= placementUi_OnClickDown;
                OnClickUp -= placementUi_onClickUp;
                OnClickMove -= placementUi_onClickMove;
            }
            placementDrawable = null;
        }

        private Vector2 dragStartPosition;
        private State state = State.Idle;
        private bool placementUi_OnClickDown(WidgetEvent evt, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                dragStartPosition = new Vector2(e.Position.X, e.Position.Y);
                var keyboardState = Keyboard.GetState();
                if (keyboardState.IsKeyDown(Key.ShiftLeft))
                    state = State.Scaling;
                else if (keyboardState.IsKeyDown(Key.ControlLeft))
                    state = State.Rotating;
                else state = State.Moving;
                return true;
            }
            return false;
        }
        private void placementUi_onClickUp(WidgetEvent evt, MouseButtonEventArgs e)
        {
            state = State.Idle;
        }
        private void placementUi_onClickMove(WidgetEvent evt, MouseMoveEventArgs e)
        {
            if (state == State.Idle)
                return;

            Debug.Assert(e.XDelta != 0 || e.YDelta != 0);

            var editorSegment = Segment.AsEditorSegment();
            var dragEndPosition = dragStartPosition + new Vector2(e.XDelta, e.YDelta);
            var dragFrom = placementDrawable.ScreenToSegment(dragStartPosition);
            var dragTo = placementDrawable.ScreenToSegment(dragEndPosition);

            //Debug.Assert(dragFrom != dragTo);

            switch (state)
            {
                case State.Moving:
                    editorSegment.PlacementPosition += dragTo - dragFrom;
                    break;
                case State.Scaling:
                    var oldScale = editorSegment.PlacementScale;
                    editorSegment.PlacementScale *= dragTo.Length / dragFrom.Length;
                    if (editorSegment.PlacementScale == 0)
                        editorSegment.PlacementScale = oldScale;
                    break;
                case State.Rotating:
                    var fromAngle = Math.Atan2(dragFrom.Y, dragFrom.X);
                    var toAngle = Math.Atan2(dragTo.Y, dragTo.X);
                    var angleDelta = toAngle - fromAngle;
                    editorSegment.PlacementRotation += angleDelta;
                    break;
            }
            dragStartPosition = dragEndPosition;
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);
            if (placementDrawable.Segment != null)
                placementDrawable.Draw(drawContext, Manager.Camera, Bounds, actualOpacity);
        }

        private enum State
        {
            Idle,
            Moving,
            Scaling,
            Rotating,
        }
    }
}
