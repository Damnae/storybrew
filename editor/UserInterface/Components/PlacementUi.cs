using BrewLib.Graphics;
using BrewLib.Graphics.Renderers;
using BrewLib.UserInterface;
using BrewLib.Util;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using StorybrewCommon.Storyboarding;
using StorybrewEditor.Storyboarding;
using StorybrewEditor.UserInterface.Drawables;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Actually, this wasn't even necessary. Since a segment can take care of its own position, rotation, and scale
        /// it ended up being easier to just directly edit those values instead of monitoring the placement scale, rotation, and position.
        /// </summary>
        //private EditorStoryboardSegment editorSegment;

        public override Vector2 MinSize => placementDrawable?.MinSize ?? Vector2.Zero;
        public override Vector2 PreferredSize => placementDrawable?.PreferredSize ?? Vector2.Zero;

        private PlacementDrawable placementDrawable;

        private readonly RenderStates linesRenderStates = new RenderStates();

        internal PlacementUITransformType GetTransformType() => transformType;

        private Vector2 mousePosition;

        public PlacementUi(WidgetManager manager) : base(manager)
        {
            placementDrawable = new PlacementDrawable(this);

            OnKeyDown += placementUi_OnKeyDown;
            OnClickDown += placementUi_OnClickDown;
            OnClickUp += placementUi_onClickUp;
            OnClickMove += placementUi_onClickMove;
            
        }

        public void ResetState()
        {
            TransformInfo info = new TransformInfo()
            {
               Position = Vector2.Zero,
               Rotation = 0,
               Scale = 1
            };

            ApplyTransform(info);
            UndoStack.Push(info);
        }

        bool redoing = false;

        private readonly Stack<TransformInfo> UndoStack = new Stack<TransformInfo>();
        private readonly Stack<TransformInfo> RedoStack = new Stack<TransformInfo>();

        private bool placementUi_OnKeyDown(WidgetEvent evt, KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    {
                        if (!activeChanges)
                            transformType = PlacementUITransformType.Move;
                        return true;
                    }
                case Key.E:
                    {
                        if (!activeChanges)
                            transformType = PlacementUITransformType.Scale;
                        return true;
                    }
                case Key.R:
                    {
                        if (!activeChanges)
                        {
                            rotationVector = Vector2.Zero;
                            transformType = PlacementUITransformType.Rotate;
                        }
                        return true;
                    }
                case Key.Z:
                    if (!e.IsRepeat && e.Control)
                    {
                        if (e.Shift && RedoStack.Count > 0)
                        {
                            RedoTransform();
                            return true;
                        }

                        if (!e.Shift && UndoStack.Count > 0)
                            UndoTransform();
                    }
                    return true;

                case Key.Y:
                    if (!e.IsRepeat && e.Control && RedoStack.Count > 0)
                    {
                        RedoTransform();
                    }
                    return true;
                default: return false;
            }
        }

        void ApplyTransform(TransformInfo info)
        {
            Segment.Position = info.Position;
            Segment.Rotation = info.Rotation;
            Segment.Scale = info.Scale;
        }
        void UndoTransform()
        {
            TransformInfo info = UndoStack.Pop();
            ApplyTransform(info);

            RedoStack.Push(info);
            redoing = false;
        }
        void RedoTransform()
        {
            TransformInfo info = RedoStack.Pop();
            if (!redoing) info = RedoStack.Pop();

            ApplyTransform(info);

            UndoStack.Push(info);
            redoing = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                
                OnClickDown -= placementUi_OnClickDown;
                OnClickUp -= placementUi_onClickUp;
                OnClickMove -= placementUi_onClickMove;
                OnKeyDown -= placementUi_OnKeyDown;
            }
            placementDrawable = null;
        }

        private Vector2 dragStartPosition;

        private bool activeChanges = false;
        private Vector2 rotationVector;
        private PlacementUIState state = PlacementUIState.Idle;
        private PlacementUITransformType transformType;

        
        private bool placementUi_OnClickDown(WidgetEvent evt, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                //editorSegment = Segment.AsEditorSegment();
                dragStartPosition = new Vector2(e.X, e.Y);
                state = PlacementUIState.Doing;
                LogChange();
                return true;
            }
            return false;
        }
        private TransformInfo GetCurrentInfo()
        {
            return new TransformInfo()
            {
                Position = Segment.Position,
                Rotation = Segment.Rotation,
                Scale = Segment.Scale
            };
        }

        
        private void LogChange()
        {
            if (UndoStack.Count > 0)
            {
                //ensure value was unique compared to the one before it.
                TransformInfo top = UndoStack.Peek();
                switch (transformType)
                {
                    case PlacementUITransformType.Move:
                        if (top.Position == Segment.Position)
                            return;
                        break;

                    case PlacementUITransformType.Rotate:
                        if (top.Rotation == Segment.Rotation)
                            return;
                        
                        break;

                    case PlacementUITransformType.Scale:
                        if (top.Scale == Segment.Scale) 
                            return;
                        break;
                }
            }
            UndoStack.Push(GetCurrentInfo());

            RedoStack.Clear();
            
        }
        private void placementUi_onClickUp(WidgetEvent evt, MouseButtonEventArgs e)
        {
            state = PlacementUIState.Idle;
            RedoStack.Push(GetCurrentInfo());
        }
        private void placementUi_onClickMove(WidgetEvent evt, MouseMoveEventArgs e)
        {
            if (state == PlacementUIState.Idle)
                return;
            
            Debug.Assert(e.XDelta != 0 || e.YDelta != 0);

            mousePosition = placementDrawable.StoryboardToScreen(new Vector2(e.X, e.Y));
            Vector2 mouseDelta = new Vector2(e.XDelta, e.YDelta);
            var dragEndPosition = dragStartPosition + mouseDelta;
            var dragFrom = placementDrawable.ScreenToSegment(dragStartPosition);
            var dragTo = placementDrawable.ScreenToSegment(dragEndPosition);

            var deltaSegment = dragTo - dragFrom;
            //Debug.Assert(dragFrom != dragTo);

            switch (transformType)
            {
                case PlacementUITransformType.Move:
                    Segment.Position += mouseDelta;
                    //editorSegment.PlacementPosition += deltaSegment;
                    break;
                case PlacementUITransformType.Scale:
                    var oldScale = Segment.Scale;
                    //editorSegment.PlacementScale *= dragTo.Length / dragFrom.Length;
                    float dScale = dragTo.Length / dragFrom.Length;
                    Segment.Scale *= dScale;
                    
                    if (Segment.Scale == 0)
                    {
                        //editorSegment.PlacementScale = oldScale;
                        Segment.Scale = oldScale;
                    }

                    break;
                case PlacementUITransformType.Rotate:
                    //calculate something here man idk
                    rotationVector = mousePosition - placementDrawable.Center;

                    var norm_rotVec = rotationVector.Normalized();
                    //offsetVector.X - cos
                    //offsetVector.Y - sin
                    Segment.Rotation = Math.Atan2(norm_rotVec.Y, norm_rotVec.X);
                    
                    break;
            }
            dragStartPosition = dragEndPosition;
            
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);
            if (placementDrawable.Segment != null)
                placementDrawable.Draw(drawContext, Manager.Camera, Bounds, actualOpacity);

            if (transformType == PlacementUITransformType.Rotate)
            {
                var renderer = DrawState.Prepare(drawContext.Get<LineRenderer>(), Manager.Camera, linesRenderStates);
                renderer.Draw(new Vector3(placementDrawable.Center), new Vector3(placementDrawable.Center+ rotationVector), Color4.Yellow);
            }
        }

        internal enum PlacementUIState
        {
            Idle, Doing
        }

        internal enum PlacementUITransformType
        {
            Move, Scale, Rotate
        }

        private class TransformInfo
        {
            internal Vector2 Position;
            internal double Rotation;
            internal double Scale;

            public override bool Equals(object obj)
            {
                return obj is TransformInfo i && GetHashCode() == i.GetHashCode();
            }

            public override int GetHashCode()
            {
                return Position.GetHashCode() ^ Rotation.GetHashCode() ^ Scale.GetHashCode();
            }
        }
    }
}
