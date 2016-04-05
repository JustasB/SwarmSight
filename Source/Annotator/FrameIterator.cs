using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwarmSight
{
    public class FrameIterator
    {
        public static int MaxUndos = 10;
        public static LinkedList<FrameIterator> PreviousStates = new LinkedList<FrameIterator>();

        public int FrameIndex;
        public int FrameCount;

        public int SequenceIndex;
        public int SequenceCount {  get { return FrameSequence.Count; } }

        /// <summary>
        /// Stores the frame indices/frame numbers
        /// </summary>
        public List<int> FrameSequence = new List<int>();

        public int BurstBeginSequenceIndex;
        public int BurstBeginFrameIndex;
        public int BurstPositionIndex;
        public int BurstPositionCount;

        public int BatchPartIndex;
        public int BatchPartCount;

        public event EventHandler Advanced;
        public event EventHandler EndOfBurst;
        public event EventHandler EndOfBurstAndSequence;

        public FrameIterator(int frameCount, int batchPartCount, int burstLength = 30)
        {
            FrameCount = frameCount;
            BurstPositionCount = burstLength;
            BatchPartCount = batchPartCount;
        }

        public void InitRandomSequence(int numRandomFrames, int beginFrame = 0, int? endFrame = null, int? seed = null)
        {
            var allFrames = Enumerable
                .Range(beginFrame, (endFrame != null ? endFrame.Value : FrameCount)+1-beginFrame)
                .ToList();

            var rand = seed != null ? new Random(seed.Value) : new Random();
            FrameSequence.Clear();

            while(FrameSequence.Count < numRandomFrames && allFrames.Count > 0)
            {
                var randomIndex = rand.Next(0, allFrames.Count);

                FrameSequence.Add(allFrames[randomIndex]);
                allFrames.RemoveAt(randomIndex);
            }

            FrameSequence.Sort();

            Init(FrameSequence[0]);
        }

        public void InitLinearSequence(int everyNthFrame, int beginFrame = 0, int? endFrame = null)
        {
            FrameSequence.Clear();

            var end = endFrame != null ? endFrame.Value : FrameCount-1;

            for (var i = beginFrame; i <= end; i+= everyNthFrame)
                FrameSequence.Add(i);

            Init(FrameSequence[0]);
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

            SaveForUndo();
        }

        private void SaveForUndo()
        {
            var state = (FrameIterator)MemberwiseClone();

            state.FrameSequence = FrameSequence.ToList();

            PreviousStates.AddLast(state);

            if (PreviousStates.Count > MaxUndos)
                PreviousStates.RemoveFirst();
        }

        public bool IsAtEndOfSequenceOrVideo
        {
            get
            {
                return SequenceIndex == SequenceCount - 1
                    || FrameIndex >= FrameCount - 1;
            }
        }

        public int FramesTillEndOfBurst
        {
            get
            {
                var burstLeft = BurstPositionCount - BurstPositionIndex;
                var sequenceLeft = SequenceCount - SequenceIndex;

                return Math.Min(burstLeft, sequenceLeft)-1;
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

            SaveForUndo();

            if (Advanced != null)
                Advanced(this, null);
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
        private void ResetBurst()
        {
            BurstPositionIndex = 0;
            SequenceIndex = BurstBeginSequenceIndex;
            FrameIndex = BurstBeginFrameIndex;
        }

        /// <summary>
        /// Moves the batch to the next part
        /// </summary>
        private void AdvanceBatchPart()
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
        private void ResetBatchPart()
        {
            BatchPartIndex = 0;
        }

        private void AdvanceBatch()
        {
            var nextBurstBeginSequenceIndex = Math.Min(SequenceCount-1, BurstBeginSequenceIndex + BurstPositionCount);

            BurstBeginSequenceIndex = nextBurstBeginSequenceIndex;
            BurstBeginFrameIndex = FrameSequence[BurstBeginSequenceIndex];
        }

        public static FrameIterator GetPreviousState()
        {
            var result = PreviousStates.Last.Value;

            PreviousStates.RemoveLast();

            return result;
        }
    }
}
