using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using SwarmVision.Filters;
using SwarmVision.Hardware;

namespace SwarmVision.HeadPartsTracking.Models
{
    public class HeadView
    {
        public HeadModel Head;
        
        public static int Height = 83;
        public static int Width = 83;


        public const int HeadWidth = 44;
        public const int HeadHeight = 47;
        public const int HeadLength = 35;//43; //Front to back distance of the head template
        public const int HeadOffsetX = 19;
        public const int HeadOffsetY = 18;

        public const string HeadTemplatePath = @"Y:\Documents\Dropbox\Research\Christina Burden\firstHead2.bmp";
        private const string AntennaTemplatePath = @"Y:\Documents\Dropbox\Research\Christina Burden\antennaSegmentTemplate.png";

        public HeadView(HeadModel head)
        {
            Head = head;
        }


        private static Frame _headTemplate;
        public static Frame HeadTemplate
        {
            get
            {
                //Draw head
                if (_headTemplate == null)
                    _headTemplate = new Frame(HeadTemplatePath, false);//.EdgeFilter().ContrastFilter(1f, 10f);

                return _headTemplate;
            }
        }
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

                        headHeight = HeadTemplate.Height;
                        headWidth = HeadTemplate.Width;
                        headTopY = (Height - headHeight) / 2;

                        //Draw it at the center
                        gfx.DrawImage(HeadTemplate.Bitmap, (Width - headWidth) / 2.0f, headTopY);
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
            
            
            headHeight = HeadTemplate.Height;
            headWidth = HeadTemplate.Width;
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
                        Thickness = (int)Head.RightAntena.Tip.Thickness,
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
                        Thickness = (int)Head.LeftAntena.Tip.Thickness,
                        Start = leftRootEnd,
                        End = leftTipEnd
                    });
            }

            //Draw segments all at once
            result.DrawSegmentsGPU(segments);

            throw new NotImplementedException();
            if (Head.Angle != 0 || Head.ScaleX != 1 || Head.ScaleY != 1)
                return result.RotateScaleGPU(Head.Angle, Head.ScaleX);

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
                int headHeight, headWidth, headTopY, midLineX = Width / 2, midlineY = Height / 2;

                gfx.Clear(Color.Black);    
                
                headHeight = HeadTemplate.Height;
                headWidth = HeadTemplate.Width;
                headTopY = (Height - headHeight) / 2;

                //Draw it at the center
                gfx.DrawImage(HeadTemplate.Bitmap, (Width - headWidth) / 2, headTopY);
                

                //Mandibles
                //DrawMandible(midLineX, headTopY, gfx, flip: true); //Right
                //DrawMandible(midLineX, headTopY, gfx); //Left

                //Proboscis
                DrawProboscis(midLineX, midlineY, gfx);

                //Antenae;
                var headCenterY = headTopY + (headHeight/2);

                DrawAntena(midLineX, headCenterY, Head.RightAntena, gfx);
                DrawAntena(midLineX, headCenterY, Head.LeftAntena,  gfx, true);
            }


            if (Head.Angle != 0 || Head.ScaleX != 1 || Head.ScaleY != 1)
                using (var raw = new Frame(result, false))
                    return raw.RotateScale(Head.Angle, Head.ScaleX, Head.ScaleY);

            else
                return new Frame(result, false);
        }

        public static int Denormalize(double value)
        {
            return (int) Math.Round(value / 100.0 * HeadLength);
        }

        private void DrawProboscis(int midLineX, int headTopY, Graphics gfx)
        {
            if (Head.Proboscis.Root.Length < 1)
                return;

            var startX = midLineX - (int) Head.Proboscis.Root.StartX;
            var startY = headTopY - (int) Head.Proboscis.Root.StartY;

            var endX = startX + (int) Math.Round(Head.Proboscis.Root.Length*Math.Sin(Head.Proboscis.Root.Angle.InRadians()));
            var endY = startY - (int) Math.Round(Head.Proboscis.Root.Length*Math.Cos(Head.Proboscis.Root.Angle.InRadians()));

            gfx.DrawLine(new Pen(Color.White, (int)Head.Proboscis.Root.Thickness), startX, startY, endX, endY);

            var tongueStartX = endX;
            var tongueStartY = endY;

            //Tongue angle same as prob
            var tongueEndX = tongueStartX + (int)Math.Round(Head.Proboscis.Tip.Length * Math.Sin(Head.Proboscis.Root.Angle.InRadians()));
            var tongueEndY = tongueStartY - (int)Math.Round(Head.Proboscis.Tip.Length * Math.Cos(Head.Proboscis.Root.Angle.InRadians()));

            gfx.DrawLine(new Pen(Color.White, (int)Head.Proboscis.Tip.Thickness), tongueStartX, tongueStartY, tongueEndX, tongueEndY);
        }

        private Point GetAntenaStart(int headCenterX, int headCenterY, TwoSegmentModel antena, bool flip = false)
        {
            var invertX = flip ? -1 : 1;

            var startX = headCenterX + (int)antena.Root.StartX * invertX;
            var startY = headCenterY + (int)antena.Root.StartY;

            return new Point(startX,startY);
        }

        private static Point GetAntenaRootEnd(Point start, TwoSegmentModel antena, bool flip = false)
        {
            var invertX = flip ? -1 : 1;

            var endX = start.X + (int)Math.Round(antena.Root.Length * Math.Sin(antena.Root.Angle.InRadians()) * invertX);
            var endY = start.Y - (int)Math.Round(antena.Root.Length * Math.Cos(antena.Root.Angle.InRadians()));

            return new Point(endX, endY);
        }

        private Point GetAntenaTipEnd(Point start, TwoSegmentModel antena, bool flip = false)
        {
            var invertX = flip ? -1 : 1;

            var tipX = start.X + (int)Math.Round(antena.Tip.Length * Math.Sin(antena.Root.Angle.InRadians() + antena.Tip.Angle.InRadians()) * invertX);
            var tipY = start.Y - (int)Math.Round(antena.Tip.Length * Math.Cos(antena.Root.Angle.InRadians() + antena.Tip.Angle.InRadians()));

            return new Point(tipX, tipY);
        }

        private void DrawAntena(int headCenterX, int headCenterY, TwoSegmentModel antena, Graphics gfx, bool flip = false)
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
                gfx.DrawLine(new Pen(Color.White, (int)antena.Tip.Thickness), rootEnd.X, rootEnd.Y, antena.TipX, antena.TipY);
                //gfx.TranslateTransform(tipStartX, tipStartY);
                //gfx.RotateTransform((float)(180 - antena.Root.Angle*-invertX - antena.Tip.Angle*-invertX));
                //gfx.DrawImage(antenaTemplate, (int)(-antena.Tip.Thickness / 2.0), 0, antena.Tip.Thickness, (int)antena.Tip.Length);
                //gfx.ResetTransform();
            }
        }
    }
}