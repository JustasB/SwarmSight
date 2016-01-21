using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cudafy;
using SwarmVision.Filters;
using SwarmVision.Hardware;
using SwarmVision.HeadPartsTracking.Models;
using Point = System.Windows.Point;

namespace SwarmVision.HeadPartsTracking.Algorithms
{
    public class AntenaPoints : IDisposable
    {
        public Point P1;
        public Point P2;
        public Point P3;

        public Point P4;
        public Point P5;
        public Point P6;


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
            P1.Multiply(scalar, scalarY);
            P2.Multiply(scalar, scalarY);
            P3.Multiply(scalar, scalarY);
            P4.Multiply(scalar, scalarY);
            P5.Multiply(scalar, scalarY);
            P6.Multiply(scalar, scalarY);

            return this;
        }

        public AntenaPoints Offset(double x, double y)
        {
            P1.Offset(x, y);
            P2.Offset(x, y);
            P3.Offset(x, y);
            P4.Offset(x, y);
            P5.Offset(x, y);
            P6.Offset(x, y);

            return this;
        }

        public AntenaPoints InvertY()
        {
            P1.InvertY();
            P2.InvertY();
            P3.InvertY();
            P4.InvertY();
            P5.InvertY();
            P6.InvertY();

            return this;
        }

        public AntenaPoints Clone()
        {
            var result = new AntenaPoints();

            result.P1 = P1.Clone();
            result.P2 = P2.Clone();
            result.P3 = P3.Clone();
            result.P4 = P4.Clone();
            result.P5 = P5.Clone();
            result.P6 = P6.Clone();

            return result;
        }

        public double L1 { get { return Distance(P1, P2); } }
        public double L2 { get { return Distance(P2, P3); } }
        public double L3 { get { return Distance(P4, P5); } }
        public double L4 { get { return Distance(P5, P6); } }

        public double Angle1 { get { return SegmentAngle(P1, P2); } }
        public double Angle2 { get { return (Angle1+360)%360 - (SegmentAngle(P2, P3)+360)%360; } }
        public double Angle3 { get { return SegmentAngle(P1, P3); } }

        public double Angle4 { get { return SegmentAngle(P4, P5); } }
        public double Angle5 { get { return Angle4 - SegmentAngle(P4, P6); } }
        public double Angle6 { get { return SegmentAngle(P4, P6); } }

        public AntenaPoints Rotate(double angle)
        {
            P1.Rotate(angle);
            P2.Rotate(angle);
            P3.Rotate(angle);
            P4.Rotate(angle);
            P5.Rotate(angle);
            P6.Rotate(angle);

            return this;
        }

        public AntenaPoints ToFrameSpace(double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double headLength)
        {
            P1 = P1.ToFrameSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P2 = P2.ToFrameSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P3 = P3.ToFrameSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P4 = P4.ToFrameSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P5 = P5.ToFrameSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P6 = P6.ToFrameSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);

            return this;
        }

        public AntenaPoints ToPriorSpace(double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double headLength)
        {
            P1 = P1.ToPriorSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P1 = P2.ToPriorSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P1 = P3.ToPriorSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P1 = P4.ToPriorSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P1 = P5.ToPriorSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);
            P1 = P6.ToPriorSpace(headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength);

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

        public static Point ToPriorSpace(this Point target, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double headLength)
        {
            target.Offset(-HeadView.Width / 2.0 + offsetX, -HeadView.Height / 2.0 + offsetY);
            target = target.Multiply(100.0 / (scaleX * headLength), 100.0 / (scaleY * headLength));
            target = target.Rotate(-(headAngle - priorAngle));

            return target;

        }

        public static Point ToFrameSpace(this Point target, double headAngle, double scaleX, double scaleY, double offsetX, double offsetY, double priorAngle, double headLength)
        {
            target = target.Rotate(headAngle - priorAngle);
            target = target.Multiply(scaleX * headLength / 100.0, scaleY * headLength / 100.0);
            target.Offset(HeadView.Width / 2.0 - offsetX, HeadView.Height / 2.0 - offsetY);

            return target;
        }

        public static Point Multiply(this Point target, double scalar, double? scalarY = null)
        {
            target.X *= scalar;
            target.Y *= scalarY ?? scalar;

            return target;
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

            //Parallel.For(0, points.Count, p =>
            for (int p = 0; p < points.Count; p++)
            {
                var x = points[p].X;
                var y = points[p].Y;

                var A = x - x1;
                var B = y - y1;

                var param = lenSq > 0 ? (A * C + B * D) / lenSq : -1;

                var xx = param >= 0 && param <= 1 ? x1 + param * C : (param < 0 ? x1 : x2);
                var yy = param >= 0 && param <= 1 ? y1 + param * D : (param < 0 ? y1 : y2);

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
        RightOrigin,
        RightJoint,
        RightTip,

        LeftOrigin,
        LeftJoint,
        LeftTip,
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

        public AntenaPoints PreviousSolution = null;
        public List<Point> TargetPoints;
        public float[,] TargetPointsGPU;
        private static List<AntenaPoints> Priors = null;
        public static Dictionary<PointLabels, List<Point>> ConvexHulls = null;

        public double OffsetY = 0;
        public double OffsetX = 0;
        public double PriorAngle = 180;
        public static string PriorPath =       @"Y:\downloads\BeeVids\priorPoints.csv";
        public static string ConvexHullsPath = @"Y:\downloads\BeeVids\convexHulls.csv";

        public override void PreProcessTarget()
        {
            if (Priors == null)
            {
                ReadPriors();
                ReadHulls();
            }


            SortDescending = false;
            ValidateRandom = false;
            NumberOfGenerations = 10;
            GenerationSize = 250;
            MutationProbability = 0.1;


            var motion = Target
                .ShapeData
                .ChangeExtentPoints(Target.MotionData, 30);

            var gapFilled = motion
                .OrderBy(pt => pt.X * 1000000 + pt.Y)
                .ToList()
                .FillGaps();

            var andDarkNow = gapFilled
                //.AsParallel()
                .Where(p => Target.ShapeData.GetColor(p).Distance(Color.Black) <= 90);

            var priorSpaced = andDarkNow
                .Select(p => new Point(p.X, p.Y).ToPriorSpace(HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.HeadLength))
                ;

            var withinPolygons = priorSpaced.Where(p =>
                    InConvexHulls(p) &&
                    !p.IsInPolygon(ConvexHulls[PointLabels.Head]) &&
                    !p.IsInPolygon(ConvexHulls[PointLabels.Mandibles])
                    )
                .ToList();

            if (withinPolygons.Count > 50)
                TargetPoints = withinPolygons.Where(p => Random.NextDouble() < 50.0/withinPolygons.Count).ToList();
            else
                TargetPoints = withinPolygons;

            Debug.WriteLine("Target Points: " + TargetPoints.Count);

            MinX = (int)ConvexHulls[PointLabels.RightTip].Min(p => p.X);
            MaxX = (int)ConvexHulls[PointLabels.LeftTip].Max(p => p.X);

            MinY = (int)ConvexHulls[PointLabels.RightTip].Min(p => p.Y);
            MaxY = (int)ConvexHulls[PointLabels.RightTip].Max(p => p.Y);

            
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

        public static void ReadPriors()
        {
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
                        P4 = new Point(4, 0),
                        P5 = new Point(column[7], column[8]),
                        P6 = new Point(column[5], column[6]),

                        P1 = new Point(-4, 0),
                        P2 = new Point(column[3], column[4]),
                        P3 = new Point(column[1], column[2]),
                    }
                    ;

                    return result;
                })
                .ToList();
        }

        public static void ReadHulls()
        {
            ConvexHulls = new Dictionary<PointLabels, List<Point>>
            {
                { PointLabels.RightOrigin, new List<Point>() },
                { PointLabels.RightJoint, new List<Point>() },
                { PointLabels.RightTip, new List<Point>() },

                { PointLabels.LeftOrigin, new List<Point>() },
                { PointLabels.LeftJoint, new List<Point>() },
                { PointLabels.LeftTip, new List<Point>() },

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

                    if (column[0] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.LeftTip].Add(new Point(column[0], column[1]));
                    }

                    if (column[2] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.RightTip].Add(new Point(column[2], column[3]));
                    }

                    if (column[4] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.RightJoint].Add(new Point(column[4], column[5]));
                    }

                    if (column[6] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.LeftJoint].Add(new Point(column[6], column[7]));
                    }

                    if (column[8] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.Mandibles].Add(new Point(column[8], column[9]));
                    }

                    if (column[10] != double.MinValue)
                    {
                        ConvexHulls[PointLabels.Head].Add(new Point(column[10], column[11]));
                    }
                });
        }

        //convert frame points to prior space scale & angle and orientation
        //Read the priors
        //Read the chulls
        //Pick from priors
        //Priors -> keep as is
        //chulls -> keep as is
        //Frame points -> project each to prior space
        //Once found, before selection, project back to frame space



        protected override AntenaPoints CreateChild(AntenaPoints parent1, AntenaPoints parent2)
        {
            var result = new AntenaPoints();

            result.P1.X = Cross(parent1.P1.X, parent2.P1.X, MinX, MaxX);
            result.P2.X = Cross(parent1.P2.X, parent2.P2.X, MinX, MaxX);
            result.P3.X = Cross(parent1.P3.X, parent2.P3.X, MinX, MaxX);
            result.P4.X = Cross(parent1.P4.X, parent2.P4.X, MinX, MaxX);
            result.P5.X = Cross(parent1.P5.X, parent2.P5.X, MinX, MaxX);
            result.P6.X = Cross(parent1.P6.X, parent2.P6.X, MinX, MaxX);

            result.P1.Y = Cross(parent1.P1.Y, parent2.P1.Y, MinY, MaxY);
            result.P2.Y = Cross(parent1.P2.Y, parent2.P2.Y, MinY, MaxY);
            result.P3.Y = Cross(parent1.P3.Y, parent2.P3.Y, MinY, MaxY);
            result.P4.Y = Cross(parent1.P4.Y, parent2.P4.Y, MinY, MaxY);
            result.P5.Y = Cross(parent1.P5.Y, parent2.P5.Y, MinY, MaxY);
            result.P6.Y = Cross(parent1.P6.Y, parent2.P6.Y, MinY, MaxY);

            return result;
        }

        protected override bool ValidChild(AntenaPoints child)
        {
            var valid =
                       child.P2.IsInPolygon(ConvexHulls[PointLabels.RightJoint])
                    && child.P3.IsInPolygon(ConvexHulls[PointLabels.RightTip])
                    && child.P5.IsInPolygon(ConvexHulls[PointLabels.LeftJoint])
                    && child.P6.IsInPolygon(ConvexHulls[PointLabels.LeftTip])

                    && Math.Abs(child.P1.X) <= 10
                    && Math.Abs(child.P4.X) <= 10
                    && Math.Abs(child.P1.Y) <= 10
                    && Math.Abs(child.P4.Y) <= 10
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

            var bestSolution = Generation.First().Key.ToFrameSpace(HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.HeadLength);

            if (PreviousSolution != null)
            {
                var leftPoints = TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.LeftTip]));
                var rightPoints = TargetPoints.Count(point => point.IsInPolygon(ConvexHulls[PointLabels.RightTip]));

                if (rightPoints <= 5)
                {
                    bestSolution.P1 = PreviousSolution.P1;
                    bestSolution.P2 = PreviousSolution.P2;
                    bestSolution.P3 = PreviousSolution.P3;
                }

                if (leftPoints <= 5)
                {
                    bestSolution.P4 = PreviousSolution.P4;
                    bestSolution.P5 = PreviousSolution.P5;
                    bestSolution.P6 = PreviousSolution.P6;
                }


            }

            PreviousSolution = bestSolution;

            return bestSolution;
        }

        public override AntenaPoints Mutated(AntenaPoints individual)
        {
            var result = new AntenaPoints();

            result.P1.X = MutateValue(individual.P1.X, MinX, MaxX);
            result.P2.X = MutateValue(individual.P2.X, MinX, MaxX);
            result.P3.X = MutateValue(individual.P3.X, MinX, MaxX);
            result.P4.X = MutateValue(individual.P4.X, MinX, MaxX);
            result.P5.X = MutateValue(individual.P5.X, MinX, MaxX);
            result.P6.X = MutateValue(individual.P6.X, MinX, MaxX);


            result.P1.Y = MutateValue(individual.P1.Y, MinY, MaxY);
            result.P2.Y = MutateValue(individual.P2.Y, MinY, MaxY);
            result.P3.Y = MutateValue(individual.P3.Y, MinY, MaxY);
            result.P4.Y = MutateValue(individual.P4.Y, MinY, MaxY);
            result.P5.Y = MutateValue(individual.P5.Y, MinY, MaxY);
            result.P6.Y = MutateValue(individual.P6.Y, MinY, MaxY);

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
                point.IsInPolygon(ConvexHulls[PointLabels.RightTip]) ||
                point.IsInPolygon(ConvexHulls[PointLabels.LeftTip]) ||
                point.IsInPolygon(ConvexHulls[PointLabels.RightJoint]) ||
                point.IsInPolygon(ConvexHulls[PointLabels.LeftJoint])
                ;

            return result;
        }

        public Point ToPriorSpace(Point p)
        {
            return p.ToPriorSpace(HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.HeadLength);
        }

        public Point ToFrameSpace(Point p)
        {
            return p.ToFrameSpace(HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, HeadView.HeadLength);
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

                var D1 = TargetPoints.DistToSegmentSquared(individual.P1, individual.P2);
                var D2 = TargetPoints.DistToSegmentSquared(individual.P2, individual.P3);

                var D3 = TargetPoints.DistToSegmentSquared(individual.P4, individual.P5);
                var D4 = TargetPoints.DistToSegmentSquared(individual.P5, individual.P6);

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
                individuals[i, 0] = (float)uncomputed[i].P1.X;
                individuals[i, 1] = (float)uncomputed[i].P2.X;
                individuals[i, 2] = (float)uncomputed[i].P3.X;
                individuals[i, 3] = (float)uncomputed[i].P4.X;
                individuals[i, 4] = (float)uncomputed[i].P5.X;
                individuals[i, 5] = (float)uncomputed[i].P6.X;

                individuals[i, 6] =  (float)uncomputed[i].P1.Y;
                individuals[i, 7] =  (float)uncomputed[i].P2.Y;
                individuals[i, 8] =  (float)uncomputed[i].P3.Y;
                individuals[i, 9] =  (float)uncomputed[i].P4.Y;
                individuals[i, 10] = (float)uncomputed[i].P5.Y;
                individuals[i, 11] = (float)uncomputed[i].P6.Y;
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