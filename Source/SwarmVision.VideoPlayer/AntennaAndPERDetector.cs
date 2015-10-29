using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Classes;
using SwarmVision.Filters;
using SwarmVision.Models;
using System.Text;

namespace SwarmVision.VideoPlayer
{
    public class AntennaAndPERDetector : VideoProcessorBase
    {
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

            public static int HeadGenerations = 5;
            public static int HeadGenerationSize = 250;

            public static Color AntennaColor = Color.FromArgb(43, 41, 23);
            public static int ColorDistance = 80;


            public static bool ShowModel = true;
            public static bool ShowMotionData = true;
            public static bool ShowShapeData = true;
            public static bool ShowColorData = true;
        }

        private Frame _prevFrameEdged;

        private static readonly HeadSearchAlgorithm HeadSearchAlgo = new HeadSearchAlgorithm();
        private static readonly AntennaSearchAlgorithm LeftAntenaSearchAlgo = new LeftAntennaSearchAlgorithm();
        private static readonly AntennaSearchAlgorithm RightAntenaSearchAlgo = new RightAntennaSearchAlgorithm();

        public static HeadModel LocationOfHead(Frame target)
        {
            return HeadSearchAlgo.Search(new FrameCollection { ShapeData = target });
        }

        public static HeadModel PositionOfAntenna(FrameCollection headEdged, HeadModel headLocation)
        {
            RightAntenaSearchAlgo.SetScaleAndRotation(headLocation.Angle, headLocation.Scale);

            var rightAntenaModel = RightAntenaSearchAlgo.Search(headEdged);

            return rightAntenaModel;
        }

        public StringBuilder sb = new StringBuilder("t, x, y"+Environment.NewLine);

        public override object OnProcessing(Frame rawFrame)
        {

            var result = new FrameComparerResults();

            var frameEdged = rawFrame
                .Clone()
                .EdgeFilter()
                .ContrastFilter(Config.ContrastSteepness, Config.ContrastShift);

            if (_prevFrameEdged != null)
            {
                //Find head using edge data only
                var headLoc = LocationOfHead(frameEdged);

                rawFrame.DrawFrame(
                    (Frame)headLoc.View,
                    (int)headLoc.Origin.X,
                    (int)headLoc.Origin.Y
                );

                //After head is detected, start detecting antennae
                if (0 == 1 && frameEdged.FrameIndex > 300)
                {
                    using (var headRaw = rawFrame.SubClip((int)headLoc.Origin.X, (int)headLoc.Origin.Y, HeadView.Width, HeadView.Height))
                    using (var headEdged = frameEdged.SubClip((int)headLoc.Origin.X, (int)headLoc.Origin.Y, HeadView.Width, HeadView.Height))
                    using (var headEdgedPrev = _prevFrameEdged.SubClip((int)headLoc.Origin.X, (int)headLoc.Origin.Y, HeadView.Width, HeadView.Height))
                    {
                        var headStack = new FrameCollection
                        {
                            ShapeData = headEdged,
                            MotionData = Compare(headEdged, headEdgedPrev, Config.MotionThreshold),
                            ColorData = headRaw.CloseToColorMap(Config.AntennaColor, Config.ColorDistance),
                        };

                        var antenaLocation = PositionOfAntenna(headStack,headLoc);

                        sb.AppendLine(frameEdged.FrameIndex + "," + antenaLocation.LeftAntena.TipX + "," + antenaLocation.LeftAntena.TipY);

                        result = new FrameComparerResults()
                        {
                            ChangedPixelsCount = antenaLocation.LeftAntena.TipY,
                            FrameIndex = rawFrame.FrameIndex
                        };

                        if (Config.ShowShapeData)
                            rawFrame.DrawFrame(
                                headStack.ShapeData,
                                (int)headLoc.Origin.X,
                                (int)headLoc.Origin.Y
                            );

                        if (Config.ShowMotionData)
                            rawFrame.DrawFrame(
                                headStack.MotionData,
                                (int)headLoc.Origin.X,
                                (int)headLoc.Origin.Y
                            );

                        if (Config.ShowColorData)
                            rawFrame.DrawFrame(
                                headStack.ColorData,
                                (int)headLoc.Origin.X,
                                (int)headLoc.Origin.Y
                            );



                        if (Config.ShowModel)
                            using (var modelView = ((Frame)antenaLocation.View).Clone().ShiftColor())
                            {
                                rawFrame.DrawFrame(
                                    modelView,
                                    (int)headLoc.Origin.X,
                                    (int)headLoc.Origin.Y
                                );
                            }
                    }
                }

                _prevFrameEdged.Dispose();
                _prevFrameEdged = null;
            }

            _prevFrameEdged = frameEdged;

            return result;
        }

        public void Reset()
        {
            if (_prevFrameEdged != null)
            {
                _prevFrameEdged.Dispose();
                _prevFrameEdged = null;
            }
        }


        public unsafe Frame Compare(Frame bitmapA, Frame bitmapB, int threshold = 20)
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
                        Math.Abs(aFirstPx[offset]     - bFirstPx[offset]) +
                        Math.Abs(aFirstPx[offset + 1] - bFirstPx[offset + 1]) +
                        Math.Abs(aFirstPx[offset + 2] - bFirstPx[offset + 2]);

                    //if (colorDifference > efficientTreshold)
                    {
                        resultFirstPx[offset] = resultFirstPx[offset+1] = resultFirstPx[offset+2] = 
                            (byte)(colorDifference / 3);
                    }
                }
            });

            return result;
        }
    }
}