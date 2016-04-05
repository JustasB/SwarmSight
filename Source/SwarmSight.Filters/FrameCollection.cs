using System;

namespace SwarmSight.Filters
{
    public class FrameCollection
    {
        public Frame ShapeData;
        public Frame MotionData;
        public Frame ColorData;

        public Frame Current;
        public Frame Prev1;
        public Frame Prev2;

        public void Dispose()
        {
            ShapeData.Dispose();
            MotionData.Dispose();
            ColorData.Dispose();

            Current.Dispose();
            Prev1.Dispose();
            Prev2.Dispose();
    }
    }
}