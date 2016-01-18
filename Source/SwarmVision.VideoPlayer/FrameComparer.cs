using System.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Classes;

namespace SwarmVision.VideoPlayer
{
    public class FrameComparer
    {
        public VideoProcessorBase Processor;
        public static List<long> PerformanceHistory = new List<long>(1000);

        public int Threshold = 50;
        public int MostRecentFrameIndex = -1; //Always resumes one frame ahead

        public VideoDecoder Decoder;
        public FrameRenderer Renderer;
        public EventHandler<FrameComparisonArgs> FrameCompared;
        public EventHandler<EventArgs> Stopped;

        public bool IsPaused;
        private Thread _bufferMonitor;

        private double LeftBountPCT;
        private double RightBountPCT;
        private double TopBountPCT;
        private double BottomBountPCT;

        public FrameComparer(VideoDecoder decoder, FrameRenderer renderer)
        {
            Decoder = decoder;
            Renderer = renderer;
        }

        public void Start()
        {
            //Should resume to the next frame (0 if at beggining)
            var resumeFrame = MostRecentFrameIndex + 1.0;

            SeekTo(resumeFrame/Decoder.VideoInfo.TotalFrames);

            //Set up the buffer monitor & start monitoring
            _bufferMonitor = new Thread(CompareFramesInBuffer) {IsBackground = true};
            _bufferMonitor.Start();

            //Start decoder & renderer
            Decoder.Start();
            Renderer.Start();
        }

        public void Pause(bool stopSelf = true)
        {
            //stop decoding, comparing, rendering
            Decoder.Stop();

            if (stopSelf)
                StopComparing();

            Renderer.Stop();

            //Cleanup buffers
            Decoder.ClearBuffer();
            Renderer.ClearBuffer();
        }

        public void SeekTo(double percentLocation)
        {
            MostRecentFrameIndex = (int) (Math.Round(Decoder.VideoInfo.TotalFrames*percentLocation, 0));

            Decoder.SeekTo((float) (Decoder.VideoInfo.Duration.TotalSeconds*percentLocation));
        }

        public void Stop()
        {
            //Stop is pause with reset to begining
            Pause();

            //Go back to beggining
            Reset();
        }

        private void StopComparing()
        {
            try
            {
                _bufferMonitor.Abort();
            }
            catch
            {
            }
        }

        private void Reset()
        {
            MostRecentFrameIndex = -1;
        }

        /// <summary>
        /// This should be called once per play start
        /// </summary>
        private void CompareFramesInBuffer()
        {
            while (true)
            {
                if (Decoder.IsBufferReady &&
                    Decoder.FramesInBuffer &&
                    Decoder.FrameBuffer.First != null &&
                    Decoder.FrameBuffer.First.Value.IsDecoded)
                {
                    var currentFrame = Decoder.FrameBuffer.First.Value;

                    if (!currentFrame.IsDecoded)
                        continue;

                    var compareResult = (FrameComparerResults) Processor.OnProcessing(currentFrame);
                        
                    //Notify of comparison results
                    if (FrameCompared != null)
                        FrameCompared(this, new FrameComparisonArgs() {Results = compareResult});
                    
                    currentFrame.Watch.Stop();

                    //Add frame to rendering queue
                    Renderer.Queue.AddLast(new ComparedFrame()
                    {
                        Frame = currentFrame,
                        ComparerResults = compareResult,
                    });

                    Debug.Print(new string('R', Renderer.Queue.Count));

                    //Retain location
                    MostRecentFrameIndex = currentFrame.FrameIndex;
                    Decoder.FrameBuffer.Remove(currentFrame);
                }
                else if (Decoder.AtEndOfVideo)
                {
                    Pause(false);

                    if (Stopped != null)
                        Stopped(this, null);

                    Reset();

                    //If no more frames and at the end, stop processing
                    return;
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }

        public void SetBounds(double leftPercent, double topPercent, double rightPercent, double bottomPercent)
        {
            LeftBountPCT = leftPercent;
            RightBountPCT = rightPercent;
            TopBountPCT = topPercent;
            BottomBountPCT = bottomPercent;
        }
    }
}