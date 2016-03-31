using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Cameras;
using StorybrewEditor.UserInterface;
using System;

namespace StorybrewEditor.ScreenLayers
{
    public class UiScreenLayer : ScreenLayer
    {
        private CameraOrtho uiCamera;
        private WidgetManager widgetManager;
        protected WidgetManager WidgetManager => widgetManager;

        private float opacity = 0;

        public override void Load()
        {
            base.Load();
            AddInputHandler(widgetManager = new WidgetManager(Manager)
            {
                Camera = uiCamera = new CameraOrtho(),
            });
        }

        public override void Resize(int width, int height)
        {
            uiCamera.VirtualHeight = (int)(height * Math.Max(1024f / width, 768f / height));
            uiCamera.VirtualWidth = width * uiCamera.VirtualHeight / height;
            widgetManager.Size = new Vector2(uiCamera.VirtualWidth, uiCamera.VirtualHeight);
            base.Resize(width, height);
        }

        public override void Update(bool isTop, bool isCovered)
        {
            base.Update(isTop, isCovered);

            if (Manager.Editor.IsFixedRateUpdate)
            {
                var targetOpacity = (isTop ? 1f : 0.3f);
                if (Math.Abs(opacity - targetOpacity) <= 0.07f) opacity = targetOpacity;
                else opacity = MathHelper.Clamp(opacity + (opacity < targetOpacity ? 0.07f : -0.07f), 0, 1);
            }
            widgetManager.Opacity = opacity * (float)TransitionProgress;
        }

        public override void Draw(DrawContext drawContext)
        {
            base.Draw(drawContext);
            widgetManager.Draw(drawContext);
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
                    if (widget.Displayed = button.Checked)
                        foreach (var otherButton in buttons)
                            if (sender != otherButton) otherButton.Checked = false;
                };
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    widgetManager.Dispose();
                    uiCamera.Dispose();
                }
                widgetManager = null;
                uiCamera = null;
                disposedValue = true;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
