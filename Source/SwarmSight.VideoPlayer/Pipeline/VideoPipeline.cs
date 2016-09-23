using SwarmSight.Filters;
using SwarmSight.VideoPlayer.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Classes;

namespace SwarmSight.VideoPlayer
{
    public class VideoPipeline
    {
        public VideoSupervisor Supervisor;
        public event EventHandler<OnFrameReady> FrameReady;
        public event EventHandler Stopped;

        public VideoInfo VideoInfo
        {
            get
            {
                return Supervisor.Processor.VD.VideoInfo; 
            }
        }

        public VideoProcessorBase VideoProcessor
        {
            get
            {
                return Supervisor.Processor.Processor;
            }
            set 
            {
                Supervisor.Processor.Processor = value;
            }
        }
        public VideoPipeline()
        {
            Supervisor = new VideoSupervisor();

            Supervisor.WorkFinished += Supervisor_WorkFinished;
            Supervisor.Renderer.FrameReady += Renderer_FrameReady;
        }

        private void Supervisor_WorkFinished(object sender, EventArgs e)
        {
            if (Stopped != null)
                Stopped(this, null);
        }

        private void Renderer_FrameReady(object sender, OnFrameReady e)
        {
            if (FrameReady != null)
                FrameReady(this, e);
        }

        public void Open(string path)
        {
            Supervisor.Open(path);
        }

        public void Start()
        {
            
            Supervisor.StartWorking();
        }

        public void Pause()
        {
            Supervisor.PauseWork();
        }

        public void Seek(double percentLocation)
        {
            Supervisor.Seek(percentLocation);
        }

        public void Stop()
        {
            Supervisor.StopWorking();
        }

        public void RunOneFrame()
        {
            Supervisor.RunOneFrame();
        }

        public bool IsAtEndOfVideo()
        {
            return Supervisor.Processor.VD.AtEndOfVideo;
        }
    }
}
