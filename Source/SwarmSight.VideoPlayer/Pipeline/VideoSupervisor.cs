using SwarmSight.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwarmSight.VideoPlayer.Pipeline
{
    public class VideoSupervisor : PipelineSupervisor
    {
        public ProcessorWorker Processor;
        public RendererWorker Renderer;

        public FrameBuffer Queue;

        public VideoSupervisor()
        {
            Processor = new ProcessorWorker();
            Renderer = new RendererWorker();

            Workers = new PipelineWorker[]
            {
                Processor,
                Renderer
            };

            Processor.VD.BufferInitialized += QueueReady;
            WorkFinished += VideoSupervisor_WorkFinished;
        }

        private void VideoSupervisor_WorkFinished(object sender, EventArgs e)
        {
            Processor.VD.ResetToBeginning();
        }

        private void QueueReady(object sender, EventArgs e)
        {
            Queue = Processor.VD.FrameDecoder.FrameBuffer;

            //Frame decoder has the main Queue
            for (int w = 0; w < Workers.Length; w++)
            {
                Workers[w].Queue = Queue;
            }
        }

        internal void Seek(double percentLocation)
        {
            if (State == WorkState.Paused)
                StopWorking();

            else if (State == WorkState.Working)
                throw new InvalidOperationException("Cannot be working to seek");

            while (Workers.Any(w => w.State != PipelineWorker.WorkerState.Stopped))
                Thread.Sleep(10);

            Processor.SeekTo(percentLocation);
        }

        internal void Open(string path)
        {
            Processor.VideoPath = path;
            Processor.VD.Open(path);
        }

        public override bool IsAtEndOfWorkLoad()
        {
            return 
                Processor.VD.AtEndOfVideo && Processor.VD.FrameBuffer.Count == 0 
                || Workers.All(w => w.State == PipelineWorker.WorkerState.Stopped)
            ;
        }

        public void RunOneFrame()
        {
            //Seek(Processor.MostRecentFrameIndex);
            
            StartWorking(oneFrame: true);

            //for (int w = 0; w < Workers.Length; w++)
            //{
            //    //Processor runs one frame, then pause
            //    //All others run normally
            //    Workers[w].StartWorking(oneItem: true);                    
            //}


            //Wait till Processor finishes one item
            //while (Workers[0].DoOneItem)
            //    Thread.Sleep(10);

            //Wait till everyone is at the same position as processor
            //while (Workers.Any(w => w.QueuePosition != Workers[0].QueuePosition))
            //    Thread.Sleep(10);

            //Supervisor and workers pause
            //PauseWork();
        }
    }
}
