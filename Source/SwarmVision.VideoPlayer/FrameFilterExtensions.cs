using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Classes;

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
            Parallel.For(yMin, yMax, new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, (int y) =>
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

                    var colorDifference = Compare(x*stepSize, y*stepSize, target, pattern);

                    resultFirstPx[offset + 0] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] =
                        (byte)(255-Math.Min(colorDifference*colorDifference, 255));
                }
            }

            return result;
        }

        private static HeadSearchAlgorithm headSearchAlgo = new HeadSearchAlgorithm();

        public static Point LocationOfHead(this Frame target, Frame headPattern)
        {
            return headSearchAlgo.GeneticSearch(target, headPattern);
        }

        private static AntenaSearchAlgorithm antenaSearchAlgo = new AntenaSearchAlgorithm();

        public static Point LocationOfLRA(this Frame target, Frame LRApattern)
        {
            return antenaSearchAlgo.GeneticSearch(target, LRApattern);
        }

        public static bool ValidConvoltionLocation(this Frame target, Frame pattern, int targetStartX, int targetStartY)
        {
            if (targetStartX + pattern.Width - 1 >= target.Width)
                return false;

            if (targetStartY + pattern.Height - 1 >= target.Height)
                return false;

            if (targetStartX < 0 || targetStartY < 0)
                return false;

            return true;
        }

        public static int Compare(this Frame target, Frame pattern, int targetStartX, int targetStartY)
        {
            var result = 255; //Max possible average color difference

            if (!target.ValidConvoltionLocation(pattern, targetStartX, targetStartY))
                return result;

            //Performance optimizations
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

            //Do each row in parallel
            Parallel.For(yMin, yMax, new ParallelOptions() {MaxDegreeOfParallelism = 1}, (int y) =>
            {
                var patternRowStart = stride * y; //Stride is width*3 bytes
                var targetRowStart = targetStride*(y + targetStartY);

                for (var x = xMin; x < xMax; x++)
                {
                    var patternOffset = x * 3 + patternRowStart;

                    if(patternPx0[patternOffset] < 3)
                        continue;

                    var targetOffset = (x + targetStartX)*3 + targetRowStart;

                    var colorDifference = MonochromeDifference(targetPx0, targetOffset, patternPx0, patternOffset);

                    result += colorDifference;
                }
            });

            return result / (pattern.Width * pattern.Height * 3);
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
