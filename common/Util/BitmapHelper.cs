using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace StorybrewCommon.Util
{
    public static class BitmapHelper
    {
        public static PinnedBitmap Blur(Bitmap source, int radius, double power)
            => Convolute(source, CalculateGaussianKernel(radius, power));

        public static double[,] CalculateGaussianKernel(int radius, double weight)
        {
            var length = radius * 2 + 1;
            var kernel = new double[length, length];
            var total = 0.0;

            var scale = 1.0 / (2.0 * Math.PI * Math.Pow(weight, 2));
            for (var y = -radius; y <= radius; y++)
                for (var x = -radius; x <= radius; x++)
                {
                    var distance = (x * x + y * y) / (2 * weight * weight);
                    var value = kernel[y + radius, x + radius] = scale * Math.Exp(-distance);
                    total += value;
                }

            for (var y = 0; y < length; y++)
                for (var x = 0; x < length; x++)
                    kernel[y, x] = kernel[y, x] / total;

            return kernel;
        }

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

        public static PinnedBitmap ConvoluteAlpha(Bitmap source, double[,] kernel, Color color)
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

                var colorRgb = (color.R << 16) | (color.G << 8) | color.B;

                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                    {
                        var a = 0.0;

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
                            }
                        }

                        var alphaInt = (int)a;
                        var alpha = (byte)((alphaInt > 255) ? 255 : ((alphaInt < 0) ? 0 : alphaInt));

                        result.Data[index++] = (alpha << 24) | colorRgb;
                    }

                return result;
            }
        }

        public static Rectangle? FindTransparencyBounds(Bitmap source)
        {
            var data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            source.UnlockBits(data);

            int xMin = int.MaxValue, xMax = int.MinValue, yMin = int.MaxValue, yMax = int.MinValue;
            var foundPixel = false;

            for (var x = 0; x < data.Width; x++)
            {
                var stop = false;
                for (var y = 0; y < data.Height; y++)
                {
                    var alpha = buffer[y * data.Stride + 4 * x + 3];
                    if (alpha != 0)
                    {
                        xMin = x;
                        stop = true;
                        foundPixel = true;
                        break;
                    }
                }
                if (stop) break;
            }

            if (!foundPixel)
                return null;

            for (var y = 0; y < data.Height; y++)
            {
                var stop = false;
                for (var x = xMin; x < data.Width; x++)
                {
                    var alpha = buffer[y * data.Stride + 4 * x + 3];
                    if (alpha != 0)
                    {
                        yMin = y;
                        stop = true;
                        break;
                    }
                }
                if (stop) break;
            }

            for (var x = data.Width - 1; x >= xMin; x--)
            {
                var stop = false;
                for (var y = yMin; y < data.Height; y++)
                {
                    var alpha = buffer[y * data.Stride + 4 * x + 3];
                    if (alpha != 0)
                    {
                        xMax = x;
                        stop = true;
                        break;
                    }
                }
                if (stop) break;
            }

            for (var y = data.Height - 1; y >= yMin; y--)
            {
                var stop = false;
                for (var x = xMin; x <= xMax; x++)
                {
                    var alpha = buffer[y * data.Stride + 4 * x + 3];
                    if (alpha != 0)
                    {
                        yMax = y;
                        stop = true;
                        break;
                    }
                }
                if (stop) break;
            }

            return Rectangle.Intersect(Rectangle.FromLTRB(xMin - 1, yMin - 1, xMax + 2, yMax + 2), new Rectangle(0, 0, source.Width, source.Height));
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
