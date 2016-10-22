using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
//using Accord.Math;
using Cudafy;
using SwarmSight.Filters;
using SwarmSight.Hardware;
using SwarmSight.HeadPartsTracking.Models;
using Point = System.Windows.Point;
using System.Data;
//using Accord.MachineLearning.VectorMachines;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Drawing.Imaging;
using DPoint = System.Drawing.Point;
using WPoint = System.Windows.Point;
using static System.Math;
using static SwarmSight.Filters.PointExtensions;
using MoreLinq;
using Settings;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SwarmSight.HeadPartsTracking.Algorithms
{
    public class AntenaPoints : IDisposable
    {
        public class RecordingConditionsCollection
        {
            /// <summary>
            /// The offset of the head receptive field 
            /// </summary>
            public DPoint HeadOffset;

            /// <summary>
            /// The dimensions of the receptive field
            /// </summary>
            public DPoint Dimensions;

            /// <summary>
            /// The rotation angle of the head with respect to screen. 0 degrees points up.
            /// </summary>
            public double RotationAngle;

            /// <summary>
            /// The x,y scales of the model within the head receptive field
            /// </summary>
            public WPoint Scale;
        }

        public RecordingConditionsCollection RecordingConditions = new RecordingConditionsCollection();

        public double[] RightSectorData;
        public double[] LeftSectorData;

        public int RightSector;
        public int LeftSector;

        /// <summary>
        /// The value of the light buzzer
        /// </summary>
        public double Buzzer;

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
            RS = RS.ToHeadSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            RFB = RFB.ToHeadSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            RFT = RFT.ToHeadSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            LS = LS.ToHeadSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            LFB = LFB.ToHeadSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();
            LFT = LFT.ToHeadSpace(headDims, headAngle, scaleX, scaleY, offsetX, offsetY, priorAngle, headLength).ToWindowsPoint();

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

    public enum HeadOrientation
    {
        Up,
        Down,
        Left,
        Right
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
        public double ScapeDistanceAtScale1;

        public int PointsToKeep = 100;
        public int MinPointsForRegression = 20;
        public AntenaPoints PreviousSolution = null;
        public List<Point> TargetPoints;
        public List<Point> TargetPointsPrev = null;
        public float[,] TargetPointsGPU;
        public double ConvexHullsFrameAngle;
        private static List<AntenaPoints> Priors = null;
        public static Dictionary<PointLabels, List<Point>> ConvexHullsPrior = null;
        public static Dictionary<PointLabels, List<DPoint>> ConvexHullsFrame = null;
        

        public double OffsetY = 0;
        public double OffsetX = 0;
        public double PriorAngle = 0;
        public static string PriorPath =       @"Y:\downloads\BeeVids\priorPoints.csv";
        public static string ConvexHullsPath = @"Y:\downloads\BeeVids\convexHulls.csv";

        public int FrameIndex = 0;
        public Frame DebugFrame;

        private Background background = null;
        private Frame model = null;
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

            var antennaColor = AppSettings.Default.AntennaColor;
            var antR = antennaColor.R;
            var antG = antennaColor.G;
            var antB = antennaColor.B;

            var threshold = 1;//AppSettings.Default.MotionThreshold;

            var result = new List<Point3D>(width*height/16);

            Parallel.For(0, height, new ParallelOptions()
            {
                //MaxDegreeOfParallelism = 1
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
                    var curDist =   (Abs(currentPx[offsetR] - antR) + Abs(currentPx[offsetG] - antG) + Abs(currentPx[offsetB] - antB)) / 3.0;
                    var prev1Dist = (Abs(prev1Px[offsetR] - antR) + Abs(prev1Px[offsetG] - antG) + Abs(prev1Px[offsetB] - antB)) / 3.0;
                    var prev2Dist = (Abs(prev2Px[offsetR] - antR) + Abs(prev2Px[offsetG] - antG) + Abs(prev2Px[offsetB] - antB)) / 3.0;

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

            //Return all
            return result
                .Select(p => new DPoint((int)p.X, (int)p.Y))
                .ToList();

            //Keep only the top active
            //var mostActive = result
            //    .OrderByDescending(p => p.Z)
            //    .Select(p => new System.Drawing.Point((int)p.X, (int)p.Y))
            //    .ToList();

            //model.ColorPixels(mostActive.Skip(result.Count * 3 / 4).ToList(), Color.Black);

            //return
            //    mostActive
            //    .Take(result.Count * 3 / 4)
            //    .ToList();
        }
        public bool EvalExpected = true;
        public override void PreProcessTarget()
        {
            if (Priors == null) ReadPriors();
            if (AppSettings.Default.CompareToManualData && (Expected == null || Expected.Count == 0)) ReadExpected();
            if (ConvexHullsPrior == null) ReadHulls();

            if (AppSettings.Default.CompareToManualData && !Expected.ContainsKey(Target.Prev1.FrameIndex))
                return;
            
            if (model == null || HeadDims.X != model.Width || HeadDims.Y != model.Height || ConvexHullsFrameAngle != HeadAngle)
            {
                model = new Frame(HeadDims.X, HeadDims.Y, Target.Current.PixelFormat, false);

                background = new MedianBackground(5);
                background.Append(Target.Prev2.SubClipped(HeadOffset.X, HeadOffset.Y, HeadDims.X, HeadDims.Y));

                ReadHullsFrame();
            }

            var modelPts = UpdateMotionModel();

            var headClip = Target.Prev1.SubClipped(HeadOffset.X, HeadOffset.Y, HeadDims.X, HeadDims.Y);
            {
                AntennaPoints.Clear();



                var leftPoints = GetTip
                (
                    headClip, model, modelPts, new Point(-0.5, 0), PointLabels.LeftFlagellumTip,
                    PointLabels.LeftFlagellumBase, LTpoints,
                    LBpoints
                );

                var rightPoints = GetTip
                (
                    headClip, model, modelPts, new Point(0.5, 0), PointLabels.RightFlagellumTip,
                    PointLabels.RightFlagellumBase, RTpoints,
                    RBpoints
                );

                
                headClip.FrameIndex = Target.Prev1.FrameIndex;

                //Update background
                background.Append(headClip);

                //Set the return value
                PreviousSolution = new AntenaPoints();

                if (leftPoints.Count > 0)
                {
                    PreviousSolution.LFB = leftPoints[PointLabels.LeftFlagellumBase].ToWindowsPoint();
                    PreviousSolution.LFT = leftPoints[PointLabels.LeftFlagellumTip].ToWindowsPoint();
                }
                if (rightPoints.Count > 0)
                {
                    PreviousSolution.RFB = rightPoints[PointLabels.RightFlagellumBase].ToWindowsPoint();
                    PreviousSolution.RFT = rightPoints[PointLabels.RightFlagellumTip].ToWindowsPoint();
                }

                PreviousSolution.RightSectorData = PrevSectors[PointLabels.RightSectorData];
                PreviousSolution.LeftSectorData = PrevSectors[PointLabels.LeftSectorData];

                //Precompute the sector with highest activation
                PreviousSolution.RightSector = PreviousSolution.RightSectorData.ToList().IndexOf(PreviousSolution.RightSectorData.Max())+1;
                PreviousSolution.LeftSector = PreviousSolution.LeftSectorData.ToList().IndexOf(PreviousSolution.LeftSectorData.Max())+1;

                PreviousSolution.RecordingConditions.HeadOffset = HeadOffset;
                PreviousSolution.RecordingConditions.Dimensions = HeadDims;
                PreviousSolution.RecordingConditions.Scale = new WPoint(ScaleX,ScaleY);
                PreviousSolution.RecordingConditions.RotationAngle = HeadAngle;


                if (AppSettings.Default.CompareToManualData && Expected.ContainsKey(Target.Prev1.FrameIndex))
                {
                    var currExp = Expected[Target.Prev1.FrameIndex];
                    var outString = Target.Prev1.FrameIndex.ToString() + ",";
                    var saveFrame = false;

                    var maxError = 0.0;
                    headClip = headClip.Clone(); //To protect the bacground calcs

                    headClip.ColorPixels(AntennaPoints, Color.Orange);

                    //Refine tip location
                    if (leftPoints.Count > 0 && currExp[13] != 0)
                    {
                        var expLFT = new System.Drawing.Point(currExp[13], currExp[14]);
                        var actLFT = leftPoints[PointLabels.LeftFlagellumTip];

                        headClip.MarkPoint(actLFT, Color.Yellow);
                        actLFT.Offset(HeadOffset.X, HeadOffset.Y);
                        var LFTdist = actLFT.Distance(expLFT);
                        outString += string.Join(",", new[] { expLFT.X, expLFT.Y, actLFT.X, actLFT.Y });

                        expLFT.Offset(-HeadOffset.X, -HeadOffset.Y);
                        headClip.MarkPoint(expLFT, Color.Red);

                        if (maxError < LFTdist)
                            maxError = LFTdist;

                        if (LFTdist > 10)
                            saveFrame = true;
                    }
                    else
                    {
                        outString += ",,,";
                    }

                    if (rightPoints.Count > 0 && currExp[19] != 0)
                    {
                        var expRFT = new System.Drawing.Point(currExp[19], currExp[20]);
                        var actRFT = rightPoints[PointLabels.RightFlagellumTip];

                        headClip.MarkPoint(actRFT, Color.Yellow);
                        actRFT.Offset(HeadOffset.X, HeadOffset.Y);
                        var RFTdist = actRFT.Distance(expRFT);
                        outString += "," + string.Join(",", new[] { expRFT.X, expRFT.Y, actRFT.X, actRFT.Y });

                        expRFT.Offset(-HeadOffset.X, -HeadOffset.Y);
                        headClip.MarkPoint(expRFT, Color.Red);
                        
                        if (maxError < RFTdist)
                            maxError = RFTdist;

                        if (RFTdist > 10)
                            saveFrame = true;
                    }
                    else
                    {
                        outString += ",,,";
                    }

                    Debug.WriteLine(outString);

                    if (DebugFrame == null)
                        DebugFrame = headClip.Clone();

                    DebugFrame.DrawFrame(headClip);
                    //DebugFrame.DrawFrame(model, headClip.Width, 0, 1, 0);
                    //DebugFrame.DrawFrame(background.Model, headClip.Width*2, 0, 1, 0);

                    //if (saveFrame)
                    //{
                    //    DebugFrame.Bitmap.Save(@"c:\temp\frames\" + maxError.Rounded().ToString("D4") + " - " + Target.Prev1.FrameIndex + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    //}

                }
            }
        }


        public void DrawTip(Frame headImage, System.Drawing.Point tipPoint, System.Drawing.Point basePoint)
        {
            using (var g = Graphics.FromImage(headImage.Bitmap))
            {
                var penThick = new Pen(Color.Yellow) { Width = 3 };
                var penThin = new Pen(Color.Blue) { Width = 1 };

                var penRed = new Pen(Color.Orange);
                var length = ToHeadSpace(new Point(1, 0)).X;

                var ellipseWidth = (0.1 * length).Rounded();
                g.DrawEllipse(penThick, (float)(tipPoint.X - ellipseWidth / 2.0), (float)(tipPoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);
                g.DrawEllipse(penThin, (float)(tipPoint.X - ellipseWidth / 2.0), (float)(tipPoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);

                //Joint
                //g.DrawEllipse(penThick, (float)(expectedBasePoint.X - ellipseWidth / 2.0), (float)(expectedBasePoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);
                //g.DrawEllipse(penThin, (float)(expectedBasePoint.X - ellipseWidth / 2.0), (float)(expectedBasePoint.Y - ellipseWidth / 2.0), ellipseWidth, ellipseWidth);

                if (TopViewLeft != null)
                {
                    var tvFrame = ToHeadSpace(TopViewLeft.Value);
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

        public Dictionary<PointLabels, DPoint> PrevTips = new Dictionary<PointLabels, DPoint>();
        public Dictionary<PointLabels, double[]> PrevSectors = new Dictionary<PointLabels, double[]>();

        public bool?[][] hullCache = null;
        public Frame antPtsDebug;

        private unsafe List<Tuple<DPoint, double>> AntPts(Frame headClip, Frame motion, PointLabels tipLabel, PointLabels baseLabel)
        {
            var result = new List<Tuple<DPoint, double>>(100);

            var antCol = AppSettings.Default.AntennaColor;
            var antR = antCol.R;
            var antG = antCol.G;
            var antB = antCol.B;

            var center = ToHeadSpace(new Point(0, 0));
            var ctrX = center.X;
            var ctrY = center.Y;

            var headRadiusSquared = ToHeadSpace(new Point(3, 0)).X - ToHeadSpace(new Point(0, 0)).X;
            headRadiusSquared *= headRadiusSquared;

            //Make a histogram of point distances
            var bgHistBinPts = Enumerable.Range(0, 256).Select(i => new List<DPoint>()).ToArray();
            var motionHistBinPts = Enumerable.Range(0, 256).Select(i => new List<DPoint>()).ToArray();

            //Keep track of how many points in histogram
            var bgHistogramTotal = 0;
            var motionHistogramTotal = 0;

            var ptsWithinHull = 0;

            var width = headClip.Width;
            var height = headClip.Height;
            var stride = headClip.Stride;

            var headFirstPx = headClip.FirstPixelPointer;
            var motionFirstPx = motion.FirstPixelPointer;
            var bgFirstPx = background.Model.FirstPixelPointer;

            var tipHull = ConvexHullsFrame[tipLabel];
            var mandiblesHull = ConvexHullsFrame[PointLabels.Mandibles];
            var jointHull = ConvexHullsFrame[baseLabel];

            //set up [x][y] cache
            if (hullCache == null)
            {
                hullCache = new bool?[width][];
                for (int x = 0; x < width; x++)
                {
                    hullCache[x] = new bool?[height];
                }
            }

            Parallel.For(0, height, new ParallelOptions
            {
                //MaxDegreeOfParallelism = 1
            },
            y =>
            {
                var rowStartOffset = y * stride;
                
                var rowBgHistBinPts = GetNewColorBin();
                var rowMotionHistBinPts = GetNewColorBin();
                var rowBgHistTot = 0;
                var rowMotionHistTot = 0;

                for (var x = 0; x < width; x++)
                {
                    //Check if point is within hull
                    var dx = (x - ctrX);
                    var dy = (y - ctrY);

                    if (dx * dx + dy * dy <= headRadiusSquared)
                        continue;

                    var hullValue = hullCache[x][y];
                    if (hullValue == null)
                    {
                        hullValue =
                            !IsInConvexPolygon(x, y, tipHull) ||
                            IsInConvexPolygon(x, y, mandiblesHull) ||
                            IsInConvexPolygon(x, y, jointHull);

                        hullCache[x][y] = hullValue;
                    }

                    if (hullValue.Value)
                        continue;

                    //Passed location tests, now test for color distances
                    var pxOffset = rowStartOffset + x * 3;
                    
                    //Count the number of pts in a hull
                    ptsWithinHull++;

                    //Head px
                    var headIntPix = *(int*)(headFirstPx+pxOffset) & 0xFFFFFFFF;
                    var headB = (headIntPix) & 255;
                    var headG = (headIntPix >> 8) & 255;
                    var headR = (headIntPix >> 16) & 255;

                    //Background px
                    var bgIntPix = *(int*)(bgFirstPx + pxOffset) & 0xFFFFFFFF;
                    var bgB = (bgIntPix) & 255;
                    var bgG = (bgIntPix >> 8) & 255;
                    var bgR = (bgIntPix >> 16) & 255;

                    //Motion px
                    var motionG = motionFirstPx[pxOffset+1];

                    var headDistNow = (Abs(antR - headR) + Abs(antG - headG) + Abs(antB - headB)); //Color distance to antenna
                    var headDistWas =     (Abs(bgR - antR) + Abs(bgG - antG) + Abs(bgB - antB)); //Difference from background
                    var headDistDiff = (headDistWas - headDistNow)/3.0;

                    //Keep track of positive changes (e.g. chages towards antenna color)
                    if (headDistDiff > 2)
                    {
                        var targetBin = rowBgHistBinPts[(int)headDistDiff];

                        targetBin.Add(new DPoint(x,y));

                        bgHistogramTotal++;
                        rowBgHistTot++;
                    }
                    
                    if (motionG > 2)
                    {
                        var targetBin = rowMotionHistBinPts[motionG];

                        targetBin.Add(new DPoint(x,y));

                        motionHistogramTotal++;
                        rowMotionHistTot++;
                    }
                }

                if (rowBgHistTot > 0)
                {
                    for (var bin = 0; bin < 256; bin++)
                    {
                        var binPts = rowBgHistBinPts[bin];

                        if(binPts.Count > 0)
                            lock (bgHistBinPts)
                            {
                                bgHistBinPts[bin].AddRange(binPts);
                            }
                    }
                }

                if (rowMotionHistTot > 0)
                {
                    for (var bin = 0; bin < 256; bin++)
                    {
                        var binPts = rowMotionHistBinPts[bin];

                        if(binPts.Count > 0)
                            lock (motionHistBinPts)
                            {
                                motionHistBinPts[bin].AddRange(binPts);
                            }
                    }
                }

            });

            //Slow motion component
            if (bgHistogramTotal > 0)
            {
                var histSum = 0.0;
                var histBin = 255;

                //var debug = headClip.Clone();
                var stopCount = (int)(ptsWithinHull * 0.04);

                //Collect the points from the histogram tail until reached X% of possible hull points
                do
                {
                    var binPoints = bgHistBinPts[histBin];
                    var binCount = binPoints.Count;

                    if(binCount > 0)
                    { 
                        histSum += binCount;
                        result.AddRange(binPoints.Select(p => new Tuple<DPoint,double>(p, histBin)));
                    }

                    //DEBUG
                    if (binCount > 0)
                    {
                        var col = Math.Min(histBin * histBin, 255); //Square the distance for emphasis
                        //debug.ColorPixels(binPoints, Color.FromArgb(col, col, col));
                    }

                    histBin--;
                }
                while (histBin > 0 && histSum < stopCount);
            }

            //Fast motion component
            if (motionHistogramTotal > 0)
            {
                var motionResult = result.Take(0).ToList();
                var histSum = 0.0;
                var histBin = 255;

                //var debug = headClip.Clone();
                var stopCount = (int)(ptsWithinHull * 0.04);

                //Collect the points from the histogram tail until reached X% of possible hull points
                do
                {
                    var binPoints = motionHistBinPts[histBin];
                    var binCount = binPoints.Count;

                    if (binCount > 0)
                    {
                        histSum += binCount;
                        motionResult.AddRange(binPoints.Select(p => new Tuple<DPoint, double>(p, histBin)));
                    }

                    ////DEBUG
                    if (binCount > 0)
                    {
                        var col = Math.Min(histBin * histBin, 255); //Square the distance for emphasis
                        //debug.ColorPixels(binPoints, Color.FromArgb(col, col, col));
                    }

                    histBin--;
                }
                while (histBin > 0 && histSum < stopCount);

                //result.AddRange(motionResult);
            }
            

            return result;
        }

        public List<DPoint>[] GetNewColorBin()
        {
            var result = new List<DPoint>[256];

            for (int i = 0; i < 256; i++)
            {
                result[i] = new List<DPoint>();
            }

            return result;
        }

        public List<DPoint> AntennaPoints = new List<DPoint>();
        private Frame t;

        private Dictionary<PointLabels,DPoint> GetTip(Frame headClip, Frame motionClip, List<System.Drawing.Point> modelPts, Point origin, PointLabels tipLabel, PointLabels baseLabel, LinkedList<Point> tipBuffer, LinkedList<Point> baseBuffer)
        {
            var result = new Dictionary<PointLabels, DPoint>();
            
            //These can be combined into one loop to avoid repeated X,Y validation checks
            //And replace deleages with for-loops
            var rawAnt = AntPts(headClip, motionClip, tipLabel, baseLabel);
            var ctr = ToHeadSpace(new Point(tipLabel == PointLabels.LeftFlagellumTip ? -0.5 : 0.5, 0));
            //var rawAntPts = rawAnt.Select(p => p.Item1).ToList();
            //var moAntPts = moAnt.Select(p => p.Item1).ToList();

            if(t == null)
                t = headClip.Clone();

            t.DrawFrame(headClip);

            //t.ColorPixels(rawAntPts, Color.White);
            //t.ColorPixels(moAntPts, Color.White);

            //Median the point list
            //t.ColorPixels(rawAnt.Select(p => p.Item1).ToList(), Color.White);
            if(rawAnt.Count > 100)
                rawAnt = rawAnt.MedianFilter(AppSettings.Default.MedianFilterRadius);
            //t = headClip.Clone();
            //t.Clone().ColorPixels(rawAnt.Select(p => p.Item1).ToList(), Color.White);

            if (rawAnt.Count > 0)
            {
                //Take the top one
                //var maxW = rawAnt.MaxBy(p => p.Item2);

                //Pool top ones
                var topPts = rawAnt.OrderByDescending(p => p.Item2).Take(10).ToList();
                //var maxPooled = ;
                var topCentroid = new DPoint(
                            topPts.Average(pt => pt.Item1.X).Rounded(),
                            topPts.Average(pt => pt.Item1.Y).Rounded()
                        );

                var med = rawAnt.MinBy(p => p.Item1.Distance(topCentroid)).Item1;

                //Square-Weighted centroid
                //var sumW = rawAnt.Sum(p => p.Item2 * p.Item2);
                //var centerX = rawAnt.Sum(p => p.Item1.X * p.Item2 * p.Item2 / sumW);
                //var centerY = rawAnt.Sum(p => p.Item1.Y * p.Item2 * p.Item2 / sumW);
                //var med = new DPoint(centerX.Rounded(), centerY.Rounded());
                //med = rawAnt.MinBy(p => p.Item1.Distance(med)).Item1;

                var pointList = rawAnt.Select(p => p.Item1).ToList();
                
                t.ColorPixels(pointList, Color.White);
                AntennaPoints.AddRange(pointList);

                //Could do with lookup table
                //var tip = CrawlToTip(t, med, ctr, 1, 34);
                var tip = CrawlToTip(t, med, ctr, 1, AppSettings.Default.TipCrawlerRadius.Rounded(), AppSettings.Default.MaxTipCrawlerHop);
                var baseLoc = CrawlToTip(t, med, ctr, 1, AppSettings.Default.TipCrawlerRadius.Rounded(), AppSettings.Default.MaxTipCrawlerHop, crawlingAway:false);

                t.MarkPoint(baseLoc, inner: Color.Black);
                t.MarkPoint(med);
                t.MarkPoint(tip, inner:Color.Orange);

                var sectors = ComputeSectorWeights(rawAnt, headClip.Width, headClip.Height, HeadAngle);
                //t.MarkSectors(sectors, headClip.Width/2, headClip.Height/2, HeadAngle, tipLabel == PointLabels.RightFlagellumTip);

                PrevTips[tipLabel] = result[tipLabel] = tip;
                PrevTips[baseLabel] = result[baseLabel] = baseLoc;

                PrevSectors[tipLabel == PointLabels.RightFlagellumTip ? PointLabels.RightSectorData : PointLabels.LeftSectorData] = sectors;
            }
            else if(PrevTips.ContainsKey(tipLabel) && PrevTips.ContainsKey(baseLabel)) //If nothing at all, use previous solution
            {
                result[tipLabel] = PrevTips[tipLabel];
                result[baseLabel] = PrevTips[baseLabel];
            }

            
            //DEBUG save
            //t.Bitmap.Save(@"c:\temp\frames\lomo\" + tipLabel.ToString() + "-" + Target.Prev1.FrameIndex + ".jpg", ImageFormat.Jpeg);

            return result;
        }

        private double[] ComputeSectorWeights(List<Tuple<DPoint, double>> points, int width, int height, double angle)
        {
            var ctrX = width / 2;
            var ctrY = height / 2;

            var sectors = 5;
            var sectorContribution = sectors / 180.0;
            var histogram = new double[sectors];

            Parallel.ForEach(points, new ParallelOptions
            {
                //MaxDegreeOfParallelism = 1
            }, 
            p =>
            {
                //Move origin to 0,0
                var x = p.Item1.X - ctrX;
                var y = p.Item1.Y - ctrY;

                var tanAngle = Atan2(y, x) * 180.0 / PI + 90;
                tanAngle -= angle;

                tanAngle = tanAngle % 360;

                if (tanAngle > 180)
                    tanAngle -= 360;
                else if (tanAngle < -180)
                    tanAngle += 360;


                var priorAngle = Abs(tanAngle);

                var sector = priorAngle * sectorContribution;

                if (sector < 0)
                    sector = 0;

                else if (sector >= sectors)
                    sector = sectors - 1;

                histogram[(int)sector] += p.Item2;
            });

            return histogram;
        }

        public System.Drawing.Point CrawlToTip(Frame target, System.Drawing.Point start, System.Drawing.Point origin, int threshold = 50, int maxDistance = 30, int maxHopDistance = 2, bool crawlingAway = true)
        {
            var loopCount = 0;
            var startColor = target.GetColor(start);
            var bestDistance = start.Distance(origin);
            var bestPoint = start;
            var visited = new bool[maxDistance * 2+1, maxDistance * 2+1];
            var visitedOffsetX = start.X - maxDistance;
            var visitedOffsetY = start.Y - maxDistance;
            var bestLocationLockpad = new object();

            var queue = new Queue<System.Drawing.Point>();
            queue.Enqueue(start);
            

            while (queue.Count > 0 && loopCount < 500)
            {
                loopCount++;
                var point = queue.Dequeue();

                Parallel.ForEach(getCandidateLocations(point, maxHopDistance), p =>
                {
                    //Check bounds
                    if (p.X < 0 || p.X >= target.Width ||
                        p.Y < 0 || p.Y >= target.Height)
                        return;
                    
                    //Check if within search radius
                    var distFromStart = p.Distance(start);

                    if (distFromStart > maxDistance)
                        return;

                    //Check if already checked
                    var visitedX = p.X - visitedOffsetX;
                    var visitedY = p.Y - visitedOffsetY;
                    var exists = visited[visitedX, visitedY];
                    //var searchTarget = p.X * 100000 + p.Y;
                    //var searchResult = visited.BinarySearch(searchTarget);
                    //var exists = searchResult >= 0;

                    if (exists)
                        return;

                    //Mark as checked, regardless of outcome
                    lock(visited)
                    {
                        visited[visitedX, visitedY] = true;
                    }

                    //Check if within color range
                    var distFromStartColor = target.GetColor(p).Distance(startColor);

                    if (distFromStartColor > threshold)
                        return;

                    //See if distance is further
                    var dist = p.Distance(origin);

                    //Don't bother if distance is much worse than the best (maximizing distance)
                    if (crawlingAway && dist < bestDistance * 0.975)
                        return;

                    //Or opposite if crawling closer (minimizing distance)
                    else if (!crawlingAway && dist > bestDistance * 1.025)
                        return;

                    //Save best distance (away = maximizing, towards = minimizing)
                    if ((crawlingAway && dist > bestDistance) || (!crawlingAway && dist < bestDistance))
                    { 
                        lock(bestLocationLockpad)
                        {
                            bestDistance = dist;
                            bestPoint = p;
                        }
                    }

                    lock(queue)
                    {
                        queue.Enqueue(p);
                    }
                    //visited.Insert(~searchResult, searchTarget);
                    //visited.ToString();
                });

                //visited.Sort();
            }

            return bestPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<DPoint> getCandidateLocations(DPoint point, int maxHopDistance)
        {
            var result = new List<DPoint>(maxHopDistance * maxHopDistance);

            Parallel.For(point.X - maxHopDistance, point.X + maxHopDistance + 1, x =>
            {
                var rowResult = result.Take(0).ToList();

                for (var y = point.Y - maxHopDistance; y <= point.Y + maxHopDistance; y++)
                {
                    if (x == point.X && y == point.Y)
                        continue;

                    rowResult.Add(new DPoint(x, y));
                }

                lock(result)
                {
                    result.AddRange(rowResult);
                }
            });

            return result;
        }

        public bool IsAntenaColored(Frame target, System.Drawing.Point p)
        {
            var pxC = target.GetColor(p);

            return pxC.Distance(Color.Black) <= 90 || Abs(pxC.GetHue() - 333) <= 10;
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

        public void ReadHulls()
        {
            var cols = File
                .ReadLines(ConvexHullsPath)
                .First()
                .Split(',')
                .Select((name, i) => new { name = name.Replace(@"""", ""), i })
                .ToDictionary(c => c.name);

            ConvexHullsPrior = new Dictionary<PointLabels, List<Point>>
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
                        ConvexHullsPrior[PointLabels.LeftFlagellumTip].Add(new Point(column[cols["lftX"].i], column[cols["lftY"].i]));
                    }

                    if (column[cols["rftX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.RightFlagellumTip].Add(new Point(column[cols["rftX"].i], column[cols["rftY"].i]));
                    }

                    if (column[cols["rfbX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.RightFlagellumBase].Add(new Point(column[cols["rfbX"].i], column[cols["rfbY"].i]));
                    }

                    if (column[cols["lfbX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.LeftFlagellumBase].Add(new Point(column[cols["lfbX"].i], column[cols["lfbY"].i]));
                    }

                    if (column[cols["rsX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.RightScape].Add(new Point(column[cols["rsX"].i], column[cols["rsY"].i]));
                    }

                    if (column[cols["lsX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.LeftScape].Add(new Point(column[cols["lsX"].i], column[cols["lsY"].i]));
                    }

                    if (column[cols["mandiblesX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.Mandibles].Add(new Point(column[cols["mandiblesX"].i], column[cols["mandiblesY"].i]));
                    }
                });

            ReadHullsFrame();
        }

        public void ReadHullsFrame()
        {
            //Convert prior spaced hulls to frame space
            ConvexHullsFrameAngle = HeadAngle;
            ConvexHullsFrame = ConvexHullsPrior.Keys.ToDictionary(k => k, v => new List<DPoint>());

            ConvexHullsPrior.Keys.ForEach(k =>
                ConvexHullsFrame[k].AddRange(ConvexHullsPrior[k].Select(pv =>
                    ToHeadSpace(pv)
                ))
            );
        }

        public static Dictionary<int, int[]> Expected = null;
        public static void ReadExpected()
        {
            var path = AppSettings.Default.ManualDataFile;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Expected = new Dictionary<int, int[]>();
                return;
            }
                

            Expected = File
                .ReadAllLines(path)
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
                       child.RFB.IsInPolygon(ConvexHullsPrior[PointLabels.RightFlagellumBase])
                    && child.RFT.IsInPolygon(ConvexHullsPrior[PointLabels.RightFlagellumTip])

                    && child.LFB.IsInPolygon(ConvexHullsPrior[PointLabels.LeftFlagellumBase])
                    && child.LFT.IsInPolygon(ConvexHullsPrior[PointLabels.LeftFlagellumTip])

                    && child.LS.IsInPolygon(ConvexHullsPrior[PointLabels.LeftScape])
                    && child.RS.IsInPolygon(ConvexHullsPrior[PointLabels.RightScape])

                    ;

            return valid;
        }

        protected override AntenaPoints SelectLocation()
        {
            return PreviousSolution;
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
                point.IsInPolygon(ConvexHullsPrior[PointLabels.RightFlagellumTip]) ||
                point.IsInPolygon(ConvexHullsPrior[PointLabels.LeftFlagellumTip]) ||
                point.IsInPolygon(ConvexHullsPrior[PointLabels.RightFlagellumBase]) ||
                point.IsInPolygon(ConvexHullsPrior[PointLabels.LeftFlagellumBase])
                ;

            return result;
        }

        public Point ToPriorSpace(Point p)
        {
            return p.ToPriorSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public System.Drawing.Point ToHeadSpace(Point p)
        {
            return p.ToHeadSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }
        public Point ToPriorSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToPriorSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
        }

        public System.Drawing.Point ToHeadSpace(System.Drawing.Point p)
        {
            return p.ToWindowsPoint().ToHeadSpace(new Point(HeadDims.X, HeadDims.Y), HeadAngle, ScaleX, ScaleY, OffsetX, OffsetY, PriorAngle, ScapeDistanceAtScale1);
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