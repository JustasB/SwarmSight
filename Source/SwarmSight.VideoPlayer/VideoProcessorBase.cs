using SwarmSight.Filters;

namespace SwarmSight.VideoPlayer
{
    public abstract class VideoProcessorBase
    {
        public virtual Frame OnAfterDecoding(Frame frame)
        {
            return frame;
        }

        public abstract object OnProcessing(Frame frame);

        //AFter decoding, apply some filters
        //During processing, do further manipulations to the frame, but also information about
        //the results, which get passed to the UI

        //Also there are parameters, which come from UI that need to be wired into the processor

        //For motion processor:
        //after decoding: nothing
        //during processing: save frame, compare to previous frame, store results of comparison, ie pixels found & count 

        //For per detector:
        //after decoding: edge filter, contrast filter
        //during processing: detect head, detect antena, detect per, return head model
        //UI shows antena location, original frame, and overpaint by the detected head

        //For painted antena processor:

        

    }
}
