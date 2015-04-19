using SwarmVision.VideoPlayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Classes
{
    public class PixelShader
    {
        public Color InsideColor = Color.Blue;
        public Color OutsideColor = Color.Yellow;

        public unsafe void Shade(Frame frame, List<Point> list, int shaderRadius)
        {
            //Performance optimizations
            var insideR = InsideColor.R;
            var insideG = InsideColor.G;
            var insideB = InsideColor.B;

            var outsideR = OutsideColor.R;
            var outsideG = OutsideColor.G;
            var outsideB = OutsideColor.B;


            var bytes = frame.FirstPixelPointer;
            var width = frame.Width;
            var height = frame.Height;

            //Do each pixel in parallel
            Parallel.ForEach<Point>(list, new ParallelOptions() {/*MaxDegreeOfParallelism = 1*/}, (Point p) =>
                {
                    var x = p.X;
                    var y = p.Y;

                    //Draw rectangular highlights of specified radius
                    var highlightStartX = Math.Max(0, x - (shaderRadius));
                    var highlightEndX = Math.Min(width, x + shaderRadius);
                    var highlightStartY = Math.Max(0, y - (shaderRadius));
                    var highlightEndY = Math.Min(height, y + shaderRadius);

                    for (var highLightX = highlightStartX; highLightX < highlightEndX; highLightX++)
                    {
                        for (var highLightY = highlightStartY; highLightY < highlightEndY; highLightY++)
                        {
                            var offset = 3*(highLightX + width*highLightY);

                            if (highLightX == highlightStartX || highLightX == highlightEndX - 1 ||
                                highLightY == highlightStartY || highLightY == highlightEndY - 1)
                            {
                                bytes[offset + 2] = outsideR;
                                bytes[offset + 1] = outsideG;
                                bytes[offset + 0] = outsideB;
                            }
                            else
                            {
                                bytes[offset + 2] = insideR;
                                bytes[offset + 1] = insideG;
                                bytes[offset + 0] = insideB;
                            }
                        }
                    }
                });
        }
    }
}