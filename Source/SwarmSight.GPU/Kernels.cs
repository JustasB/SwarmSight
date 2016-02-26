using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cudafy;
using Cudafy.Atomics;

namespace SwarmSight.Hardware
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

                //if (distanceToLine > segment.Thickness.Value) continue;

                //target[idx] = segment.ColorB;
                //target[idx + 1] = segment.ColorG;
                //target[idx + 2] = segment.ColorR;

                //break; //If pixel belongs to one segment, don't bother painting it again
            }
        }



        /// <summary>
        /// Rotates a sequence of images, each layed out side by side, with allocated blank space in the second half for the rotated results
        /// </summary>
        [Cudafy]
        public static void RepeatedRotateScaleKernel(GThread thread,
            byte[] target, float[,] transforms,
            int unitStride, int unitWidth, int unitHeight,
            int targetStride, int targetWidth, int targetHeight)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            var item = x / unitWidth; //Item is based on how far right we are in the buffer
            var xOffset = item * unitWidth;
            var itemAngle = transforms[0, item];
            var itemScale = transforms[1, item];

            RotateScaleKernel(thread, target, target, targetStride, 
                unitWidth, unitHeight, targetWidth, xOffset, targetWidth, 
                itemAngle, itemScale);
        }

        [Cudafy]
        public static void RotateScaleKernel(GThread thread, byte[] result, byte[] target, int stride,
                                        int imageWidth, int imageHeight, int widthLimit, float sourceXOffset, float resultXOffset,
                                        float angle, float scale)
        {
            // compute thread dimension
            float x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            float y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= widthLimit || y >= imageHeight)
                return;

            //Compute source address, assuming center of image is the origin
            float xA = (x - imageWidth / 2.0f) - sourceXOffset;
            float yA = (y - imageHeight / 2.0f);

            float xR = GMath.Floor(1.0f / scale * (xA * GMath.Cos(-angle) - yA * GMath.Sin(-angle)));
            float yR = GMath.Floor(1.0f / scale * (xA * GMath.Sin(-angle) + yA * GMath.Cos(-angle)));

            float src_x = xR + imageWidth / 2.0f + sourceXOffset;
            float src_y = yR + imageHeight / 2.0f;

            float minX = sourceXOffset;
            float maxX = sourceXOffset + imageWidth;           

            //Interpolate, as long as the source px is on screen
            if (src_x >= minX && src_x < maxX && src_y >= 0.0f && src_y < imageHeight)
            {
                // BI - LINEAR INTERPOLATION
                float src_x0 = (int)(src_x);
                float src_x1 = (src_x0 + 1);
                float src_y0 = (int)(src_y);
                float src_y1 = (src_y0 + 1);

                //Get remainders
                float sx = (src_x - src_x0);
                float sy = (src_y - src_y0);

                //Make sure source pixels are within image
                src_x0 = GMath.Min(GMath.Max(minX, src_x0), maxX);
                src_x1 = GMath.Min(GMath.Max(minX, src_x1), maxX);
                src_y0 = GMath.Min(GMath.Max(0f, src_y0), imageHeight);
                src_y1 = GMath.Min(GMath.Max(0f, src_y1), imageHeight);

                int idx_src00 = (int)(3 * src_x0 + src_y0 * stride);
                int idx_src10 = (int)(3 * src_x1 + src_y0 * stride);
                int idx_src01 = (int)(3 * src_x0 + src_y1 * stride);
                int idx_src11 = (int)(3 * src_x1 + src_y1 * stride);

                //Debug paint source pix green
                //result[idx_src00 + 1] = 255; 

                int idx = (int)(3 * (x + resultXOffset) + y * stride);

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

            //Paint dest px blue
            //int idx = (int)(3 * (x + resultXOffset) + y * stride);
            //target[idx] = 255;
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

            var B = top[topIndex];
            var G = top[topIndex + 1];
            var R = top[topIndex + 2];

            if (B > 3 || G > 3 || R > 3)
            {
                target[targetIndex] = B;
                target[targetIndex + 1] = G;
                target[targetIndex + 2] = R;
            }
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
            //Store px sums in shared memory
            var pixelSums = thread.AllocateShared<int>("pixelSums", (int)Filters.BlockSideLength, (int)Filters.BlockSideLength);
            pixelSums[thread.threadIdx.x, thread.threadIdx.y] = 0;

            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x < patternWidth && y < patternHeight)
            {
                var sourceIndex = 3 * (x + sourceStartX) + (y + sourceStartY) * sourceStride;
                var patternIndex = 3 * x + y * patternStride;

                //Ignore if close to black
                if (pattern[patternIndex] > 3)
                {
                    pixelSums[thread.threadIdx.x, thread.threadIdx.y] =
                        Distance(pattern[patternIndex], source[sourceIndex]) +
                        Distance(pattern[patternIndex + 1], source[sourceIndex + 1]) +
                        Distance(pattern[patternIndex + 2], source[sourceIndex + 2]);
                }
            }

            //Wait till all block threads have finished
            thread.SyncThreads();

            //Use the first thread of each block to add up the block sums. CPU will need to add up the grid sum.
            if (thread.threadIdx.x == 0 && thread.threadIdx.y == 0)
            {
                int blockSum = 0;

                //Add up the pixel sums to a block sum
                for (var ty = 0; ty < (int)Filters.BlockSideLength; ty++)
                    for (var tx = 0; tx < (int)Filters.BlockSideLength; tx++)

                        blockSum += pixelSums[tx, ty];

                //Store the block's sum
                blockSums[thread.blockIdx.x, thread.blockIdx.y] = blockSum;
            }

            //var window = (int)Filters.BlockSideLength / 2;
            
            //while (window > 0)
            //{
            //    if (thread.threadIdx.x < window)
            //        pixelSums[thread.threadIdx.x, thread.threadIdx.y] += pixelSums[thread.threadIdx.x+window, thread.threadIdx.y];

            //    window /= 2;

            //    thread.SyncThreads();
            //}

            //if (thread.threadIdx.x == 0 && thread.threadIdx.y == 0)
            //{
            //    int blockSum = 0;

            //    for (var ty = 0; ty < (int)Filters.BlockSideLength; ty++)
            //        blockSum += pixelSums[0, ty];

            //    //Store the block's sum
            //    blockSums[thread.blockIdx.x, thread.blockIdx.y] = blockSum;
            //}
        }

        /// <summary>
        /// Launch this relative to the pattern. And ensure blockSums is grid sized.
        /// </summary>
        [Cudafy]
        public static void RepeatedAverageColorDifferenceKernel(GThread thread,
            byte[] source, int sourceStride, int sourceWidth, int sourceHeight,
            byte[] patterns, int patternsStride, int patternsWidth, int patternsHeight, int patternsOffsetX,
            int patternStride, int patternWidth, int patternHeight,
            int[,] patternLocations, int[,,] blockAvgs)
        {

            var threadX = thread.threadIdx.x;
            var threadY = thread.threadIdx.y;

            int x = thread.blockIdx.x * thread.blockDim.x + threadX;
            int y = thread.blockIdx.y * thread.blockDim.y + threadY;

            //Store px sums in shared memory
            var pixelAvgDistance = thread.AllocateShared<float>("pixelSums", (int)Filters.BlockSideLength, (int)Filters.BlockSideLength);
            pixelAvgDistance[threadX, threadY] = 0;
            
            var patternId = x / patternWidth;
            var sourceStartX = patternLocations[patternId, 0];
            var sourceStartY = patternLocations[patternId, 1];

            if (x < patternsWidth && y < patternsHeight)
            {
                var patternIndex = 3 * (x + patternsOffsetX) + y * patternsStride;
                var sourceIndex = 3 * ((x % patternWidth) + sourceStartX) + (y + sourceStartY) * sourceStride;

                pixelAvgDistance[threadX, threadY] = ComputePixelError(patterns, patternIndex, source, sourceIndex);
                
                //Paint pattern blue with the overlap average
                //patterns[patternIndex] = (byte)pixelAvgDistance[threadX, threadY];
            }

            

            //Wait till all block threads have finished
            thread.SyncThreads();

            //Use the first thread of each block to add up the block sums. CPU will need to add up the grid sum.
            if (threadX == 0 && threadY == 0)
            {
                float thisBlockAvg = 0;
                float nextBlockAvg = 0;

                //Add up the pixel sums to a block sum
                for (var ty = 0; ty < (int)Filters.BlockSideLength; ty++)
                {
                    for (var tx = 0; tx < (int)Filters.BlockSideLength; tx++)
                    {
                        if ((x + tx) / patternWidth == x / patternWidth)
                        {
                            thisBlockAvg += pixelAvgDistance[tx, ty];
                        }
                        else
                        {
                            nextBlockAvg += pixelAvgDistance[tx, ty];
                        }
                    }
                }

                var blocksPerPattern = (int)GMath.Ceiling(patternWidth / thread.blockDim.x);

                //Store the block's avgs
                if (thisBlockAvg > 0)
                    blockAvgs[patternId, thread.blockIdx.x % blocksPerPattern, thread.blockIdx.y] = (int)GMath.Round(thisBlockAvg);
                

                if (nextBlockAvg > 0)
                {
                    var isLastLastPattern = (patternsWidth / patternWidth) - 1 == patternId;

                    //Last block of this pattern is the first block of next pattern
                    if (!isLastLastPattern)
                        blockAvgs[patternId + 1, 0, thread.blockIdx.y] = (int)GMath.Round(nextBlockAvg);
                }
            }
        }

        [Cudafy]
        private static float ComputePixelError(byte[] patterns, int patternIndex, byte[] source, int sourceIndex)
        {
            return NonBlackAvgLinearDistance(patterns, patternIndex, source, sourceIndex);
        }

        [Cudafy]
        private static float NonBlackAvgLinearDistance(byte[] patterns, int patternIndex, byte[] source, int sourceIndex)
        {
            if (patterns[patternIndex] > 3)
            {
                var distA = Distance(patterns[patternIndex    ], source[sourceIndex    ]);
                var distB = Distance(patterns[patternIndex + 1], source[sourceIndex + 1]);
                var distC = Distance(patterns[patternIndex + 2], source[sourceIndex + 2]);

                return (distA + distB + distC) / 3f;
            }

            return 0;
        }

        [Cudafy]
        private static float SquareDistance(byte[] patterns, int patternIndex, byte[] source, int sourceIndex)
        {
            var distA = SquareOf(Distance(patterns[patternIndex], source[sourceIndex]));
            var distB = SquareOf(Distance(patterns[patternIndex + 1], source[sourceIndex + 1]));
            var distC = SquareOf(Distance(patterns[patternIndex + 2], source[sourceIndex + 2]));

            return (distA + distB + distC) / 3.0f;
        }

        [Cudafy]
        private static float GrossColorMatch(byte[] patterns, int patternIndex, byte[] source, int sourceIndex)
        {
            if ((patterns[patternIndex] > 200 && source[sourceIndex] > 200) ||
                (patterns[patternIndex] < 100 && source[sourceIndex] < 100))
                return 0;

            return 1;
        }

        [Cudafy]
        private static float SquareOf(float value)
        {
            return value * value;
        }

        [Cudafy]
        [CudafyInline(eCudafyInlineMode.Force)]
        public static int Distance(byte a, byte b)
        {
            if (a > b)
                return a - b;

            return b - a;
        }

        [Cudafy]
        public static void EmptyKernel(GThread thread, int param)
        {
            
        }

        /// <summary>
        /// Repeatedly copies a one array into result sequentialy. Result must be multiple sized of source.
        /// </summary>
        [Cudafy]
        public static void RepeatCopyKernel(GThread thread,
            byte[] source, int stride, int sourceWidth, int sourceHeight,
            byte[] result, int resultStride, int resultWidth, int resultHeight)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= sourceWidth || y >= sourceHeight)
                return;

            //Copy source into multiple blocks

            var sourceIndex = 3 * x + y * stride;

            var sourceValueA = source[sourceIndex  ];
            var sourceValueB = source[sourceIndex+1];
            var sourceValueC = source[sourceIndex+2];

            var times = resultWidth / sourceWidth;

            for(var f = 0; f < times; f++)
            {
                var resultIndex = (3 * x + f*stride) + resultStride * y;

                result[resultIndex  ] = sourceValueA;
                result[resultIndex+1] = sourceValueB;
                result[resultIndex+2] = sourceValueC;
            }
        }

        /// <summary>
        /// Repeatedly copies a one array into result sequentialy. Result must be multiple sized of source.
        /// </summary>
        [Cudafy]
        public static void RepeatCopyKernelParallel(GThread thread,
            byte[] source, int stride, int sourceWidth, int sourceHeight,
            byte[] result, int resultStride, int resultWidth, int resultHeight)
        {
            int x = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;
            int y = thread.blockIdx.y * thread.blockDim.y + thread.threadIdx.y;

            if (x >= resultWidth || y >= resultHeight)
                return;

            //Copy in to this from the appropriate place in source

            var sourceIndex = 3 * (x % sourceWidth) + y * stride;
            var resultIndex = 3 * x + y * resultStride;
            
            result[resultIndex    ] = source[sourceIndex  ];
            result[resultIndex + 1] = source[sourceIndex+1];
            result[resultIndex + 2] = source[sourceIndex+2];
        }

        [Cudafy]
        public static void MinDistanceToSegmentsKernel(GThread thread,
            float[] distances, float[,] individuals, int indivCount, float[,] points, int pointCount)
        {
            var i = thread.blockIdx.x * thread.blockDim.x + thread.threadIdx.x;

            if (i >= indivCount)
                return;

            var minSum = 0.0f;

            var X1 = individuals[i, 0];
            var X2 = individuals[i, 1];
            var X3 = individuals[i, 2];
            var X4 = individuals[i, 3];
            var X5 = individuals[i, 4];
            var X6 = individuals[i, 5];

            var Y1 = individuals[i, 0+6];
            var Y2 = individuals[i, 1+6];
            var Y3 = individuals[i, 2+6];
            var Y4 = individuals[i, 3+6];
            var Y5 = individuals[i, 4+6];
            var Y6 = individuals[i, 5+6];
            
            for (var p = 0; p < pointCount; p++)
            {
                var X = points[p, 0];
                var Y = points[p, 1];

                var D1 = DistToSegmentSquared(X, Y, X1, Y1, X2, Y2);
                var D2 = DistToSegmentSquared(X, Y, X2, Y2, X3, Y3);
                var D3 = DistToSegmentSquared(X, Y, X4, Y4, X5, Y5);
                var D4 = DistToSegmentSquared(X, Y, X5, Y5, X6, Y6);

                var distSquared = GMath.Min(GMath.Min(D1, D2), GMath.Min(D3, D4));

                minSum += GMath.Sqrt(distSquared);
            }

            var L1 = GMath.Sqrt(SquareOf(X1- X2) + SquareOf(Y1-Y2));
            var L2 = GMath.Sqrt(SquareOf(X2- X3) + SquareOf(Y2-Y3));
            var L3 = GMath.Sqrt(SquareOf(X4- X5) + SquareOf(X4-X5));
            var L4 = GMath.Sqrt(SquareOf(X5- X6) + SquareOf(X5-X6));

            distances[i] = minSum + L1 + L2 + L3 + L4;
        }

        [Cudafy]
        public static float DistToSegmentSquared(float x, float y, float x1, float y1, float x2, float y2)
        {
            var A = x - x1;
            var B = y - y1;
            var C = x2 - x1;
            var D = y2 - y1;

            var lenSq = C * C + D * D;

            var param = lenSq > 0 ? (A * C + B * D) / lenSq : -1;

            var xx = param >= 0 && param <= 1 ? x1 + param * C : (param < 0 ? x1 : x2);
            var yy = param >= 0 && param <= 1 ? y1 + param * D : (param < 0 ? y1 : y2);

            var dx = x - xx;
            var dy = y - yy;

            return (dx * dx + dy * dy); //Math.Sqrt
        }
    }
}
