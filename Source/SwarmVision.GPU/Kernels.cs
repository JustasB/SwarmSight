using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cudafy;
using Cudafy.Atomics;

namespace SwarmVision.Hardware
{
    public class Kernels
    {
        public const int SegmentCacheSize = 10;
        /// <summary>
        /// GPU constant memory
        /// </summary>
        [Cudafy]
        public static float[,] SegmentCache = new float[SegmentCacheSize,10];

        [Cudafy]
        public static void DrawLinesKernel(GThread thread,
                                           byte[] target, int stride, int imageWidth, int imageHeight,
                                           int segmentCount)
        {
            // compute thread dimension
            float x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            float y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= imageWidth || y >= imageHeight)
                return;

            // compute target address
            int idx = (int)(3 * x + y * stride);

            for (var s = 0; s < segmentCount; s++)
            {
                //Check if pixel belongs to any of the segments, then color it

                var Dx = SegmentCache[s, 0];
                var Dy = SegmentCache[s, 1];
                var StartY = SegmentCache[s, 2];
                var StartX = SegmentCache[s, 3];
                var Product = SegmentCache[s, 4];

                //var segment = SegmentCache[s];


                //float distanceToLine;

                //if (segment.Dx == 0 && segment.Dy == 0)
                //    continue;

                //if (segment.Dx == 0) //Vertical
                //    distanceToLine = GMath.Abs((int)segment.StartY - y);

                //else if (segment.Dy == 0) //Horizontal
                //    distanceToLine = GMath.Abs((int)segment.StartX - x);

                //else
                //{
                //    //Fancy, fast line distance computations (search "Distance to line defined by two points formula")
                //    var numerator = segment.Dy * x - segment.Dx * y + segment.Product;
                //    if (numerator < 0) numerator *= -1;
                //    distanceToLine = numerator / segment.Length;
                //}

                //if (distanceToLine > segment.Thickness) continue;

                //target[idx] = segment.ColorB;
                //target[idx + 1] = segment.ColorG;
                //target[idx + 2] = segment.ColorR;

                //break; //If pixel belongs to one segment, don't bother painting it again
            }
        }

        [Cudafy]
        public static void RotateScaleKernel(GThread thread, byte[] result, byte[] target, int stride,
                                        int imageWidth, int imageHeight,
                                        float angle, float scale)
        {
            // compute thread dimension
            float x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            float y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= imageWidth || y >= imageHeight)
                return;

            //// compute target address
            int idx = (int)(3 * x + y * stride);

            float xA = (x - imageWidth / 2.0f);
            float yA = (y - imageHeight / 2.0f);

            float xR = GMath.Floor(1.0f / scale * (xA * GMath.Cos(-angle) - yA * GMath.Sin(-angle)));
            float yR = GMath.Floor(1.0f / scale * (xA * GMath.Sin(-angle) + yA * GMath.Cos(-angle)));

            float src_x = xR + imageWidth / 2.0f;
            float src_y = yR + imageHeight / 2.0f;

            if (src_x >= 0.0f && src_x < imageWidth && src_y >= 0.0f && src_y < imageHeight)
            {
                // BI - LINEAR INTERPOLATION
                float src_x0 = (int)(src_x);
                float src_x1 = (src_x0 + 1);
                float src_y0 = (int)(src_y);
                float src_y1 = (src_y0 + 1);

                float sx = (src_x - src_x0);
                float sy = (src_y - src_y0);


                int idx_src00 = (int)GMath.Min(GMath.Max(0.0f, 3 * src_x0 + src_y0 * stride), stride * imageHeight - 3.0f);
                int idx_src10 = (int)GMath.Min(GMath.Max(0.0f, 3 * src_x1 + src_y0 * stride), stride * imageHeight - 3.0f);
                int idx_src01 = (int)GMath.Min(GMath.Max(0.0f, 3 * src_x0 + src_y1 * stride), stride * imageHeight - 3.0f);
                int idx_src11 = (int)GMath.Min(GMath.Max(0.0f, 3 * src_x1 + src_y1 * stride), stride * imageHeight - 3.0f);


                result[idx] = (byte)(
                    (1.0f - sx) * (1.0f - sy) * target[idx_src00] +
                    (sx) * (1.0f - sy) * target[idx_src10] +
                    (1.0f - sx) * (sy) * target[idx_src01] +
                    (sx) * (sy) * target[idx_src11]
                );

                result[idx + 1] = (byte)(
                    (1.0f - sx) * (1.0f - sy) * target[idx_src00 + 1] +
                    (sx) * (1.0f - sy) * target[idx_src10 + 1] +
                    (1.0f - sx) * (sy) * target[idx_src01 + 1] +
                    (sx) * (sy) * target[idx_src11 + 1]
                );

                result[idx + 2] = (byte)(
                    (1.0f - sx) * (1.0f - sy) * target[idx_src00 + 2] +
                    (sx) * (1.0f - sy) * target[idx_src10 + 2] +
                    (1.0f - sx) * (sy) * target[idx_src01 + 2] +
                    (sx) * (sy) * target[idx_src11 + 2]
                );
            }
            else
            {
                result[idx] = result[idx + 1] = result[idx + 2] = 0;
            }
        }

        /// <summary>
        /// Rotates the Source by angle degrees and returns it in the Destination. 3 Bytes per pixel. Stride is the width of one row.
        /// </summary>
        [Cudafy]
        public static void SimpleRotateKernel(GThread thread, 
            byte[] destination, byte[] source, int stride, int imageWidth, int imageHeight, float deg)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= imageWidth || y >= imageHeight)
                return;

            int xc = imageWidth - imageWidth / 2;
            int yc = imageHeight - imageHeight / 2;

            int newx = (int)(((float)x - xc) * GMath.Cos(-deg) - ((float)y - yc) * GMath.Sin(-deg) + xc);
            int newy = (int)(((float)x - xc) * GMath.Sin(-deg) + ((float)y - yc) * GMath.Cos(-deg) + yc);

            if (newx >= 0 && newx < imageWidth && newy >= 0 && newy < imageHeight)
            {
                destination[(3 * x + y * stride)] = source[(3 * newx + newy * stride)];
                destination[(3 * x + y * stride) + 1] = source[(3 * newx + newy * stride) + 1];
                destination[(3 * x + y * stride) + 2] = source[(3 * newx + newy * stride) + 2];
            }
        }

        /// <summary>
        /// Applies an edge filter to the frame. Non-deterministic along the block boundaries.
        /// </summary>
        [Cudafy]
        public static void EdgeFilterKernel(GThread thread, 
            byte[] source, int stride, int imageWidth, int imageHeight, int filterSize)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= imageWidth || y >= imageHeight)
                return;

            var index = 3 * x + y * stride;

            //take the pixel above, left, right, below and compare to the middle pixel
            var colorDifference = 0.0;

            var xLeft = x - filterSize;
            var xRight = x + filterSize;
            var yUp = y - filterSize;
            var yDown = y + filterSize;

            var middleB = source[x * 3 + y * stride];
            var middleG = source[x * 3 + y * stride+1];
            var middleR = source[x * 3 + y * stride+2];

            if (xLeft >= 0)
                colorDifference +=
                                    GMath.Abs(middleB - source[xLeft * 3 + y * stride]) +
                                    GMath.Abs(middleG - source[xLeft * 3 + y * stride + 1]) +
                                    GMath.Abs(middleR - source[xLeft * 3 + y * stride + 2]);

            if (xRight < imageWidth)
                colorDifference +=
                                    GMath.Abs(middleB - source[xRight * 3 + y * stride]) +
                                    GMath.Abs(middleG - source[xRight * 3 + y * stride + 1]) +
                                    GMath.Abs(middleR - source[xRight * 3 + y * stride + 2]);

            if (yUp >= 0)
                colorDifference +=
                                    GMath.Abs(middleB - source[x * 3 + yUp * stride]) +
                                    GMath.Abs(middleG - source[x * 3 + yUp * stride + 1]) +
                                    GMath.Abs(middleR - source[x * 3 + yUp * stride + 2]);

            if (yDown < imageHeight)
                colorDifference +=
                                    GMath.Abs(middleB - source[x * 3 + yDown * stride]) +
                                    GMath.Abs(middleG - source[x * 3 + yDown * stride + 1]) +
                                    GMath.Abs(middleR - source[x * 3 + yDown * stride + 2]);

            //This will only work within blocks, pixels across block edge based on scheduling
            //That's ok for this task, as it will have small impact
            thread.SyncThreads();

            source[index] = source[index + 1] = source[index + 2] =
                (byte)(colorDifference / 12);
            
        }

        [Cudafy]
        public static void ContrastFilterKernel(GThread thread,
            byte[] source, int stride, int imageWidth, int imageHeight, float extent, float shift)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= imageWidth || y >= imageHeight)
                return;

            var index = 3 * x + y * stride;

            source[index    ] = (byte) (255 / (1 + (float)GMath.Exp(-extent * (source[index    ] - shift))));
            source[index + 1] = (byte) (255 / (1 + (float)GMath.Exp(-extent * (source[index + 1] - shift))));
            source[index + 2] = (byte) (255 / (1 + (float)GMath.Exp(-extent * (source[index + 2] - shift))));
        }

        /// <summary>
        /// Draws the top on top of target. Launch relative to top.
        /// </summary>
        [Cudafy]
        public static void DrawOnTopKernel(GThread thread,
            byte[] target, int targetStride, int targetWidth, int targetHeight,
            byte[] top, int topStride, int topWidth, int topHeight,
            int targetStartX, int targetStartY)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= topWidth || y >= topHeight)
                return;

            var targetX = x + targetStartX;
            var targetY = y + targetStartY;

            if (targetX >= targetWidth || targetY >= targetHeight)
                return;

            var targetIndex = 3 * (targetX) + (targetY) * targetStride;
            var topIndex = 3 * x + y * topStride;

            target[targetIndex    ] = top[topIndex];
            target[targetIndex + 1] = top[topIndex + 1];
            target[targetIndex + 2] = top[topIndex + 2];
        }

        /// <summary>
        /// Takes a subclip of the source. Launch this relative to the result. 
        /// </summary>
        [Cudafy]
        public static void SubClipKernel(GThread thread,
            byte[] source, int stride, int imageWidth, int imageHeight,
            byte[] result, int resultStride, int resultWidth, int resultHeight,
            int sourceStartX, int sourceStartY)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= resultWidth || y >= resultHeight)
                return;

            var sourceX = x + sourceStartX;
            var sourceY = y + sourceStartY;

            if (sourceX >= imageWidth || sourceY >= imageHeight)
                return;

            var sourceIndex = 3 * (sourceX) + (sourceY) * stride;
            var resultIndex = 3 * x + y * resultStride;

            result[resultIndex  ] = source[sourceIndex  ];
            result[resultIndex+1] = source[sourceIndex+1];
            result[resultIndex+2] = source[sourceIndex+2];
        }

        /// <summary>
        /// Launch this relative to the pattern. And ensure blockSums is grid sized.
        /// </summary>
        [Cudafy]
        public static void AverageColorDifferenceKernel(GThread thread,
            byte[] source, int sourceStride, int sourceWidth, int sourceHeight,
            byte[] pattern, int patternStride, int patternWidth, int patternHeight,
            int sourceStartX, int sourceStartY, int[,] blockSums)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= patternWidth || y >= patternHeight)
                return;

            var sourceIndex = 3 * (x + sourceStartX) + (y + sourceStartY) * sourceStride;
            var patternIndex = 3 * x + y * patternStride;

            //Store px sums in shared memory
            var pixelSums = thread.AllocateShared<int>("pixelSums", (int)Filters.BlockSideLength, (int)Filters.BlockSideLength);
            pixelSums[x, y] = 0;

            //Ignore if close to black
            if (pattern[patternIndex] > 3)
            {
                pixelSums[x,y] = (int) (
                    GMath.Abs(pattern[patternIndex    ] - source[sourceIndex    ]) +
                    GMath.Abs(pattern[patternIndex + 1] - source[sourceIndex + 1]) +
                    GMath.Abs(pattern[patternIndex + 2] - source[sourceIndex + 2])
                );
            }

            //Wait till all block threads have finished
            thread.SyncThreads();

            //Use the first thread to add up the block sums. CPU will need to add up the grid sum.
            if (x == 0 && y == 0)
            {
                for (var tx = 0; tx < thread.blockDim.x; tx++)
                    for (var ty = 0; ty < thread.blockDim.y; ty++)
                        thread.atomicAdd(ref blockSums[thread.blockIdx.x, thread.blockIdx.y], pixelSums[tx, ty]);
            }
        }
    }
}
