using BrewLib.Graphics;
using BrewLib.Graphics.Drawables;
using OpenTK;

namespace BrewLib.UserInterface
{
    public class DrawableContainer : Widget
    {
        public override Vector2 MinSize => drawable?.MinSize ?? Vector2.Zero;
        public override Vector2 PreferredSize => drawable?.PreferredSize ?? Vector2.Zero;

        Drawable drawable = NullDrawable.Instance;
        public Drawable Drawable
        {
            get => drawable;
            set
            {
                if (drawable == value) return;
                drawable = value;
                InvalidateAncestorLayout();
            }
        }
        public DrawableContainer(WidgetManager manager) : base(manager) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { }
            drawable = null;
            base.Dispose(disposing);
        }
        protected override void DrawBackground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawBackground(drawContext, actualOpacity);
            drawable?.Draw(drawContext, Manager.Camera, Bounds, actualOpacity);
        }
        public void SetFromSkin(string name) => Drawable = Manager.Skin.GetDrawable(name);
    }
}