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
using Accord.MachineLearning.VectorMachines;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Drawing.Imaging;
using DPoint = System.Drawing.Point;
using WPoint = System.Windows.Point;
using MoreLinq;

namespace SwarmSight.HeadPartsTracking.Algorithms
{
    public class AntenaPoints : IDisposable
    {
        /// <summary>
        /// Right scape
        /// </summary>
        public Point RS;

        /// <summary>
        /// Right flagellum base
        /// </summary>
        public Point RFB;

        /// <summary>
        /// Right flagellum tip
        /// </summary>
        public Point RFT;

        /// <summary>
        /// Left scape
        /// </summary>
        public Point LS;

        /// <summary>
        /// Left flagellum base
        /// </summary>
        public Point LFB;

        /// <summary>
        /// Left flagellum tip
        /// </summary>
        public Point LFT;

        /// <summary>
        /// Left flagellum tip (from scape) angle
        /// </summary>
        public double LTA;

        /// <summary>
        /// Right flagellum tip (from scape) angle
        /// </summary>
        public double RTA;

        /// <summary>
        /// Left scape (to flagellum base) angle
        /// </summary>
        public double LSA;

        /// <summary>
        /// Right scape (to flagellum base) angle
        /// </summary>
        public double RSA;

        

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
            RS = RS.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            RFB = RFB.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            RFT = RFT.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            LS = LS.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            LFB = LFB.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            LFT = LFT.ToFrameSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();

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
        public static double ToPriorAngle(this double tanhAngle)
        {
            var zeroTothree60 = (tanhAngle - 90 + 360) % 360;

            if (zeroTothree60 > 180)
                return zeroTothree60 - 360;
            else
                return zeroTothree60;
        }
        public static double Distance(this Point a, Point b)
        {
            var x = b.X - a.X;
            var y = b.Y - a.Y;

            return Math.Sqrt(x*x + y*y);
        }
        public static double Distance(this System.Drawing.Point a, System.Drawing.Point b)
        {
            var x = b.X - a.X;
            var y = b.Y - a.Y;

            return Math.Sqrt(x * x + y * y);
        }

        public static Point ToWindowsPoint(this System.Drawing.Point target)
        {
            return new Point(target.X, target.Y);
        }
        public static PointF ToPointF(this Point target)
        {
            return new PointF((float) target.X, (float) target.Y);
        }
        public static PointF ToPointF(this System.Drawing.Point target)
        {
            return new PointF(target.X, target.Y);
        }
        public static Point Moved(this Point target, double byX, double byY)
        {
            return new Point(target.X + byX, target.Y + byY);
        }
        public static System.Drawing.Point Moved(this System.Drawing.Point target, int byX, int byY)
        {
            return new System.Drawing.Point(target.X + byX, target.Y + byY);
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

        public static System.Drawing.Point ToFrameSpace(this Point target, Point windowDims, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double scapeDistance)
        {
            target = target.Multiply(scaleX * scapeDistance, scaleY * scapeDistance);
            target = target.Rotate(headAngle - priorAngle);
            target.Offset(windowDims.X / 2.0 - offsetX, windowDims.Y / 2.0 - offsetY);

            return target.ToDrawingPoint();
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

        public static double Positive(this double target)
        {
            return Math.Abs(target);
        }
        public static int Positive(this int target)
        {
            return Math.Abs(target);
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

        public static double? Median<TColl, TValue>(
            this IEnumerable<TColl> source,
            Func<TColl, TValue> selector)
        {
            return source.Select<TColl, TValue>(selector).Median();
        }

        public static double? Median<T>(
            this IEnumerable<T> source)
        {
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
                source = source.Where(x => x != null);

            int count = source.Count();
            if (count == 0)
                return null;

            source = source.OrderBy(n => n);

            int midpoint = count / 2;
            if (count % 2 == 0)
                return (Convert.ToDouble(source.ElementAt(midpoint - 1)) + Convert.ToDouble(source.ElementAt(midpoint))) / 2.0;
            else
                return Convert.ToDouble(source.ElementAt(midpoint));
        }

        public static Point ToPoint(this PointF pt)
        {
            return new Point(pt.X, pt.Y);
        }


        public static Point GetPointDistanceAwayOnLine(this Point startPt, double length, Point linePoint, Point closerToPoint)
        {
            PointF int1, int2;

            Regression.FindLineCircleIntersections((float)startPt.X, (float)startPt.Y, (float)length,
                                startPt.ToPointF(), linePoint.ToPointF(), out int1, out int2);

            var result =
                int1.ToPoint().Distance(closerToPoint) < int2.ToPoint().Distance(closerToPoint) ?
                int1 : int2;

            return result.ToPoint();
        }

        public static Point3D CombineMirrorPoints(Point top, Point side, double d, double angle)
        {
            var b = d + top.X;
            var alpha = 90 - angle;
            var zprime = -side.X;
            var c = Math.Cos(alpha * Math.PI / 180.0) * zprime;
            var e = b + c;
            var g = e / Math.Cos(angle * Math.PI / 180.0);
            var f = Math.Sqrt(g * g + zprime * zprime);
            var h = Math.Sqrt(f * f - b * b);

            return new Point3D(top.X, top.Y, h);
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
        private Frame headClip = null;
        public int[] RightBins;
        public int[] LeftBins;
        public int RightHighest;
        public int LeftHighest;


        public System.Drawing.Point HeadOffset;
        public System.Drawing.Point HeadDims;
        private LinkedList<Point> LTpoints = new LinkedList<Point>();
        private LinkedList<Point> LBpoints = new LinkedList<Point>();
        private LinkedList<Point> RTpoints = new LinkedList<Point>();
        private LinkedList<Point> RBpoints = new LinkedList<Point>();
        public int MotionModelThreshold = 20;
        public Point? TopViewLeft = null;

        private unsafe List<System.Drawing.Point> UpdateMotionModel()
        {
            var decay = 0.15;//0.85;

            var modelPx = model.FirstPixelPointer;

            var currentPx = Target.Current.FirstPixelPointer;
            var prev1Px = Target.Prev1.FirstPixelPointer;
            var prev2Px = Target.Prev2.FirstPixelPointer;

            var width = Math.Min(HeadDims.X, Target.Current.Width - HeadOffset.X);
            var height = Math.Min(HeadDims.Y, Target.Current.Height - HeadOffset.Y);
            var stride = Target.Current.Stride;
            var modelStride = model.Stride;
            var offsetX = HeadOffset.X;
            var offsetY = HeadOffset.Y;

            var antennaColor = AntennaAndPERDetector.Config.AntennaColors[0];
            var antR = antennaColor.R;
            var antG = antennaColor.G;
            var antB = antennaColor.B;

            var threshold = MotionModelThreshold;

            var result = new List<Point3D>(width*height/16);

            Parallel.For(0, height, new ParallelOptions()
            {
                MaxDegreeOfParallelism = 1
            }, 
            (int y) =>
            {
                var rowStart = stride * (y + offsetY);
                var modelRowStart = modelStride * y;
                var rowTotal = new List<Point3D>(width/16);

                for (var x = 0; x < width; x++)
                {
                    var modelOffsetG = x * 3 + modelRowStart + 1;

                    var offsetB = (x + offsetX) * 3 + rowStart;
                    var offsetG = offsetB + 1;
                    var offsetR = offsetB + 2;
                    
                    //Optimized mem access
                    var curDist =   (Math.Abs(currentPx[offsetR] - antR) + Math.Abs(currentPx[offsetG] - antG) + Math.Abs(currentPx[offsetB] - antB)) / 3.0;
                    var prev1Dist = (Math.Abs(prev1Px[offsetR] - antR) + Math.Abs(prev1Px[offsetG] - antG) + Math.Abs(prev1Px[offsetB] - antB)) / 3.0;
                    var prev2Dist = (Math.Abs(prev2Px[offsetR] - antR) + Math.Abs(prev2Px[offsetG] - antG) + Math.Abs(prev2Px[offsetB] - antB)) / 3.0;

                    //var combMotionDist = ((prev2Dist - prev1Dist) + (curDist - prev1Dist)) / 2.0;
                    var combMotionDist = (prev2Dist + curDist - 2 * prev1Dist) / 2.0; //Simplified

                    //var modelDecayed = (modelPx[modelOffsetB] * decay).Rounded();

                    var currVal = modelPx[modelOffsetG];
                    var newModel = currVal * decay + combMotionDist;

                    if (newModel <= threshold)
                    {
                        newModel = 0;
                    }
                    else //Collect points over threshold
                    {
                        if (newModel > 255)
                            newModel = 255;
                        
                        rowTotal.Add(new Point3D(x, y, newModel));
                    }

                    modelPx[modelOffsetG] = (byte)newModel;
                }

                lock(result)
                {
                    result.AddRange(rowTotal);
                }
            });

            var mostActive = result
                .OrderByDescending(p => p.Z)
                .Select(p => new System.Drawing.Point((int)p.X, (int)p.Y))
                .ToList();

            model.ColorPixels(mostActive.Skip(result.Count * 3 / 4).ToList(), Color.Black);

            return
                mostActive
                .Take(result.Count * 3 / 4)
                .ToList();
        }
        public bool EvalExpected = false;
        public override void PreProcessTarget()
        {
            if (Priors == null)
            {
                ReadPriors();
                ReadHulls();
                ReadExpected();
            }

            //if (!Expected.ContainsKey(Target.Prev1.FrameIndex))
            //    return;

            SortDescending = false;
            ValidateRandom = false;
            NumberOfGenerations = 1;// 150;
            GenerationSize = 10;// 50;
            MutationProbability = 0.1;
            PercentRandom = 80;
            MutationRange = 1.0;
            
            if (model == null)
            {
                model = new Frame(HeadDims.X, HeadDims.Y, Target.Current.PixelFormat, false);
            }
            
            var modelPts = UpdateMotionModel();

            using (var headClip = Target.Prev1.SubClipped(HeadOffset.X, HeadOffset.Y, HeadDims.X, HeadDims.Y))
            using (var contrastedHead = headClip.ContrastFilter(0.1f, 60f))
            {
                var leftPoints = GetTip
                (
                    headClip, contrastedHead, model, modelPts, new Point(-0.5, 0),
                    PointLabels.LeftFlagellumTip, PointLabels.LeftFlagellumBase,
                    LTpoints, LBpoints
                );

                var rightPoints = GetTip
                (
                    headClip, contrastedHead, model, modelPts, new Point(0.5, 0),
                    PointLabels.RightFlagellumTip, PointLabels.RightFlagellumBase, 
                    RTpoints, RBpoints
                );

                PreviousSolution = new AntenaPoints();

                if (leftPoints.Count > 0)
                {
                    PreviousSolution.LFT = leftPoints[PointLabels.LeftFlagellumTip];
                }
                if (rightPoints.Count > 0)
                {
                    PreviousSolution.RFT = rightPoints[PointLabels.RightFlagellumTip];
                }


                if (EvalExpected && Expected.ContainsKey(Target.Prev1.FrameIndex))
                {
                    var currExp = Expected[Target.Prev1.FrameIndex];
                    var outString = Target.Prev1.FrameIndex.ToString() + ",";
                    var saveFrame = false;

                    #region save tip images
                    //Save tip images
                    //var lMoved = new Point(currExp[1], currExp[2]).Moved(-HeadOffset.X, -HeadOffset.Y);
                    //var rMoved = new Point(currExp[3], currExp[4]).Moved(-HeadOffset.X, -HeadOffset.Y);
                    //var winSize = 7;
                    //var lClip = curClip.SubClipped((int)lMoved.X - winSize / 2, (int)lMoved.Y - winSize / 2, winSize, winSize);
                    //var rClip = curClip.SubClipped((int)rMoved.X - winSize / 2, (int)rMoved.Y - winSize / 2, winSize, winSize);
                    //var rand1 = curClip.SubClipped(new Random().Next(0,curClip.Width), new Random().Next(0, curClip.Width), winSize, winSize);
                    //var rand2 = curClip.SubClipped(new Random().Next(0, curClip.Width), new Random().Next(0, curClip.Width), winSize, winSize);

                    //File.AppendAllLines(@"c:\temp\frames\tips.csv", new[]
                    //{
                    //    "1,"+string.Join(",", lClip.ToAccordInput(0))+","+string.Join(",", lClip.ToAccordInput(1))+","+string.Join(",", lClip.ToAccordInput(2)),
                    //    "1,"+string.Join(",", rClip.ToAccordInput(0))+","+string.Join(",", rClip.ToAccordInput(1))+","+string.Join(",", rClip.ToAccordInput(2)),
                    //    "-1,"+string.Join(",", rand1.ToAccordInput(0))+","+string.Join(",", rand1.ToAccordInput(1))+","+string.Join(",", rand1.ToAccordInput(2)),
                    //    "-1,"+string.Join(",", rand2.ToAccordInput(0))+","+string.Join(",", rand2.ToAccordInput(1))+","+string.Join(",", rand2.ToAccordInput(2)),
                    //}); 
                    #endregion

                    //Refine tip location
                    if (leftPoints.Count > 0)
                    {
                        var expLFT = new System.Drawing.Point(currExp[1], currExp[2]);
                        var actLFT = ToFrameSpace(leftPoints[PointLabels.LeftFlagellumTip]);

                        headClip.MarkPoint(actLFT, Color.Yellow);
                        actLFT.Offset(HeadOffset.X, HeadOffset.Y);
                        var LFTdist = actLFT.Distance(expLFT);
                        outString += string.Join(",", new[] { expLFT.X, expLFT.Y, actLFT.X, actLFT.Y });

                        expLFT.Offset(-HeadOffset.X, -HeadOffset.Y);
                        headClip.MarkPoint(expLFT, Color.Red);

                        if (LFTdist > 6)
                            saveFrame = true;
                    }
                    else
                    {
                        outString += ",,";
                    }

                    if (rightPoints.Count > 0)
                    {
                        var expRFT = new System.Drawing.Point(currExp[3], currExp[4]);
                        var actRFT = ToFrameSpace(rightPoints[PointLabels.RightFlagellumTip]);

                        headClip.MarkPoint(actRFT, Color.Yellow);
                        actRFT.Offset(HeadOffset.X, HeadOffset.Y);
                        var RFTdist = actRFT.Distance(expRFT);
                        outString += "," + string.Join(",", new[] { expRFT.X, expRFT.Y, actRFT.X, actRFT.Y });

                        expRFT.Offset(-HeadOffset.X, -HeadOffset.Y);
                        headClip.MarkPoint(expRFT, Color.Red);

                        if (RFTdist > 6)
                            saveFrame = true;
                    }
                    else
                    {
                        outString += ",,,";
                    }

                    Debug.WriteLine(outString);


                    
                }
                else
                {
                    if (leftPoints.Count > 0)
                        DrawTip(headClip,
                            ToFrameSpace(leftPoints[PointLabels.LeftFlagellumTip]),
                            ToFrameSpace(leftPoints[PointLabels.LeftFlagellumBase]));

                    if (rightPoints.Count > 0)
                        DrawTip(headClip,
                            ToFrameSpace(rightPoints[PointLabels.RightFlagellumTip]),
                            ToFrameSpace(rightPoints[PointLabels.RightFlagellumBase]));


                    if (DebugFrame != null)
                        DebugFrame.Dispose();

                    DebugFrame = new Frame(headClip.Width * 2, headClip.Height, headClip.PixelFormat, false);
                    DebugFrame.DrawFrame(headClip, 0, 0, 1, 0);
                    DebugFrame.DrawFrame(model, headClip.Width, 0, 1, 0);

                    //if (saveFrame)
                    {
                        DebugFrame.Bitmap.Save(@"c:\temp\frames\" + Target.Prev1.FrameIndex + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    if (DebugFrame != null)
                    { 
                        DebugFrame.Dispose();
                        DebugFrame = null;
                    }
                }
            }

            return;

            #region motion quadrants
            ////compute the sum of motion in a set of quadrants
            ////sweep though pixels and add their values to a bin based on their location
            //var regionCount = 5; //per side, should be even
            //LeftBins = new int[regionCount];
            //RightBins = new int[regionCount];

            //model.ForEachPoint((p, c) =>
            //{
            //    if(c.R > 35)
            //    {
            //        var pt = ToPriorSpace(p.ToWindowsPoint());
            //        var degs = Math.Atan2(pt.Y, pt.X) * 180.0 / Math.PI;
            //        degs = degs < -90 ? degs + 360.0 : degs;

            //        if (Math.Abs(degs) <= 90.0)
            //        {
            //            var regionIndex = Math.Min(regionCount - 1, (int)(degs.Scale(-90, 90, 0, 1) * regionCount));
            //            RightBins[regionIndex] += c.R;
            //        }
            //        else
            //        {
            //            var regionIndex = Math.Min(regionCount - 1, (int)(degs.Scale(90, 270, 0, 1) * regionCount));
            //            LeftBins[regionIndex] += c.R;
            //        }
            //    }
            //});

            //if (RightBins.Max() > 150)
            //    RightHighest = RightBins.ToList().IndexOf(RightBins.Max());

            //if(LeftBins.Max() > 150)
            //    LeftHighest = regionCount - 1 - LeftBins.ToList().IndexOf(LeftBins.Max());


            //Debug.WriteLine(leftHighest + "," + rightHighest);

            //var directionFrame = model.Clone();
            //directionFrame.ColorIfTrue(Color.Yellow, p =>
            //{
            //    if (Random.NextDouble() < 0.1)
            //    {
            //        var pt = ToPriorSpace(p.ToWindowsPoint());
            //        var degs = Math.Atan2(pt.Y, pt.X)*180.0/Math.PI;
            //        degs = degs < -90 ? degs + 360.0 : degs;

            //        if (Math.Abs(Math.Abs(degs)-90) < 0.5)
            //            return true;

            //        if (Math.Abs(degs) < 90.0)
            //        {
            //            var regionIndex = Math.Min(regionCount - 1, (int) (degs.Scale(-90, 90, 0, 1)*regionCount));

            //            return regionIndex == RightHighest;
            //        }
            //        else
            //        {
            //            var regionIndex = Math.Min(regionCount - 1, (int) (degs.Scale(90, 270, 0, 1)*regionCount));

            //            return regionIndex == regionCount - 1 - LeftHighest;
            //        }
            //    }

            //    return false;
            //}); 
            #endregion

            #region old
            //var priorSpaced = model
            //    .PointsOverThreshold(36)
            //    .AsParallel()
            //    .Select(p => new Point(p.X, p.Y).ToPriorSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance))
            //    .ToList();

            //var leftSide = priorSpaced
            //    .Where(p =>
            //        p.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumTip])
            //     || p.IsInPolygon(ConvexHulls[PointLabels.LeftFlagellumBase]))
            //     .OrderByDescending(p => model.GetColor(ToFrameSpace(p).ToDrawingPoint()).R)
            //     .ToList();

            //var rightSide = priorSpaced
            //    .Where(p =>
            //        p.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumTip])
            //     || p.IsInPolygon(ConvexHulls[PointLabels.RightFlagellumBase]))
            //     .OrderByDescending(p => model.GetColor(ToFrameSpace(p).ToDrawingPoint()).R)
            //     .ToList();

            //var top = leftSide
            //    .AsParallel()
            //    .Take((leftSide.Count * 0.5).Rounded())
            //    .Union(
            //        rightSide
            //        .AsParallel()
            //        .Take((rightSide.Count * 0.5).Rounded())
            //    )
            //    .ToList();

            ////Reduce the point count
            //var pointCount = top.Count;

            //if (pointCount > PointsToKeep)
            //    top = top.Where(p => Random.NextDouble() < (double) PointsToKeep/pointCount).ToList();


            //var withinPolygons = top.Where(p =>
            //        !p.IsInPolygon(ConvexHulls[PointLabels.Head]) &&
            //        !p.IsInPolygon(ConvexHulls[PointLabels.Mandibles])
            //        )
            //    .ToList();

            //TargetPoints = withinPolygons;
            ////TargetPointsPrev = withinPolygons;

            ////using (var test = Target.ShapeData.Clone())
            ////{
            ////    test.ColorPixels(TargetPoints.Select(p => ToFrameSpace(p).ToDrawingPoint()).ToList(), Color.Yellow);
            ////    test.Bitmap.Save(@"y:\downloads\beevids\downframes\" + FrameIndex + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            ////    FrameIndex++;
            ////}

            ////Debug.WriteLine("Target Points:" 
            ////    + " R: " + TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.RightTip]))
            ////    + " L: " + TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.LeftTip])));

            //MinX = (int)ConvexHulls[PointLabels.RightFlagellumTip].Min(p => p.X);
            //MaxX = (int)ConvexHulls[PointLabels.LeftFlagellumTip].Max(p => p.X);

            //MinY = (int)ConvexHulls[PointLabels.RightFlagellumTip].Min(p => p.Y);
            //MaxY = (int)ConvexHulls[PointLabels.RightFlagellumTip].Max(p => p.Y);


            //if (GPU.UseGPU)
            //{
            //    //Store target points copy on GPU
            //    var points = new float[TargetPoints.Count, 2];
            //    for (var i = 0; i < TargetPoints.Count; i++)
            //    {
            //        points[i, 0] = (float)TargetPoints[i].X;
            //        points[i, 1] = (float)TargetPoints[i].Y;
            //    }

            //    if(TargetPointsGPU != null)
            //        GPU.Current.Free(TargetPointsGPU);

            //    if(TargetPoints.Count == 0)
            //        points = new float[1,2]; //One dummy point at the origin

            //    TargetPointsGPU = GPU.Current.CopyToDevice(points);
            //}
            #endregion
        }

        private void DrawTip(Frame headImage, System.Drawing.Point tipPoint, System.Drawing.Point basePoint)
        {
            using (var g = Graphics.FromImage(headImage.Bitmap))
            {
                var penThick = new Pen(Color.Yellow) { Width = 3 };
                var penThin = new Pen(Color.Blue) { Width = 1 };

                var penRed = new Pen(Color.Orange);
                var length = ToFrameSpace(new Point(1, 0)).X;

                var ellipseWidth = (0.1 * length).Rounded();
                g.DrawEllipse(penThick, (float)(tipPoint.X - ellipseWidth / 2.0), (float)(tipPoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);
                g.DrawEllipse(penThin, (float)(tipPoint.X - ellipseWidth / 2.0), (float)(tipPoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);

                //Joint
                //g.DrawEllipse(penThick, (float)(expectedBasePoint.X - ellipseWidth / 2.0), (float)(expectedBasePoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);
                //g.DrawEllipse(penThin, (float)(expectedBasePoint.X - ellipseWidth / 2.0), (float)(expectedBasePoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);

                if(TopViewLeft != null)
                {
                    var tvFrame = ToFrameSpace(TopViewLeft.Value);
                    g.DrawLine(penThick, 0, (float)(tvFrame.Y * 1.2), 1000, (float)(tvFrame.Y * 1.2));
                    g.DrawLine(penThin, 0, (float)(tvFrame.Y * 0.8), 1000, (float)(tvFrame.Y * 0.8));
                }

                var leftVanish = basePoint.ToWindowsPoint().GetPointDistanceAwayOnLine(length * 100, tipPoint.ToWindowsPoint(), tipPoint.ToWindowsPoint()).ToDrawingPoint();

                if (double.IsNaN(leftVanish.X) || int.MinValue == leftVanish.X)
                    leftVanish = tipPoint;

                g.DrawLine(penThick, tipPoint, leftVanish);
                g.DrawLine(penThin, tipPoint, leftVanish);

                var tipPointPrior = ToPriorSpace(tipPoint);
                var basePointPrior = ToPriorSpace(basePoint);

                var angle = (Math.Atan2(-tipPointPrior.Y - (-basePointPrior.Y),
                    basePointPrior.X - tipPointPrior.X) * 180.0 / Math.PI).ToPriorAngle();

                //var binCount = 5;
                //var binWidth = 180.0 / 5;
                //var binIndex = Math.Min(binCount - 1, Math.Floor(angle / binWidth));

                //g.DrawArc(penThick, 0, 0, headImage.Width, headImage.Height, (float)(binIndex*binWidth-90), (float)binWidth); 


                g.DrawString(angle.Rounded().ToString() + "°", new Font(FontFamily.GenericSansSerif, 10.0f, System.Drawing.FontStyle.Regular),
                    new SolidBrush(Color.Yellow), (float)(tipPoint.X - ellipseWidth / 2.0 + 1), (float)(tipPoint.Y + ellipseWidth + 1));
                g.DrawString(angle.Rounded().ToString() + "°", new Font(FontFamily.GenericSansSerif, 10.0f, System.Drawing.FontStyle.Regular),
                    new SolidBrush(Color.Blue), (float)(tipPoint.X - ellipseWidth / 2.0), (float)(tipPoint.Y + ellipseWidth));
            }
        }

        public Dictionary<PointLabels, Point> PrevTips = new Dictionary<PointLabels, Point>();

        private List<Tuple<DPoint, double>> AntPts(Frame headClip, PointLabels tipLabel, PointLabels baseLabel)
        {
            var pts = new List<Tuple<DPoint, double>>(100);

            headClip.ForEachPoint((p,c) => {
                var pPrior = ToPriorSpace(p);
                var dist = c.Distance(Color.FromArgb(29, 29, 17));

                var include =
                pPrior.Distance(new Point(0, 0)) > 3.0 &&
                dist < 60
                && !pPrior.IsInPolygon(ConvexHulls[PointLabels.Mandibles])
                && !pPrior.IsInPolygon(ConvexHulls[baseLabel])
                && pPrior.IsInPolygon(ConvexHulls[tipLabel]);

                if (include)
                    lock (pts)
                    {
                        pts.Add(new Tuple<DPoint, double>(p, 255 * (1 - dist / 60)));
                    }
            });
            //headClip.ColorPixels(pts.Select(p => p.Item1).ToList(), Color.Blue);

            return pts;
        }
        private List<Tuple<DPoint,double>> AntPts2(Frame headClip, PointLabels tipLabel, PointLabels baseLabel)
        {
            var pts = new List<Tuple<DPoint, double>>(100);

            headClip.ForEachPoint((p, c) => {
                var pPrior = ToPriorSpace(p);

                var include =
                pPrior.Distance(new Point(0, 0)) > 3.0 &&
                c.G > 10 
                && !pPrior.IsInPolygon(ConvexHulls[PointLabels.Mandibles])
                && !pPrior.IsInPolygon(ConvexHulls[baseLabel])
                && pPrior.IsInPolygon(ConvexHulls[tipLabel]);

                if (include)
                    lock (pts)
                    {
                        pts.Add(new Tuple<DPoint, double>(p, 255 * c.G / 245));
                    }
            });

            return pts;
        }

        private Dictionary<PointLabels,Point> GetTip(Frame headClip, Frame contrastedHead, Frame motionClip, List<System.Drawing.Point> modelPts, Point origin, PointLabels tipLabel, PointLabels baseLabel, LinkedList<Point> tipBuffer, LinkedList<Point> baseBuffer)
        {
            var result = new Dictionary<PointLabels, Point>();
            
            var rawAnt = AntPts(contrastedHead, tipLabel, baseLabel);
            var moAnt = AntPts2(motionClip, tipLabel, baseLabel);
            var ctr = ToFrameSpace(new Point(tipLabel == PointLabels.LeftFlagellumTip ? -0.5 : 0.5, 0));
            //var rawAntPts = rawAnt.Select(p => p.Item1).ToList();
            //var moAntPts = moAnt.Select(p => p.Item1).ToList();

            var t = headClip.Clone();
            //t.ColorPixels(rawAntPts, Color.White);
            //t.ColorPixels(moAntPts, Color.White);

            
            rawAnt.AddRange(moAnt);

            //Median the point list
            //t.ColorPixels(rawAnt.Select(p => p.Item1).ToList(), Color.White);
            if(rawAnt.Count > 100)
                rawAnt = rawAnt.MedianFilter(1);
            //t = headClip.Clone();
            //t.Clone().ColorPixels(rawAnt.Select(p => p.Item1).ToList(), Color.White);

            if (rawAnt.Count > 0)
            {
                //Weighted centroid
                var sumW = rawAnt.Sum(p => p.Item2);
                var centerX = rawAnt.Sum(p => p.Item1.X * p.Item2 / sumW);
                var centerY = rawAnt.Sum(p => p.Item1.Y * p.Item2 / sumW);
                var med = new DPoint(centerX.Rounded(), centerY.Rounded());

                //Ensure an actual point
                med = rawAnt.MinBy(p => p.Item1.Distance(med)).Item1;

                t.ColorPixels(rawAnt.Select(p => p.Item1).ToList(), Color.White);

                //Could do with lookup table
                var tip = CrawlToTip(t, med, ctr, 1, 34);
                
                
                t.MarkPoint(med);
                t.MarkPoint(tip, inner:Color.Orange);

                PrevTips[tipLabel] = result[tipLabel] = ToPriorSpace(tip);
                PrevTips[baseLabel] = result[baseLabel] = ToPriorSpace(med);
            }
            else if(PrevTips.Count > 0) //If nothing at all, use previous solution
            {
                result[tipLabel] = PrevTips[tipLabel];
                result[baseLabel] = PrevTips[baseLabel];
            }

            
            //Always save
            t.Bitmap.Save(@"c:\temp\frames\lomo\" + tipLabel.ToString() + "-" + Target.Prev1.FrameIndex + ".jpg", ImageFormat.Jpeg);

            return result;
        }

        public System.Drawing.Point CrawlToTip(Frame target, System.Drawing.Point start, System.Drawing.Point origin, int threshold = 50, int radius = 30)
        {
            var loopCount = 0;
            var startColor = target.GetColor(start);
            var bestDistance = start.Distance(origin);
            var bestPoint = start;
            var visited = new bool[radius * 2+1, radius * 2+1];
            var visitedOffset = new System.Drawing.Point(start.X - radius, start.Y - radius);
            var queue = new Queue<System.Drawing.Point>();
            queue.Enqueue(start);

            while (queue.Count > 0 && loopCount < 500)
            {
                loopCount++;
                var point = queue.Dequeue();

                new List<System.Drawing.Point>
                {
                    new System.Drawing.Point(point.X - 1, point.Y),
                    new System.Drawing.Point(point.X + 1, point.Y),
                    new System.Drawing.Point(point.X, point.Y - 1),
                    new System.Drawing.Point(point.X, point.Y + 1),

                    new System.Drawing.Point(point.X - 2, point.Y),
                    new System.Drawing.Point(point.X + 2, point.Y),
                    new System.Drawing.Point(point.X, point.Y - 2),
                    new System.Drawing.Point(point.X, point.Y + 2),

                    new System.Drawing.Point(point.X - 2, point.Y - 2),
                    new System.Drawing.Point(point.X + 2, point.Y + 2),
                    new System.Drawing.Point(point.X + 2, point.Y - 2),
                    new System.Drawing.Point(point.X - 2, point.Y + 2),
                }
                .ForEach(p =>
                {
                    //Check bounds
                    if (p.X < 0 || p.X >= target.Width ||
                        p.Y < 0 || p.Y >= target.Height)
                        return;
                    
                    //Check if within search radius
                    var distFromStart = p.Distance(start);

                    if (distFromStart > radius)
                        return;

                    //Check if already checked
                    var visitedPos = new System.Drawing.Point(p.X - visitedOffset.X, p.Y - visitedOffset.Y);
                    var exists = visited[visitedPos.X, visitedPos.Y];
                    //var searchTarget = p.X * 100000 + p.Y;
                    //var searchResult = visited.BinarySearch(searchTarget);
                    //var exists = searchResult >= 0;

                    if (exists)
                        return;

                    //Mark as checked, regardless of outcome
                    visited[visitedPos.X, visitedPos.Y] = true;
                    
                    //Check if within color range
                    var distFromStartColor = target.GetColor(p).Distance(startColor);

                    if (distFromStartColor > threshold)
                        return;

                    //See if distance is further
                    var dist = p.Distance(origin);

                    //Don't bother if distance is much worse than the best
                    if (dist < bestDistance * 0.975)
                        return;

                    //Save best distance
                    if (dist > bestDistance)
                    { 
                        bestDistance = dist;
                        bestPoint = p;
                    }
                    queue.Enqueue(p);
                    
                    //visited.Insert(~searchResult, searchTarget);
                    //visited.ToString();
                });

                //visited.Sort();
            }

            return bestPoint;
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

                    //Compute important angles
                    result.LTA = (Math.Atan2(result.LS.Y - result.LFT.Y, result.LS.X - result.LFT.X) * 180.0 / Math.PI).ToPriorAngle();
                    result.RTA = (Math.Atan2(result.RS.Y - result.RFT.Y, result.RS.X - result.RFT.X) * 180.0 / Math.PI).ToPriorAngle();

                    result.LSA = (Math.Atan2(result.LS.Y - result.LFB.Y, result.LS.X - result.LFB.X) * 180.0 / Math.PI).ToPriorAngle();
                    result.RSA = (Math.Atan2(result.RS.Y - result.RFB.Y, result.RS.X - result.RFB.X) * 180.0 / Math.PI).ToPriorAngle();


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

        public static string ExpectedPath = @"\\psf\Home\Downloads\BeeVids\19Feb16-Start Hept Tests\B4-Feb19-2M-Heptanol.mov_HandAnnotated_20160326 1605.csv";
        public static Dictionary<int, int[]> Expected = new Dictionary<int, int[]>();
        public static void ReadExpected()
        {
            Expected = File
                .ReadAllLines(ExpectedPath)
                .Skip(1)
                .Select(line =>
                {
                    var column = line
                        .Split(',')
                        .Select(col => { int parsed; int.TryParse(col, out parsed); return parsed; })
                        .ToArray();

                    return column;
                })
                .ToDictionary(line => line[0]);
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

            var bestSolution = Generation.First().Key.ToFrameSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);

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
            motionModel.ColorPixels(TargetPoints.Select(p => ToFrameSpace(p)).ToList(), Color.Blue);

            
            //DebugFrame.DrawFrame(fitnessMap, HeadDims.X, 0, 1, 0);
            DebugFrame.DrawFrame(motionModel, HeadDims.X*2, 0, 1, 0);
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
            return p.ToPriorSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
        }

        public System.Drawing.Point ToFrameSpace(Point p)
        {
            return p.ToFrameSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
        }
        public Point ToPriorSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToPriorSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
        }

        public System.Drawing.Point ToFrameSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToFrameSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.ScapeDistance);
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