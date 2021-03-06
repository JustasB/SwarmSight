﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SwarmSight.Filters;
using SwarmSight.VideoPlayer;

namespace Classes
{
    public class OnFrameBufferReady : EventArgs
    {
        public LinkedList<Frame> FrameBuffer;
    }

    public class OnFrameReady : EventArgs
    {
        public Frame Frame;
    }

    public class OnFrameReadyToRender : EventArgs
    {
        public ComparedFrame Frame;
    }

    public class OnVideoEndedArgs : EventArgs
    {
    }

    public class FrameComparisonArgs : EventArgs
    {
        public MotionProcessorResult Results;
    }
}