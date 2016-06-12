using OpenTK;
using StorybrewEditor.Graphics;
using StorybrewEditor.Graphics.Drawables;

namespace StorybrewEditor.UserInterface
{
    public class DrawableContainer : Widget
    {
        public override Vector2 MinSize => drawable?.MinSize ?? Vector2.Zero;
        public override Vector2 PreferredSize => drawable?.PreferredSize ?? Vector2.Zero;

        private Drawable drawable = NullDrawable.Instance;
        public Drawable Drawable
        {
            get { return drawable; }
            set
            {
                if (drawable == value) return;
                drawable = value;
                InvalidateAncestorLayout();
            }
        }

        public DrawableContainer(WidgetManager manager) : base(manager)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            drawable = null;
            base.Dispose(disposing);
        }

        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);
            drawable?.Draw(drawContext, Manager.Camera, Bounds, actualOpacity);
        }

        public void SetFromSkin(string name)
            => Drawable = Manager.Skin.GetDrawable(name);
    }
}
