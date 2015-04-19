using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SwarmVision.VideoPlayer;

namespace Classes
{
    public class FrameRenderer
    {
        public EventHandler<OnFrameReady> FrameReady;
        public LinkedList<ComparedFrame> Queue;
        public int ShadeRadius = 1;
        public bool ShowMotion = true;

        private Thread _renderJob;
        private PixelShader _shader;

        public FrameRenderer()
        {
            _shader = new PixelShader();
            Queue = new LinkedList<ComparedFrame>();
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
                if (Queue.Count > 0)
                {
                    //If more than one frame in queue, skip the earlier ones
                    while (Queue.Count > 1)
                    {
                        var skipFrame = Queue.First;
                        skipFrame.Value.Frame.Dispose();
                        Queue.Remove(skipFrame);
                    }

                    var comparedFrame = Queue.First;

                    //Shade if motion is visible
                    if (ShowMotion)
                    {
                        if (comparedFrame == null)
                            continue;

                        _shader.Shade
                            (
                                comparedFrame.Value.Frame,
                                comparedFrame.Value.ComparerResults.ChangedPixels,
                                ShadeRadius
                            );
                    }

                    if (FrameReady != null)
                        FrameReady(this, new OnFrameReady() {Frame = comparedFrame.Value.Frame});

                    Queue.Remove(comparedFrame);
                    comparedFrame.Value.Frame.Dispose();
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }
    }
}