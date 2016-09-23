using Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmSight.VideoPlayer.Pipeline
{
    public class RendererWorker : PipelineWorker
    {
        public event EventHandler<OnFrameReady> FrameReady;

        public override void BeginWork()
        {

        }

        public override void FinishWork()
        {
            
        }

        public override bool IsCurrentItemReadyForWork()
        {
            return CurrentItem != null && CurrentItem.IsProcessed && CurrentItem.IsReadyForRender;
        }

        public override void WorkOnCurrentItem()
        {
            if (FrameReady != null)
                FrameReady(this, new OnFrameReady() { Frame = CurrentItem });

            Queue.RemoveFirst();
        }
    }
}
