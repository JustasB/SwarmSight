using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Classes;
using SwarmVision.Models;
using Point = System.Drawing.Point;

namespace SwarmVision.VideoPlayer
{
    public unsafe static class FrameFilterExtensions
    {
        public static Frame EdgeFilter(this Frame target, int size = 1)
        {
            var result = target.Clone();

            //Performance optimizations
            var firstPx = target.FirstPixelPointer;
            var resultFirstPx = result.FirstPixelPointer;
            var height = target.Height;
            var width = target.Width;
            var stride = target.Stride;
            var xMin = 0;
            var xMax = width;
            var yMin = 0;
            var yMax = height;

            //Do each row in parallel
            Parallel.For(yMin, yMax, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
            }, (int y) =>
            {
                var rowStart = stride * y; //Stride is width*3 bytes

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x*3 + rowStart;

                    //take the pixel above, left, right, below and compare to the middle pixel
                    var colorDifference = 0;

                    if (x - size >= xMin)
                        colorDifference += ColorDifference(firstPx, offset, firstPx, OffsetOf(x - size, y, stride, 3));

                    if (x + size < xMax)
                        colorDifference += ColorDifference(firstPx, offset, firstPx, OffsetOf(x + size, y, stride, 3));

                    if (y - size >= yMin)
                        colorDifference += ColorDifference(firstPx, offset, firstPx, OffsetOf(x, y - size, stride, 3));

                    if (y + size < yMax)
                        colorDifference += ColorDifference(firstPx, offset, firstPx, OffsetOf(x, y + size, stride, 3));

                    resultFirstPx[offset + 0] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] = 
                        (byte) (colorDifference / 12);
                }
            });

            return result;
        }

        public static Frame Convolve(this Frame target, Frame pattern, int stepSize = 1)
        {
            var resultWidth = target.Width/stepSize;
            var resultHeight = target.Height/stepSize;
            var bmp = new Bitmap(resultWidth, resultHeight, target.PixelFormat);
            var result = Frame.FromBitmap(bmp);
            result.ShalowCopy(target);

            var resultFirstPx = result.FirstPixelPointer;

            for (var y = 0; y < result.Height; y++)
            {
                var rowStart = result.Stride * y;

                for (var x = 0; x < result.Width; x++)
                {
                    var offset = x * 3 + rowStart;

                    var colorDifference = target.Compare(pattern, x * stepSize, y * stepSize);

                    resultFirstPx[offset + 0] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] =
                        (byte)(255-Math.Min(colorDifference*colorDifference, 255));
                }
            }

            return result;
        }

        private static readonly HeadSearchAlgorithm HeadSearchAlgo = new HeadSearchAlgorithm();
        //private static readonly SimulatedAnnealingAlgorithm HeadSearchAlgo = new SimulatedAnnealingAlgorithm();

        public static HeadModel LocationOfHead(this Frame target)
        {
            return HeadSearchAlgo.Search(target);
        }

        public static void DrawRectangle(this Frame target, int x, int y, int width, int height)
        {
            for (var row = 0; row < height; row++)
            {
                var offsetL = target.FirstPixelPointer + OffsetOf(x,         y+row, target.Stride, 3);
                var offsetR = target.FirstPixelPointer + OffsetOf(x+width-1, y+row, target.Stride, 3);

                offsetL[0] = offsetL[1] = offsetL[2] = offsetR[0] = offsetR[1] = offsetR[2] = 255;
            }

            for (var col = 0; col < width; col++)
            {
                var offsetT = target.FirstPixelPointer + OffsetOf(x + col, y,            target.Stride, 3);
                var offsetB = target.FirstPixelPointer + OffsetOf(x + col, y + height-1, target.Stride, 3);

                offsetT[0] = offsetT[1] = offsetT[2] = offsetB[0] = offsetB[1] = offsetB[2] = 255;
            }
        }

        public static void DrawFrame(this Frame target, Frame source, int x, int y)
        {

            for (var row = 0; row <source.Height; row++)
            {
                for (var col = 0; col < source.Width; col++)
                {
                    var offsetT = target.FirstPixelPointer + OffsetOf(x + col, y + row, target.Stride, 3);
                    var offsetS = source.FirstPixelPointer + OffsetOf(col,     row,       source.Stride, 3);

                    if (offsetS[0] > 3)
                        offsetT[0] = offsetS[0];

                    if (offsetS[1] > 3)
                        offsetT[1] = offsetS[1];

                    if (offsetS[2] > 3)
                        offsetT[2] = offsetS[2];
                }
            }

            
        }

        public static Frame Trim(this Frame target)
        {
            var height = target.Height;
            var width = target.Width;
            var minY = 0;
            var maxY = height - 1;
            var minX = 0;
            var maxX = width - 1;

            //Find top
            for (var row = 0; row < height; row++)
            {
                for (var col = 0; col < width; col++)
                {
                    var offset = target.FirstPixelPointer + OffsetOf(col, row, target.Stride, 3);

                    if (offset[0] > 5 || offset[1] > 5 || offset[2] > 5)
                    {
                        minY = row;
                        row = height;

                        break;
                    }
                }

                
            }

            //Find bottom
            for (var row = height-1; row >= minY; row--)
            {
                for (var col = 0; col < width; col++)
                {
                    var offset = target.FirstPixelPointer + OffsetOf(col, row, target.Stride, 3);

                    if (offset[0] > 5 || offset[1] > 5 || offset[2] > 5)
                    {
                        maxY = row;
                        row = minY-1;
                        break;
                    }
                }
            }

            //Find left
            for (var col = 0; col < width; col++)
            {
                for (var row = minY; row <= maxY; row++)
                {
                    var offset = target.FirstPixelPointer + OffsetOf(col, row, target.Stride, 3);

                    if (offset[0] > 5 || offset[1] > 5 || offset[2] > 5)
                    {
                        minX = col;
                        col = width;
                        break;
                    }
                }
            }

            //Find right
            for (var col = width-1; col >= minX; col--)
            {
                for (var row = minY; row <= maxY; row++)
                {
                    var offset = target.FirstPixelPointer + OffsetOf(col, row, target.Stride, 3);

                    if (offset[0] > 5 || offset[1] > 5 || offset[2] > 5)
                    {
                        maxX = col;
                        col = minX-1;
                        break;
                    }
                }
            }

            var bmp = new Bitmap(maxX - minX + 1, maxY - minY + 1, PixelFormat.Format24bppRgb);
            
            using (var gfx = Graphics.FromImage(bmp))
            {
                gfx.DrawImage(target.Bitmap, new Point(-minX, -minY));
            }

            

            return Frame.FromBitmap(bmp);
        }

        public static double StdDev<T>(this IEnumerable<T> list, Func<T, double> values)
        {
            var mean = 0.0;
            var sum = 0.0;
            var stdDev = 0.0;
            var n = 0;
            foreach (var value in list.Select(values))
            {
                n++;
                var delta = value - mean;
                mean += delta / n;
                sum += delta * (value - mean);
            }
            if (1 < n)
                stdDev = Math.Sqrt(sum / (n - 1));

            return stdDev;

        }

        public static Frame RotateScale(this Frame target, double angle, double scale = 1.0, Color? bgColor = null)
        {
            if (bgColor == null)
                bgColor = Color.Black;

            //New blank bmp that will hold the transformed target
            var bmp = new Bitmap(target.Width, target.Height, PixelFormat.Format24bppRgb);

            //gfx will perform the transformations on bmp
            var gfx = Graphics.FromImage(bmp);

            //Set the background
            gfx.Clear(bgColor.Value);

            //Set the scaling (if greater than 5%)
            if(Math.Abs(scale - 1.0) > 0.05)
                gfx.ScaleTransform((float)scale, (float)scale, MatrixOrder.Append);

            //Move the world origin to center of bmp, adjusted for scale
            gfx.TranslateTransform((float) (bmp.Width / 2.0 / scale), (float) (bmp.Height / 2.0 / scale));

            //Apply rotations around the center (if greater than 1 degree)
            if(Math.Abs(angle) > 1)
                gfx.RotateTransform((float) angle);

            gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.CompositingQuality = CompositingQuality.HighQuality;

            //Paint the original onto the new bitmap (offset from the center)
            gfx.DrawImage(target.Bitmap, new PointF(-target.Width / 2.0f, -target.Height / 2.0f));

            gfx.Dispose();

            return Frame.FromBitmap(bmp);
        }

        public static double NormalizeAngle(double angleDeg)
        {
            double angle = angleDeg/180*2*Math.PI;
            double division = angle / (Math.PI / 2);
            double fraction = Math.Ceiling(division) - division;

            return (fraction * Math.PI / 2);
        }

        public static Tuple<int, int> RotatedSize(int width, int height, double angle)
        {
            var normalizedRotationAngle = (angle % 45.0) / 180.0 * Math.PI;
            var newWidth = (int)Math.Ceiling(Math.Cos(normalizedRotationAngle) * width + Math.Sin(normalizedRotationAngle) * height);
            var newHeight = (int)Math.Ceiling(Math.Cos(normalizedRotationAngle) * height + Math.Sin(normalizedRotationAngle) * width);

            return new Tuple<int, int>(newWidth, newHeight);
        }

        public static bool ValidConvolutionLocation(this Frame target, int patternWidth, int patternHeight, int targetStartX, int targetStartY, double angle = 0)
        {
            if (angle % 180 != 0.0)
            {
                var rotatedSize = RotatedSize(patternWidth, patternHeight, angle);

                patternWidth = rotatedSize.Item1;
                patternHeight = rotatedSize.Item2;
            }

            if (targetStartX + patternWidth - 1 >= target.Width)
                return false;

            if (targetStartY + patternHeight - 1 >= target.Height)
                return false;

            if (targetStartX < 0 || targetStartY < 0)
                return false;

            return true;
        }

        public static double Compare(this Frame target, Frame pattern, int targetStartX, int targetStartY)
        {
            var result = 255; //Max possible average color difference

            if (!target.ValidConvolutionLocation(pattern.Width, pattern.Height, targetStartX, targetStartY))
                return result;

            //Performance optimizations
            if(pattern.PixelBytes == null)
                throw new ArgumentException("Pattern bytes are null");

            var targetPx0 = target.FirstPixelPointer;
            var patternPx0 = pattern.FirstPixelPointer;
            var height = pattern.Height;
            var width = pattern.Width;
            var stride = pattern.Stride;
            var targetStride = target.Stride;
            var xMin = 0;
            var xMax = width;
            var yMin = 0;
            var yMax = height;

            fixed (byte* tPx0 = target.PixelBytes, pPx0 = pattern.PixelBytes)
            {
                //Do each row in parallel
                Parallel.For(yMin, yMax, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, (int y) =>
                {
                    var patternRowStart = stride * y; //Stride is width*3 bytes
                    var targetRowStart = targetStride * (y + targetStartY);

                    for (var x = xMin; x < xMax; x++)
                    {
                        var patternOffset = x * 3 + patternRowStart;

                        if (patternPx0[patternOffset] < 3)
                            continue;

                        var targetOffset = (x + targetStartX) * 3 + targetRowStart;

                        var colorDifference = MonochromeDifference(targetPx0, targetOffset, patternPx0, patternOffset);

                        result += colorDifference;
                    }
                });
            }

            return result / (pattern.Width * pattern.Height * 3.0);
        }

        private static int OffsetOf(int x, int y, int stride, int bytesPerPixel)
        {
            return stride*y + x*bytesPerPixel;
        }

        private static int MonochromeDifference(byte* firstPixel1, int offset1, byte* firstPixel2, int offset2)
        {
            return
                Math.Abs(firstPixel1[offset1 + 0] - firstPixel2[offset2 + 0]) * 3;
        }

        private static int ColorDifference(byte* firstPixel1, int offset1, byte* firstPixel2, int offset2)
        {
            return
                Math.Abs(firstPixel1[offset1 + 0] - firstPixel2[offset2 + 0]) +
                Math.Abs(firstPixel1[offset1 + 1] - firstPixel2[offset2 + 1]) +
                Math.Abs(firstPixel1[offset1 + 2] - firstPixel2[offset2 + 2]);
        }
    }
}
