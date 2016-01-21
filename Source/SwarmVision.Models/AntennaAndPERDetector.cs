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
using SwarmVision.Filters;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using SwarmVision.VideoPlayer;
using SwarmVision.HeadPartsTracking.Algorithms;
using SwarmVision.HeadPartsTracking.Models;
using AForge.Neuro;
using Classes;

namespace SwarmVision.HeadPartsTracking
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

            public static int HeadGenerations = 500;
            public static int HeadGenerationSize = 100;

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

        public StringBuilder sb = new StringBuilder("t, x, y"+Environment.NewLine);

        //Network NNprobX = Network.Load(@"C:\temp\25percent.net_Proboscis TipX");
        //Network NNprobY = Network.Load(@"C:\temp\25percent.net_Proboscis TipY");
        double prevProbX = 0;
        double prevProbY = 0;
        
        LinkedList<Frame> pastFrames = new LinkedList<Frame>();
        private int pastFramesNeeded = 1;
        private AntenaParams prevAntenaModel;
        HeadSearchAlgorithm headFinder = new HeadSearchAlgorithm();
        FastAntennaSearchAlgorithm aa = new FastAntennaSearchAlgorithm();
        private HeadModel bestHead = null;
        private int frameIndex = 0;
        public override object OnProcessing(Frame rawFrame)
        {
            var result = new FrameComparerResults();

            if (bestHead == null)
            {
                //var frameEdged = rawFrame
                //    .Clone()
                //    .EdgeFilter();

                //bestHead = headFinder.Search(new FrameCollection() { ShapeData = frameEdged });
                //Debug.WriteLine("Best Fitness: " + headFinder.BestRecentFitness);

                bestHead = new HeadModel()
                    {
                        Angle = new AngleInDegrees(179.827, 0, 180),
                        Origin = new System.Windows.Point(61.6358455487457, 30.4848220715814),
                        ScaleX = new MinMaxDouble(0.901, 0, 1),
                        ScaleY = new MinMaxDouble(1.045, 0, 2)
                    };

                //Background search? periodic search?
                //Cache from disk, after checking file sum
                
            }

            var antenaPoints = FastFindAnetana(rawFrame, bestHead.Origin.ToDrawingPoint(), bestHead.Angle,
                                                bestHead.ScaleX, bestHead.ScaleY);
            
            if (antenaPoints != null)
            {
                using (var g = Graphics.FromImage(rawFrame.Bitmap))
                {
                    var pen = new Pen(Color.Yellow, 1);
                    //g.DrawEllipse(pen, antenaPoints.P1.Moved(loc.Origin.X,loc.Origin.Y).EnclosingRectangle(2));
                    //g.DrawEllipse(pen, antenaPoints.P2.Moved(loc.Origin.X, loc.Origin.Y).EnclosingRectangle(2));
                    //g.DrawEllipse(pen, antenaPoints.P3.Moved(loc.Origin.X, loc.Origin.Y).EnclosingRectangle(2));
                    //g.DrawEllipse(pen, antenaPoints.P4.Moved(loc.Origin.X, loc.Origin.Y).EnclosingRectangle(2));
                    //g.DrawEllipse(pen, antenaPoints.P5.Moved(loc.Origin.X, loc.Origin.Y).EnclosingRectangle(2));
                    //g.DrawEllipse(pen, antenaPoints.P6.Moved(loc.Origin.X, loc.Origin.Y).EnclosingRectangle(2));

                    g.DrawLines(pen, new[]
                        {
                            antenaPoints.P1.Moved(bestHead.Origin.X, bestHead.Origin.Y).ToPointF(),
                            antenaPoints.P2.Moved(bestHead.Origin.X, bestHead.Origin.Y).ToPointF(),
                            antenaPoints.P3.Moved(bestHead.Origin.X, bestHead.Origin.Y).ToPointF()
                        });
                    g.DrawLines(pen, new[]
                        {
                            antenaPoints.P4.Moved(bestHead.Origin.X, bestHead.Origin.Y).ToPointF(),
                            antenaPoints.P5.Moved(bestHead.Origin.X, bestHead.Origin.Y).ToPointF(),
                            antenaPoints.P6.Moved(bestHead.Origin.X, bestHead.Origin.Y).ToPointF()
                        });
                }
            }
            
            rawFrame.Bitmap.Save(@"c:\temp\frames\" + frameIndex + ".bmp", ImageFormat.Bmp);
            frameIndex++;

            return result;
        }

        public void Reset()
        {
            if (_prevFrame != null)
            {
                _prevFrame.Dispose();
                _prevFrame = null;
            }
        }

         
        public AntenaPoints FastFindAnetana(Frame rawFrame, Point origin, double angle, double scaleX, double scaleY)
        {
            var rawClone = rawFrame.Clone();

            if (pastFrames.Count >= pastFramesNeeded)
            {
                using (var clipped = rawFrame.SubClipped(origin.X, origin.Y, HeadView.Width, HeadView.Height))
                using (var clippedPrev = pastFrames.Last.Value.SubClipped(origin.X, origin.Y, HeadView.Width, HeadView.Height))
                //using (var motion = clipped.ChangeExtent(clippedPrev)) //Motion signal 
                //using (var motionT = motion.Threshold(30))
                //using (var motionTF = motionT.FillGaps())
                //using (var nowAntena = clipped.ReMap(pc => (1 - pc.Distance(Color.Black) / 255.0).ToColor()))
                //using (var nowAntennaT = nowAntena.Threshold(255 - 90))
                //using (var nowTips = clipped.ReMap(pc => { var d = (int)(255 - pc.Distance(Config.AntennaColors[1])); return d.ToColor(); })) //and Now tip color
                //using (var nowTipsT = nowTips.Threshold(255 - 3))
                //using (var antPlusTips = nowAntennaT.Or(nowTipsT))
                //using (var combo = motionTF.And(antPlusTips))
                {
                    aa.HeadAngle = angle;
                    aa.ScaleX = scaleX;
                    aa.ScaleY = scaleY;
                    
                    var antenaPoints = aa.Search(new FrameCollection() {ShapeData = clipped, MotionData = clippedPrev});

                    aa.SearchTimings();

                    //DecorateAntena(rawFrame, origin, combo);

                    return antenaPoints;
                }
            }

            pastFrames.AddLast(rawClone);

            if (pastFrames.Count > pastFramesNeeded)
            {
                var garb = pastFrames.First();
                garb.Dispose();
                pastFrames.Remove(garb);
            }

            return null;
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
                        antena.Tip = new Point((int) (mostDistantPoints.Average(p => p.X)),
                                               (int) mostDistantPoints.Average(p => p.Y));

                        var stdevX = oneAntennaPx.StdDev(p => p.X);
                        var stdevY = oneAntennaPx.StdDev(p => p.Y);

                        if (stdevY/stdevX > 1.5)
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
                                                antena.Tip.X - diameter/2,
                                                antena.Tip.Y - diameter/2,
                                                diameter,
                                                diameter
                                    );

                                gfx.DrawEllipse(new Pen(Color.White, 1),
                                                middlePt.X - diameter/2,
                                                middlePt.Y - diameter/2,
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