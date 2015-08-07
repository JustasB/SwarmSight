using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace SwarmVision.Models
{
    public class HeadView
    {
        public HeadModel Head;
        
        public static int Height = 83;
        public static int Width = 83;

        private const string HeadTemplatePath = @"Y:\Documents\Dropbox\Research\Christina Burden\firstHead.bmp";

        public HeadView(HeadModel head)
        {
            Head = head;
        }

        private static Bitmap headTemplate;

        public Bitmap Draw()
        {
            var result = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            using (var gfx = Graphics.FromImage(result))
            {
                int headHeight, headWidth, headTopY, midLineX = Width / 2;

                gfx.Clear(Color.White);

                //Draw head
                if (headTemplate == null)
                    headTemplate = new Bitmap(HeadTemplatePath);
                
                headHeight = headTemplate.Height;
                headWidth = headTemplate.Width;
                headTopY = (Height - headHeight) / 2;

                //Draw it at the center
                gfx.DrawImage(headTemplate, (Width - headWidth) / 2.0f, headTopY);
                

                //Mandibles
                DrawMandible(midLineX, headTopY, gfx, flip: true); //Right
                DrawMandible(midLineX, headTopY, gfx); //Left

                //Proboscis
                DrawProboscis(midLineX, headTopY, gfx);

                //Antenae;
                var headCenterY = headTopY + (headHeight/2);

                DrawAntena(midLineX, headCenterY, Head.RightAntena, gfx);
                DrawAntena(midLineX, headCenterY, Head.LeftAntena, gfx, true);
            }

            return result;
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

        private void DrawAntena(int headCenterX, int headCenterY, AntenaModel antena, Graphics gfx, bool flip = false)
        {
            var invertX = flip ? -1 : 1;

            var startX = headCenterX - (int)antena.Root.Start.X;
            var startY = headCenterY - (int)antena.Root.Start.Y;

            var endX = startX + (int)Math.Round(antena.Root.Length * Math.Sin(antena.Root.AngleRad) * invertX);
            var endY = startY - (int)Math.Round(antena.Root.Length * Math.Cos(antena.Root.AngleRad));

            gfx.DrawLine(new Pen(Color.Black, antena.Root.Thickness), startX, startY, endX, endY);

            var tipStartX = endX;
            var tipStartY = endY;

            //Tongue angle same as prob
            var tipEndX = tipStartX + (int)Math.Round(antena.Tip.Length * Math.Sin(antena.Root.AngleRad + antena.Tip.AngleRad) * invertX);
            var tipEndY = tipStartY - (int)Math.Round(antena.Tip.Length * Math.Cos(antena.Root.AngleRad + antena.Tip.AngleRad));

            gfx.DrawLine(new Pen(Color.Black, antena.Tip.Thickness), tipStartX, tipStartY, tipEndX, tipEndY);
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