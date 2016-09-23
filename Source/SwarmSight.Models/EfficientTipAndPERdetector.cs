using SwarmSight.VideoPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwarmSight.Filters;
using Classes;
using DPoint = System.Drawing.Point;
using WPoint = System.Windows.Point;
using Point = System.Windows.Point;
using Color = System.Drawing.Color;
using Settings;
using System.IO;
using System.Windows;
using SwarmSight.HeadPartsTracking.Algorithms;
using MoreLinq;
using static System.Math;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

namespace SwarmSight.HeadPartsTracking
{
    public class EfficientTipAndPERdetector : VideoProcessorBase
    {
        public class TipAndPERResult
        {
            public SideResult Left;
            public SideResult Right;
            public PartResult Proboscis;
            public int TreatmentSensorValue;
        }

        public class PartResult
        {
            public SpacePoint Tip;
            public List<SpacePoint> DetectedPoints;
        }

        public class SideResult : PartResult
        {
            public SpacePoint Base;
            public double[] SectorCounts;

            public double Angle
            {
                get
                {
                    var result = Atan2(
                        -Tip.PriorPoint.Y - -Base.PriorPoint.Y,
                        Tip.PriorPoint.X - Base.PriorPoint.X
                        )
                        / PI * 180.0 //To Degrees
                        - 90; //Facing UP

                    //Within +/-180
                    if (result < -180)
                        result += 360;

                    return result;
                }
            }

            public int DominantSector
            {
                get
                {
                    if (SectorCounts != null && SectorCounts.Length > 0)
                    {
                        var max = SectorCounts.Max();

                        for (int i = 0; i < SectorCounts.Length; i++)
                        {
                            if (SectorCounts[i] == max)
                                return i + 1;
                        }

                    }

                    throw new Exception("No sector data to compute the dominant sector");
                }
            }
        }

        private int FastMotionHistLength = 3;
        private int SlowMotionHistLength = 30;
        private double TailPercent = 0.04;

        private FrameBuffer queue;
        private Frame previousFrame;
        private Frame currentFrame;

        private SpaceManager Space;

        private Thread backgroundUpdater;
        private Frame subclippedHead;
        private Frame stdHeadClip;
        private Frame ProboscisModel;
        private Frame FastMotionModel;
        private Frame SlowMotionModel;
        private Frame StationaryModel;
        private MedianBackground background;
        private PointHistogram ProbHist;
        private PointHistogram LeftFastMotionHist;
        private PointHistogram LeftSlowMotionHist;
        private PointHistogram LeftStationaryHist;
        private PointHistogram RightFastMotionHist;
        private PointHistogram RightSlowMotionHist;
        private PointHistogram RightStationaryHist;

        private Frame LeftCrawlSurface = null;
        private Frame RightCrawlSurface = null;
        private Frame ProboscisCrawlSurface = null;
        private SideResult LeftMostRecentSoln;
        private SideResult RightMostRecentSoln;

        public Dictionary<int, TipAndPERResult> Results;

        public static string ConvexHullsPath = @"Assets\Hulls\convexHulls.csv";
        public static Dictionary<PointLabels, List<Point>> ConvexHullsPrior = null;
        public static Dictionary<PointLabels, List<DPoint>> ConvexHullsFrame = null;
        public static Dictionary<PointLabels, List<DPoint>> ConvexHullsStandard = null;
        public SpacePoint ProboscisHullBase;
        public List<DPoint> LeftActivePoints;
        public List<DPoint> RightActivePoints;
        public List<DPoint> ProboscisActivePoints;
        public List<DPoint> AllActivePoints;

        public Frame TargetHeadClip
        {
            get
            {
                //We're computing for T-1 frame
                return queue.GetNthBeforeLast(1);
            }
        }

        private void Init()
        {
            //Receptive field location
            if (Space == null)
            {
                InitSpace();
            }

            //Frame history buffer
            if (queue == null)
                queue = new FrameBuffer(FastMotionHistLength, Space.StandardWidth, Space.StandardHeight);

            if (subclippedHead == null)
            {
                subclippedHead = new Frame(Space.HeadDims.X, Space.HeadDims.Y);
            }

            if (stdHeadClip == null)
            {
                stdHeadClip = new Frame(Space.StandardWidth, Space.StandardHeight);
            }

            if (ConvexHullsPrior == null)
                ReadHulls();

            if (ProboscisHullBase == null)
                ReadHullsFrame();

            if (AllActivePoints == null)
                ComputeStandardHullPoints();

            if (background == null)
                background = new MedianBackground(SlowMotionHistLength);

            if (FastMotionModel == null)
            {
                FastMotionModel = new Frame(Space.StandardWidth, Space.StandardHeight);
                SlowMotionModel = FastMotionModel.Clone();
                ProboscisModel = FastMotionModel.Clone();
                StationaryModel = FastMotionModel.Clone();

                ProbHist = new PointHistogram();
                LeftFastMotionHist = new PointHistogram();
                LeftSlowMotionHist = new PointHistogram();
                LeftStationaryHist = new PointHistogram();
                RightFastMotionHist = new PointHistogram();
                RightSlowMotionHist = new PointHistogram();
                RightStationaryHist = new PointHistogram();
            }

            if(Results == null)
                Results = new Dictionary<int, TipAndPERResult>();
        }

        private void InitSpace()
        {
            Space = new SpaceManager();
            Space.HeadOffset = new DPoint(AppSettings.Default.HeadX, AppSettings.Default.HeadY);
            Space.HeadAngle = AppSettings.Default.HeadAngle;
            Space.ScaleX = AppSettings.Default.HeadScale;
            Space.ScapeDistanceAtScale1 = AppSettings.Default.ScapeDistanceAtScale1;
        }

        public override void OnProcessing(Frame frame)
        {
            var result = new TipAndPERResult();
            currentFrame = frame;

            Init();

            //Treatment Sensor - read brightness from the target point before the rest of the frame is discarded
            result.TreatmentSensorValue = (int)Round(currentFrame.GetColor(AppSettings.Default.TreatmentSensor).GetBrightness() * 100, 0);
            currentFrame.ProcessorResult = result;

            EnqueueStandardHeadClip();

            //Ensure enough frames
            if (queue.Count < 3)
            {
                JoinBackgroundUpdater();
                previousFrame = currentFrame;

                //First frame does not have antenna info
                if (queue.Count == 1)
                    currentFrame.IsReadyForRender = true;

                return;
            }

            //Fast motion model - 3 frame op
            UpdateFastMotionModel();

            UpdateProboscisModel();

            UpdateStationaryModel();

            //Ensure background has been updated
            JoinBackgroundUpdater();

            //Slow motion - diff from median bg
            UpdateSlowMotionModel();

            //Append to the result of the previous frame
            var prevResult = (TipAndPERResult)previousFrame.ProcessorResult;

            //In parallel, get left and right tip coords:    
            var leftThread = new Thread(() =>
            {
                prevResult.Left = GetCoords(leftSide: true);
            });
            leftThread.Start();

            var rightThread = new Thread(() =>
            {
                prevResult.Right = GetCoords(leftSide: false);
            });
            rightThread.Start();

            //Detect PER while doing left and right
            prevResult.Proboscis = GetProbLoc();

            rightThread.Join();
            leftThread.Join();            

            //To be used in CSV saving
            Results[previousFrame.FrameIndex] = prevResult;

            previousFrame.ProcessorResult = prevResult;
            previousFrame.IsReadyForRender = true;


            //Update the previous frame
            previousFrame = currentFrame;
        }

        public void Annotate(Frame target)
        {
            using (var g = Graphics.FromImage(target.Bitmap))
            {
                var yellow1 = new Pen(Color.Yellow, 1);
                var blue7 = new Pen(Color.Blue, 5);
                var red = new Pen(Color.Red, 1);
                var black = new Pen(Color.Black, 1);

                var yellow3 = new Pen(Color.Yellow, 3);
                var blue1 = new Pen(Color.Blue, 1);
                var white1 = new Pen(Color.White, 1);

                //Model points
                if(AppSettings.Default.ShowModel && target?.ProcessorResult != null)
                {
                    var procResult = (TipAndPERResult)target.ProcessorResult;

                    Action<List<SpacePoint>> Draw = (pts) =>
                    {
                        if (pts == null || pts.Count == 0)
                            return;

                        //var scale = new SpacePoint().FromStandardSpace(new DPoint(100, 100)).SubclippedPoint;

                        //target.ColorPixels(pts.Select(p => p.FramePoint).ToList(), yellow1.Color);

                        var brush = new SolidBrush(Color.Yellow);
                        var scale = pts[0].Space.ScaleX;

                        pts.ForEach(p =>
                        {
                            g.FillEllipse(brush, new Rectangle((p.FramePoint.X - scale / 2).Rounded(), (p.FramePoint.Y - scale / 2).Rounded(), scale.Rounded(), scale.Rounded()));
                        });
                    };


                    Draw(procResult?.Left?.DetectedPoints);
                    Draw(procResult?.Right?.DetectedPoints);
                    Draw(procResult?.Proboscis?.DetectedPoints);
                }

                //Hulls
                if (ConvexHullsPrior != null)
                {
                    InitSpace();
                    
                    var mouthBoundary = ConvexHullsPrior[PointLabels.Mandibles].Select(p => new SpacePoint(Space).FromPriorSpace(p).FramePoint.ToPointF()).ToArray();
                    var left = ConvexHullsPrior[PointLabels.LeftFlagellumTip].Select(p => new SpacePoint(Space).FromPriorSpace(p).FramePoint.ToPointF()).ToArray();
                    var right = ConvexHullsPrior[PointLabels.RightFlagellumTip].Select(p => new SpacePoint(Space).FromPriorSpace(p).FramePoint.ToPointF()).ToArray();
                    var rightJ = ConvexHullsPrior[PointLabels.RightFlagellumBase].Select(p => new SpacePoint(Space).FromPriorSpace(p).FramePoint.ToPointF()).ToArray();
                    var leftJ = ConvexHullsPrior[PointLabels.LeftFlagellumBase].Select(p => new SpacePoint(Space).FromPriorSpace(p).FramePoint.ToPointF()).ToArray();
                    var head = ConvexHullsPrior[PointLabels.Head].Select(p => new SpacePoint(Space).FromPriorSpace(p).FramePoint.ToPointF()).ToArray();
                    var prob = ConvexHullsPrior[PointLabels.Proboscis].Select(p => new SpacePoint(Space).FromPriorSpace(p).FramePoint.ToPointF()).ToArray();


                    g.FillPolygon(new SolidBrush(Color.FromArgb(125, 0, 0, 0)), mouthBoundary);
                    g.FillPolygon(new SolidBrush(Color.FromArgb(125, 0, 0, 0)), head);

                    //Debug exclusion zones
                    //var exc = GetExclusionPolygons()[0].Select(p => new SpacePoint(Space).FromStandardSpace(p.ToDrawingPoint()).FramePoint.ToPointF()).ToArray();
                    //g.DrawPolygon(white1, exc);

                    g.DrawPolygon(white1, left);
                    g.DrawPolygon(white1, right);
                    g.DrawPolygon(white1, prob);

                    //g.DrawPolygon(yellow1, mouthBoundary);
                    //g.DrawPolygon(yellow1, rightJ);
                    //g.DrawPolygon(yellow1, leftJ);
                    g.DrawPolygon(white1, mouthBoundary);
                    g.DrawPolygon(white1, head);


                }

                //Outer border
                g.DrawPolygon(black, new[]
                {
                    AppSettings.Default.Origin.ToPointF(),
                    AppSettings.Default.Origin.Moved(Space.HeadDims.X, 0).ToPointF(),
                    AppSettings.Default.Origin.Moved(Space.HeadDims.X, Space.HeadDims.Y).ToPointF(),
                    AppSettings.Default.Origin.Moved(0, Space.HeadDims.Y).ToPointF()
                });

                //Detected tip locations
                if (target?.ProcessorResult != null)
                {
                    var procResult = (TipAndPERResult)target.ProcessorResult;

                    if (procResult?.Left?.Tip != null)
                    {
                        DrawCircle(g, yellow3, blue1, procResult.Left.Tip.FramePoint);
                    }

                    if (procResult?.Right?.Tip != null)
                    {
                        DrawCircle(g, yellow3, blue1, procResult.Right.Tip.FramePoint);
                    }

                    if (procResult?.Proboscis != null)
                    {
                        DrawCircle(g, yellow3, blue1, procResult.Proboscis.Tip.FramePoint);
                    }
                }

                //Draw the origin
                var center = new SpacePoint(Space).FromPriorSpace(new WPoint(0, 0)).FramePoint;
                g.FillEllipse(new SolidBrush(Color.Yellow), new Rectangle(center.X - 1, center.Y - 1, 3, 3));
                g.FillEllipse(new SolidBrush(Color.Blue), new Rectangle(center.X, center.Y, 1, 1));
            }

            //target.Bitmap.Save(@"Y:\Downloads\BeeVids\19Feb16-Start Hept Tests\frames\" + target.FrameIndex + ".jpg", ImageFormat.Jpeg);
        }

        private static void DrawCircle(Graphics g, Pen yellow3, Pen blue1, DPoint center)
        {
            var middle = 9;
            var thickness = 3;
            var full = middle + 2 * thickness;
            var left = center.X - middle / 2 - thickness;
            var top = center.Y - middle / 2 - thickness;
            var tipRect = new Rectangle(left, top, full, full);
            g.DrawEllipse(yellow3, tipRect);
            g.DrawEllipse(blue1, tipRect);
        }

        private void JoinBackgroundUpdater()
        {
            if (backgroundUpdater != null)
            {
                backgroundUpdater.Join();
                backgroundUpdater = null;
            }
        }

        private SideResult GetCoords(bool leftSide)
        {
            var activePoints = leftSide ? LeftActivePoints : RightActivePoints;
            var FastMotionHist = leftSide ? LeftFastMotionHist : RightFastMotionHist;
            var SlowMotionHist = leftSide ? LeftSlowMotionHist : RightSlowMotionHist;
            var StationaryHist = leftSide ? LeftStationaryHist : RightStationaryHist;
            var prevSoln = leftSide ? LeftMostRecentSoln : RightMostRecentSoln;

            //Fast tail
            var fastTail = FastMotionHist
                .FromPointList(FastMotionModel, activePoints)
                .GetTail(TailPercent, AppSettings.Default.FastThreshold);


            //Slow tail
            var slowTail = SlowMotionHist
                .FromPointList(SlowMotionModel, activePoints)
                .GetTail(TailPercent, AppSettings.Default.SlowThreshold);

            //Combine and then Median filter
            slowTail.AddRange(fastTail);

            var tailsDeNoised = slowTail.MedianFilter(radius: 1);

            //Check for extremely slow - stationary
            var statTail = StationaryHist
                .FromPointList(StationaryModel, activePoints)
                .GetTail(0.05, AppSettings.Default.StationaryThreshold);

            //Pick the model with most points
            tailsDeNoised = tailsDeNoised.Count > statTail.Count ? tailsDeNoised : statTail;

            //Or if not even stationary's (e.g. very fast), bail with prev soln 
            if (tailsDeNoised.Count < 10)
            {   
                return prevSoln;
            }  

            //Tips
            //Centroid
            var centroid = GetCentroid(tailsDeNoised);

            //Crawl to tip
            var crawlSurface = leftSide ? LeftCrawlSurface : RightCrawlSurface;

            if (crawlSurface == null)
            {
                crawlSurface = new Frame(Space.StandardWidth, Space.StandardHeight);

                if (leftSide)
                    LeftCrawlSurface = crawlSurface;
                else
                    RightCrawlSurface = crawlSurface;
            }

            var pointList = tailsDeNoised.Select(p => p.Item1).ToList();
            crawlSurface.ColorPixels(activePoints, Color.Yellow);
            crawlSurface.ColorPixels(pointList, Color.White);

            var ctr = new SpacePoint(Space)
                .FromPriorSpace(new WPoint(leftSide ? -0.5 : 0.5, 0))
                .StandardPoint;

            //Could do with lookup table
            var tip = CrawlToTip(crawlSurface, centroid, ctr, 1, AppSettings.Default.TipCrawlerRadius.Rounded(), AppSettings.Default.MaxTipCrawlerHop);
            var baseLoc = CrawlToTip(crawlSurface, centroid, ctr, 1, AppSettings.Default.TipCrawlerRadius.Rounded(), AppSettings.Default.MaxTipCrawlerHop, crawlingAway: false);

            //For Debugging
            //crawlSurface.MarkPoint(baseLoc, inner: Color.Yellow, outer: Color.Black);
            //crawlSurface.MarkPoint(centroid, outer: Color.Black);
            //crawlSurface.MarkPoint(tip, inner: Color.Orange, outer: Color.Black);
            //crawlSurface.MarkPoint(ctr, inner: Color.Purple, outer: Color.Black);

            //Sectors
            var sectors = ComputeSectorWeights(tailsDeNoised, Space.StandardWidth, Space.StandardHeight, Space.HeadAngle);

            var result = new SideResult()
            {
                Tip = new SpacePoint(Space).FromStandardSpace(tip),
                Base = new SpacePoint(Space).FromStandardSpace(baseLoc),
                DetectedPoints = pointList.Select(sp => new SpacePoint(Space).FromStandardSpace(sp)).ToList(),
                SectorCounts = sectors
            };

            if (leftSide)
                LeftMostRecentSoln = result;
            else
                RightMostRecentSoln = result;

            return result;
        }

        private PartResult GetProbLoc()
        {
            //if (currentFrame.FrameIndex == 649)
            //    System.Diagnostics.Debug.Write("");

            var activePoints = ProboscisActivePoints;

            //Get top active points
            var probTail = ProbHist
                .FromPointList(ProboscisModel, ProboscisActivePoints)
                .GetTail(0.35, AppSettings.Default.StationaryThreshold);

            var tailsDeNoised = probTail.MedianFilter(radius: 1);
            
            ////Tips
            ////Centroid
            var centroid = ProboscisHullBase.StandardPoint;
            if (tailsDeNoised.Count > 0)
            {
                var closestToCentroid = tailsDeNoised.MinBy(p => p.Item1.Distance(centroid)).Item1;
                if (closestToCentroid.Distance(centroid) <= 2)
                    centroid = closestToCentroid;
            }

            ////Crawl to tip
            var crawlSurface = ProboscisCrawlSurface;

            if (crawlSurface == null)
            {
                crawlSurface = new Frame(Space.StandardWidth, Space.StandardHeight);
                ProboscisCrawlSurface = crawlSurface;
            }

            var pointList = tailsDeNoised.Select(p => p.Item1).ToList();
            //var pointList = tailsDeNoised;
            crawlSurface.ColorPixels(ProboscisActivePoints, Color.Black);
            crawlSurface.ColorPixels(pointList, Color.White);

            var ctr = new SpacePoint(Space)
                .FromPriorSpace(new WPoint(0, 0))
                .StandardPoint;

            //Could do with lookup table
            var tip = CrawlToTip(crawlSurface, centroid, ctr, 1, AppSettings.Default.TipCrawlerRadius.Rounded(), AppSettings.Default.MaxTipCrawlerHop);
            
            //For debugging
            //crawlSurface.MarkPoint(centroid);
            //crawlSurface.MarkPoint(tip, inner: Color.Orange);
            
            return new PartResult
            {
                Tip = new SpacePoint(Space).FromStandardSpace(tip),
                DetectedPoints = pointList.Select(sp => new SpacePoint(Space).FromStandardSpace(sp)).ToList()
            };
        }

        private DPoint GetCentroid(List<Tuple<DPoint, double>> points)
        {
            //Pool top ones
            var topPts = points.OrderByDescending(p => p.Item2).Take(10).ToList();

            var topCentroid = new DPoint(
                        topPts.Average(pt => pt.Item1.X).Rounded(),
                        topPts.Average(pt => pt.Item1.Y).Rounded()
                    );

            //Make sure it's an actual point in list
            var med = points.MinBy(p => p.Item1.Distance(topCentroid)).Item1;

            return med;
        }

        private void EnqueueStandardHeadClip()
        {
            var isDuplicate =
                queue.Count > 0 &&
                !currentFrame.IsDifferentFrom(queue.Last);

            //Don't add duplicates to frame history
            if (isDuplicate)
                return;

            //Subclip head
            subclippedHead.DrawFrame(currentFrame, 0, 0, 1, 0,
                Space.HeadOffset.X, Space.HeadOffset.Y);

            //Resize to standard into stdHeadClip
            subclippedHead.ScaleHQ(Space.StandardWidth, Space.StandardHeight, stdHeadClip.Bitmap);

            //We're computing T-1 frame's antenna positions
            //T-2 frame should be the last frame part of the background
            if (queue.Count >= FastMotionHistLength - 1)
            {
                //Because T has not been added yet, T-2 is still T-1 
                var forBackground = queue.GetNthBeforeLast(1);

                //BG update takes a while, do it in parallel
                backgroundUpdater = new Thread(() =>
                {
                    //Add to background queue
                    background.Append(forBackground);
                });

                backgroundUpdater.Start();
            }

            //Enque into the buffer
            //Remove oldest frame
            if (queue.Count >= FastMotionHistLength)
            {
                queue.RemoveFirst();
            }

            //Add new one
            queue.Enqueue(stdHeadClip);
        }

        private unsafe void UpdateProboscisModel()
        {
            var modelPx = ProboscisModel.FirstPixelPointer;

            var currentPx = queue.GetNthBeforeLast(1).FirstPixelPointer;
            var motionPx = FastMotionModel.FirstPixelPointer;

            var width = Space.StandardWidth;
            var height = Space.StandardHeight;
            var stride = queue.Last.Stride;
            var modelStride = ProboscisModel.Stride;

            var antennaColor = AppSettings.Default.AntennaColor;
            var antR = antennaColor.R;
            var antG = antennaColor.G;
            var antB = antennaColor.B;

            var threshold = 1;

            ProboscisActivePoints.AsParallel().ForAll(p =>
            {
                var y = p.Y;
                var x = p.X;

                var rowStart = stride * y;
                var modelRowStart = modelStride * y;

                var modelOffsetG = x * 3 + modelRowStart + 1;

                var offsetB = x * 3 + rowStart;
                var offsetG = offsetB + 1;
                var offsetR = offsetB + 2;

                //Optimized mem access
                var curDist = 
                (
                    Abs(currentPx[offsetR] - antR) + 
                    Abs(currentPx[offsetG] - antG) + 
                    Abs(currentPx[offsetB] - antB)
                ) 
                / 3.0;

                //Subtract any moving antenna pixels
                var motionG = motionPx[offsetG];

                var newModel = 255 - curDist - 2*motionG;

                if (newModel <= threshold)
                {
                    newModel = 0;
                }
                else //Collect points over threshold
                {
                    if (newModel > 255)
                        newModel = 255;
                }

                modelPx[modelOffsetG] = (byte)newModel;
            });
        }

        private unsafe void UpdateFastMotionModel()
        {
            var decay = 0.15;

            var modelPx = FastMotionModel.FirstPixelPointer;

            var currentPx = queue.GetNthBeforeLast(0).FirstPixelPointer;
            var prev1Px = queue.GetNthBeforeLast(1).FirstPixelPointer;
            var prev2Px = queue.GetNthBeforeLast(2).FirstPixelPointer;

            var width = Space.StandardWidth;
            var height = Space.StandardHeight;
            var stride = queue.Last.Stride;
            var modelStride = FastMotionModel.Stride;

            var antennaColor = AppSettings.Default.AntennaColor;
            var antR = antennaColor.R;
            var antG = antennaColor.G;
            var antB = antennaColor.B;

            var threshold = 1;

            AllActivePoints.AsParallel().ForAll(p =>
            {
                var y = p.Y;
                var x = p.X;

                var rowStart = stride * y;
                var modelRowStart = modelStride * y;

                var modelOffsetG = x * 3 + modelRowStart + 1;

                var offsetB = x * 3 + rowStart;
                var offsetG = offsetB + 1;
                var offsetR = offsetB + 2;

                //Optimized mem access
                var curDist = (Abs(currentPx[offsetR] - antR) + Abs(currentPx[offsetG] - antG) + Abs(currentPx[offsetB] - antB)) / 3.0;
                var prev1Dist = (Abs(prev1Px[offsetR] - antR) + Abs(prev1Px[offsetG] - antG) + Abs(prev1Px[offsetB] - antB)) / 3.0;
                var prev2Dist = (Abs(prev2Px[offsetR] - antR) + Abs(prev2Px[offsetG] - antG) + Abs(prev2Px[offsetB] - antB)) / 3.0;

                //var combMotionDist = ((prev2Dist - prev1Dist) + (curDist - prev1Dist)) / 2.0;
                var combMotionDist = (prev2Dist + curDist - 2 * prev1Dist) / 2.0; //Simplified

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
                }

                modelPx[modelOffsetG] = (byte)newModel;
            });
        }

        private unsafe void UpdateSlowMotionModel()
        {
            var antCol = AppSettings.Default.AntennaColor;
            var antR = antCol.R;
            var antG = antCol.G;
            var antB = antCol.B;

            var width = TargetHeadClip.Width;
            var height = TargetHeadClip.Height;
            var stride = TargetHeadClip.Stride;

            var headFirstPx = TargetHeadClip.FirstPixelPointer;
            var bgFirstPx = background.Model.FirstPixelPointer;
            var modelFirstPx = SlowMotionModel.FirstPixelPointer;

            AllActivePoints.AsParallel().ForAll(p =>
            {
                var y = p.Y;
                var x = p.X;

                var rowStartOffset = y * stride;
                var pxOffset = rowStartOffset + x * 3;

                //Head px
                var headIntPix = *(int*)(headFirstPx + pxOffset) & 0xFFFFFFFF;
                var headB = (headIntPix) & 255;
                var headG = (headIntPix >> 8) & 255;
                var headR = (headIntPix >> 16) & 255;

                //Background px
                var bgIntPix = *(int*)(bgFirstPx + pxOffset) & 0xFFFFFFFF;
                var bgB = (bgIntPix) & 255;
                var bgG = (bgIntPix >> 8) & 255;
                var bgR = (bgIntPix >> 16) & 255;

                var headDistNow = (Abs(antR - headR) + Abs(antG - headG) + Abs(antB - headB)); //Color distance to antenna
                var headDistWas = (Abs(bgR - antR) + Abs(bgG - antG) + Abs(bgB - antB)); //Difference from background
                var headDistDiff = (headDistWas - headDistNow) / 3.0;

                modelFirstPx[pxOffset + 1] = (byte)(headDistDiff > 2 ? headDistDiff : 0);
            });
        }

        private unsafe void UpdateStationaryModel()
        {
            var antCol = AppSettings.Default.AntennaColor;
            var antR = antCol.R;
            var antG = antCol.G;
            var antB = antCol.B;

            var width = TargetHeadClip.Width;
            var height = TargetHeadClip.Height;
            var stride = TargetHeadClip.Stride;

            var headFirstPx = TargetHeadClip.FirstPixelPointer;
            var modelFirstPx = StationaryModel.FirstPixelPointer;

            var threshold = 150;

            AllActivePoints.AsParallel().ForAll(p =>
            {
                var y = p.Y;
                var x = p.X;

                var rowStartOffset = y * stride;
                var pxOffset = rowStartOffset + x * 3;

                //Head px
                var headIntPix = *(int*)(headFirstPx + pxOffset) & 0xFFFFFFFF;
                var headB = (headIntPix) & 255;
                var headG = (headIntPix >> 8) & 255;
                var headR = (headIntPix >> 16) & 255;

                var avgColCloseness = 255 - (Abs(antR - headR) + Abs(antG - headG) + Abs(antB - headB)) / 3.0;

                modelFirstPx[pxOffset + 1] = (byte)(avgColCloseness <= threshold ? 0 : avgColCloseness);
            });
        }

        public DPoint CrawlToTip(Frame target, System.Drawing.Point start, System.Drawing.Point origin, int threshold = 50, int maxDistance = 30, int maxHopDistance = 2, bool crawlingAway = true)
        {
            var loopCount = 0;
            var startColor = target.GetColor(start);

            if(startColor.GetBrightness() == 0)
                return start;

            var bestDistance = start.Distance(origin);
            var bestPoint = start;
            var visited = FrameFilterExtensions.CreateJaggedArray<bool[][]>(maxDistance * 2 + 1, maxDistance * 2 + 1);
            var visitedOffsetX = start.X - maxDistance;
            var visitedOffsetY = start.Y - maxDistance;
            var bestLocationLockpad = new object();

            var queue = new Queue<System.Drawing.Point>();
            queue.Enqueue(start);


            while (queue.Count > 0 && loopCount < 500)
            {
                loopCount++;
                var point = queue.Dequeue();
                var candidates = getCandidateLocations(point, maxHopDistance);

                Parallel.ForEach(candidates, p =>
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
                    var exists = visited[visitedX][visitedY];

                    if (exists)
                        return;

                    //Mark as checked, regardless of outcome
                    lock (visited)
                    {
                        visited[visitedX][visitedY] = true;
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
                        lock (bestLocationLockpad)
                        {
                            bestDistance = dist;
                            bestPoint = p;
                        }
                    }

                    lock (queue)
                    {
                        queue.Enqueue(p);
                    }

                });
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

                lock (result)
                {
                    result.AddRange(rowResult);
                }
            });

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

        #region Hull Operations
        private void ComputeStandardHullPoints()
        {
            //Use hulls to pre-compute active field points
            //Side hulls
            LeftActivePoints = GetHullPoints(
                include: new[] { PointLabels.LeftFlagellumTip },
                exclude: new[] { PointLabels.Mandibles, PointLabels.Head, PointLabels.Proboscis }
            );

            RightActivePoints = GetHullPoints(
                include: new[] { PointLabels.RightFlagellumTip },
                exclude: new[] { PointLabels.Mandibles, PointLabels.Head, PointLabels.Proboscis }
            );

            ProboscisActivePoints = GetHullPoints(
                include: new[] { PointLabels.Proboscis },
                exclude: new PointLabels[0]
            );

            //Full hull
            AllActivePoints = LeftActivePoints
                .Union(RightActivePoints)
                .Union(ProboscisActivePoints)
                .Distinct()
                .ToList();
        }

        private List<DPoint> GetHullPoints(PointLabels[] include, PointLabels[] exclude)
        {
            var result = new List<DPoint>();

            var exclusionZones = GetExclusionPolygons();

            for (int y = 0; y < Space.StandardHeight; y++)
            {
                for (int x = 0; x < Space.StandardWidth; x++)
                {
                    var addPt = false;

                    //Check if included
                    for (int inc = 0; inc < include.Length; inc++)
                    {
                        if (PointExtensions.IsInConvexPolygon(x, y, ConvexHullsStandard[include[inc]]))
                        {
                            addPt = true;
                            break;
                        }
                    }

                    //Check if excluded
                    if (addPt)
                    {
                        for (int exc = 0; exc < exclude.Length; exc++)
                        {
                            if (PointExtensions.IsInConvexPolygon(x, y, ConvexHullsStandard[exclude[exc]]))
                            {
                                addPt = false;
                                break;
                            }
                        }
                    }

                    //Check if in one of the user exclusion zones
                    if(addPt)
                    {
                        for (int exc = 0; exc < exclusionZones.Count; exc++)
                        {
                            if (PointExtensions.IsPointInPolygon(x, y, exclusionZones[exc]))
                            {
                                addPt = false;
                                break;
                            }
                        }
                    }

                    if (addPt)
                        result.Add(new DPoint(x, y));
                }
            }

            return result;
        }

        private List<WPoint[]> GetExclusionPolygons()
        {
            var zones = AppSettings.Default.ExclusionZones.Split('|');
            var result = new List<WPoint[]>();

            foreach (var zone in zones)
            {
                if (string.IsNullOrWhiteSpace(zone))
                    continue;

                var points = zone.Split(';');
                var polypts = new List<WPoint>();

                foreach (var point in points)
                {
                    var xy = point.Split(',');
                    var p = new DPoint(int.Parse(xy[0]), int.Parse(xy[1]));
                    var spacep = new SpacePoint(Space).FromFrameSpace(p);
                    polypts.Add(spacep.StandardPoint.ToWindowsPoint());
                }

                result.Add(polypts.ToArray());
            }

            return result;
        }

        public void ReadHulls()
        {
            var cols = File
                .ReadLines(ConvexHullsPath)
                .First()
                .Split(',')
                .Select((name, i) => new { name = name.Replace(@"""", ""), i })
                .ToDictionary(c => c.name);

            //THIS METHOD SHOULD BE REFACTORED TO REMOVE DUPLICATION
            ConvexHullsPrior = new Dictionary<PointLabels, List<Point>>
            {
                { PointLabels.RightScape, new List<Point>() },
                { PointLabels.RightFlagellumBase, new List<Point>() },
                { PointLabels.RightFlagellumTip, new List<Point>() },

                { PointLabels.LeftScape, new List<Point>() },
                { PointLabels.LeftFlagellumBase, new List<Point>() },
                { PointLabels.LeftFlagellumTip, new List<Point>() },

                { PointLabels.Mandibles, new List<Point>() },
                { PointLabels.Proboscis, new List<Point>() },
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

                    if (column[cols["probX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.Proboscis].Add(new Point(column[cols["probX"].i], column[cols["probY"].i]));
                    }

                    if (column[cols["headX"].i] != double.MinValue)
                    {
                        ConvexHullsPrior[PointLabels.Head].Add(new Point(column[cols["headX"].i], column[cols["headY"].i]));
                    }
                });

            ReadHullsFrame();
        }

        public void ReadHullsFrame()
        {
            //Convert prior spaced hulls to frame space
            ConvexHullsFrame = ConvexHullsPrior.Keys.ToDictionary(k => k, v => new List<DPoint>());

            ConvexHullsPrior.Keys.ForEach(k =>
                ConvexHullsFrame[k].AddRange(ConvexHullsPrior[k].Select(pv =>
                    Space.ToHeadSpaceFromPriorSpace(pv)
                ))
            );

            //Add standard space hulls
            ConvexHullsStandard = ConvexHullsPrior.Keys.ToDictionary(k => k, v => new List<DPoint>());

            ConvexHullsPrior.Keys.ForEach(k =>
                ConvexHullsStandard[k].AddRange(ConvexHullsFrame[k].Select(pv =>
                    Space.ToStandardSpaceFromHeadSpace(pv)
                ))
            );

            //Find the proboscis base point
            var closestPts = ConvexHullsPrior[PointLabels.Proboscis]
                .OrderByDescending(p => p.Y)
                .Take(2)
                .ToList();

            ProboscisHullBase = new SpacePoint(Space)
                .FromPriorSpace
                (new WPoint(
                    closestPts.Average(p => p.X), 
                    closestPts.Average(p => p.Y)
                ));
        }

        #endregion
    }
}
