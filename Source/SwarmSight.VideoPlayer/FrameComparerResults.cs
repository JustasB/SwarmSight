using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwarmSight.VideoPlayer;
using SwarmSight.Filters;

namespace Classes
{
    public class MotionProcessorResult
    {
        public int Threshold { get; set; }
        public int ChangedPixelsCount { get; set; }
        public TimeSpan FrameTime { get; set; }
        public int FrameIndex { get; set; }
        public List<Point> ChangedPixels { get; set; }
        public Frame Frame { get; set; }

        public MotionProcessorResult()
        {
            ChangedPixels = new List<Point>();
        }
    }
}