using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using SwarmVision.Filters;
using SwarmVision.Hardware;

namespace SwarmVision.Models
{
    public class HeadView
    {
        public HeadModel Head;
        
        public static int Height = 83;
        public static int Width = 83;

        private const string HeadTemplatePath = @"Y:\Documents\Dropbox\Research\Christina Burden\firstHead2.bmp";
        private const string AntennaTemplatePath = @"Y:\Documents\Dropbox\Research\Christina Burden\antennaSegmentTemplate.png";

        public HeadView(HeadModel head)
        {
            Head = head;
        }


        private static Frame headTemplate;
        private static Frame headAlone;

        public static Frame PlainHeadGPU
        {
            get
            {
                int headHeight, headWidth, headTopY, midLineX = Width / 2;

                //If head by itself has not been cached, create it once, and then cache it
                if (headAlone == null)
                {
                    var bmp = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

                    using (var gfx = Graphics.FromImage(bmp))
                    {
                        gfx.Clear(Color.Black);

                        //Draw head
                        if (headTemplate == null)
                            headTemplate = new Frame(HeadTemplatePath, false).EdgeFilter().ContrastFilter(1f, 10f);

                        headHeight = headTemplate.Height;
                        headWidth = headTemplate.Width;
                        headTopY = (Height - headHeight) / 2;

                        //Draw it at the center
                        gfx.DrawImage(headTemplate.Bitmap, (Width - headWidth) / 2.0f, headTopY);
                    }

                    headAlone = new Frame(bmp, true);
                }

                return headAlone;
            }
        }

        public Frame DrawGPU()
        {
            int headHeight, headWidth, headTopY, midLineX = Width / 2;
            
            var result = PlainHeadGPU.Clone();
            
            
            headHeight = headTemplate.Height;
            headWidth = headTemplate.Width;
            headTopY = (Height - headHeight) / 2;

            var segments = new List<LineSegment>();

            //Mandibles
            //DrawMandible(midLineX, headTopY, gfx, flip: true); //Right
            //DrawMandible(midLineX, headTopY, gfx); //Left

            //Proboscis
            //DrawProboscis(midLineX, headTopY, gfx);

            //Antenae;
            var headCenterY = headTopY + (headHeight / 2);

            //Right antena
            if (Head.RightAntena.Tip.Length > 0)
            {
                var rightRootStart = GetAntenaStart(midLineX, headCenterY, Head.RightAntena);
                var rightRootEnd = GetAntenaRootEnd(rightRootStart, Head.RightAntena);
                var rightTipEnd = GetAntenaTipEnd(rightRootEnd, Head.RightAntena);
                segments.Add(new LineSegment
                    {
                        Thickness = Head.RightAntena.Tip.Thickness,
                        Start = rightRootEnd,
                        End = rightTipEnd
                    });
            }

            //Left antena
            if (Head.LeftAntena.Tip.Length > 0)
            {
                var leftRootStart = GetAntenaStart(midLineX, headCenterY, Head.LeftAntena, true);
                var leftRootEnd = GetAntenaRootEnd(leftRootStart, Head.LeftAntena, true);
                var leftTipEnd = GetAntenaTipEnd(leftRootEnd, Head.LeftAntena, true);
                segments.Add(new LineSegment
                    {
                        Thickness = Head.LeftAntena.Tip.Thickness,
                        Start = leftRootEnd,
                        End = leftTipEnd
                    });
            }

            //Draw segments all at once
            result.DrawSegmentsGPU(segments);

            if (Head.Angle != 0 || Head.Scale != 1)
                return result.RotateScaleGPU(Head.Angle, Head.Scale);

            else
                return result;
        }

        public Frame Draw(bool storeOnGPU)
        {
            if (storeOnGPU)
                return DrawGPU();

            var result = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            using (var gfx = Graphics.FromImage(result))
            {
                int headHeight, headWidth, headTopY, midLineX = Width / 2;

                gfx.Clear(Color.Black);

                //Draw head
                if (headTemplate == null)
                    headTemplate = new Frame(HeadTemplatePath, false).EdgeFilter().ContrastFilter(1f,10f);
                    
                
                headHeight = headTemplate.Height;
                headWidth = headTemplate.Width;
                headTopY = (Height - headHeight) / 2;

                //Draw it at the center
                gfx.DrawImage(headTemplate.Bitmap, (Width - headWidth) / 2.0f, headTopY);
                

                //Mandibles
                //DrawMandible(midLineX, headTopY, gfx, flip: true); //Right
                //DrawMandible(midLineX, headTopY, gfx); //Left

                //Proboscis
                //DrawProboscis(midLineX, headTopY, gfx);

                //Antenae;
                var headCenterY = headTopY + (headHeight/2);

                DrawAntena(midLineX, headCenterY, Head.RightAntena, gfx);
                DrawAntena(midLineX, headCenterY, Head.LeftAntena,  gfx, true);
            }


            if (Head.Angle != 0 || Head.Scale != 1)
                using (var raw = new Frame(result, false))
                    return raw.RotateScale(Head.Angle, Head.Scale);

            else
                return new Frame(result, false);
        }   

        private void DrawProboscis(int midLineX, int headTopY, Graphics gfx)
        {
            var startX = midLineX - (int) Head.Proboscis.Proboscis.Start.X;
            var startY = headTopY - (int) Head.Proboscis.Proboscis.Start.Y;

            var endX = startX + (int) Math.Round(Head.Proboscis.Proboscis.Length*Math.Sin(Head.Proboscis.Proboscis.AngleRad));
            var endY = startY - (int) Math.Round(Head.Proboscis.Proboscis.Length*Math.Cos(Head.Proboscis.Proboscis.AngleRad));

            gfx.DrawLine(new Pen(Color.Black, Head.Proboscis.Proboscis.Thickness), startX, startY, endX, endY);

            var tongueStartX = endX;
            var tongueStartY = endY;

            //Tongue angle same as prob
            var tongueEndX = tongueStartX + (int)Math.Round(Head.Proboscis.Tongue.Length * Math.Sin(Head.Proboscis.Proboscis.AngleRad));
            var tongueEndY = tongueStartY - (int)Math.Round(Head.Proboscis.Tongue.Length * Math.Cos(Head.Proboscis.Proboscis.AngleRad));

            gfx.DrawLine(new Pen(Color.Black, Head.Proboscis.Tongue.Thickness), tongueStartX, tongueStartY, tongueEndX, tongueEndY);
        }

        private Point GetAntenaStart(int headCenterX, int headCenterY, AntenaModel antena, bool flip = false)
        {
            var invertX = flip ? -1 : 1;

            var startX = headCenterX + (int)antena.Root.Start.X * invertX;
            var startY = headCenterY + (int)antena.Root.Start.Y;

            return new Point(startX,startY);
        }

        private static Point GetAntenaRootEnd(Point start, AntenaModel antena, bool flip = false)
        {
            var invertX = flip ? -1 : 1;

            var endX = start.X + (int)Math.Round(antena.Root.Length * Math.Sin(antena.Root.AngleRad) * invertX);
            var endY = start.Y - (int)Math.Round(antena.Root.Length * Math.Cos(antena.Root.AngleRad));

            return new Point(endX, endY);
        }

        private Point GetAntenaTipEnd(Point start, AntenaModel antena, bool flip = false)
        {
            var invertX = flip ? -1 : 1;

            var tipX = start.X + (int)Math.Round(antena.Tip.Length * Math.Sin(antena.Root.AngleRad + antena.Tip.AngleRad) * invertX);
            var tipY = start.Y - (int)Math.Round(antena.Tip.Length * Math.Cos(antena.Root.AngleRad + antena.Tip.AngleRad));

            return new Point(tipX, tipY);
        }

        private void DrawAntena(int headCenterX, int headCenterY, AntenaModel antena, Graphics gfx, bool flip = false)
        {
            var rootStart = GetAntenaStart(headCenterX, headCenterY, antena, flip);
            var rootEnd = GetAntenaRootEnd(rootStart, antena, flip);

            if (antena.Root.Length > 0)
            {
                //Don't draw the first segment, just the second
                //gfx.DrawLine(new Pen(Color.White, antena.Root.Thickness), startX, startY, endX, endY);
                //gfx.TranslateTransform(startX, startY);
                //gfx.RotateTransform((float)(180 - antena.Root.Angle*-invertX));
                //gfx.DrawImage(antenaTemplate, (int)(-antena.Root.Thickness / 2.0), 0, antena.Root.Thickness, (int)antena.Root.Length);
                //gfx.ResetTransform();
            }

            var tipEnd = GetAntenaTipEnd(rootEnd, antena, flip);

            antena.TipX = tipEnd.X;
            antena.TipY = tipEnd.Y;

            if (antena.Tip.Length > 0)
            {
                gfx.DrawLine(new Pen(Color.White, antena.Tip.Thickness), rootEnd.X, rootEnd.Y, antena.TipX, antena.TipY);
                //gfx.TranslateTransform(tipStartX, tipStartY);
                //gfx.RotateTransform((float)(180 - antena.Root.Angle*-invertX - antena.Tip.Angle*-invertX));
                //gfx.DrawImage(antenaTemplate, (int)(-antena.Tip.Thickness / 2.0), 0, antena.Tip.Thickness, (int)antena.Tip.Length);
                //gfx.ResetTransform();
            }
        }

        private void DrawMandible(int midLine, int headTopY, Graphics gfx, bool flip = false)
        {
            var invert = flip ? -1 : 1;

            //Right mandible, origin is top of the head, midline
            var mandible = Head.Mandible;

            var startX = midLine + (int) Math.Round(mandible.Start.X) * invert;
            var startY = headTopY - (int) Math.Round(mandible.Start.Y);

            var endX = startX - (int) Math.Round(mandible.Length*Math.Cos(mandible.AngleRad)) * invert;
            var endY = startY - (int) Math.Round(mandible.Length*Math.Sin(mandible.AngleRad));

            gfx.DrawLine(new Pen(Color.Black, mandible.Thickness), startX, startY, endX, endY);
        }
    }
}