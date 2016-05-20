using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SwarmSight.Filters
{
    public abstract class Background
    {
        public Frame Model;
        public int Size;

        protected Queue<Frame> frames;

        public Background(int size)
        {
            frames = new Queue<Frame>(size);
            Size = size;
        }

        public void Append(Frame frame)
        {
            //Check if over-sized
            if (frames.Count == Size)
            {
                var discard = frames.Dequeue();

                //Remove the frame values from the model
                AddToModel(discard, -1);

                discard.Dispose();
                discard = null;
            }

            frames.Enqueue(frame);

            //Add the frame values to the model
            AddToModel(frame, +1);
        }

        protected abstract unsafe void AddToModel(Frame frame, int sign);
        protected abstract unsafe void UpdateModel();
    }

    public class AverageBackground : Background
    {
        public AverageBackground(int size) : base(size)
        {

        }

        protected override unsafe void AddToModel(Frame frame, int sign)
        {
            if (Model == null)
                Model = new Frame(frame.Width, frame.Height, frame.PixelFormat, false);

            var resultFirstPx = Model.FirstPixelPointer;
            var resultStride = Model.Stride;

            var targetFirstPx = frame.FirstPixelPointer;
            var targetStride = frame.Stride;

            var n = Size;
            var height = Model.Height;
            var width = Model.Width;
            var multiplier = sign >= 0 ? 1 : -1;

            Parallel.For(0, height, y =>
            {
                var resultRowStart = resultFirstPx + y * resultStride;
                var rowStart = targetFirstPx + y * targetStride;

                for (var x = 0; x < width; x++)
                {
                    var x3 = x * 3;
                    var resultPxAddr = resultRowStart + x3;
                    var pxAddr = rowStart + x3;

                    *(resultPxAddr) += (byte)(*(pxAddr) * multiplier / n);
                    *(resultPxAddr + 1) += (byte)(*(pxAddr + 1) * multiplier / n);
                    *(resultPxAddr + 2) += (byte)(*(pxAddr + 2) * multiplier / n);
                }
            });
        }

        protected override void UpdateModel()
        {
            
        }
    }

    public class MedianBackground : Background
    {
        public MedianBackground(int size) : base(size)
        {

        }
        
        private int[,,,] histograms = null;
        private int frameCount = 0;
        private double[,,] medianBinOffset;

        protected override unsafe void AddToModel(Frame frame, int sign)
        {
            var height = frame.Height;
            var width = frame.Width;

            if (Model == null)
            {
                Model = new Frame(width, height, frame.PixelFormat, false);
                histograms = new int[height, width, 3, 256];
                medianBinOffset = new double[height, width, 3];

                //Start at -0.5 -> to make the first frame the median
                for (int i = 0; i < medianBinOffset.GetLength(0); i++)
                    for (int j = 0; j < medianBinOffset.GetLength(1); j++)
                        for (int c = 0; c < medianBinOffset.GetLength(2); c++)
                            medianBinOffset[i, j, c] = -0.5;
            }

            var modelFirstPx = Model.FirstPixelPointer;
            var modelStride = Model.Stride;

            var newFrameFirstPx = frame.FirstPixelPointer;
            var newFrameStride = frame.Stride;

            var n = Size;

            var multiplier = sign >= 0 ? 1 : -1;

            frameCount += multiplier;

            Parallel.For(0, height, new ParallelOptions
            {
                //MaxDegreeOfParallelism = 1
            }, 
            y =>
            {
                var modelRowStart = modelFirstPx + y * modelStride;
                var newFrameRowStart = newFrameFirstPx + y * newFrameStride;

                for (var x = width - 1; x >= 0 ; x--)
                {
                    var x3 = x * 3;
                    var modelPxAddr = modelRowStart + x3;
                    var newFramePxAddr = newFrameRowStart + x3;

                    //For every color channel, update their medians, by 
                    //updating the locations of the halfway points
                    for (var c = 2; c >= 0; c--)
                    {
                        //Get the new color value
                        var newColorValue = *(newFramePxAddr + c);

                        //Update the histogram
                        histograms[y, x, c, newColorValue] += multiplier;

                        //Update the halfway-point
                        var currBin = *(modelPxAddr + c); //The model contains the medians
                        var halfwayMovedBy = (newColorValue < currBin ? -0.5 : 0.5)*multiplier;
                        var newOffset = medianBinOffset[y,x,c] + halfwayMovedBy;
                        

                        //No move if added to existing median bin
                        if (currBin != newColorValue)
                        {
                            //NOTE: Treating offset value of BinCount-0.5 as between Bin and Bin+1;

                            //If added to after or current, and no longer in the current bin, go to the next one
                            if (halfwayMovedBy > 0 && newOffset > histograms[y, x, c, currBin] - 0.5)
                            {
                                newOffset = 0;

                                //Find the next bin that has elements
                                do
                                {
                                    currBin++;
                                }
                                while (histograms[y, x, c, currBin] == 0);

                                *(modelPxAddr + c) = currBin;
                            }

                            //If added to befor, and no longer in the current bin, go to the previous one
                            else if (newOffset < 0)
                            {
                                var binCount = 0;

                                //Find an earlier bin with elements
                                do
                                {
                                    currBin--;
                                    binCount = histograms[y, x, c, currBin];
                                }
                                while (binCount == 0);
                                
                                newOffset = binCount - 0.5;

                                *(modelPxAddr + c) = currBin;
                            }
                        }

                        medianBinOffset[y, x, c] = newOffset;
                    }
                }
            });

            //frameCount++;
            //if (frameCount <= Size  || frameCount % Size == 0)
            //    UpdateModel();
        }
        

        protected override unsafe void UpdateModel()
        {
            
        }
    }


}
