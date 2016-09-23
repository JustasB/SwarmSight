using System.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Classes;

namespace SwarmSight.VideoPlayer
{
    public class FrameComparer
    {
        public VideoProcessorBase Processor;

        public int MostRecentFrameIndex = -1; //Always resumes one frame ahead

        public VideoDecoder Decoder;
        public FrameRenderer Renderer;
        public EventHandler<FrameComparisonArgs> FrameCompared;
        public EventHandler<EventArgs> Stopped;
        
        public bool IsPaused;

        private Thread _bufferMonitor;
        private bool _canPlayNext = true; //used for playing one frame at a time

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
                bool readyToCompare = false;

                if(Decoder.FrameDecoder != null && Decoder.FrameBuffer != null)
                    lock (Decoder.FrameBuffer)
                    {
                        readyToCompare = Decoder.IsBufferReady &&
                            Decoder.FramesInBuffer &&
                            Decoder.FrameBuffer.First != null &&
                            Decoder.FrameBuffer.First.IsDecoded &&
                            _canPlayNext;
                    }

                if (readyToCompare)
                {
                    var currentFrame = Decoder.FrameBuffer.First;

                    if (!currentFrame.IsDecoded)
                        continue;

                    //Processor.OnProcessing(currentFrame);
                    
                    currentFrame.Watch.Stop();

                    //Retain location of last processed frame
                    MostRecentFrameIndex = currentFrame.FrameIndex;

                    //Add frame to rendering queue
                    currentFrame.IsProcessed = true;
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
    }
}