using System;

namespace SwarmSight.Filters
{
    public class FrameCollection
    {
        public Frame ShapeData;
        public Frame MotionData;
        public Frame ColorData;

        public void Dispose()
        {
            ShapeData.Dispose();
            MotionData.Dispose();
            ColorData.Dispose();
        }
    }
}