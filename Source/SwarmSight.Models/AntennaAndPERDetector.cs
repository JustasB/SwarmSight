using Classes;
using Settings;
using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking.Algorithms;
using SwarmSight.HeadPartsTracking.Models;
using SwarmSight.VideoPlayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

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
        FrameBuffer pastFrames;
        Frame resultFrame;
        private int pastFramesNeeded = 3;
        HeadSearchAlgorithm headFinder = new HeadSearchAlgorithm();
        FastAntennaSearchAlgorithm aa = new FastAntennaSearchAlgorithm();
        public Dictionary<int, AntenaPoints> frameData = new Dictionary<int, AntenaPoints>();

        //public List<string> dataFrame = new List<string>(); //for buzzer
        public override void OnProcessing(Frame rawFrame)
        {
            var result = new FrameComparerResults();

            //var subd = rawFrame.SubClipped(201, 48, 233, 256);
            //var larger = new Frame(233 + 10, 256 + 10, rawFrame.PixelFormat, false);
            //larger.DrawFrame(subd, 5, 5, 1,0);

            //larger.Bitmap.Save(@"Y:\Downloads\BeeVids\19Feb16-Start Hept Tests\B1-Feb19-2M-heptanal\" + rawFrame.FrameIndex + ".jpg", ImageFormat.Jpeg);

            if (pastFrames == null)
                pastFrames = new FrameBuffer(3, rawFrame.Width, rawFrame.Height);

            if (resultFrame == null)
                resultFrame = rawFrame.Clone();

            var antenaPoints = FastFindAnetana(
                ref rawFrame,
                new Point(AppSettings.Default.HeadX, AppSettings.Default.HeadY),
                new Point(AppSettings.Default.HeadW, AppSettings.Default.HeadH),
                AppSettings.Default.HeadAngle,
                AppSettings.Default.HeadScale, AppSettings.Default.HeadScale,
                AppSettings.Default.ScapeDistanceAtScale1
            );



            //var buzzer = rawFrame.GetColor(new System.Drawing.Point(261,49)).GetBrightness();

            //dataFrame.Add(rawFrame.FrameIndex + "," + buzzer + "," + aa.LeftHighest + "," + aa.RightHighest);

            //if(ll.DebugFrame != null && aa.DebugFrame != null)
            //{
            //    using (var comb = new Frame(aa.DebugFrame.Width, aa.DebugFrame.Height + ll.DebugFrame.Height, aa.DebugFrame.PixelFormat, false))
            //    {
            //        comb.DrawFrame(ll.DebugFrame);
            //        comb.DrawFrame(aa.DebugFrame, 0, ll.DebugFrame.Height, 1, 0);
            //        comb.Bitmap.Save(@"c:\temp\frames\" + rawFrame.FrameIndex + ".jpg", ImageFormat.Jpeg);
            //    }
            //}

            //var antenaPoints = (AntenaPoints)null;

            //Show the prev frame
            resultFrame.DrawFrame(pastFrames.Count > 1 ? pastFrames.GetNthAfterFirst(1) : rawFrame,0,0,1,0);
            result.Frame = resultFrame;

            if (antenaPoints != null)
            {
                antenaPoints.Buzzer = rawFrame.GetColor(new System.Drawing.Point(333, 28)).R;

                frameData[result.Frame.FrameIndex] = antenaPoints;
            }
            //if(AppSettings.De)

            if(AppSettings.Default.ShowMotion)
                result.Frame.ColorPixels(aa.AntennaPoints.Select(p => p.Moved(AppSettings.Default.HeadX,AppSettings.Default.HeadY)).ToList(), Color.White);

            if (AppSettings.Default.ShowSectors)
            {
                foreach (var sectors in aa.PrevSectors)
                {
                    result.Frame.MarkSectors(
                        sectors: sectors.Value, 
                        headCtrX: AppSettings.Default.HeadX + AppSettings.Default.Dimensions.X/2, 
                        headCtrY: AppSettings.Default.HeadY + AppSettings.Default.Dimensions.Y/2, 
                        headHeight: AppSettings.Default.Dimensions.Y,
                        headAngle: AppSettings.Default.HeadAngle, 
                        color: Color.White,
                        isRight: sectors.Key == PointLabels.RightSectorData
                    );
                }
            }

            using (var g = Graphics.FromImage(result.Frame.Bitmap))
            {
                var yellow5 = new Pen(Color.Yellow, 3);
                var blue7 = new Pen(Color.Blue, 5);
                var red = new Pen(Color.Red, 1);
                var black = new Pen(Color.Black, 1);

                if (antenaPoints != null)
                {

                    g.DrawLines(blue7, new[]
                    {
                        //antenaPoints.RS.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.RFB.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.RFT.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()
                    });
                    g.DrawLines(blue7, new[]
                    {
                        //antenaPoints.LS.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.LFB.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.LFT.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()
                    });
                    g.DrawLines(yellow5, new[]
                    {
                        //antenaPoints.RS.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.RFB.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.RFT.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()
                    });
                    g.DrawLines(yellow5, new[]
                    {
                        //antenaPoints.LS.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.LFB.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF(),
                        antenaPoints.LFT.Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()
                    });
                }

                if(FastAntennaSearchAlgorithm.ConvexHullsPrior != null)
                { 
                    var headBoundary = FastAntennaSearchAlgorithm.ConvexHullsPrior[PointLabels.Head].Select(p => aa.ToHeadSpace(p).Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()).ToArray();
                    var mouthBoundary = FastAntennaSearchAlgorithm.ConvexHullsPrior[PointLabels.Mandibles].Select(p => aa.ToHeadSpace(p).Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()).ToArray();
                    var left = FastAntennaSearchAlgorithm.ConvexHullsPrior[PointLabels.LeftFlagellumTip].Select(p => aa.ToHeadSpace(p).Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()).ToArray();
                    var right = FastAntennaSearchAlgorithm.ConvexHullsPrior[PointLabels.RightFlagellumTip].Select(p => aa.ToHeadSpace(p).Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()).ToArray();
                    var rightJ = FastAntennaSearchAlgorithm.ConvexHullsPrior[PointLabels.RightFlagellumBase].Select(p => aa.ToHeadSpace(p).Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()).ToArray();
                    var leftJ = FastAntennaSearchAlgorithm.ConvexHullsPrior[PointLabels.LeftFlagellumBase].Select(p => aa.ToHeadSpace(p).Moved(AppSettings.Default.Origin.X, AppSettings.Default.Origin.Y).ToPointF()).ToArray();
                


                    //g.DrawPolygon(red, headBoundary);
                    g.DrawPolygon(red, mouthBoundary);
                    g.DrawPolygon(red, left);
                    g.DrawPolygon(red, right);
                    g.DrawPolygon(red, rightJ);
                    g.DrawPolygon(red, leftJ);
                }
                g.DrawPolygon(black, new[]
                {
                    AppSettings.Default.Origin.ToPointF(),
                    AppSettings.Default.Origin.Moved(AppSettings.Default.HeadW, 0).ToPointF(),
                    AppSettings.Default.Origin.Moved(AppSettings.Default.HeadW, AppSettings.Default.HeadH).ToPointF(),
                    AppSettings.Default.Origin.Moved(0, AppSettings.Default.HeadH).ToPointF()
                });
            }

            rawFrame.DrawFrame(result.Frame);

            return;
        }

        private List<Point3D> leftTips3D = new List<Point3D>();
        private FastAntennaSearchAlgorithm ll = new FastAntennaSearchAlgorithm();
        public AntenaPoints FastFindAnetana(ref Frame rawFrame, Point origin, Point dims, double angle, double scaleX, double scaleY, double scapeDist)
        {
            var antenaPoints = (AntenaPoints)null;

            //Discard duplicate frames
            if (pastFrames.Count == 0 || rawFrame.IsDifferentFrom(pastFrames.Last))
            {
                //Remove old frames
                if (pastFrames.Count >= pastFramesNeeded)
                {
                    pastFrames.RemoveFirst();
                }

                pastFrames.Enqueue(rawFrame);
            }
            

            if (pastFrames.Count >= pastFramesNeeded)
            {
                aa.HeadAngle = angle;
                aa.ScaleX = scaleX;
                aa.ScaleY = scaleY;
                aa.HeadOffset = origin;
                aa.HeadDims = dims;
                aa.ScapeDistanceAtScale1 = scapeDist;
                aa.EvalExpected = true;

                antenaPoints = aa.Search(new FrameCollection()
                {
                    Current = pastFrames.GetNthAfterFirst(2),
                    Prev1 = pastFrames.GetNthAfterFirst(1),
                    Prev2 = pastFrames.GetNthAfterFirst(0)
                });
            }
            

            return antenaPoints;
        }

        private void DecorateAntena(Frame rawFrame, Point origin, Frame combo)
        {
            combo.ColorPixels(aa.TargetPoints.Select(p => aa.ToHeadSpace(p)).ToList(), Color.Blue);


            //Draw prior space
            using (var g = Graphics.FromImage(combo.Bitmap))
            {
                //Prior origin and axes
                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToHeadSpace(new System.Windows.Point(-200, 0)),
                    aa.ToHeadSpace(new System.Windows.Point(200, 0)));

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToHeadSpace(new System.Windows.Point(-200, -50)),
                    aa.ToHeadSpace(new System.Windows.Point(200, -50)));

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToHeadSpace(new System.Windows.Point(-200, 50)),
                    aa.ToHeadSpace(new System.Windows.Point(200, 50)));

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToHeadSpace(new System.Windows.Point(0, -200)),
                    aa.ToHeadSpace(new System.Windows.Point(0, 200)));

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToHeadSpace(new System.Windows.Point(50, -200)),
                    aa.ToHeadSpace(new System.Windows.Point(50, 200)));

                g.DrawLine(new Pen(Color.Red, 1),
                    aa.ToHeadSpace(new System.Windows.Point(-50, -200)),
                    aa.ToHeadSpace(new System.Windows.Point(-50, 200)));

                //All the hulls
                foreach (var hull in FastAntennaSearchAlgorithm.ConvexHullsPrior.Keys)
                {
                    var hullPts = FastAntennaSearchAlgorithm
                        .ConvexHullsPrior[hull]
                        .Select(v => aa.ToHeadSpace(v))
                        .ToArray();

                    if (hullPts.Length > 2)
                        g.DrawPolygon(new Pen(Color.Blue, 1), hullPts);
                }
            }

            rawFrame.DrawFrame(combo, origin.X, origin.Y, alpha: 0.4, threshold: 0);
        }



        //public object FindAnetana(Frame rawFrame)
        //{
        //    var result = new FrameComparerResults();
        //    var rawClone = rawFrame.Clone();

        //    if (pastFrames.Count >= pastFramesNeeded)
        //    {
        //        using (var darkened = rawFrame.MapOfDarkened(pastFrames.Last()))
        //        using (var darkcolor = rawFrame.CloseToColorMap(Config.AntennaColors[0], Config.ColorDistance))
        //        using (var combo = darkened.AveragePixels(darkcolor))
        //        {
        //            Array.Copy(combo.PixelBytes, rawFrame.PixelBytes, rawFrame.PixelBytesLength);

        //            var whitePxs = combo.PointsOverThreshold(128);

        //            //do this based on head orientation
        //            var regression = new Regression();
        //            var minSeg1Length = 30;
        //            var maxSeg1Length = 70;
        //            var aveSeg1Length = 50;
        //            var maxSeg2Length = 100;
        //            var maxDistToTip = 150;
        //            var origin = new Point(325, 270);
        //            var searchPolygon = new Point[]
        //                {
        //                    new Point(311, 316), new Point(440, 190), new Point(470, 352), new Point(366, 366),
        //                    new Point(332, 337)
        //                };
        //            var path = new GraphicsPath();
        //            path.AddPolygon(searchPolygon);
        //            var region = new Region(path);

        //            rawFrame.ColorIfTrue(Color.Orange, p =>
        //            {
        //                lock (region)
        //                {
        //                    return region.IsVisible(p.X, p.Y);
        //                }
        //            });
        //            rawFrame.ColorIfTrue(Color.LimeGreen,
        //                                 p => regression.Distance(p.ToWindowsPoint(), origin.ToWindowsPoint()).Between(maxSeg1Length, maxDistToTip));

        //            var oneAntennaPx = whitePxs
        //                .Where(p =>
        //                       p.X > origin.X &&
        //                       regression.Distance(p.ToWindowsPoint(), origin.ToWindowsPoint()).Between(maxSeg1Length, maxDistToTip)
        //                       && region.IsVisible(p.X, p.Y)
        //                )
        //                .OrderByDescending(p => regression.Distance(p.ToWindowsPoint(), origin.ToWindowsPoint()))
        //                .Skip(5)
        //                .ToList();

        //            var antena = new AntenaParams();

        //            if (oneAntennaPx.Count >= 50)
        //            {
        //                var mostDistantPoints =
        //                    oneAntennaPx.OrderByDescending(p => regression.Distance(p, origin))
        //                                .Take(14)
        //                                .Skip(2)
        //                                .Take(10)
        //                                .ToList();
        //                antena.Tip = new Point((int)(mostDistantPoints.Average(p => p.X)),
        //                                       (int)mostDistantPoints.Average(p => p.Y));

        //                var stdevX = oneAntennaPx.StdDev(p => p.X);
        //                var stdevY = oneAntennaPx.StdDev(p => p.Y);

        //                if (stdevY / stdevX > 1.5)
        //                {
        //                    //If nearly vertical line, flip axes because regression is poor for vertical lines
        //                    antena.Seg2 = regression
        //                        .RegressLine(oneAntennaPx.Select(p => new Point(p.Y, p.X)).ToList())
        //                        .Invert();
        //                }
        //                else
        //                {
        //                    antena.Seg2 = regression.RegressLine(oneAntennaPx);
        //                }

        //                rawFrame.ColorPixels(oneAntennaPx, Color.Blue);
        //                rawFrame.ColorPixels(mostDistantPoints, Color.Yellow);
        //            }
        //            else if (prevAntenaModel != null)
        //            {
        //                antena = prevAntenaModel;
        //            }


        //            if (antena.Seg2 != null)
        //            {
        //                using (var gfx = Graphics.FromImage(rawFrame.Bitmap))
        //                {
        //                    var pointsOn2ndSec = regression.PointsOnLineDistanceAway(origin, antena.Seg2, aveSeg1Length);

        //                    if (pointsOn2ndSec.Count == 0 && prevAntenaModel != null)
        //                    {
        //                        antena = prevAntenaModel;
        //                        pointsOn2ndSec = regression.PointsOnLineDistanceAway(origin, antena.Seg2, aveSeg1Length);
        //                    }

        //                    if (pointsOn2ndSec.Count > 0)
        //                    {
        //                        var middlePt = pointsOn2ndSec[0];

        //                        if (regression.Distance(pointsOn2ndSec[0], antena.Tip) >
        //                            regression.Distance(pointsOn2ndSec[1], antena.Tip))
        //                            middlePt = pointsOn2ndSec[1];

        //                        gfx.DrawLines(new Pen(Color.Blue, 7), new PointF[]
        //                            {
        //                                new PointF(origin.X, origin.Y),
        //                                new PointF(middlePt.X, middlePt.Y)
        //                            });

        //                        gfx.DrawLines(new Pen(Color.Blue, 7), new PointF[]
        //                            {
        //                                new PointF(middlePt.X, middlePt.Y),
        //                                new PointF(antena.Tip.X, antena.Tip.Y),
        //                            });

        //                        gfx.DrawLines(new Pen(Color.Yellow, 5), new PointF[]
        //                            {
        //                                new PointF(origin.X, origin.Y),
        //                                new PointF(middlePt.X, middlePt.Y)
        //                            });

        //                        gfx.DrawLines(new Pen(Color.Yellow, 5), new PointF[]
        //                            {
        //                                new PointF(middlePt.X, middlePt.Y),
        //                                new PointF(antena.Tip.X, antena.Tip.Y),
        //                            });

        //                        var diameter = 31;
        //                        gfx.DrawEllipse(new Pen(Color.White, 1),
        //                                        antena.Tip.X - diameter / 2,
        //                                        antena.Tip.Y - diameter / 2,
        //                                        diameter,
        //                                        diameter
        //                            );

        //                        gfx.DrawEllipse(new Pen(Color.White, 1),
        //                                        middlePt.X - diameter / 2,
        //                                        middlePt.Y - diameter / 2,
        //                                        diameter,
        //                                        diameter
        //                            );

        //                        prevAntenaModel = new AntenaParams
        //                        {
        //                            Seg2 = antena.Seg2,
        //                            Tip = antena.Tip,
        //                            LineX = antena.LineX
        //                        };
        //                    }


        //                }
        //            }
        //            //rawFrame.Bitmap.Save(@"c:\temp\darkened\" + rawFrame.FrameIndex + ".bmp");

        //            if (pastFrames.Count > pastFramesNeeded)
        //            {
        //                pastFrames.First().Dispose();
        //                pastFrames.RemoveFirst();
        //            }
        //        }
        //    }

        //    pastFrames.AddLast(rawClone);

        //    return result;
        //}
    }
}