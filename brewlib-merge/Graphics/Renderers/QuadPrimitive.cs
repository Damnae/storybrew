using System.Runtime.InteropServices;

namespace BrewLib.Graphics.Renderers
{
    [StructLayout(LayoutKind.Sequential)]
    public struct QuadPrimitive
    {
        public float x1, y1, u1, v1; public int color1;
        public float x2, y2, u2, v2; public int color2;
        public float x3, y3, u3, v3; public int color3;
        public float x4, y4, u4, v4; public int color4;
    }
}