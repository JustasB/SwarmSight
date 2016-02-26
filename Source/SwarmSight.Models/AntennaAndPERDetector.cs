using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines;
using SwarmSight.Filters;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using SwarmSight.VideoPlayer;
using SwarmSight.HeadPartsTracking.Algorithms;
using SwarmSight.HeadPartsTracking.Models;
using AForge.Neuro;
using Classes;

namespace SwarmSight.HeadPartsTracking
{
    public class AntennaAndPERDetector : VideoProcessorBase
    {
        private class AntenaParams
        {
            public Regression.RegressionResult Seg2 { get; set; }
            public Point Tip { get; set; }
            public int LineX { get; set; }
        }

        public static class Config
        {
            public static double ShapeWeight = 1;
            public static double MotionWeight = 5;
            public static double ColorWeight = 0.2;

            public static int MotionThreshold = 20;

            public static float ContrastSteepness = 1f;
            public static float ContrastShift = 10f;

            public static int AntennaGenerations = 5;
            public static int AntennaGenerationSize = 25;

            public static int HeadGenerations = 10;
            public static int HeadGenerationSize = 50;

            public static List<Color> AntennaColors = new List<Color>
            {

                Color.FromArgb(29, 29, 17),
                Color.FromArgb(147, 120, 128),
            };
            public static int ColorDistance = 40;


            public static bool ShowModel = true;
            public static bool ShowMotionData = true;
            public static bool ShowShapeData = true;
            public static bool ShowColorData = true;
        }

        private Frame _prevFrame;

        private static readonly HeadSearchAlgorithm HeadSearchAlgo = new HeadSearchAlgorithm();
        private static readonly AntennaSearchAlgorithm LeftAntenaSearchAlgo = new LeftAntennaSearchAlgorithm();
        private static readonly AntennaSearchAlgorithm RightAntenaSearchAlgo = new RightAntennaSearchAlgorithm();

        public static HeadModel LocationOfHead(Frame target)
        {
            return HeadSearchAlgo.Search(new FrameCollection { ShapeData = target });
        }

        public static HeadModel PositionOfAntenna(FrameCollection headEdged, HeadModel headLocation)
        {
            //RightAntenaSearchAlgo.SetScaleAndRotation(headLocation.Angle, headLocation.Scale);

            var rightAntenaModel = RightAntenaSearchAlgo.Search(headEdged);

            return rightAntenaModel;
        }

        public StringBuilder sb = new StringBuilder("t, x, y" + Environment.NewLine);
        double prevProbX = 0;
        double prevProbY = 0;

        LinkedList<Frame> pastFrames = new LinkedList<Frame>();
        private int pastFramesNeeded = 2;
        private AntenaParams prevAntenaModel;
        HeadSearchAlgorithm headFinder = new HeadSearchAlgorithm();
        FastAntennaSearchAlgorithm aa = new FastAntennaSearchAlgorithm();
        public HeadModel BestHead = null;
        private int frameIndex = 0;


        public List<string> dataFrame = new List<string>(); 
        public override object OnProcessing(Frame rawFrame)
        {
            var result = new FrameComparerResults();

            var antenaPoints = FastFindAnetana(ref rawFrame, BestHead.Origin.ToDrawingPoint(), BestHead.Dimensions.ToDrawingPoint(), BestHead.Angle,
                                                BestHead.ScaleX, BestHead.ScaleY);


            //var buzzer = rawFrame.GetColor(new System.Drawing.Point(344, 47)).R;
            var buzzer = rawFrame.GetColor(new System.Drawing.Point(261,49)).GetBrightness();

            dataFrame.Add(rawFrame.FrameIndex + "," + buzzer + "," + aa.LeftHighest + "," + aa.RightHighest);

            if(aa.DebugFrame != null)
                aa.DebugFrame.Bitmap.Save(@"c:\temp\frames\" + rawFrame.FrameIndex + ".jpg", ImageFormat.Jpeg);
            
            //var antenaPoints = (AntenaPoints)null;
            
            using (var g = Graphics.FromImage(rawFrame.Bitmap))
            {
                var yellow = new Pen(Color.Yellow, 1);
                var red = new Pen(Color.Red, 1);
                var black = new Pen(Color.Black, 1);

                if (antenaPoints != null)
                {
                    g.DrawLines(yellow, new[]
                    {
                        antenaPoints.RS.Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF(),
                        antenaPoints.RFB.Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF(),
                        antenaPoints.RFT.Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()
                    });
                    g.DrawLines(yellow, new[]
                    {
                        antenaPoints.LS.Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF(),
                        antenaPoints.LFB.Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF(),
                        antenaPoints.LFT.Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()
                    });
                }

                if(FastAntennaSearchAlgorithm.ConvexHulls != null)
                { 
                    var headBoundary = FastAntennaSearchAlgorithm.ConvexHulls[PointLabels.Head].Select(p => aa.ToFrameSpace(p).Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()).ToArray();
                    var mouthBoundary = FastAntennaSearchAlgorithm.ConvexHulls[PointLabels.Mandibles].Select(p => aa.ToFrameSpace(p).Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()).ToArray();
                    var left = FastAntennaSearchAlgorithm.ConvexHulls[PointLabels.LeftFlagellumTip].Select(p => aa.ToFrameSpace(p).Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()).ToArray();
                    var right = FastAntennaSearchAlgorithm.ConvexHulls[PointLabels.RightFlagellumTip].Select(p => aa.ToFrameSpace(p).Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()).ToArray();
                    var rightJ = FastAntennaSearchAlgorithm.ConvexHulls[PointLabels.RightFlagellumBase].Select(p => aa.ToFrameSpace(p).Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()).ToArray();
                    var leftJ = FastAntennaSearchAlgorithm.ConvexHulls[PointLabels.LeftFlagellumBase].Select(p => aa.ToFrameSpace(p).Moved(BestHead.Origin.X, BestHead.Origin.Y).ToPointF()).ToArray();
                


                    //g.DrawPolygon(red, headBoundary);
                    g.DrawPolygon(red, mouthBoundary);
                    g.DrawPolygon(red, left);
                    g.DrawPolygon(red, right);
                    g.DrawPolygon(red, rightJ);
                    g.DrawPolygon(red, leftJ);
                }
                g.DrawPolygon(black, new[]
                {
                    BestHead.Origin.ToPointF(),
                    BestHead.Origin.Moved(BestHead.Dimensions.X, 0).ToPointF(),
                    BestHead.Origin.Moved(BestHead.Dimensions.X, BestHead.Dimensions.Y).ToPointF(),
                    BestHead.Origin.Moved(0, BestHead.Dimensions.Y).ToPointF()
                });
            }
            

            return result;
        }

        public void Reset()
        {
            if (_prevFrame != null)
            {
                _prevFrame.Dispose();
                //_prevFrame = null;
            }
        }


        public AntenaPoints FastFindAnetana(ref Frame rawFrame, Point origin, Point dims, double angle, double scaleX, double scaleY)
        {
            var rawClone = rawFrame.Clone();
            var antenaPoints = (AntenaPoints)null;

            if (pastFrames.Count >= pastFramesNeeded)
            {
                var clipped =
                    new List<Frame> { rawFrame, pastFrames.Last.Value, pastFrames.First.Value }
                    .AsParallel()
                    .Select(f => f.SubClipped(origin.X, origin.Y, dims.X, dims.Y))
                    .ToList();

                aa.HeadAngle = angle;
                aa.ScaleX = scaleX;
                aa.ScaleY = scaleY;

                antenaPoints = aa.Search(new FrameCollection() { ShapeData = clipped[0], MotionData = clipped[1], ColorData = clipped[2] });

                //aa.SearchTimings(); //DebugWrite slows perf

                clipped.AsParallel().ForAll(f => f.Dispose());

                //Overwrite the current frame with the past one
                rawFrame.DrawFrame(pastFrames.Last.Value, 0, 0, 1, 0);
            }

            pastFrames.AddLast(rawClone);

            if (pastFrames.Count > pastFramesNeeded)
            {
                var oldestFrame = pastFrames.First;
                oldestFrame.Value.Dispose();
                pastFrames.Remove(oldestFrame);
                oldestFrame.Value = null;
            }

            return antenaPoints;
        }

        private void DecorateAntena(Frame rawFrame, Point origin, Frame combo)
        {
            combo.ColorPixels(aa.TargetPoints.Select(p => aa.ToFrameSpace(p).ToDrawingPoint()).ToList(), Color.Blue);


            //Draw prior space
            using (var g = Graphics.FromImage(combo.Bitmap))
            {
                //Prior origin and axes
                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToFrameSpace(new System.Windows.Point(-200, 0)).ToDrawingPoint(),
                    aa.ToFrameSpace(new System.Windows.Point(200, 0)).ToDrawingPoint());

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToFrameSpace(new System.Windows.Point(-200, -50)).ToDrawingPoint(),
                    aa.ToFrameSpace(new System.Windows.Point(200, -50)).ToDrawingPoint());

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToFrameSpace(new System.Windows.Point(-200, 50)).ToDrawingPoint(),
                    aa.ToFrameSpace(new System.Windows.Point(200, 50)).ToDrawingPoint());

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToFrameSpace(new System.Windows.Point(0, -200)).ToDrawingPoint(),
                    aa.ToFrameSpace(new System.Windows.Point(0, 200)).ToDrawingPoint());

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToFrameSpace(new System.Windows.Point(50, -200)).ToDrawingPoint(),
                    aa.ToFrameSpace(new System.Windows.Point(50, 200)).ToDrawingPoint());

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToFrameSpace(new System.Windows.Point(-50, -200)).ToDrawingPoint(),
                    aa.ToFrameSpace(new System.Windows.Point(-50, 200)).ToDrawingPoint());

                //All the hulls
                foreach (var hull in FastAntennaSearchAlgorithm.ConvexHulls.Keys)
                {
                    var hullPts = FastAntennaSearchAlgorithm
                        .ConvexHulls[hull]
                        .Select(v => aa.ToFrameSpace(v).ToDrawingPoint())
                        .ToArray();

                    if (hullPts.Length > 2)
                        g.DrawPolygon(new Pen(Color.Blue, 1), hullPts);
                }
            }

            rawFrame.DrawFrame(combo, origin.X, origin.Y, alpha: 0.4, threshold: 0);
        }


        public object FindAnetana(Frame rawFrame)
        {
            var result = new FrameComparerResults();
            var rawClone = rawFrame.Clone();

            if (pastFrames.Count >= pastFramesNeeded)
            {
                using (var darkened = rawFrame.MapOfDarkened(pastFrames.Last()))
                using (var darkcolor = rawFrame.CloseToColorMap(Config.AntennaColors[0], Config.ColorDistance))
                using (var combo = darkened.AveragePixels(darkcolor))
                {
                    Array.Copy(combo.PixelBytes, rawFrame.PixelBytes, rawFrame.PixelBytesLength);

                    var whitePxs = combo.PointsOverThreshold(128);

                    //do this based on head orientation
                    var regression = new Regression();
                    var minSeg1Length = 30;
                    var maxSeg1Length = 70;
                    var aveSeg1Length = 50;
                    var maxSeg2Length = 100;
                    var maxDistToTip = 150;
                    var origin = new Point(325, 270);
                    var searchPolygon = new Point[]
                        {
                            new Point(311, 316), new Point(440, 190), new Point(470, 352), new Point(366, 366),
                            new Point(332, 337)
                        };
                    var path = new GraphicsPath();
                    path.AddPolygon(searchPolygon);
                    var region = new Region(path);

                    rawFrame.ColorIfTrue(Color.Orange, p =>
                    {
                        lock (region)
                        {
                            return region.IsVisible(p.X, p.Y);
                        }
                    });
                    rawFrame.ColorIfTrue(Color.LimeGreen,
                                         p => regression.Distance(p, origin).Between(maxSeg1Length, maxDistToTip));

                    var oneAntennaPx = whitePxs
                        .Where(p =>
                               p.X > origin.X &&
                               regression.Distance(p, origin).Between(maxSeg1Length, maxDistToTip)
                               && region.IsVisible(p.X, p.Y)
                        )
                        .OrderByDescending(p => regression.Distance(p, origin))
                        .Skip(5)
                        .ToList();

                    var antena = new AntenaParams();

                    if (oneAntennaPx.Count >= 50)
                    {
                        var mostDistantPoints =
                            oneAntennaPx.OrderByDescending(p => regression.Distance(p, origin))
                                        .Take(14)
                                        .Skip(2)
                                        .Take(10)
                                        .ToList();
                        antena.Tip = new Point((int)(mostDistantPoints.Average(p => p.X)),
                                               (int)mostDistantPoints.Average(p => p.Y));

                        var stdevX = oneAntennaPx.StdDev(p => p.X);
                        var stdevY = oneAntennaPx.StdDev(p => p.Y);

                        if (stdevY / stdevX > 1.5)
                        {
                            //If nearly vertical line, flip axes because regression is poor for vertical lines
                            antena.Seg2 = regression
                                .RegressLine(oneAntennaPx.Select(p => new Point(p.Y, p.X)).ToList())
                                .Invert();
                        }
                        else
                        {
                            antena.Seg2 = regression.RegressLine(oneAntennaPx);
                        }

                        rawFrame.ColorPixels(oneAntennaPx, Color.Blue);
                        rawFrame.ColorPixels(mostDistantPoints, Color.Yellow);
                    }
                    else if (prevAntenaModel != null)
                    {
                        antena = prevAntenaModel;
                    }


                    if (antena.Seg2 != null)
                    {
                        using (var gfx = Graphics.FromImage(rawFrame.Bitmap))
                        {
                            var pointsOn2ndSec = regression.PointsOnLineDistanceAway(origin, antena.Seg2, aveSeg1Length);

                            if (pointsOn2ndSec.Count == 0 && prevAntenaModel != null)
                            {
                                antena = prevAntenaModel;
                                pointsOn2ndSec = regression.PointsOnLineDistanceAway(origin, antena.Seg2, aveSeg1Length);
                            }

                            if (pointsOn2ndSec.Count > 0)
                            {
                                var middlePt = pointsOn2ndSec[0];

                                if (regression.Distance(pointsOn2ndSec[0], antena.Tip) >
                                    regression.Distance(pointsOn2ndSec[1], antena.Tip))
                                    middlePt = pointsOn2ndSec[1];

                                gfx.DrawLines(new Pen(Color.Blue, 7), new PointF[]
                                    {
                                        new PointF(origin.X, origin.Y),
                                        new PointF(middlePt.X, middlePt.Y)
                                    });

                                gfx.DrawLines(new Pen(Color.Blue, 7), new PointF[]
                                    {
                                        new PointF(middlePt.X, middlePt.Y),
                                        new PointF(antena.Tip.X, antena.Tip.Y),
                                    });

                                gfx.DrawLines(new Pen(Color.Yellow, 5), new PointF[]
                                    {
                                        new PointF(origin.X, origin.Y),
                                        new PointF(middlePt.X, middlePt.Y)
                                    });

                                gfx.DrawLines(new Pen(Color.Yellow, 5), new PointF[]
                                    {
                                        new PointF(middlePt.X, middlePt.Y),
                                        new PointF(antena.Tip.X, antena.Tip.Y),
                                    });

                                var diameter = 31;
                                gfx.DrawEllipse(new Pen(Color.White, 1),
                                                antena.Tip.X - diameter / 2,
                                                antena.Tip.Y - diameter / 2,
                                                diameter,
                                                diameter
                                    );

                                gfx.DrawEllipse(new Pen(Color.White, 1),
                                                middlePt.X - diameter / 2,
                                                middlePt.Y - diameter / 2,
                                                diameter,
                                                diameter
                                    );

                                prevAntenaModel = new AntenaParams
                                {
                                    Seg2 = antena.Seg2,
                                    Tip = antena.Tip,
                                    LineX = antena.LineX
                                };
                            }


                        }
                    }
                    //rawFrame.Bitmap.Save(@"c:\temp\darkened\" + rawFrame.FrameIndex + ".bmp");

                    if (pastFrames.Count > pastFramesNeeded)
                    {
                        pastFrames.First().Dispose();
                        pastFrames.RemoveFirst();
                    }
                }
            }

            pastFrames.AddLast(rawClone);

            return result;
        }
    }
}