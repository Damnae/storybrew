using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System;

namespace StorybrewEditor.ScreenLayers.Test
{
    public class FlowLayoutTest : UiScreenLayer
    {
        private FlowLayout flowLayout1;
        private FlowLayout flowLayout2;
        private FlowLayout flowLayout3;

        public override void Load()
        {
            base.Load();

            WidgetManager.Root.StyleName = "panel";
            WidgetManager.Root.Add(flowLayout1 = new FlowLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.TopLeft,
                AnchorTo = UiAlignment.TopLeft,
                Padding = new FourSide(16),
                Spacing = 10,
                //FitChildren = true,
                //Fill = true,
            });
            foreach (var word in @"Sanae from class 3-B was an ordinary girl, one you might find anywhere. But one day, her pets ran away from her when she was at the lake.".Split(' '))
                flowLayout1.Add(new Label(WidgetManager) { Text = word, });

            WidgetManager.Root.StyleName = "panel";
            WidgetManager.Root.Add(flowLayout2 = new FlowLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.TopRight,
                AnchorTo = UiAlignment.TopRight,
                Padding = new FourSide(16),
                //FitChildren = true,
                //Fill = true,
            });
            foreach (var letter in @"Sanae from class 3-B was an ordinary girl, one you might find anywhere. But one day, her pets ran away from her when she was at the lake.")
                flowLayout2.Add(new Label(WidgetManager) { Text = letter.ToString() });

            WidgetManager.Root.Add(flowLayout3 = new FlowLayout(WidgetManager)
            {
                AnchorTarget = WidgetManager.Root,
                AnchorFrom = UiAlignment.Bottom,
                AnchorTo = UiAlignment.Bottom,
                Padding = new FourSide(16),
                //FitChildren = true,
                //Fill = true,
            });
            foreach (var letter in @"三年B組のサナエさんは、どこにでもいる普通の女の子でした。ところがある日、彼女の飼っていたペットが湖で逃げてしまいました。")
                flowLayout3.Add(new Label(WidgetManager) { Text = letter.ToString(), });
        }

        public override void Update(bool isTop, bool isCovered)
        {
            base.Update(isTop, isCovered);
            
            flowLayout1.Pack((float)(300 + Math.Sin(Manager.Editor.Time * 0.3) * 200));
            flowLayout2.Pack((float)(300 + Math.Sin(Manager.Editor.Time * 0.3) * 200));
            flowLayout3.Pack((float)(300 + Math.Sin(Manager.Editor.Time * 0.3) * 200), Manager.Editor.Window.Height / 2);
        }
    }
}
