using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using SwarmSight.Hardware;
using Cudafy;
using Cudafy.Host;
using Point = System.Drawing.Point;

namespace SwarmSight.Filters
{
    public unsafe static class FrameFilterExtensions
    {
        public static void CopyToWriteableBitmap(this Frame source, WriteableBitmap writeableBitmap)
        {
            // Reserve the back buffer for updates.
            writeableBitmap.Lock();

            var targetFirstPx = (int) writeableBitmap.BackBuffer;
            var targetStride = writeableBitmap.BackBufferStride;
            var sourceFirstPx = source.FirstPixelPointer;
            var sourceStride = source.Stride;

            Parallel.For(0, source.Height, y =>
            {
                for (var x = 0; x < source.Width; x++)
                {
                    // Get a pointer to the back buffer.
                    var pBackBuffer = targetFirstPx + y*targetStride + x*3;

                    var sourceOffset = sourceStride * y + x*3;

                    // Compute the pixel's color.v
                    var newValue = sourceFirstPx[sourceOffset+2] << 16; // R
                    newValue |=    sourceFirstPx[sourceOffset+1] << 8;  // G
                    newValue |=    sourceFirstPx[sourceOffset  ] << 0;  // B

                    // Assign the color data to the pixel.
                    *((int*) pBackBuffer) = newValue;
                }
            });

            // Specify the area of the bitmap that changed.
            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, source.Width, source.Height));

            // Release the back buffer and make it available for display.
            writeableBitmap.Unlock();
        }
        public static Frame SubClipped(this Frame target, int x, int y, int width, int height)
        {
            x = Math.Max(0, Math.Min(target.Width - width, x));
            y = Math.Max(0, Math.Min(target.Height - height, y));

            if (!target.IsOnGPU)
            {
                //return new Frame(target.Bitmap.Clone(new Rectangle(x, y, width, height), target.Bitmap.PixelFormat),
                //    false);

                var result = new Frame(width, height, target.Bitmap.PixelFormat, false);

                result.DrawFrame(target, 0, 0, 1, 0, x, y);

                return result;
            }
            else
            {
                var resultStride = 4*(target.Width*3 + 31)/32;
                var resultBytes = GPU.Current.Allocate<byte>(height*resultStride);

                GPU.Current.Launch
                    (
                        Hardware.Filters.Grid(width, height), Hardware.Filters.Block,
                        "SubClipKernel", target.PixelBytes, target.Stride, target.Width, target.Height,
                        resultBytes, resultStride, width, height, x, y
                    );

                return new Frame(width, height, resultStride, target.PixelFormat, resultBytes, true);
            }
        }
        public static Frame MapByDistanceFunction(this Frame target, Func<Point, double> distanceFunction)
        {
            var result = new Frame(new Bitmap(target.Width, target.Height, target.PixelFormat), false);

            var firstPx = result.FirstPixelPointer;

            Parallel.For(0, target.Height, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
            }, (int y) =>
            {
                var rowStart = target.Stride * y; //Stride is width*3 bytes

                for (var x = 0; x < target.Width; x++)
                {
                    var distance = distanceFunction(new Point(x, y));
                    var offset = x * 3 + rowStart;

                    firstPx[offset + 0] = 
                        firstPx[offset + 1] =
                        firstPx[offset + 2] = (byte)(255 * distance);
                }
            });

            return result;
        }
        public static Frame MapIfTrue(this Frame target, Color color, Func<Point, bool> conditional)
        {
            var result = new Frame(new Bitmap(target.Width, target.Height, target.PixelFormat), false);

            var firstPx = result.FirstPixelPointer;

            Parallel.For(0, target.Height, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
            }, (int y) =>
            {
                var rowStart = target.Stride * y; //Stride is width*3 bytes

                for (var x = 0; x < target.Width; x++)
                {
                    if (conditional(new Point(x, y)))
                    {
                        var offset = x * 3 + rowStart;

                        firstPx[offset + 0] = color.B;
                        firstPx[offset + 1] = color.G;
                        firstPx[offset + 2] = color.R;
                    }
                }
            });

            return result;
        }
        public static Frame AveragePixels(this Frame[] frames)
        {
            var target = frames[0];
            var result = new Frame(new Bitmap(target.Width, target.Height, target.PixelFormat), false);
            var resultPx = result.FirstPixelPointer;
            var count = (double)frames.Length;

            for (var f = 0; f < frames.Length; f++)
            {
                var targetPx = frames[f].FirstPixelPointer;
                
                Parallel.For(0, target.Height, new ParallelOptions()
                {
                    //MaxDegreeOfParallelism = 1
                }, (int y) =>
                {
                    var rowStart = target.Stride * y; //Stride is width*3 bytes

                    for (var x = 0; x < target.Width; x++)
                    {
                        var offset = x * 3 + rowStart;

                        var contribution = Color.FromArgb(
                            (int)Math.Round(targetPx[offset + 2] / count),
                             (int)Math.Round(targetPx[offset + 1] / count),
                              (int)Math.Round(targetPx[offset] / count)
                            );


                        resultPx[offset + 0] += contribution.B;
                        resultPx[offset + 1] += contribution.G;
                        resultPx[offset + 2] += contribution.R;

                    }
                });
            }

            return result;
        }
        
        public static Frame ReMap(this Frame target, Func<Color, Color> mapFunction)
        {
            var result = new Frame(new Bitmap(target.Width, target.Height, target.PixelFormat), false);

            var targetPx = target.FirstPixelPointer;
            var resultPx = result.FirstPixelPointer;

            Parallel.For(0, target.Height, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
            }, (int y) =>
            {
                var rowStart = target.Stride * y; //Stride is width*3 bytes

                for (var x = 0; x < target.Width; x++)
                {
                    var offset = x * 3 + rowStart;
                    var color = Color.FromArgb(targetPx[offset + 2], targetPx[offset + 1], targetPx[offset]);
                    var remapColor = mapFunction(color);

                    resultPx[offset + 0] = remapColor.B;
                    resultPx[offset + 1] = remapColor.G;
                    resultPx[offset + 2] = remapColor.R;
                    
                }
            });

            return result;
        }
        public static Frame TwoFramePixelWiseOperation(this Frame target, Frame secondFrame, Func<Color, Color, Color> operatorFunction)
        {
            var result = new Frame(new Bitmap(target.Width, target.Height, target.PixelFormat), false);

            var targetPx = target.FirstPixelPointer;
            var secondPx = secondFrame.FirstPixelPointer;
            var resultPx = result.FirstPixelPointer;

            Parallel.For(0, target.Height, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
            }, (int y) =>
            {
                var rowStart = target.Stride * y; //Stride is width*3 bytes

                for (var x = 0; x < target.Width; x++)
                {
                    var offset = x * 3 + rowStart;
                    var color = Color.FromArgb(targetPx[offset + 2], targetPx[offset + 1], targetPx[offset]);
                    var color2 = Color.FromArgb(secondPx[offset + 2], secondPx[offset + 1], secondPx[offset]);
                    var remapColor = operatorFunction(color, color2);

                    resultPx[offset + 0] = remapColor.B;
                    resultPx[offset + 1] = remapColor.G;
                    resultPx[offset + 2] = remapColor.R;

                }
            });

            return result;
        }
        public static Frame And(this Frame a, Frame b, int threshold = 127)
        {
            return a.TwoFramePixelWiseOperation(b, (colorA, colorB) =>
                colorA.B >= threshold && colorB.B >= threshold
                    ? Color.White
                    : Color.Black);
        }
        public static Frame Or(this Frame a, Frame b, int threshold = 127)
        {
            return a.TwoFramePixelWiseOperation(b, (colorA, colorB) =>
                colorA.B >= threshold || colorB.B >= threshold
                    ? Color.White
                    : Color.Black);
        }
        public static void ColorIfTrue(this Frame target, Color color, Func<Point, bool> conditional)
        {
            var firstPx = target.FirstPixelPointer;

            Parallel.For(0, target.Height, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
            }, (int y) =>
            {
                var rowStart = target.Stride * y; //Stride is width*3 bytes

                for (var x = 0; x < target.Width; x++)
                {
                    if (conditional(new Point(x, y)))
                    {
                        var offset = x*3 + rowStart;

                        firstPx[offset + 0] = color.B;
                        firstPx[offset + 1] = color.G;
                        firstPx[offset + 2] = color.R;
                    }
                }
            });
        }
        public static bool Between(this double target, double low, double high)
        {
            return low <= target && target <= high;
        }

        public delegate Color SurroundingPixelsMapFunction(Color? top, Color? left, Color? bottom, Color? right, Color current);

        public static List<Point> FillGaps(this List<Point> target, int size = 1)
        {
            var addedPoints = new List<Point>(target.Count);
            var lookup = target.ToLookup(point => point);

            Parallel.For(0, target.Count, i =>
            {
                var center = target[i];
                var top =    new Point(center.X,        center.Y - size - 1);
                var bottom = new Point(center.X,        center.Y + size + 1);
                var left =   new Point(center.X - size - 1, center.Y);
                var right =  new Point(center.X + size + 1, center.Y);

                if(lookup.Contains(top))
                    lock(addedPoints) {  addedPoints.Add(new Point(center.X, center.Y - 1)); }

                if (lookup.Contains(bottom))
                    lock (addedPoints) { addedPoints.Add(new Point(center.X, center.Y + 1)); }

                if (lookup.Contains(left))
                    lock (addedPoints) { addedPoints.Add(new Point(center.X - 1, center.Y)); }

                if (lookup.Contains(right))
                    lock (addedPoints) { addedPoints.Add(new Point(center.X + 1, center.Y)); }
            });

            target.AddRange(addedPoints.Distinct());
            target = target.Distinct().ToList();

            return target;
        }

        public static Frame FillGaps(this Frame target, int size = 1, int threshold = 127)
        {
            return target.ReMapBasedOnSurroundingPixels((t,l,b,r,c) =>
            {
                //if bottom-top or left-right are white, then fill in the middle
                if (t.HasValue && b.HasValue && t.Value.B >= threshold && b.Value.B >= threshold)
                    return Color.White;

                if (l.HasValue && r.HasValue && l.Value.B >= threshold && r.Value.B >= threshold)
                    return Color.White;

                return c; //Keep as is
            }, 
            size);
        }

        public static Frame ReMapBasedOnSurroundingPixels(this Frame target, SurroundingPixelsMapFunction mapFunction, int size = 1)
        {
            var result = new Frame(new Bitmap(target.Width, target.Height, target.PixelFormat), false);

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
                    var offset = x * 3 + rowStart;

                    var leftAddr = x - size >= xMin ?  OffsetOf(x - size, y, stride, 3) : (int?)null;
                    var rightAddr = x + size < xMax ?  OffsetOf(x + size, y, stride, 3) : (int?)null;
                    var topAddr = y - size >= yMin ?   OffsetOf(x, y - size, stride, 3) : (int?)null;
                    var bottomAddr = y + size < yMax ? OffsetOf(x, y + size, stride, 3) : (int?)null;

                    var currentAddr = OffsetOf(x, y, stride, 3);

                    var top = topAddr != null ?       Color.FromArgb(firstPx[topAddr.Value + 2], firstPx[topAddr.Value + 1], firstPx[topAddr.Value]) : (Color?)null;
                    var left = leftAddr != null ?     Color.FromArgb(firstPx[leftAddr.Value + 2], firstPx[leftAddr.Value + 1], firstPx[leftAddr.Value]) : (Color?)null;
                    var right = rightAddr != null ?   Color.FromArgb(firstPx[rightAddr.Value + 2], firstPx[rightAddr.Value + 1], firstPx[rightAddr.Value]) : (Color?)null;
                    var bottom = bottomAddr != null ? Color.FromArgb(firstPx[bottomAddr.Value + 2], firstPx[bottomAddr.Value + 1], firstPx[bottomAddr.Value]) : (Color?)null;

                    var current = Color.FromArgb(firstPx[currentAddr + 2], firstPx[currentAddr + 1], firstPx[currentAddr]);

                    var resultColor = mapFunction(top, left, bottom, right, current);

                    resultFirstPx[offset    ] = resultColor.B;
                    resultFirstPx[offset + 1] = resultColor.G;
                    resultFirstPx[offset + 2] = resultColor.R;
                }
            });


            return result;
        }
        public static Frame EdgeFilter(this Frame target, int size = 1)
        {
            if (target.IsOnGPU)
                return target.EdgeFilterGPU(size);

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

        public static Frame EdgeFilterGPU(this Frame target, int size = 1)
        {
            var result = target;

            GPU.Current.Launch
            (
                Hardware.Filters.Grid(target.Width, target.Height), Hardware.Filters.Block,
                "EdgeFilterKernel", target.PixelBytes, target.Stride, target.Width, target.Height, size
            );

            return result;
        }

        public static Frame ContrastFilterGPU(this Frame target, float extent = 0.01f, float shift = 127.5f)
        {
            var result = target;

            GPU.Current.Launch
            (
                Hardware.Filters.Grid(target.Width, target.Height), Hardware.Filters.Block,
                "ContrastFilterKernel", target.PixelBytes, target.Stride, target.Width, target.Height, extent, shift
            );

            return result;
        }

        public static void DrawSegmentsGPU(this Frame target, List<LineSegment> segments)
        {
            if (segments.Count == 0)
                return;
            
            GPU.Current.CopyToConstantMemory(LineSegment.ToFloatArray(segments), Kernels.SegmentCache);

            GPU.Current.Launch
            (
                Hardware.Filters.Grid(target.Width, target.Height), Hardware.Filters.Block,
                "DrawLinesKernel", target.PixelBytes, target.Stride, target.Width, target.Height,
                segments.Count
            );
        }

        public static List<Point> PointsOverThreshold(this Frame target, int threshold)
        {
            var result = new List<Point>();

            //Performance optimizations
            var firstPx = target.FirstPixelPointer;
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
                var rowResult = new List<Point>();

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    if(firstPx[offset] > threshold) //B only
                        rowResult.Add(new Point(x,y));
                }

                lock (result)
                {
                    result.AddRange(rowResult);
                }
            });

            return result;
        }

        public static void ForEachPoint(this Frame target, Action<Point,Color> action)
        {
            //Performance optimizations
            var firstPx = target.FirstPixelPointer;
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
                var rowResult = new Dictionary<Point, Color>();

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    var pt = new Point(x, y);
                    var col = Color.FromArgb(firstPx[offset + 2], firstPx[offset + 1], firstPx[offset]);

                    action(pt, col);
                }
            });
        }

        public static Frame Threshold(this Frame target, int colorThreshold)
        {
            return target.ReMap(pixelColor => pixelColor.B >= colorThreshold ? Color.White : Color.Black);
        }


        public static void ColorPixels(this Frame target, List<Point> points, Color color)
        {
            var firstPx = target.FirstPixelPointer;

            points.AsParallel().ForAll(p =>
            {
                var offset = 3*p.X + p.Y*target.Stride;

                firstPx[offset + 0] = color.B;
                firstPx[offset + 1] = color.G;
                firstPx[offset + 2] = color.R;
            });
        }



        public static Frame ContrastFilter(this Frame target, float extent = 0.01f, float shift = 127.5f)
        {
            if (target.IsOnGPU)
                return target.ContrastFilterGPU(extent, shift);

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
                    var offset = x * 3 + rowStart;

                    resultFirstPx[offset + 0] = (byte)Logistic(firstPx[offset + 0], 255, extent, shift);
                    resultFirstPx[offset + 1] = (byte)Logistic(firstPx[offset + 1], 255, extent, shift);
                    resultFirstPx[offset + 2] = (byte)Logistic(firstPx[offset + 2], 255, extent, shift);

                }
            });

            return result;
        }

        public static double Logistic(float x, float height, float extent, float shift)
        {
            return height/(1 + (float)Math.Exp(-extent*(x - shift)));
        }
        public static Frame CloseToColorMap(this Frame target, Color color, int distance = 20)
        {
            var result = new Frame(new Bitmap(target.Width, target.Height, target.PixelFormat), false);            

            var targetPx0 = target.FirstPixelPointer;
            var targetStride = target.Stride;
            var resultPx0 = result.FirstPixelPointer;
            var xMin = 0;
            var xMax = target.Width;
            var yMin = 0;
            var yMax = target.Height;
            var R = color.R;
            var G = color.G;
            var B = color.B;

            fixed (byte* tPx0 = target.PixelBytes)
            {
                //Do each row in parallel
                Parallel.For(yMin, yMax, new ParallelOptions() { /*MaxDegreeOfParallelism = 1*/ }, (int y) =>
                {
                    var targetRowStart = targetStride * y;

                    for (var x = xMin; x < xMax; x++)
                    {
                        var targetOffset = x * 3 + targetRowStart;

                        //Order is BGR. Paint px white if close to the color
                        if (Math.Abs(targetPx0[targetOffset]   - B) <= distance &&
                            Math.Abs(targetPx0[targetOffset+1] - G) <= distance &&
                            Math.Abs(targetPx0[targetOffset+2] - R) <= distance)
                                resultPx0[targetOffset] = resultPx0[targetOffset+1] = resultPx0[targetOffset+2] = 255;

                    }
                });
            }

            return result;
        }
        public static Frame Convolve(this Frame target, Frame pattern, int stepSize = 1)
        {
            var resultWidth = target.Width/stepSize;
            var resultHeight = target.Height/stepSize;
            var bmp = new Bitmap(resultWidth, resultHeight, target.PixelFormat);
            var result = new Frame(bmp, false);
            result.ShalowCopy(target);

            var resultFirstPx = result.FirstPixelPointer;

            for (var y = 0; y < result.Height; y++)
            {
                var rowStart = result.Stride * y;

                for (var x = 0; x < result.Width; x++)
                {
                    var offset = x * 3 + rowStart;

                    var colorDifference = target.AverageColorDifference(pattern, x * stepSize, y * stepSize);

                    resultFirstPx[offset + 0] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] =
                        (byte)(255-Math.Min(colorDifference*colorDifference, 255));
                }
            }

            return result;
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

        public static void DrawFrame(this Frame target, Frame source, int targetStartX, int targetStartY, double alpha = 1.0, int threshold = 3, int sourceStartX = 0, int sourceStartY = 0)
        {
            var tX = targetStartX;
            var tY = targetStartY;
            var sX = sourceStartX;
            var sY = sourceStartY;

            if (target.IsOnGPU)
            {
                target.DrawFrameGPU(source, tX, tY);
                return;
            }

            var height = Math.Min(target.Height, source.Height);
            var width = Math.Min(target.Width, source.Width);
            var px01 = target.FirstPixelPointer;
            var px02 = source.FirstPixelPointer;
            var targetStride = target.Stride;
            var sourceStride = source.Stride;
            var thresh = threshold;
            var alph = alpha;
            var oneMinusAlpha = 1 - alpha;

            //Simple case of full overwrite without threshold
            if (alph == 1.0 && thresh == 0)
            {
                //for (var row = 0; row < height; row++)
                Parallel.For(0, height, row =>
                {
                    var yT = px01 + targetStride * (tY + row);
                    var yS = px02 + sourceStride * (sY + row);

                    for (var col = 0; col < width; col++)
                    {
                        var offsetT = yT + (tX + col) * 3;
                        var offsetS = yS + (sX + col) * 3;

                        offsetT[0] = offsetS[0];
                        offsetT[1] = offsetS[1];
                        offsetT[2] = offsetS[2];
                    }
                });
            }
            else
            {
                for (var row = 0; row < height; row++)
                {
                    for (var col = 0; col < width; col++)
                    {
                        var offsetT = px01 + targetStride * (tY + row) + (tX + col) * 3;
                        var offsetS = px02 + sourceStride * row + col * 3;

                        if (thresh == 0 || (offsetS[0] >= thresh && offsetS[1] >= thresh && offsetS[2] >= thresh))
                        {
                            offsetT[0] = (byte)(offsetS[0] * alph + offsetT[0] * oneMinusAlpha);
                            offsetT[1] = (byte)(offsetS[1] * alph + offsetT[1] * oneMinusAlpha);
                            offsetT[2] = (byte)(offsetS[2] * alph + offsetT[2] * oneMinusAlpha);
                        }
                    }
                }
            }
            
        }

        public static void DrawFrameGPU(this Frame target, Frame top, int x, int y)
        {
            GPU.Current.Launch
            (
                Hardware.Filters.Grid(top.Width, top.Height), Hardware.Filters.Block, "DrawOnTopKernel",
                target.PixelBytes, target.Stride, target.Width, target.Height,
                top.PixelBytes, top.Stride, top.Width, top.Height,
                x , y
            );
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

            

            return new Frame(bmp,false);
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

        public static Frame RotateScaleGPU(this Frame target, double angle, double scale = 1.0, Color? bgColor = null)
        {
            var resultBytes = GPU.Current.Allocate<byte>(target.PixelBytesLength);
            var result = new Frame(target.Width, target.Height, target.Stride,target.PixelFormat, resultBytes, true);
            result.ShalowCopy(target);

            GPU.Current.Launch
            (
                Hardware.Filters.Grid(target.Width, target.Height), Hardware.Filters.Block,
                Kernels.RotateScaleKernel, result.PixelBytes, target.PixelBytes, target.Stride, target.Width, target.Height, target.Width, 0f, 0f, (float)(angle/180*GMath.PI), (float)scale
            );

            return result;
        }

        public static Frame RotateScale(this Frame target, double angle, double scaleX = 1.0, double scaleY = 1.0, Color? bgColor = null)
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
            //if(Math.Abs(scale - 1.0) > 0.05)
                gfx.ScaleTransform((float)scaleX, (float)scaleY, MatrixOrder.Append);

            //Move the world origin to center of bmp, adjusted for scale
            gfx.TranslateTransform((float) (bmp.Width / 2.0 / scaleX), (float) (bmp.Height / 2.0 / scaleY));

            //Apply rotations around the center (if greater than 1 degree)
            if(Math.Abs(angle) > 1)
                gfx.RotateTransform((float) angle);

            gfx.InterpolationMode = InterpolationMode.NearestNeighbor;
            gfx.SmoothingMode = SmoothingMode.HighSpeed;
            gfx.CompositingQuality = CompositingQuality.HighSpeed;

            //Paint the original onto the new bitmap (offset from the center)
            gfx.DrawImage(target.Bitmap, new PointF(-target.Width / 2.0f, -target.Height / 2.0f));

            gfx.Dispose();

            return new Frame(bmp, false);
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
            //if (angle % 180 != 0.0)
            //{
            //    var rotatedSize = RotatedSize(patternWidth, patternHeight, angle);

            //    patternWidth = rotatedSize.Item1;
            //    patternHeight = rotatedSize.Item2;
            //}

            if (targetStartX + patternWidth - 1 >= target.Width)
                return false;

            if (targetStartY + patternHeight - 1 >= target.Height)
                return false;

            if (targetStartX < 0 || targetStartY < 0)
                return false;

            return true;
        }



        public static Frame Subtract(this Frame source, Frame what, int xStart, int yStart)
        {
            var result = source.Clone();

            var targetPx0 = source.FirstPixelPointer;
            var whatPx0 = what.FirstPixelPointer;
            var resultPx0 = result.FirstPixelPointer;
            var targetStride = source.Stride;
            var whatStride = what.Stride;
            var xMin = xStart;
            var xMax = Math.Min(source.Width, xStart+what.Width);
            var yMin = yStart;
            var yMax = Math.Min(source.Height, yStart+what.Height);

            fixed (byte* tPx0 = source.PixelBytes, wPx0 = what.PixelBytes)
            {
                //Do each row in parallel
                Parallel.For(yMin, yMax, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, (int y) =>
                {
                    var targetRowStart = targetStride * y;
                    var whatRowStart = whatStride*(y-yMin);

                    for (var x = xMin; x < xMax; x++)
                    {
                        var targetOffset = x * 3 + targetRowStart;
                        var whatOffset = (x - xStart)*3 + whatRowStart;

                        resultPx0[targetOffset    ] = (byte)(Math.Max(targetPx0[targetOffset  ] - whatPx0[whatOffset  ], 0));
                        resultPx0[targetOffset + 1] = (byte)(Math.Max(targetPx0[targetOffset+1] - whatPx0[whatOffset+1], 0));
                        resultPx0[targetOffset + 2] = (byte)(Math.Max(targetPx0[targetOffset+2] - whatPx0[whatOffset+2], 0));

                    }
                });
            }

            source.Dispose();

            return result;
        }

        public static double AverageColorDifference(this Frame target, Frame pattern, int targetStartX, int targetStartY)
        {
            if (target.IsOnGPU)
                return target.AverageColorDifferenceGPU(pattern, targetStartX, targetStartY);

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
                var rowResults = new int[yMax - yMin];

                //Do each row in parallel
                Parallel.For(yMin, yMax, new ParallelOptions() { /*MaxDegreeOfParallelism = 1*/ }, (int y) =>
                {
                    var patternRowStart = stride * y; //Stride is width*3 bytes
                    var targetRowStart = targetStride * (y + targetStartY);

                    for (var x = xMin; x < xMax; x++)
                    {
                        var patternOffset = x * 3 + patternRowStart;
                        var targetOffset = (x + targetStartX) * 3 + targetRowStart;

                        var patternValue = patternPx0[patternOffset];

                        if (patternValue <= 3)
                            continue;

                        var px0dist = patternValue - targetPx0[targetOffset];

                        rowResults[y - yMin] += (px0dist*px0dist);
                        //rowResults[y - yMin] += Math.Abs(px0dist);
                    }
                });

                result = rowResults.Sum();
            }

            return result / (pattern.Width * pattern.Height);
        }

        public static double AverageColorDifferenceGPU(this Frame target, Frame pattern, int targetStartX, int targetStartY, int stream = 0)
        {
            return target.AverageColorDifferenceGPU(pattern.PixelBytes, pattern.Stride, pattern.Width, pattern.Height, targetStartX, targetStartY, stream);
        }

        public static double AverageColorDifferenceGPU(this Frame target, byte[] patternBytes, 
            int patternStride, int patternWidth, int patternHeight, int targetStartX, int targetStartY, int stream = 0)
        {
            var result = 255; //Max possible average color difference

            if (!target.ValidConvolutionLocation(patternWidth, patternHeight, targetStartX, targetStartY))
                return result;

            if (patternBytes == null)
                throw new ArgumentException("Pattern GPU bytes are null");

            var grid = Hardware.Filters.Grid(patternWidth, patternHeight);
            var dev_blockSums = GPU.Current.Allocate<int>(grid.x,grid.y);

            GPU.Current.LaunchAsync
            (
                grid, Hardware.Filters.Block, stream,
                Kernels.AverageColorDifferenceKernel, 
                target.PixelBytes, target.Stride, target.Width, target.Height,
                patternBytes, patternStride, patternWidth, patternHeight,
                targetStartX, targetStartY, dev_blockSums
            );
            
            var host_blockSums_pinned = GPU.Current.HostAllocate<int>(grid.x, grid.y);
            GPU.Current.CopyFromDeviceAsync(dev_blockSums, 0, host_blockSums_pinned, 0, grid.x*grid.y,stream);

            GPU.Current.SynchronizeStream(stream);

            var host_blockSums = new int[grid.x, grid.y];
            GPGPU.CopyOnHost(host_blockSums_pinned, 0, host_blockSums, 0, grid.x * grid.y);

            //Add up the grid sums
            result = host_blockSums.Cast<int>().Sum();

            //Cleanup
            GPU.Current.HostFree(host_blockSums_pinned);
            GPU.Current.Free(dev_blockSums);

            return result / (patternWidth * patternHeight * 1.0f);
        }

        public static Frame ScaleHQ(this Frame target, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            var image = target.Bitmap;

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            target.Dispose();

            return new Frame(destImage, false);
        }

        public static Frame AveragePixels(this Frame bitmapA, Frame bmp)
        {
            var result = new Frame(new Bitmap(bitmapA.Width, bitmapA.Height, bitmapA.PixelFormat), false);

            //Do each row in parallel
            Parallel.For(0, bitmapA.Height, 
                new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, 
            (int y) =>
            {
                var rowStart = bitmapA.Stride * y; //Stride is width*3 bytes

                for (var x = 0; x < bitmapA.Width; x++)
                {
                    var offset = x * 3 + rowStart;

                    for(var b = 0; b < 3; b++)
                    {
                        var byteAddr = offset + b;
                        var byteAddrSum = bitmapA.FirstPixelPointer[byteAddr] + bmp.FirstPixelPointer[byteAddr];

                        result.FirstPixelPointer[byteAddr] = (byte) (byteAddrSum /2);
                    }
                }
            });

            return result;
        }

        public static bool SameSizeAs(this Frame a, Frame b)
        {
            return a.Width == b.Width && a.Height == b.Height;
        }

        public static Frame Changed(this Frame bitmapA, Frame bitmapB, int threshold = 20)
        {
            var result = new Frame(new Bitmap(bitmapB.Width, bitmapB.Height, bitmapB.PixelFormat), false);

            //Performance optimizations
            var aFirstPx = bitmapA.FirstPixelPointer;
            var bFirstPx = bitmapB.FirstPixelPointer;
            var resultFirstPx = result.FirstPixelPointer;
            var height = bitmapA.Height;
            var width = bitmapA.Width;
            var stride = bitmapA.Stride;
            const int xMin = 0;
            var xMax = width;
            const int yMin = 0;
            var yMax = height;

            //Do each row in parallel
            Parallel.For(yMin, yMax, new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, (int y) =>
            {
                var rowStart = stride * y; //Stride is width*3 bytes

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    var colorDifference =
                        (
                            Math.Abs(aFirstPx[offset] - bFirstPx[offset]) +
                            Math.Abs(aFirstPx[offset + 1] - bFirstPx[offset + 1]) +
                            Math.Abs(aFirstPx[offset + 2] - bFirstPx[offset + 2])
                        )
                        /
                        3.0
                        ;



                    if (colorDifference >= threshold)
                    {
                        resultFirstPx[offset] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] = 255;
                    }
                }
            });

            return result;
        }

        public static Frame ChangeExtent(this Frame bitmapA, Frame bitmapB)
        {
            var result = new Frame(new Bitmap(bitmapB.Width, bitmapB.Height, bitmapB.PixelFormat), false);

            //Performance optimizations
            var aFirstPx = bitmapA.FirstPixelPointer;
            var bFirstPx = bitmapB.FirstPixelPointer;
            var resultFirstPx = result.FirstPixelPointer;
            var height = bitmapA.Height;
            var width = bitmapA.Width;
            var stride = bitmapA.Stride;
            const int xMin = 0;
            var xMax = width;
            const int yMin = 0;
            var yMax = height;

            //Do each row in parallel
            Parallel.For(yMin, yMax, new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, (int y) =>
            {
                var rowStart = stride * y; //Stride is width*3 bytes

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    var colorDifference =
                        (
                            Math.Abs(aFirstPx[offset] - bFirstPx[offset]) +
                            Math.Abs(aFirstPx[offset + 1] - bFirstPx[offset + 1]) +
                            Math.Abs(aFirstPx[offset + 2] - bFirstPx[offset + 2])
                        )
                        /
                        3.0
                        ;

                    resultFirstPx[offset] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] = (byte)(colorDifference);
                }
            });

            return result;
        }

        public static List<Point> ChangeExtentPoints(this Frame bitmapA, Frame bitmapB, int threshold, Rect? regionOfInterest = null)
        {
            var result = new List<Point>(1000);

            //Performance optimizations
            var aFirstPx = bitmapA.FirstPixelPointer;
            var bFirstPx = bitmapB.FirstPixelPointer;
            var height = bitmapA.Height;
            var width = bitmapA.Width;
            var stride = bitmapA.Stride;


            var xMin = 0;
            var xMax = width;
            var yMin = 0;
            var yMax = height;

            if(regionOfInterest != null)
            {
                xMin = (int)Math.Round(regionOfInterest.Value.Left);
                xMax = (int)Math.Round(regionOfInterest.Value.Right);
                yMin = (int)Math.Round(regionOfInterest.Value.Top);
                yMax = (int)Math.Round(regionOfInterest.Value.Bottom);
            }

            var rowTotals = new List<Point>[yMax-yMin];

            //Do each row in parallel
            Parallel.For(yMin, yMax, new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, (int y) =>
            {
                rowTotals[y-yMin] = new List<Point>();
                var rowStart = stride * y; //Stride is width*3 bytes

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    var colorDifference =
                        (
                            Math.Abs(aFirstPx[offset] - bFirstPx[offset]) +
                            Math.Abs(aFirstPx[offset + 1] - bFirstPx[offset + 1]) +
                            Math.Abs(aFirstPx[offset + 2] - bFirstPx[offset + 2])
                        )
                        /
                        3.0
                        ;

                    if (colorDifference >= threshold)
                    {
                        rowTotals[y-yMin].Add(new Point(x,y));
                    }
                }
            });

            return rowTotals.SelectMany(y => y).ToList();
        }


        public static Frame MapOfDarkened(this Frame bitmapA, Frame bitmapB)
        {
            var result = new Frame(new Bitmap(bitmapB.Width, bitmapB.Height, bitmapB.PixelFormat), false);

            //Performance optimizations
            var aFirstPx = bitmapA.FirstPixelPointer;
            var bFirstPx = bitmapB.FirstPixelPointer;
            var resultFirstPx = result.FirstPixelPointer;
            var height = bitmapA.Height;
            var width = bitmapA.Width;
            var stride = bitmapA.Stride;
            const int xMin = 0;
            var xMax = width;
            const int yMin = 0;
            var yMax = height;

            //Do each row in parallel
            Parallel.For(yMin, yMax, new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, (int y) =>
            {
                var rowStart = stride * y; //Stride is width*3 bytes

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    var colorDifference =
                        (aFirstPx[offset] - bFirstPx[offset]) +
                        aFirstPx[offset + 1] - bFirstPx[offset + 1] +
                        aFirstPx[offset + 2] - bFirstPx[offset + 2];

                    //Darkened if went from 255 to 0 => 0 - 255 => neg, 255 - 0 => pos

                    if (colorDifference < -40)
                    {
                        resultFirstPx[offset] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] =
                            (byte)(colorDifference / 3);
                    }
                }
            });

            return result;
        }


        public static Frame Compare(this Frame bitmapA, Frame bitmapB, int threshold = 20)
        {
            var result = new Frame(new Bitmap(bitmapB.Width, bitmapB.Height, bitmapB.PixelFormat), false);

            //Performance optimizations
            var efficientTreshold = threshold * 3;
            var aFirstPx = bitmapA.FirstPixelPointer;
            var bFirstPx = bitmapB.FirstPixelPointer;
            var resultFirstPx = result.FirstPixelPointer;
            var height = bitmapA.Height;
            var width = bitmapA.Width;
            var stride = bitmapA.Stride;
            const int xMin = 0;
            var xMax = width;
            const int yMin = 0;
            var yMax = height;

            //Do each row in parallel
            Parallel.For(yMin, yMax, new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, (int y) =>
            {
                var rowStart = stride * y; //Stride is width*3 bytes

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    var colorDifference =
                        Math.Abs(aFirstPx[offset] - bFirstPx[offset]) +
                        Math.Abs(aFirstPx[offset + 1] - bFirstPx[offset + 1]) +
                        Math.Abs(aFirstPx[offset + 2] - bFirstPx[offset + 2]);

                    //if (colorDifference > efficientTreshold)
                    {
                        resultFirstPx[offset] = resultFirstPx[offset + 1] = resultFirstPx[offset + 2] =
                            (byte)(colorDifference / 3);
                    }
                }
            });

            return result;
        }

        public static double CompareOveralp(this Frame target, Frame pattern, int targetStartX, int targetStartY, int whiteThreshold = 175)
        {
            var result = 0.0; //No overlap

            if (!target.ValidConvolutionLocation(pattern.Width, pattern.Height, targetStartX, targetStartY))
                return result;

            //Performance optimizations
            if (pattern.PixelBytes == null)
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
                var rowResults = new int[yMax - yMin];

                //Do each row in parallel
                Parallel.For(yMin, yMax, new ParallelOptions() { /*MaxDegreeOfParallelism = 1*/ }, (int y) =>
                {
                    var patternRowStart = stride * y; //Stride is width*3 bytes
                    var targetRowStart = targetStride * (y + targetStartY);

                    for (var x = xMin; x < xMax; x++)
                    {
                        var patternOffset = x * 3 + patternRowStart;
                        var isPatternWhite = patternPx0[patternOffset] >= whiteThreshold;

                        if (!isPatternWhite) 
                            continue;

                        var targetOffset = (x + targetStartX) * 3 + targetRowStart;
                        var isTargetWhite = targetPx0[targetOffset] >= whiteThreshold;

                        if (isTargetWhite)
                            rowResults[y - yMin]++;
                    }
                });

                result = rowResults.Sum();
            }

            return 1- (result / (pattern.Width * pattern.Height));
        }

        public static Frame ShiftColor(this Frame target)
        {
            var result = target.Clone();

            var targetPx0 = target.FirstPixelPointer;
            var resultPx0 = result.FirstPixelPointer;
            var targetStride = target.Stride;
            const int xMin = 0;
            var xMax = target.Width;
            const int yMin = 0;
            var yMax = target.Height;

            fixed (byte* tPx0 = target.PixelBytes)
            {
                //Do each row in parallel
                Parallel.For(yMin, yMax, new ParallelOptions() { /*MaxDegreeOfParallelism = 10*/ }, (int y) =>
                {
                    var targetRowStart = targetStride * y;

                    for (var x = xMin; x < xMax; x++)
                    {
                        var targetOffset = x * 3 + targetRowStart;

                        resultPx0[targetOffset] =     (byte) (targetPx0[targetOffset]/2);
                        //resultPx0[targetOffset + 1] = (byte)(targetPx0[targetOffset + 1]);
                        resultPx0[targetOffset + 2] = (byte)(targetPx0[targetOffset + 2] / 2);

                    }
                });
            }

            target.Dispose();

            return result;
        }

        private static int OffsetOf(int x, int y, int stride, int bytesPerPixel)
        {
            return stride*y + x*bytesPerPixel;
        }

        private static int MonochromeDifference(byte* firstPixel1, int offset1, byte* firstPixel2, int offset2)
        {
            var absDiff = Math.Abs(firstPixel1[offset1 + 0] - firstPixel2[offset2 + 0]) * 3;

            //firstPixel2[offset2 + 0] = (byte) absDiff;

            return absDiff;
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
