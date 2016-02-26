using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmVision
{
    public class FrameIterator
    {
        public int FrameIndex;
        public int FrameCount;

        public int SequenceIndex;
        public int SequenceCount {  get { return FrameSequence.Count; } }
        public List<int> FrameSequence = new List<int>();

        public int BurstBeginSequenceIndex;
        public int BurstBeginFrameIndex;
        public int BurstPositionIndex;
        public int BurstPositionCount;

        public int BatchPartIndex;
        public int BatchPartCount;

        public FrameIterator(int frameCount, int batchPartCount, int burstLength = 30)
        {
            FrameCount = frameCount;
            BurstPositionCount = burstLength;
            BatchPartCount = batchPartCount;
        }

        public void InitRandomSequence(int numRandomFrames, int beginFrame = 0)
        {
            var frameP = (double)numRandomFrames / FrameCount;
            var rand = new Random();

            FrameSequence.Clear();

            for (var i = 0; i < FrameCount; i++)
                if (rand.NextDouble() <= frameP)
                    FrameSequence.Add(i);

            Init(beginFrame);
        }

        public void InitLinearSequence(int everyNthFrame, int beginFrame = 0)
        {
            FrameSequence.Clear();

            for (var i = 0; i < FrameCount; i+= everyNthFrame)
                FrameSequence.Add(i);

            Init(beginFrame);
        }

        /// <summary>
        /// Sets up the iterator to begin batched-frame-iteration starting at the selected frame index
        /// </summary>
        private void Init(int frameIndex = 0)
        {
            var frameLocationInSequence = FrameSequence.BinarySearch(frameIndex);

            if (frameLocationInSequence < 0)
            {
                var closest = FrameSequence
                    .Select((f,i) => new { Dist = Math.Abs(f - frameIndex), index = i })
                    .OrderBy(f => f.Dist)
                    .First()
                    .index;

                SequenceIndex = closest;
            }
            else
                SequenceIndex = frameLocationInSequence;

            FrameIndex = BurstBeginFrameIndex = FrameSequence[SequenceIndex];
            BurstBeginSequenceIndex = SequenceIndex;

            ResetBurst();
            ResetBatchPart();
        }

        public bool IsAtEndOfSequenceOrVideo
        {
            get
            {
                return SequenceIndex == SequenceCount - 1
                    || FrameIndex >= FrameCount - 1;
            }
        }

        /// <summary>
        /// Moves one position up the burst. This should be called after checking IsAtEndOfBurst first
        /// </summary>
        public void AdvanceBurst()
        {
            if (!IsAtEndOfBurstOrSequenceOrVideo)
            {
                BurstPositionIndex++;
                SequenceIndex++;
                FrameIndex = FrameSequence[SequenceIndex];
            }

            else //At end of burst
            {
                if (!IsAtEndOfBatch)
                    AdvanceBatchPart();

                else //at end of batch
                {
                    ResetBatchPart();

                    if(!IsAtEndOfSequenceOrVideo)
                        AdvanceBatch();
                }

                ResetBurst();
            }
        }

        /// <summary>
        /// Returns true if the current burst position is the last position within the burst
        /// </summary>
        public bool IsAtEndOfBurstOrSequenceOrVideo
        {
            get
            {
                return
                    BurstPositionIndex == BurstPositionCount - 1
                    || IsAtEndOfSequenceOrVideo
                ;
            }
        }

        /// <summary>
        /// Moves the burst position to the beggining of the burst 
        /// </summary>
        public void ResetBurst()
        {
            BurstPositionIndex = 0;
            SequenceIndex = BurstBeginSequenceIndex;
            FrameIndex = BurstBeginFrameIndex;
        }

        /// <summary>
        /// Moves the batch to the next part
        /// </summary>
        public void AdvanceBatchPart()
        {
            BatchPartIndex++;
        }

        /// <summary>
        /// Returns true if at the last part of the batch
        /// </summary>
        public bool IsAtEndOfBatch { get { return BatchPartIndex == BatchPartCount - 1; } }

        /// <summary>
        /// Moves the burst position to the beggining of the burst 
        /// </summary>
        public void ResetBatchPart()
        {
            BatchPartIndex = 0;
        }

        public void AdvanceBatch()
        {
            var nextBurstBeginSequenceIndex = Math.Min(SequenceCount-1, BurstBeginSequenceIndex + BurstPositionCount);

            BurstBeginSequenceIndex = nextBurstBeginSequenceIndex;
            BurstBeginFrameIndex = FrameSequence[BurstBeginSequenceIndex];
        }


    }
}
