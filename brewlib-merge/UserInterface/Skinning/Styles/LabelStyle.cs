using BrewLib.Util;
using OpenTK.Graphics;
using System.Drawing;

namespace BrewLib.UserInterface.Skinning.Styles
{
    public class LabelStyle : WidgetStyle
    {
        public string FontName;
        public float FontSize;
        public BoxAlignment TextAlignment;
        public StringTrimming Trimming;
        public Color4 Color;
    }
}