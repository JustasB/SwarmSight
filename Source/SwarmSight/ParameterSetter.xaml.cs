using SwarmSight.Filters;
using SwarmSight.VideoPlayer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DPoint = System.Drawing.Point;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for ParameterSetter.xaml
    /// </summary>
    public partial class ParameterSetter : Window
    {
        public DPoint Offset;
        public DPoint Dims;
        public string Path;

        private WriteableBitmap bmpSource;
        private Frame allFrames;
        private Background frames = new MedianBackground(15);

        public ParameterSetter()
        {
            InitializeComponent();
        }

        public void Init()
        {
            var player = new VideoDecoder();
            player.Open(Path);
            player.PlayerOutputWidth = player.VideoInfo.Width;
            player.PlayerOutputHeight = player.VideoInfo.Height;

            //Play 15 equally spaced frames from the video
            player.Start(customArgs: "-r " + 15.0 / player.VideoInfo.Duration.TotalSeconds);

            var index = 0;

            allFrames = new Frame(Dims.X * 5, Dims.X * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, false);

            bmpSource = new WriteableBitmap(allFrames.Width, allFrames.Height, 94, 94, PixelFormats.Bgr24, null);
            canvas.Source = bmpSource;

            Dispatcher.InvokeAsync(new Action(() => 
            { 
                while (index < 15)
                {
                    var frame = player.PlayNextFrame();

                    if (frame != null)
                    {
                        var subclip = frame.SubClipped(Offset.X, Offset.Y, Dims.X, Dims.Y);

                        frames.Append(subclip);

                        allFrames.DrawFrame(subclip, Dims.X * (index % 5), Dims.Y * (index / 5), 1, 0);

                        if(index == 14)
                            allFrames.DrawFrame(frames.Model, Dims.X * (index % 5), Dims.Y * (index / 5), 1, 0);

                        index++;
                    }

                    allFrames.CopyToWriteableBitmap(bmpSource);
                    Thread.Sleep(1);
                }
            }));
        }
        
        private void contrastThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (allFrames == null)
                return;

            var result = allFrames.ContrastFilter(0.2f, (float)contrastThreshold.Value);

            result.CopyToWriteableBitmap(bmpSource);
        }


    }
}
