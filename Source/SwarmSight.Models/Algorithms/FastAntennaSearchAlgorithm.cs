using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;
using Cudafy;
using SwarmSight.Filters;
using SwarmSight.Hardware;
using SwarmSight.HeadPartsTracking.Models;
using Point = System.Windows.Point;
using System.Data;
using Mono.CSharp;

namespace SwarmSight.HeadPartsTracking.Algorithms
{
    public class AntenaPoints : IDisposable
    {
        public Point RS;
        public Point RFB;
        public Point RFT;

        public Point LS;
        public Point LFB;
        public Point LFT;


        public void Dispose()
        {

        }

        public double Distance(Point a, Point b)
        {
            var x = a.X - b.X;
            var y = a.Y - b.Y;

            return Math.Sqrt(x*x + y*y);
        }

        public double SegmentAngle(Point a, Point b)
        {
            return Math.Atan2(b.Y - a.Y, b.X - a.X)*180.0/Math.PI;
        }

        public AntenaPoints Multiply(double scalar, double? scalarY = null)
        {
            RS.Multiply(scalar, scalarY);
            RFB.Multiply(scalar, scalarY);
            RFT.Multiply(scalar, scalarY);
            LS.Multiply(scalar, scalarY);
            LFB.Multiply(scalar, scalarY);
            LFT.Multiply(scalar, scalarY);

            return this;
        }

        public AntenaPoints Offset(double x, double y)
        {
            RS.Offset(x, y);
            RFB.Offset(x, y);
            RFT.Offset(x, y);
            LS.Offset(x, y);
            LFB.Offset(x, y);
            LFT.Offset(x, y);

            return this;
        }

        public AntenaPoints InvertY()
        {
            RS.InvertY();
            RFB.InvertY();
            RFT.InvertY();
            LS.InvertY();
            LFB.InvertY();
            LFT.InvertY();

            return this;
        }

        public AntenaPoints Clone()
        {
            var result = new AntenaPoints();

            result.RS = RS.Clone();
            result.RFB = RFB.Clone();
            result.RFT = RFT.Clone();
            result.LS = LS.Clone();
            result.LFB = LFB.Clone();
            result.LFT = LFT.Clone();

            return result;
        }

        public double L1 { get { return Distance(RS, RFB); } }
        public double L2 { get { return Distance(RFB, RFT); } }
        public double L3 { get { return Distance(LS, LFB); } }
        public double L4 { get { return Distance(LFB, LFT); } }

        public double Angle1 { get { return SegmentAngle(RS, RFB); } }
        public double Angle2 { get { return (Angle1+360)%360 - (SegmentAngle(RFB, RFT)+360)%360; } }
        public double Angle3 { get { return SegmentAngle(RS, RFT); } }

        public double Angle4 { get { return SegmentAngle(LS, LFB); } }
        public double Angle5 { get { return Angle4 - SegmentAngle(LS, LFT); } }
        public double Angle6 { get { return SegmentAngle(LS, LFT); } }

        public AntenaPoints Rotate(double angle)
        {
            RS.Rotate(angle);
            RFB.Rotate(angle);
            RFT.Rotate(angle);
            LS.Rotate(angle);
            LFB.Rotate(angle);
            LFT.Rotate(angle);

            return this;
        }

        public AntenaPoints ToFrameSpace(Point headDims, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double headLength)
        {
            RS = RS.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            RFB = RFB.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            RFT = RFT.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            LS = LS.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            LFB = LFB.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            LFT = LFT.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);

            return this;
        }

        public AntenaPoints ToPriorSpace(Point headDims, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double headLength)
        {
            RS = RS.ToPriorSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            RS = RFB.ToPriorSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            RS = RFT.ToPriorSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            RS = LS.ToPriorSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            RS = LFB.ToPriorSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            RS = LFT.ToPriorSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);

            return this;
        }
    }

    public static class PointExtensions
    {
        public static double Distance(this Color a, Color b)
        {
            return ( Math.Abs(b.R - a.R) + Math.Abs(b.G - a.G) + Math.Abs(b.B - a.B) ) / 3.0;
        }
        public static double Distance(this Point a, Point b)
        {
            var x = b.X - a.X;
            var y = b.Y - a.Y;

            return Math.Sqrt(x*x + y*y);
        }

        public static Point ToWindowsPoint(this System.Drawing.Point target)
        {
            return new Point(target.X, target.Y);
        }
        public static PointF ToPointF(this Point target)
        {
            return new PointF((float) target.X, (float) target.Y);
        }

        public static Point Moved(this Point target, double byX, double byY)
        {
            return new Point(target.X + byX, target.Y + byY);
        }
        public static Rectangle EnclosingRectangle(this Point target, int radius)
        {
            return new Rectangle(
                (target.X - radius).Rounded(),
                (target.Y - radius).Rounded(),
                radius*2+1,
                radius*2+1
            );
        }

        public static Point ToPriorSpace(this Point target, Point windowDims, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double scapeDistance)
        {
            target.Offset(-windowDims.X / 2.0 + offsetX, -windowDims.Y / 2.0 + offsetY);
            target = target.Multiply(1.0 / (scaleX * scapeDistance), 1.0 / (scaleY * scapeDistance));
            target = target.Rotate(-(headAngle - priorAngle));

            return target;

        }

        public static Point ToFrameSpace(this Point target, Point windowDims, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double scapeDistance)
        {
            target = target.Multiply(scaleX * scapeDistance, scaleY * scapeDistance);
            target = target.Rotate(headAngle - priorAngle);
            target.Offset(windowDims.X / 2.0 - offsetX, windowDims.Y / 2.0 - offsetY);

            return target;
        }

        public static Point Multiply(this Point target, double scalar, double? scalarY = null)
        {
            target.X *= scalar;
            target.Y *= scalarY ?? scalar;

            return target;
        }
        public static List<Point> AndNeighbors(this Point p)
        {
            var result = new List<Point>(8);

            for(var x = -1; x <= 1; x++)
            for(var y = -1; y <= 1; y++)
            {
                result.Add(new Point(p.X + x, p.Y + y));
            }

            return result;
        }
        public static System.Drawing.Point ToDrawingPoint(this Point target)
        {
            return new System.Drawing.Point(target.X.Rounded(), target.Y.Rounded());
        }
        public static Point InvertY(this Point target)
        {
            target.Y *= -1;

            return target;
        }

        public static Point Clone(this Point target)
        {
            return new Point(target.X, target.Y);
        }

        public static Point Rotate(this Point target, double angle)
        {
            var result = target;
            var degRadian = angle*Math.PI/180.0;

            var cos = Math.Cos(degRadian);
            var sin = Math.Sin(degRadian);

            result.X = target.X * cos - target.Y * sin;
            result.Y = target.X * sin + target.Y * cos;

            target = result;

            return result;
        }

        public static bool IsInPolygon(this Point testPoint, List<Point> polygon)
        {
            //Cannot be part of empty polygon
            if (polygon.Count == 0)
            {
                return false;
            }

            //With 1-pt polygon, only if it's the point
            if (polygon.Count == 1)
            {
                return polygon[0] == testPoint;
            }

            //n>2 Keep track of cross product sign changes
            var pos = 0;
            var neg = 0;

            for (var i = 0; i < polygon.Count; i++)
            {
                //If point is in the polygon
                if (polygon[i] == testPoint)
                    return true;

                //Form a segment between the i'th point
                var x1 = polygon[i].X;
                var y1 = polygon[i].Y;

                //And the i+1'th, or if i is the last, with the first point
                var i2 = i < polygon.Count - 1 ? i + 1 : 0;

                var x2 = polygon[i2].X;
                var y2 = polygon[i2].Y;

                var x = testPoint.X;
                var y = testPoint.Y;

                //Compute the cross product
                var d = (x - x1)*(y2 - y1) - (y - y1)*(x2 - x1);

                if (d > 0) pos++;
                if (d < 0) neg++;

                //If the sign changes, then point is outside
                if (pos > 0 && neg > 0)
                    return false;
            }

            //If no change in direction, then on same side of all segments, and thus inside
            return true;
        }

        public static Stopwatch DistanceTime = new Stopwatch();
        public static double[] DistToSegmentSquared(this List<Point> points, Point x1y1, Point x2y2)
        {
            return DistToSegmentSquared(points, x1y1.X, x1y1.Y, x2y2.X, x2y2.Y);
        }

        public static double[] DistToSegmentSquared(this List<Point> points, Double x1, Double y1, Double x2, Double y2)
        {
            var C = x2 - x1;
            var D = y2 - y1;
            var lenSq = C * C + D * D;
            var result = new double[points.Count];
            var _x1 = x1;
            var _y1 = y1;
            var _x2 = x2;
            var _y2 = y2;

            //Parallel.For(0, points.Count, p =>
            for (int p = 0; p < points.Count; p++)
            {
                var pt = points[p];

                var x = pt.X;
                var y = pt.Y;

                var A = x - _x1;
                var B = y - _y1;

                var param = lenSq > 0 ? (A * C + B * D) / lenSq : -1;

                var xx = param >= 0 && param <= 1 ? _x1 + param * C : (param < 0 ? _x1 : _x2);
                var yy = param >= 0 && param <= 1 ? _y1 + param * D : (param < 0 ? _y1 : _y2);

                var dx = x - xx;
                var dy = y - yy;

                result[p] = (dx * dx + dy * dy); //Math.Sqrt
            }
            //);

            return result;
        }

        public static int Rounded(this double target)
        {
            return (int) Math.Round(target);
        }

        public static Color ToColor(this int bwValue)
        {
            return Color.FromArgb(bwValue, bwValue, bwValue);
        }

        public static Color ToColor(this double index)
        {
            var d = (255*index).Rounded();

            return Color.FromArgb(d, d, d);
        }

        public static double Scale(this double target, double targetRangeL, double targetRangeH, double resultRangeL, double resultRangeH)
        {
            return (target - targetRangeL) / (targetRangeH - targetRangeL) * (resultRangeH - resultRangeL) + resultRangeL;
        }
    }

    public enum HeadOrientation
    {
        Up,
        Down,
        Left,
        Right
    }
    public enum PointLabels
    {
        RightScape,
        RightFlagellumBase,
        RightFlagellumTip,

        LeftScape,
        LeftFlagellumBase,
        LeftFlagellumTip,
        Mandibles,
        Head
    }

    public class FastAntennaSearchAlgorithm : GeneticAlgoBase<AntenaPoints>
    {
        public int MinX = 0;
        public int MaxX = 1024;
        public int MinY = 0;
        public int MaxY = 768;

        public double HeadAngle;
        public double ScaleX;
        public double ScaleY;

        public int PointsToKeep = 100;
        public int MinPointsForRegression = 20;
        public AntenaPoints PreviousSolution = null;
        public List<Point> TargetPoints;
        public List<Point> TargetPointsPrev = null;
        public float[,] TargetPointsGPU;
        private static List<AntenaPoints> Priors = null;
        public static Dictionary<PointLabels, List<Point>> ConvexHulls = null;

        public double OffsetY = 0;
        public double OffsetX = 0;
        public double PriorAngle = 0;
        public static string PriorPath =       @"Y:\downloads\BeeVids\priorPoints.csv";
        public static string ConvexHullsPath = @"Y:\downloads\BeeVids\convexHulls.csv";

        public int FrameIndex = 0;
        public Frame DebugFrame;

        private Frame model = null;
        public int[] RightBins;
        public int[] LeftBins;
        public int RightHighest;
        public int LeftHighest;
        public override void PreProcessTarget()
        {
            if (Priors == null)
            {
                ReadPriors();
                ReadHulls();
            }


            SortDescending = false;
            ValidateRandom = false;
            NumberOfGenerations = 1;// 150;
            GenerationSize = 10;// 50;
            MutationProbability = 0.1;
            PercentRandom = 80;
            MutationRange = 1.0;
            
            var decay = 0.15;//0.85;

            if (model == null)
            {
                model = new Frame(Target.ShapeData.Width, Target.ShapeData.Height, Target.ShapeData.PixelFormat, false);
            }
            using (var prevMotionFrame = Target.MotionData.TwoFramePixelWiseOperation(Target.ColorData, (color, color1) =>
            {
                var prevDist = color1.Distance(AntennaAndPERDetector.Config.AntennaColors[0]);
                var currDist = color.Distance(AntennaAndPERDetector.Config.AntennaColors[0]);

                return
                    ((prevDist - currDist) / 2.0 + 127)
                    .Rounded()
                    .ToColor();
            }))
            using (var nextMotionFrame = Target.MotionData.TwoFramePixelWiseOperation(Target.ShapeData, (color, color1) =>
            {
                var prevDist = color1.Distance(AntennaAndPERDetector.Config.AntennaColors[0]);
                var currDist = color.Distance(AntennaAndPERDetector.Config.AntennaColors[0]);

                return
                    ((prevDist - currDist) / 2.0 + 127)
                    .Rounded()
                    .ToColor();
            }))
            using (var combMotionFrame = nextMotionFrame.TwoFramePixelWiseOperation(prevMotionFrame, (color, color1) =>
            {
                return ((color.R + color1.R) / 2.0).Rounded().ToColor();
            }))
            using (var reduced = model.ReMap(c => Color.FromArgb(
                (int)(c.R * decay),
                (int)(c.G * decay),
                (int)(c.B * decay))))
            {

                model.Dispose();
                model = reduced.TwoFramePixelWiseOperation(combMotionFrame, (color, color1) =>
                {
                    var val = (Math.Min(Math.Max(color.R + 2 * (color1.R - 127), 0), 255));

                    if (val <= 35)
                        val = 0;

                    return Color.FromArgb(val, val, val);
                });



                //compute the sum of motion in a set of quadrants
                //sweep though pixels and add their values to a bin based on their location
                var regionCount = 5; //per side, should be even
                LeftBins = new int[regionCount];
                RightBins = new int[regionCount];

                model.ForEachPoint((p, c) =>
                {
                    if(c.R > 35)
                    {
                        var pt = ToPriorSpace(p.ToWindowsPoint());
                        var degs = Math.Atan2(pt.Y, pt.X) * 180.0 / Math.PI;
                        degs = degs < -90 ? degs + 360.0 : degs;

                        if (Math.Abs(degs) <= 90.0)
                        {
                            var regionIndex = Math.Min(regionCount - 1, (int)(degs.Scale(-90, 90, 0, 1) * regionCount));
                            RightBins[regionIndex] += c.R;
                        }
                        else
                        {
                            var regionIndex = Math.Min(regionCount - 1, (int)(degs.Scale(90, 270, 0, 1) * regionCount));
                            LeftBins[regionIndex] += c.R;
                        }
                    }
                });

                if (RightBins.Max() > 150)
                    RightHighest = RightBins.ToList().IndexOf(RightBins.Max());

                if(LeftBins.Max() > 150)
                    LeftHighest = regionCount - 1 - LeftBins.ToList().IndexOf(LeftBins.Max());


                //Debug.WriteLine(leftHighest + "," + rightHighest);

                var directionFrame = model.Clone();
                directionFrame.ColorIfTrue(Color.Yellow, p =>
                {
                    if (Random.NextDouble() < 0.1)
                    {
                        var pt = ToPriorSpace(p.ToWindowsPoint());
                        var degs = Math.Atan2(pt.Y, pt.X)*180.0/Math.PI;
                        degs = degs < -90 ? degs + 360.0 : degs;

                        if (Math.Abs(Math.Abs(degs)-90) < 0.5)
                            return true;

                        if (Math.Abs(degs) < 90.0)
                        {
                            var regionIndex = Math.Min(regionCount - 1, (int) (degs.Scale(-90, 90, 0, 1)*regionCount));

                            return regionIndex == RightHighest;
                        }
                        else
                        {
                            var regionIndex = Math.Min(regionCount - 1, (int) (degs.Scale(90, 270, 0, 1)*regionCount));

                            return regionIndex == regionCount - 1 - LeftHighest;
                        }
                    }

                    return false;
                });


                if (DebugFrame != null)
                    DebugFrame.Dispose();

                DebugFrame = new Frame(Target.ShapeData.Width * 3, Target.ShapeData.Height, Target.ShapeData.PixelFormat, false);

                DebugFrame.DrawFrame(Target.MotionData, 0, 0, 1, 0);
                DebugFrame.DrawFrame(model, prevMotionFrame.Width, 0, 1, 0);
                DebugFrame.DrawFrame(directionFrame, prevMotionFrame.Width * 2, 0, 1, 0); directionFrame.Dispose();
            }

            return;

            var andDarkNow = model
                .PointsOverThreshold(36);

            var priorSpaced = andDarkNow
                .AsParallel()
                .Select(p => new Point(p.X, p.Y).ToPriorSpace(new Point(Target.ShapeData.Width, Target.ShapeData.Height), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance))
                ;

            var leftSide = priorSpaced
                .Where(p =>
                    p.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumTip])
                 || p.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumBase]))
                 .OrderByDescending(p => model.GetColor(ToFrameSpace(p).ToDrawingPoint()).R)
                 .ToList();

            var rightSide = priorSpaced
                .Where(p =>
                    p.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumTip])
                 || p.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumBase]))
                 .OrderByDescending(p => model.GetColor(ToFrameSpace(p).ToDrawingPoint()).R)
                 .ToList();

            var top = leftSide
                .AsParallel()
                .Take((leftSide.Count * 0.5).Rounded())
                .Union(
                    rightSide
                    .AsParallel()
                    .Take((rightSide.Count * 0.5).Rounded())
                )
                .ToList();
            
            //Reduce the point count
            var pointCount = top.Count;

            if (pointCount > PointsToKeep)
                top = top.Where(p => Random.NextDouble() < (double) PointsToKeep/pointCount).ToList();


            var withinPolygons = top.Where(p =>
                    !p.IsInPolygon(ConvexHulls[PointLabels.Head]) &&
                    !p.IsInPolygon(ConvexHulls[PointLabels.Mandibles])
                    )
                .ToList();

            TargetPoints = withinPolygons;
            //TargetPointsPrev = withinPolygons;

            //using (var test = Target.ShapeData.Clone())
            //{
            //    test.ColorPixels(TargetPoints.Select(p => ToFrameSpace(p).ToDrawingPoint()).ToList(), Color.Yellow);
            //    test.Bitmap.Save(@"y:\downloads\beevids\downframes\" + FrameIndex + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            //    FrameIndex++;
            //}

            //Debug.WriteLine("Target Points:" 
            //    + " R: " + TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.RightTip]))
            //    + " L: " + TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.LeftTip])));

            MinX = (int)ConvexHulls[PointLabels.RightFlagellumTip].Min(p => p.X);
            MaxX = (int)ConvexHulls[PointLabels.LeftFlagellumTip].Max(p => p.X);

            MinY = (int)ConvexHulls[PointLabels.RightFlagellumTip].Min(p => p.Y);
            MaxY = (int)ConvexHulls[PointLabels.RightFlagellumTip].Max(p => p.Y);

            
            if (GPU.UseGPU)
            {
                //Store target points copy on GPU
                var points = new float[TargetPoints.Count, 2];
                for (var i = 0; i < TargetPoints.Count; i++)
                {
                    points[i, 0] = (float)TargetPoints[i].X;
                    points[i, 1] = (float)TargetPoints[i].Y;
                }
                
                if(TargetPointsGPU != null)
                    GPU.Current.Free(TargetPointsGPU);

                if(TargetPoints.Count == 0)
                    points = new float[1,2]; //One dummy point at the origin

                TargetPointsGPU = GPU.Current.CopyToDevice(points);
            }
        }
        public bool IsAntenaColored(Frame target, System.Drawing.Point p)
        {
            var pxC = target.GetColor(p);

            return pxC.Distance(Color.Black) <= 90 || Math.Abs(pxC.GetHue() - 333) <= 10;
        }

        public static void ReadPriors()
        {
            var cols = File
                .ReadLines(PriorPath)
                .First()
                .Split(',')
                .Select((name,i) => new { name = name.Replace(@"""",""), i })
                .ToDictionary(c => c.name);

            Priors = File
                .ReadAllLines(PriorPath)
                .Skip(1)
                .Select(line =>
                {
                    var column = line
                        .Split(',')
                        .Select(col => { double parsed; double.TryParse(col, out parsed); return parsed; })
                        .ToArray();

                    //Parse csv columns
                    var result = new AntenaPoints()
                    {
                        LS = new Point(column[cols["lsX"].i], column[cols["lsY"].i]),
                        LFB = new Point(column[cols["lfbX"].i], column[cols["lfbY"].i]),
                        LFT = new Point(column[cols["lftX"].i], column[cols["lftY"].i]),

                        RS = new Point(column[cols["rsX"].i], column[cols["rsY"].i]),
                        RFB = new Point(column[cols["rfbX"].i], column[cols["rfbY"].i]),
                        RFT = new Point(column[cols["rftX"].i], column[cols["rftY"].i]),
                    };

                    return result;
                })
                .ToList();
        }

        public static void ReadHulls()
        {
            var cols = File
                .ReadLines(ConvexHullsPath)
                .First()
                .Split(',')
                .Select((name, i) => new { name = name.Replace(@"""", ""), i })
                .ToDictionary(c => c.name);

            ConvexHulls = new Dictionary<PointLabels, List<Point>>
            {
                { PointLabels.RightScape, new List<Point>() },
                { PointLabels.RightFlagellumBase, new List<Point>() },
                { PointLabels.RightFlagellumTip, new List<Point>() },

                { PointLabels.LeftScape, new List<Point>() },
                { PointLabels.LeftFlagellumBase, new List<Point>() },
                { PointLabels.LeftFlagellumTip, new List<Point>() },

                { PointLabels.Mandibles, new List<Point>() },
                { PointLabels.Head, new List<Point>() },

            };

            File
                .ReadAllLines(ConvexHullsPath)
                .Skip(1)
                .ToList()
                .ForEach(line =>
                {
                    var column = line
                        .Split(',')
                        .Select(col => { double parsed; return double.TryParse(col, out parsed) ? parsed : double.MinValue; })
                        .ToArray();

                    if (column[cols["lftX"].i] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.LeftFlagellumTip].Add(new Point(column[cols["lftX"].i], column[cols["lftY"].i]));
                    }

                    if (column[cols["rftX"].i] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.RightFlagellumTip].Add(new Point(column[cols["rftX"].i], column[cols["rftY"].i]));
                    }

                    if (column[cols["rfbX"].i] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.RightFlagellumBase].Add(new Point(column[cols["rfbX"].i], column[cols["rfbY"].i]));
                    }

                    if (column[cols["lfbX"].i] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.LeftFlagellumBase].Add(new Point(column[cols["lfbX"].i], column[cols["lfbY"].i]));
                    }

                    if (column[cols["rsX"].i] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.RightScape].Add(new Point(column[cols["rsX"].i], column[cols["rsY"].i]));
                    }

                    if (column[cols["lsX"].i] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.LeftScape].Add(new Point(column[cols["lsX"].i], column[cols["lsY"].i]));
                    }

                    if (column[cols["mandiblesX"].i] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.Mandibles].Add(new Point(column[cols["mandiblesX"].i], column[cols["mandiblesY"].i]));
                    }

                    //if (column[cols["lftX"].i] != double.MinValue)
                    //{
                    //    ConvexHulls[PointLabels.Head].Add(new Point(column[10], column[11]));
                    //}
                });
        }

        protected override AntenaPoints CreateChild(AntenaPoints parent1, AntenaPoints parent2)
        {
            var result = new AntenaPoints();

            result.RS.X = Cross(parent1.RS.X, parent2.RS.X, MinX, MaxX);
            result.RFB.X = Cross(parent1.RFB.X, parent2.RFB.X, MinX, MaxX);
            result.RFT.X = Cross(parent1.RFT.X, parent2.RFT.X, MinX, MaxX);
            result.LS.X = Cross(parent1.LS.X, parent2.LS.X, MinX, MaxX);
            result.LFB.X = Cross(parent1.LFB.X, parent2.LFB.X, MinX, MaxX);
            result.LFT.X = Cross(parent1.LFT.X, parent2.LFT.X, MinX, MaxX);

            result.RS.Y = Cross(parent1.RS.Y, parent2.RS.Y, MinY, MaxY);
            result.RFB.Y = Cross(parent1.RFB.Y, parent2.RFB.Y, MinY, MaxY);
            result.RFT.Y = Cross(parent1.RFT.Y, parent2.RFT.Y, MinY, MaxY);
            result.LS.Y = Cross(parent1.LS.Y, parent2.LS.Y, MinY, MaxY);
            result.LFB.Y = Cross(parent1.LFB.Y, parent2.LFB.Y, MinY, MaxY);
            result.LFT.Y = Cross(parent1.LFT.Y, parent2.LFT.Y, MinY, MaxY);

            return result;
        }

        protected override bool ValidChild(AntenaPoints child)
        {
            var valid =
                       child.RFB.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumBase])
                    && child.RFT.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumTip])

                    && child.LFB.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumBase])
                    && child.LFT.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumTip])

                    && child.LS.IsInPolygon(ConvexHulls[PointLabels.LeftScape])
                    && child.RS.IsInPolygon(ConvexHulls[PointLabels.RightScape])

                    ;

            return valid;
        }

        protected override AntenaPoints SelectLocation()
        {
            if (GPU.UseGPU)
            {
                //Free the target points since they're only useful once per frame
                if (TargetPointsGPU != null)
                {
                    GPU.Current.Free(TargetPointsGPU);
                    TargetPointsGPU = null;
                }
            }

            var bestSolution = Generation.First().Key.ToFrameSpace(new Point(Target.ShapeData.Width, Target.ShapeData.Height), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);

            if (PreviousSolution != null)
            {
                var leftPoints = TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumTip]));
                var rightPoints = TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumTip]));

                if (rightPoints <= MinPointsForRegression)
                {
                    bestSolution.RS = PreviousSolution.RS;
                    bestSolution.RFB = PreviousSolution.RFB;
                    bestSolution.RFT = PreviousSolution.RFT;
                }

                if (leftPoints <= MinPointsForRegression)
                {
                    bestSolution.LS = PreviousSolution.LS;
                    bestSolution.LFB = PreviousSolution.LFB;
                    bestSolution.LFT = PreviousSolution.LFT;
                }


            }

            PreviousSolution = bestSolution;

            var motionModel = model.Clone();

            ////take the best solution, and eval the tip location actoss the whole side of the head
            ////so for each pixel, eval the fitness
            //var fitnessMap = motionModel.MapByDistanceFunction(pt =>
            //{
            //    var newTry = bestSolution.Clone();

            //    if (pt.X <= motionModel.Width / 2.0)
            //        newTry.LFT = ToPriorSpace(pt.ToWindowsPoint());
            //    else
            //        newTry.RFT = ToPriorSpace(pt.ToWindowsPoint());

            //    lock (fitnesScores)
            //    {
            //        ComputeFitnessCPU(new AntenaPoints[] { newTry });

            //        return Math.Log(fitnesScores[0]) / 10.0;
            //    }
            //});

            //Draw the model
            using (var g = Graphics.FromImage(motionModel.Bitmap))
            {
                var yellow = new Pen(Color.Yellow, 1);
                var red = new Pen(Color.Red, 1);
                var black = new Pen(Color.Black, 1);

                g.DrawLines(yellow, new[]
                {
                    bestSolution.RS.ToPointF(),
                    bestSolution.RFB.ToPointF(),
                    bestSolution.RFT.ToPointF()
                });
                g.DrawLines(yellow, new[]
                {
                    bestSolution.LS.ToPointF(),
                    bestSolution.LFB.ToPointF(),
                    bestSolution.LFT.ToPointF()
                });

                var headBoundary = ConvexHulls[PointLabels.Head].Select(p => ToFrameSpace(p).ToPointF()).ToArray();
                var mouthBoundary = ConvexHulls[PointLabels.Mandibles].Select(p => ToFrameSpace(p).ToPointF()).ToArray();
                var left = ConvexHulls[PointLabels.LeftFlagellumTip].Select(p => ToFrameSpace(p).ToPointF()).ToArray();
                var right = ConvexHulls[PointLabels.RightFlagellumTip].Select(p => ToFrameSpace(p).ToPointF()).ToArray();
                var rightJ = ConvexHulls[PointLabels.RightFlagellumBase].Select(p => ToFrameSpace(p).ToPointF()).ToArray();
                var leftJ = ConvexHulls[PointLabels.LeftFlagellumBase].Select(p => ToFrameSpace(p).ToPointF()).ToArray();



                //g.DrawPolygon(red, headBoundary);
                g.DrawPolygon(red, mouthBoundary);
                g.DrawPolygon(red, left);
                g.DrawPolygon(red, right);
                g.DrawPolygon(red, rightJ);
                g.DrawPolygon(red, leftJ);
            }
            motionModel.ColorPixels(TargetPoints.Select(p => ToFrameSpace(p).ToDrawingPoint()).ToList(), Color.Blue);

            
            //DebugFrame.DrawFrame(fitnessMap, Target.ShapeData.Width, 0, 1, 0);
            DebugFrame.DrawFrame(motionModel, Target.ShapeData.Width*2, 0, 1, 0);
            motionModel.Dispose();
            //fitnessMap.Dispose();

            return bestSolution;
        }

        public override AntenaPoints Mutated(AntenaPoints individual)
        {
            var result = new AntenaPoints();

            result.RS.X = MutateValue(individual.RS.X, MinX, MaxX);
            result.RFB.X = MutateValue(individual.RFB.X, MinX, MaxX);
            result.RFT.X = MutateValue(individual.RFT.X, MinX, MaxX);
            result.LS.X = MutateValue(individual.LS.X, MinX, MaxX);
            result.LFB.X = MutateValue(individual.LFB.X, MinX, MaxX);
            result.LFT.X = MutateValue(individual.LFT.X, MinX, MaxX);


            result.RS.Y = MutateValue(individual.RS.Y, MinY, MaxY);
            result.RFB.Y = MutateValue(individual.RFB.Y, MinY, MaxY);
            result.RFT.Y = MutateValue(individual.RFT.Y, MinY, MaxY);
            result.LS.Y = MutateValue(individual.LS.Y, MinY, MaxY);
            result.LFB.Y = MutateValue(individual.LFB.Y, MinY, MaxY);
            result.LFT.Y = MutateValue(individual.LFT.Y, MinY, MaxY);

            return result;
        }
        protected override AntenaPoints CreateNewRandomMember()
        {
            return Priors[Random.Next(0, Priors.Count)];
        }

        private const double LengthMinimizationBegin = 0;//0.75;

        private static bool InConvexHulls(Point point)
        {
            var result =
                point.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumTip]) ||
                point.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumTip]) ||
                point.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumBase]) ||
                point.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumBase])
                ;

            return result;
        }

        public Point ToPriorSpace(Point p)
        {
            return p.ToPriorSpace(new Point(Target.ShapeData.Width, Target.ShapeData.Height), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
        }

        public Point ToFrameSpace(Point p)
        {
            return p.ToFrameSpace(new Point(Target.ShapeData.Width, Target.ShapeData.Height), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
        }
        public Point ToPriorSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToPriorSpace(new Point(Target.ShapeData.Width, Target.ShapeData.Height), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
        }

        public Point ToFrameSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToFrameSpace(new Point(Target.ShapeData.Width, Target.ShapeData.Height), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
        }

        private static float[] fitnesScores;
        public override void ComputeFitness()
        {
            if (fitnesScores == null || fitnesScores.Length < Generation.Count)
                fitnesScores = new float[Generation.Count];

            var uncomputed = Generation
                .Where(pair => pair.Value == InitialFitness)
                .Select(pair => pair.Key)
                .ToArray();

            if (GPU.UseGPU) 
                ComputeFitnessGPU(uncomputed); 
            else 
                ComputeFitnessCPU(uncomputed);

            Parallel.For(0, uncomputed.Length, i =>
            {
                //for (int i = 0; i < Generation.Count; i++) {
                Generation[uncomputed[i]] = fitnesScores[i];
                //}
            });
        }

        public void ComputeFitnessCPU(AntenaPoints[] uncomputed)
        {
            Parallel.For(0, uncomputed.Length, new ParallelOptions() { /*MaxDegreeOfParallelism = 1*/ }, i =>
            ////for (var i = 0; i < Generation.Count; i++)
            {
                var individual = uncomputed[i];
                var sumOfMinDistances = 0.0;

                var D1 = TargetPoints.DistToSegmentSquared(individual.RS, individual.RFB);
                var D2 = TargetPoints.DistToSegmentSquared(individual.RFB, individual.RFT);

                var D3 = TargetPoints.DistToSegmentSquared(individual.LS, individual.LFB);
                var D4 = TargetPoints.DistToSegmentSquared(individual.LFB, individual.LFT);

                //sum of minimum distances from each point defined by the segments
                for (var p = 0; p < TargetPoints.Count; p++)
                {
                    var distSquared = Math.Min(Math.Min(D1[p], D2[p]), Math.Min(D3[p], D4[p]));

                    sumOfMinDistances += Math.Sqrt(distSquared);
                }

                fitnesScores[i] = (float)(sumOfMinDistances + individual.L1 + individual.L2 + individual.L3 + individual.L4);
            });
            //}
        }

        public void ComputeFitnessGPU(AntenaPoints[] uncomputed)
        {
            //For each individual (25-2000)
            //Compute distance from segments defined by it (4 segs) and all of the target points (20-500)
            //Total parallel computations indivs*4*points
            //Stratgies: 1 thread/individual -> controllable by user, utilizes the avail thread pool
            //1 thread per ivividual*point -> more complicated
            //1 thread per point -> points will vary by frame, may not be good use of pool
            //Lets go with 1/individual

            var gpu = GPU.Current;

            const int blockSize = 256;
            var gridSize = (int)Math.Ceiling(uncomputed.Length/(1.0*blockSize));
            var individuals = new float[uncomputed.Length, 12];

            //Convert the segments into arrays
            for (var i = 0; i < uncomputed.Length; i++)
            {
                individuals[i, 0] = (float)uncomputed[i].RS.X;
                individuals[i, 1] = (float)uncomputed[i].RFB.X;
                individuals[i, 2] = (float)uncomputed[i].RFT.X;
                individuals[i, 3] = (float)uncomputed[i].LS.X;
                individuals[i, 4] = (float)uncomputed[i].LFB.X;
                individuals[i, 5] = (float)uncomputed[i].LFT.X;

                individuals[i, 6] =  (float)uncomputed[i].RS.Y;
                individuals[i, 7] =  (float)uncomputed[i].RFB.Y;
                individuals[i, 8] =  (float)uncomputed[i].RFT.Y;
                individuals[i, 9] =  (float)uncomputed[i].LS.Y;
                individuals[i, 10] = (float)uncomputed[i].LFB.Y;
                individuals[i, 11] = (float)uncomputed[i].LFT.Y;
            }

            var minDistancesDev = gpu.Allocate<float>(uncomputed.Length);
            var individualsDev = gpu.CopyToDevice(individuals);

            //Compute min distances from points to each segment
            gpu.Launch
            (
                gridSize, blockSize, Kernels.MinDistanceToSegmentsKernel,
                minDistancesDev, individualsDev, uncomputed.Length, TargetPointsGPU, TargetPoints.Count
            );

            //DEBUG CODE - DON'T DELETE
            //var points = new float[TargetPoints.Count, 2];
            //if (TargetPoints.Count > 0)
            //    gpu.CopyFromDevice(TargetPointsGPU, points);

            //Kernels.MinDistanceToSegmentsKernel(new GThread(0, 0, new GBlock(new GGrid(1), 256, 0, 0)),
            //    fitnesScores, individuals, uncomputed.Length, points, TargetPoints.Count);

            //Retrieve distances from GPU
            gpu.CopyFromDevice(minDistancesDev, fitnesScores);

            gpu.Free(minDistancesDev);
            gpu.Free(individualsDev);
        }
    }
}