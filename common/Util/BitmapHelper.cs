using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace StorybrewCommon.Util
{
    public static class BitmapHelper
    {
        public static PinnedBitmap Blur(Bitmap source, int radius, double power)
            => Convolute(source, calculateGaussianKernel(radius, power));

        public static PinnedBitmap Convolute(Bitmap source, double[,] kernel)
        {
            var kernelHeight = kernel.GetUpperBound(0) + 1;
            var kernelWidth = kernel.GetUpperBound(1) + 1;

            if ((kernelWidth % 2) == 0 || (kernelHeight % 2) == 0)
                throw new InvalidOperationException("Invalid kernel size");

            using (var pinnedSource = PinnedBitmap.FromBitmap(source))
            {
                var width = source.Width;
                var height = source.Height;
                var result = new PinnedBitmap(width, height);

                var index = 0;
                var halfKernelWidth = kernelWidth >> 1;
                var halfKernelHeight = kernelHeight >> 1;

                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                    {
                        var a = 0.0;
                        var r = 0.0;
                        var g = 0.0;
                        var b = 0.0;

                        for (var kernelX = -halfKernelWidth; kernelX <= halfKernelWidth; kernelX++)
                        {
                            var pixelX = kernelX + x;
                            if (pixelX < 0)
                                pixelX = 0;
                            else if (pixelX >= width)
                                pixelX = width - 1;

                            for (var kernelY = -halfKernelHeight; kernelY <= halfKernelHeight; kernelY++)
                            {
                                var pixelY = kernelY + y;
                                if (pixelY < 0)
                                    pixelY = 0;
                                else if (pixelY >= height)
                                    pixelY = height - 1;

                                var col = pinnedSource.Data[pixelY * width + pixelX];
                                var k = kernel[kernelY + halfKernelWidth, kernelX + halfKernelHeight];
                                a += ((col >> 24) & 0x000000FF) * k;
                                r += ((col >> 16) & 0x000000FF) * k;
                                g += ((col >> 8) & 0x000000FF) * k;
                                b += ((col) & 0x000000FF) * k;
                            }
                        }

                        var alphaInt = (int)a;
                        var alpha = (byte)((alphaInt > 255) ? 255 : ((alphaInt < 0) ? 0 : alphaInt));
                        if (alpha == 1) alpha = 0;

                        var redInt = (int)r;
                        var red = (byte)((redInt > 255) ? 255 : ((redInt < 0) ? 0 : redInt));

                        var greenInt = (int)g;
                        var green = (byte)((greenInt > 255) ? 255 : ((greenInt < 0) ? 0 : greenInt));

                        var blueInt = (int)b;
                        var blue = (byte)((blueInt > 255) ? 255 : ((blueInt < 0) ? 0 : blueInt));

                        result.Data[index++] = (alpha << 24) | (red << 16) | (green << 8) | blue;
                    }

                return result;
            }
        }

        private static double[,] calculateGaussianKernel(int radius, double weight)
        {
            var length = radius * 2 + 1;
            var kernel = new double[length, length];
            var total = 0.0;

            var scale = 1.0 / (2.0 * Math.PI * Math.Pow(weight, 2));
            for (var y = -radius; y <= radius; y++)
                for (var x = -radius; x <= radius; x++)
                {
                    var distance = (x * x + y * y) / (2 * weight * weight);
                    kernel[y + radius, x + radius] = scale * Math.Exp(-distance);
                    total += kernel[y + radius, x + radius];
                }

            for (var y = 0; y < length; y++)
                for (var x = 0; x < length; x++)
                    kernel[y, x] = kernel[y, x] / total;

            return kernel;
        }

        public class PinnedBitmap : IDisposable
        {
            private GCHandle handle;

            public readonly Bitmap Bitmap;
            public readonly int[] Data;

            public PinnedBitmap(int width, int height)
            {
                Data = new int[width * height];
                handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
                Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject());
            }

            public static PinnedBitmap FromBitmap(Bitmap bitmap)
            {
                var result = new PinnedBitmap(bitmap.Width, bitmap.Height);
                using (var graphics = Graphics.FromImage(result.Bitmap))
                    graphics.DrawImage(bitmap, 0, 0);
                return result;
            }

            public bool disposed;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                Bitmap.Dispose();
                handle.Free();
            }
        }
    }
}
