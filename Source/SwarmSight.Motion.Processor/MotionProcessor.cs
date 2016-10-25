using Classes;
using SwarmSight.Filters;
using SwarmSight.VideoPlayer;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SwarmSight.Motion.Processor
{
    public class MotionProcessor : VideoProcessorBase
    {
        private PixelShader shader = new PixelShader();
        private FrameBuffer buffer;
        private int framesNeeded = 2;
        public int Threshold = 30;

        public double LeftBoundPCT;
        public double RightBoundPCT = 1;
        public double TopBoundPCT;
        public double BottomBoundPCT = 1;

        public bool ShowMotion = true;
        public int ShadeRadius = 1;

        public override void OnProcessing(Frame current)
        {
            if(buffer == null || !current.SameSizeAs(buffer.First))
                buffer = new FrameBuffer(framesNeeded, current.Width, current.Height);

            var result = new MotionProcessorResult();

            //Check for duplicate frames, and assume previous result for them
            if(buffer.Count > 0 && !current.IsDifferentFrom(buffer.First))
            {
                current.ProcessorResult = (MotionProcessorResult) buffer.First.ProcessorResult;
                current.IsReadyForRender = true;
                return;
            }

            if (buffer.Count == framesNeeded)
            {
                buffer.RemoveFirst();
            }

            buffer.Enqueue(current);            

            if (buffer.Count == framesNeeded)
            {
                var prev = buffer.First;

                var roi = new System.Windows.Rect
                (
                    new Point(current.Width * LeftBoundPCT, current.Height * TopBoundPCT),
                    new Point(current.Width * RightBoundPCT, current.Height * BottomBoundPCT)
                );

                var changedPixels = current.ChangeExtentPoints(prev, Threshold, roi);

                result.Threshold = Threshold;

                result.ChangedPixels = changedPixels;
                result.ChangedPixelsCount = changedPixels.Count;

                result.Frame = current;
                result.FrameIndex = current.FrameIndex;
                result.FrameTime = current.FrameTime;
            }

            current.ProcessorResult = result;

            current.IsReadyForRender = true;
        }

        public void SetBounds(double leftPercent, double topPercent, double rightPercent, double bottomPercent)
        {
            LeftBoundPCT = leftPercent;
            RightBoundPCT = rightPercent;
            TopBoundPCT = topPercent;
            BottomBoundPCT = bottomPercent;
        }

        public void Annotate(Frame frame)
        {
            var result = (MotionProcessorResult)frame.ProcessorResult;

            if(result != null)
                shader.Shade(frame, result.ChangedPixels, ShadeRadius);
        }
    }

}