using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SwarmSight.VideoPlayer;

namespace Classes
{
    public class FrameRenderer
    {
        public EventHandler<OnFrameReady> FrameReady;
        public FrameBuffer<ComparedFrame> Queue;
        public int ShadeRadius = 1;
        public bool ShowMotion = true;

        private Thread _renderJob;
        private PixelShader _shader;

        public FrameRenderer()
        {
            _shader = new PixelShader();
            Queue = new FrameBuffer<ComparedFrame>();
        }

        public void Start()
        {
            if (_renderJob != null)
                try
                {
                    _renderJob.Abort();
                }
                catch
                {
                }

            _renderJob = new Thread(RenderFrames) {IsBackground = true};
            _renderJob.Start();
        }

        public void Stop()
        {
            try
            {
                _renderJob.Abort();
            }
            catch
            {
            }
        }

        public void ClearBuffer()
        {
            //Clear the queue
            foreach (var frame in Queue)
            {
                frame.Frame.Dispose();
            }

            Queue.Clear();
        }

        private void RenderFrames()
        {
            while (true)
            {
                try
                {
                    if (Queue.Count > 0)
                    {
                        //If more than one frame in queue, skip the earlier ones
                        //while (Queue.Count > 1)
                        //{
                        //    var skipFrame = Queue.First;
                        //    skipFrame.Value.Frame.Dispose();
                        //    Queue.Remove(skipFrame);
                        //}

                        var comparedFrame = Queue.First;

                        //Shade if motion is visible
                        if (ShowMotion)
                        {
                            if (comparedFrame == null)
                                continue;

                            if (comparedFrame.Value.ComparerResults != null)
                                _shader.Shade
                                    (
                                        comparedFrame.Value.Frame,
                                        comparedFrame.Value.ComparerResults.ChangedPixels,
                                        ShadeRadius
                                    );
                        }

                        if (FrameReady != null)
                            FrameReady(this, new OnFrameReady() {Frame = comparedFrame.Value.Frame});

                        comparedFrame.Value.Frame.Dispose();
                        Queue.Remove(comparedFrame);
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Renderer Invalid Operation Exception");
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Renderer null reference exception");
                }
            }
        }
    }
}