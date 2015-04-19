using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SwarmVision.VideoPlayer
{
    internal class BitmapManipulator
    {
        public BitmapManipulator()
        {
            int imageWidth = 1920;
            int imageHeight = 1080;

            PixelFormat fmt = PixelFormat.Format32bppRgb;
            int pixelFormatSize = Image.GetPixelFormatSize(fmt);

            int stride = imageWidth*pixelFormatSize;
            dynamic padding = 32 - (stride%32);
            if (padding < 32)
                stride += padding;

            int[] pixels = new int[(stride/32)*imageHeight + 1];
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            IntPtr addr = Marshal.UnsafeAddrOfPinnedArrayElement(pixels, 0);

            Bitmap bitmap = new Bitmap(imageWidth, imageHeight, stride/8, fmt, addr);
        }


        public void Brightness(ref int[] pixels, float scale)
        {
            int r = 0;
            int g = 0;
            int b = 0;
            int mult = Convert.ToInt32(1024f*scale);
            int pixel = 0;

            for (int i = 0; i <= pixels.Length - 1; i++)
            {
                pixel = pixels[i];
                r = pixel & 255;
                g = (pixel >> 8) & 255;
                b = (pixel >> 16) & 255;

                //brightness calculation
                //shift right by 10 <=> divide by 1024
                r = (r*mult) >> 10;
                g = (g*mult) >> 10;
                b = (b*mult) >> 10;

                //clamp to between 0 and 255
                if (r < 0)
                    r = 0;
                if (g < 0)
                    g = 0;
                if (b < 0)
                    b = 0;
                r = (r & 255);
                g = (g & 255);
                b = (b & 255);

                pixels[i] = r | (g << 8) | (b << 16) | 0;
            }
        }
    }
}