using SwarmSight.Filters;

namespace SwarmSight.VideoPlayer
{
    public abstract class VideoProcessorBase
    {
        public virtual Frame OnAfterDecoding(Frame frame)
        {
            return frame;
        }

        public abstract void OnProcessing(Frame frame);
    }
}
