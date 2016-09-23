using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwarmSight.Filters;
using Classes;

namespace SwarmSight.VideoPlayer.Pipeline
{
    public class ProcessorWorker : PipelineWorker
    {
        public VideoDecoder VD;
        public string VideoPath;
        
        public VideoProcessorBase Processor;
        public int MostRecentFrameIndex = -1; //Always resumes one frame ahead

        public ProcessorWorker()
        {
            VD = new VideoDecoder();
        }

        public override void BeginWork()
        {
            VD.Open(VideoPath);

            //Should resume to the next frame (0 if at beggining)
            var resumeFrame = MostRecentFrameIndex + 1.0;

            SeekTo(resumeFrame / VD.VideoInfo.TotalFrames);
            
            VD.Start();
        }

        public override bool IsCurrentItemReadyForWork()
        {
            //VideoDecoder places Frame in queue and sets decoded flag
            return CurrentItem != null && CurrentItem.IsDecoded;
        }

        public override void WorkOnCurrentItem()
        {
            Processor.OnProcessing(CurrentItem);

            CurrentItem.Watch.Stop();

            //Retain location of last processed frame
            MostRecentFrameIndex = CurrentItem.FrameIndex;

            //Mark frame finished
            CurrentItem.IsProcessed = true;
        }

        public override void FinishWork()
        {
            VD.Stop();
            MostRecentFrameIndex = -1;
        }

        public void SeekTo(double percentLocation)
        {
            MostRecentFrameIndex = (int)(Math.Round(VD.VideoInfo.TotalFrames * percentLocation, 0));

            VD.SeekTo((float)(VD.VideoInfo.Duration.TotalSeconds * percentLocation));
        }
    }
}
