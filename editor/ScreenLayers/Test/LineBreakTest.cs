﻿using OpenTK;
using StorybrewEditor.Graphics.Drawables;
using StorybrewEditor.UserInterface;

namespace StorybrewEditor.ScreenLayers.Test
{
    public class LineBreakTest : UiScreenLayer
    {
        private Label label1;
        private Label label2;

        public override void Load()
        {
            base.Load();

            WidgetManager.Root.StyleName = "panel";

            WidgetManager.Root.Add(label1 = new Label(WidgetManager, new TextMultiDrawable())
            {
                Text = @"Sanae from class 3-B was an ordinary girl, one you might find anywhere. But one day, her pets ran away from her when she was at the lake.",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.BottomLeft,
                AnchorTo = UiAlignment.Left,
                Offset = new Vector2(16, -8),
            });

            WidgetManager.Root.Add(label2 = new Label(WidgetManager, new TextMultiDrawable())
            {
                Text = @"三年B組のサナエさんは、どこにでもいる普通の女の子でした。ところがある日、彼女の飼っていたペットが湖で逃げてしまいました。",
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.TopLeft,
                AnchorTo = UiAlignment.Left,
                Offset = new Vector2(16, 8),
            });
        }

        public override void Update(bool isTop, bool isCovered)
        {
            base.Update(isTop, isCovered);

            label1.Pack(Manager.Editor.InputManager.Mouse.X - label1.Bounds.Left);
            label2.Pack(Manager.Editor.InputManager.Mouse.X - label2.Bounds.Left);
        }
    }
}
