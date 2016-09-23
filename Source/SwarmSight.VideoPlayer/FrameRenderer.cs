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
        public VideoDecoder Decoder;
        public EventHandler<OnFrameReady> FrameReady;
        public int ShadeRadius = 1;
        public bool ShowMotion = true;

        private Thread _renderJob;
        private PixelShader _shader;

        public FrameRenderer()
        {
            _shader = new PixelShader();
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

        private void RenderFrames()
        {
            while (true)
            {
                try
                {
                    if (Decoder.FrameBuffer.Count > 0 && Decoder.FrameBuffer.First.IsProcessed)
                    {
                        var comparedFrame = Decoder.FrameBuffer.First;

                        if (FrameReady != null)
                            FrameReady(this, new OnFrameReady() {Frame = comparedFrame});

                        Decoder.FrameBuffer.RemoveFirst();
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