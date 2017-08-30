using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Math;
using DPoint = System.Drawing.Point;
using WPoint = System.Windows.Point;

namespace SwarmSight.HeadPartsTracking
{
    public class SpacePoint
    {
        public SpaceManager Space;
        public SpaceManager ModelSpace;

        /// <summary>
        /// Coordinates within FrameSpace are X,Y video coordinates within a video frame. eg. (0,0) is the top left corner of the video.
        /// </summary>
        public DPoint FramePoint;

        /// <summary>
        /// Coordinates within SubclippedSpace are video coordinates relative to the top left corner of the "Antenna Sensor" widget.
        /// </summary>
        public DPoint SubclippedPoint;

        /// <summary>
        /// Coordinates within the StandardSpace are video coordinates relative to a 100px by 100px "standard" Antenna Sensor widget.
        /// </summary>
        public DPoint StandardPoint;

        /// <summary>
        /// Coordinates within the PriorSpace are coordinates relative to the scaled and rotated midpoint between the scapes of bees during recording conditions of the prior hulls. 
        /// e.g. (0,0) is the midpoint between the antenna scapes (the center), and (0,100) is in front of the animal 100 pixels away from the center.
        /// </summary>
        public WPoint PriorPoint;

        /// <summary>
        /// Coordinates within ModelSpace are relative to the top left coordinates of a 100x100 upright oriented model of the head.
        /// </summary>
        public DPoint ModelPoint;

        public SpacePoint(SpaceManager spaceManager)
        {
            Space = spaceManager;

            ModelSpace = new SpaceManager()
            {
                ScaleX = 1,
                ScapeDistanceAtScale1 = spaceManager.ScapeDistanceAtScale1
            };
        }

        public SpacePoint FromStandardSpace(DPoint source)
        {
            StandardPoint = source;
            SubclippedPoint = Space.ToSubclippedSpaceFromStandardSpace(StandardPoint);
            PriorPoint = Space.ToPriorSpaceFromSubclippedSpace(SubclippedPoint);
            FramePoint = Space.ToFrameSpaceFromSubclippedSpace(SubclippedPoint);

            ModelPoint = ModelSpace.ToStandardSpaceFromSubclippedSpace(ModelSpace.ToSubclippedSpaceFromPriorSpace(PriorPoint));

            return this;
        }

        public SpacePoint FromPriorSpace(WPoint source)
        {
            PriorPoint = source;
            SubclippedPoint = Space.ToSubclippedSpaceFromPriorSpace(source);
            StandardPoint = Space.ToStandardSpaceFromSubclippedSpace(SubclippedPoint);
            FramePoint = Space.ToFrameSpaceFromSubclippedSpace(SubclippedPoint);

            ModelPoint = ModelSpace.ToStandardSpaceFromSubclippedSpace(ModelSpace.ToSubclippedSpaceFromPriorSpace(PriorPoint));

            return this;
        }

        public SpacePoint FromFrameSpace(DPoint source)
        {
            FramePoint = source;
            SubclippedPoint = Space.ToSubclippedSpaceFromFrameSpace(source);
            StandardPoint = Space.ToStandardSpaceFromSubclippedSpace(SubclippedPoint);
            PriorPoint = Space.ToPriorSpaceFromSubclippedSpace(SubclippedPoint);

            ModelPoint = ModelSpace.ToStandardSpaceFromSubclippedSpace(ModelSpace.ToSubclippedSpaceFromPriorSpace(PriorPoint));

            return this;
        }
    }
}
