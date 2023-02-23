using BrewLib.Graphics;
using BrewLib.Graphics.Cameras;
using BrewLib.ScreenLayers;
using BrewLib.UserInterface;
using OpenTK;
using System;

namespace StorybrewEditor.ScreenLayers
{
    public class UiScreenLayer : ScreenLayer
    {
        CameraOrtho uiCamera;
        protected WidgetManager WidgetManager { get; private set; }
        float opacity = 0;

        public override void Load()
        {
            base.Load();

            var editor = Manager.GetContext<Editor>();
            AddInputHandler(WidgetManager = new WidgetManager(Manager, editor.InputManager, editor.Skin)
            {
                Camera = uiCamera = new CameraOrtho()
            });
        }
        public override void Resize(int width, int height)
        {
            uiCamera.VirtualHeight = (int)(height * Math.Max(1024f / width, 768f / height));
            uiCamera.VirtualWidth = width * uiCamera.VirtualHeight / height;
            WidgetManager.Size = new Vector2(uiCamera.VirtualWidth, uiCamera.VirtualHeight);
            base.Resize(width, height);
        }
        public override void Update(bool isTop, bool isCovered)
        {
            base.Update(isTop, isCovered);

            if (Manager.GetContext<Editor>().IsFixedRateUpdate)
            {
                var targetOpacity = isTop ? 1 : .3f;
                if (Math.Abs(opacity - targetOpacity) <= .07) opacity = targetOpacity;
                else opacity = MathHelper.Clamp(opacity + (opacity < targetOpacity ? .07f : -.07f), 0, 1);
            }
            WidgetManager.Opacity = opacity * (float)TransitionProgress;
        }
        public override void Draw(DrawContext drawContext, double tween)
        {
            base.Draw(drawContext, tween);
            WidgetManager.Draw(drawContext);
        }
        protected void MakeTabs(Button[] buttons, Widget[] widgets)
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                var widget = widgets[i];

                button.Checkable = true;
                widget.Displayed = button.Checked;

                button.OnValueChanged += (sender, e) =>
                {
                    if (widget.Displayed = button.Checked) foreach (var otherButton in buttons)
                        if (sender != otherButton && otherButton.Checked != false) otherButton.Checked = false;
                };
            }
        }

        #region IDisposable Support

        bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    WidgetManager.Dispose();
                    uiCamera.Dispose();
                }
                WidgetManager = null;
                uiCamera = null;
                disposed = true;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}