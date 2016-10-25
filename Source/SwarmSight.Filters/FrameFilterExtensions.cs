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
using System.Windows.Media.Media3D;
using static System.Math;
using SwarmSight.Filters.ColorDemo;
using System.Runtime.CompilerServices;

namespace SwarmSight.Filters
{
    public unsafe static class FrameFilterExtensions
    {
        public static void MarkSectors(this Frame target, double[] sectors, int headCtrX, int headCtrY, int headHeight, double headAngle, Color color, bool isRight = true)
        {
            var startingAngleRad = headAngle / 180.0 * PI;
            var sectorWidth = PI / sectors.Length;
            var radius = headHeight;
            var maxSectorValue = headCtrX * headHeight * 255 / sectors.Length;
            var maxBinValue = sectors.Max();

            var prevEnd = new PointF();
            var prevShade = 0.0;

            using (var g = Graphics.FromImage(target.Bitmap))
            {
                for (var s = 0; s <= sectors.Length; s++)
                {
                    var sectorAngle = startingAngleRad + (isRight ? +1 : -1) * s * sectorWidth;

                    var endX = (float)(headCtrX + Sin(sectorAngle) * radius);
                    var endY = (float)(headCtrY - Cos(sectorAngle) * radius);
                    
                    if (s > 0 && (int)prevShade > 0)
                    {
                        g.FillPolygon(new SolidBrush(Color.FromArgb((int)prevShade,color.R,color.G,color.B)), new[] 
                        {
                            new PointF(headCtrX, headCtrY),
                            prevEnd,
                            new PointF(endX, endY),
                            new PointF(headCtrX, headCtrY)
                        });
                    }

                    if (s < sectors.Length)
                    {
                        //Relative to max possible activation
                        //prevShade = Min(1, sectors[s] / maxSectorValue / 0.02)*255*0.5;
                        prevShade = sectors[s] / maxBinValue * 255;
                        prevEnd = new PointF(endX, endY);
                    }
                }
            }
        }
        public static void MarkPoint(this Frame target, Point pt, Color? inner = null, Color? outer = null)
        {
            if (inner == null)
                inner = Color.Yellow;

            if (outer == null)
                outer = Color.Blue;

            using (var g = Graphics.FromImage(target.Bitmap))
            {
                var inC = new Pen(inner.Value, 1);
                var outC = new Pen(outer.Value, 1);

                g.DrawRectangle(inC, pt.X, pt.Y, 1, 1);
                g.DrawRectangle(outC, pt.X-1, pt.Y-1, 3, 3);
            }
        }

        public static Frame Averaged(Queue<Frame> frames)
        {
            var result = new Frame(frames.First().Width, frames.First().Height, frames.First().PixelFormat, false);

            var resultFirstPx = result.FirstPixelPointer;
            var resultStride = result.Stride;
            var frameCount = frames.Count;
            var height = result.Height;
            var width = result.Width;

            var firstPxs = new byte*[frameCount];

            for (var f = 0; f < frameCount; f++)
            {
                firstPxs[f] = frames.ElementAt(f).FirstPixelPointer;
            }

            Parallel.For(0, height, y =>
            {
                var resultRowStart = resultFirstPx + y * resultStride;

                var rowStarts = new byte*[frameCount];

                for (var f = 0; f < frameCount; f++)
                {
                    rowStarts[f] = firstPxs[f] + y * resultStride;
                }

                for (var x = 0; x < width; x++)
                {
                    var pxSumB = 0;
                    var pxSumG = 0;
                    var pxSumR = 0;
                    var x3 = x * 3;

                    for (var f = 0; f < frameCount; f++)
                    {
                        var pxAddr = rowStarts[f] + x3;

                        pxSumB += *(pxAddr);
                        pxSumG += *(pxAddr+1);
                        pxSumR += *(pxAddr)+2;
                    }

                    var resultPxAddr = resultRowStart + x3;

                    *(resultPxAddr) = (byte)(pxSumB / frameCount);
                    *(resultPxAddr + 1) = (byte)(pxSumG / frameCount);
                    *(resultPxAddr + 2) = (byte)(pxSumR / frameCount);
                }
            });

            return result;
        }

        public static void CopyToWriteableBitmap(this Frame source, WriteableBitmap writeableBitmap)
        {
            // Reserve the back buffer for updates.
            writeableBitmap.Lock();

            var targetFirstPx = (byte*)writeableBitmap.BackBuffer;
            var targetStride = writeableBitmap.BackBufferStride;
            var sourceFirstPx = source.FirstPixelPointer;
            var sourceStride = source.Stride;

            Parallel.For(0, source.Height, y =>
            {
                for (var x = 0; x < source.Width; x++)
                {
                    var offset = sourceStride * y + x*3;

                    targetFirstPx[offset] = sourceFirstPx[offset];
                    targetFirstPx[offset+1] = sourceFirstPx[offset+1];
                    targetFirstPx[offset+2] = sourceFirstPx[offset+2];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(this Color a, Color b)
        {
            return (Math.Abs(b.R - a.R) + Math.Abs(b.G - a.G) + Math.Abs(b.B - a.B)) / 3.0;
        }

        public static Point? FindNearbyColor(this Frame target, Point start, int radius, Color color)
        {
            var result = new List<Point3D>();
            var startingColorDist = target.GetColor(start).Distance(color);

            var targetColor = color;
            var tX = Math.Max(0, start.X - radius);
            var tY = Math.Max(0, start.Y - radius);

            var height = Math.Min(target.Height, tY + 2*radius);
            var width = Math.Min(target.Width, tX + 2 * radius);
            var px01 = target.FirstPixelPointer;
            var targetStride = target.Stride;
            
            Parallel.For(tY, height, row =>
            {
                var yT = px01 + targetStride * row;
                var rowResult = new List<Point3D>();

                for (var col = tX; col < width; col++)
                {
                    var offsetT = yT + col * 3;
                    var pxColor = Color.FromArgb(offsetT[2], offsetT[1], offsetT[0]);
                    var dist = pxColor.Distance(targetColor);

                    if(dist < startingColorDist)
                        rowResult.Add(new Point3D(col, row, dist));
                }

                lock(result)
                {
                    result.AddRange(rowResult);
                }
            });

            result = result
                .OrderBy(p => p.Z)
                .ToList();

            if (result.Count > 0)
            { 
                return new Point((int)result[0].X, (int)result[0].Y);
            }

            return null;
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

        public static Frame ReMap(this Frame target, Func<Point, Color, Color> mapFunction)
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
                    var remapColor = mapFunction(new Point(x,y), color);

                    resultPx[offset] = remapColor.B;
                    resultPx[offset + 1] = remapColor.G;
                    resultPx[offset + 2] = remapColor.R;

                }
            });

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

        public static bool IsDifferentFrom(this Frame target, Frame target2)
        {
            var firstPx = target.FirstPixelPointer;
            var firstPx2 = target2.FirstPixelPointer;

            //Check first row only
            for (var x = 0; x < target.Width; x++)
            {
                var offset = x * 3;

                if(
                    firstPx[offset + 0] != firstPx2[offset + 0] ||
                    firstPx[offset + 1] != firstPx2[offset + 1] ||
                    firstPx[offset + 2] != firstPx2[offset + 2]
                )
                {
                    return true;
                }
            }

            return false;
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

        public static Frame MedianBlur(this Frame target, int size = 2)
        {
            return target.ReMap((p, c) =>
            {
                var pxs = target.GetSurroundingPixels(p,size);
                var med = pxs.OrderBy(px => px.Value.GetBrightness()).Skip(pxs.Count / 2).First().Value;
                return med;
            });
        }

        public static List<Tuple<Point, double>> MedianFilter(this List<Tuple<Point,double>> target, int radius = 2)
        {
            if (target.Count == 0)
                return target;

            var min = target[0].Item1;
            var max = target[0].Item1;
            var avgZ = 0.0;

            target.ForEach(i =>
            {
                //X
                if (i.Item1.X < min.X)
                    min.X = i.Item1.X;

                else if (i.Item1.X > max.X)
                    max.X = i.Item1.X;

                //Y
                if (i.Item1.Y < min.Y)
                    min.Y = i.Item1.Y;

                else if (i.Item1.Y > max.Y)
                    max.Y = i.Item1.Y;

                avgZ += i.Item2;
            });

            avgZ /= target.Count;

            var width = max.X - min.X + 1;
            var height = max.Y - min.Y + 1;
            var grid = new int[width, height];

            Parallel.ForEach(target, i => grid[i.Item1.X-min.X, i.Item1.Y-min.Y] = (int)i.Item2);
            
            var length = radius * 2 + 1;
            var totalHalf = length * length / 2;
            var result = new List<Tuple<Point, double>>(100);

            Parallel.For(0, height, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
            }, 
            gy =>
            {
                var rowResult = new List<Tuple<Point, double>>(10);

                for(var gx = 0; gx < width; gx++)
                {
                    var wxStart = gx - radius;
                    var wyStart = gy - radius;

                    var balance = 0;

                    //Get surrounding pixel values
                    for (var wy = 0; wy < length; wy++)
                    {
                        var gwy = wyStart + wy;

                        for (var wx = 0; wx < length; wx++)
                        {
                            var gwx = wxStart + wx;

                            if (gwy < 0 || gwx < 0 || gwy >= height || gwx >= width)
                                balance--;

                            else if (grid[gwx, gwy] > 0)
                                balance++;

                            else
                                balance--;

                            if (Abs(balance) > totalHalf)
                                goto doneWithPixel;
                        }
                    }

                    doneWithPixel:;

                    if (balance > 0)
                    {
                        var zVal = grid[gx, gy];

                        rowResult.Add(new Tuple<Point,double>(new Point(gx+min.X,gy+min.Y), zVal > 0 ? zVal : avgZ));
                    }
                }

                lock(result) { result.AddRange(rowResult); }
            });
            

            return result;
        }

        public static Dictionary<Point,Color> GetSurroundingPixels(this Frame target, Point center, int radius = 2)
        {
            var length = radius * 2 + 1;
            var result = new Dictionary<Point, Color>(length * length);
            
            var txStart = center.X - radius;
            var tyStart = center.Y - radius;
            var width = target.Width;
            var height = target.Height;

            for (var y = 0; y < length; y++)
            {
                for(var x = 0; x < length; x++)
                {
                    var tx = txStart + x;
                    var ty = tyStart + y;
                    var pt = new Point(tx, ty);

                    if(tx < 0 || ty < 0 || tx >= width || ty >= height)
                    {
                        result.Add(pt, Color.Black);
                    }
                    else
                    {
                        result.Add(pt, target.GetColor(tx, ty));
                    }
                }
            }

            return result;
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

        public static List<Point> PointsOverThreshold(this Frame target, int threshold, int channel = 0)
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
            var chan = channel;

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
                    var offset = x * 3 + rowStart + chan;

                    if(firstPx[offset] > threshold)
                        rowResult.Add(new Point(x,y));
                }

                lock (result)
                {
                    result.AddRange(rowResult);
                }
            });

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="action">X,Y,R,G,B</param>
        public static void ForEachPoint(this Frame target, Action<int,int,int,int,int> action)
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

                for (var x = xMin; x < xMax; x++)
                {
                    var offset = x * 3 + rowStart;

                    action(x, y, firstPx[offset + 2], firstPx[offset + 1], firstPx[offset]);
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
            //var shiftConst = 240.0f / 255f;

            var LogisticTable = Enumerable
                .Range(0, 256)
                .Select(x => Logistic(x, 255, extent, shift))
                .ToArray();

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

                    var r = firstPx[offset + 2];
                    var g = firstPx[offset + 1];
                    var b = firstPx[offset];

                    var brightness = (r+g+b)/3;
                    var newBrightness = LogisticTable[brightness];
                    var adjust = (newBrightness - brightness);

                    var newb = b + adjust;
                    var newg = g + adjust;
                    var newr = r + adjust;

                    resultFirstPx[offset] = (byte)(newb < 0 ? 0 : newb > 255 ? 255 : newb);
                    resultFirstPx[offset + 1] = (byte)(newg < 0 ? 0 : newg > 255 ? 255 : newg);
                    resultFirstPx[offset + 2] = (byte)(newr < 0 ? 0 : newr > 255 ? 255 : newr);

                    //continue;

                    ////Convert to HSL space change lum, then convert back to RGB -- SLOW

                    //var color = new HSLColor(firstPx[offset + 2], firstPx[offset + 1], firstPx[offset]);
                    //var oldBrighness = (float)color.Luminosity;
                    //var ratio = Logistic(oldBrighness, 240, extent, shift/255*240);
                    //color.Luminosity = ratio;
                    //var rgb = (Color)color;
                    //resultFirstPx[offset] = (byte)(Min((byte)255,Max((byte)0, rgb.B)));
                    //resultFirstPx[offset+1] = (byte)(Min((byte)255, Max((byte)0, rgb.G)));
                    //resultFirstPx[offset+2] = (byte)(Min((byte)255, Max((byte)0, rgb.R)));


                    ////Shfit each component independently - PROBLESM WITH SATURATION
                    //resultFirstPx[offset + 0] = (byte)Logistic(firstPx[offset + 0], 255, extent, shift);
                    //resultFirstPx[offset + 1] = (byte)Logistic(firstPx[offset + 1], 255, extent, shift);
                    //resultFirstPx[offset + 2] = (byte)Logistic(firstPx[offset + 2], 255, extent, shift);

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
                        
                        var avgDist = (Math.Abs(targetPx0[targetOffset] - B) +
                            Math.Abs(targetPx0[targetOffset + 1] - G) +
                            Math.Abs(targetPx0[targetOffset + 2] - R)) / 3.0;

                        //Order is BGR. Paint px white if close to the color
                        if (avgDist <= distance)
                            resultPx0[targetOffset] = resultPx0[targetOffset+1] = resultPx0[targetOffset+2] = (byte)(255-avgDist);

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

        /// <summary>
        /// Draws the top frame on top of the bottom frame
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        public static void DrawFrame(this Frame bottom, Frame top)
        {
            bottom.DrawFrame(top, 0, 0, 1, 0);
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
                Parallel.For(yMin, yMax, new ParallelOptions()
                {
                    //MaxDegreeOfParallelism = 1
                }, (int y) =>
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

        public static Frame ScaleHQ(this Frame target, int width, int height, Bitmap dest = null)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = dest ?? new Bitmap(width, height, PixelFormat.Format24bppRgb);
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

            if (dest == null)
            {
                target.Dispose();

                return new Frame(destImage, false);
            }
            else
            {
                return null;
            }
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

        public static T CreateJaggedArray<T>(params int[] lengths)
        {
            return (T)InitializeJaggedArray(typeof(T).GetElementType(), 0, lengths);
        }

        public static object InitializeJaggedArray(Type type, int index, int[] lengths)
        {
            Array array = Array.CreateInstance(type, lengths[index]);
            Type elementType = type.GetElementType();

            if (elementType != null)
            {
                for (int i = 0; i < lengths[index]; i++)
                {
                    array.SetValue(
                        InitializeJaggedArray(elementType, index + 1, lengths), i);
                }
            }

            return array;
        }
    }
}
