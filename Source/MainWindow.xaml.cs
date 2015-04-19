using System.Linq;
using Classes;
using Justas.Bees.VideoPlayer;
using OxyPlot.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace SwarmVision
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        const string PlaySymbol = "4";
        const string PauseSymbol = ";";
        double _quality = 0.25;
        List<Point> _activity = new List<Point>();

        private MainViewModel _chart;
        private VideoDecoder _decoder;
        private FrameComparer _comparer;
        private FrameRenderer _renderer;

        public MainWindow()
        {
            InitializeComponent();
            
            ToggleCompare();
            SetupChart();
            SetupPlayer();
        }

        private void SetupChart()
        {
            _chart = new MainViewModel();
            _activity = new List<Point>(100);

            chartPlaceholder.Children.Add(new PlotView()
                {
                    Model = _chart.MyModel,
                    Width = chartPlaceholder.Width,
                    Height = chartPlaceholder.Height,
                });
        }

        private void SetupPlayer()
        {
            _decoder = new VideoDecoder();
            _renderer = new FrameRenderer();
            _comparer = new FrameComparer(_decoder, _renderer);

            _comparer.FrameCompared += OnFrameCompared;
            _renderer.FrameReady += OnFrameReadyToRender;
            _comparer.Stopped += OnStopped;
        }

        private void Open()
        {
            var videoFile = txtFileName.Text;

            if(!System.IO.File.Exists(videoFile))
            {
                MessageBox.Show("Please select a valid video file");
                return;
            }

            _decoder.Open(videoFile);

            //Set chart range to number of frames in vid
            if (_comparer.MostRecentFrameIndex == -1)
            {
                _chart.SetRange(0, _decoder.VideoInfo.TotalFrames);
                _activity = new List<Point>(_decoder.VideoInfo.TotalFrames);
            }
        }

        private void Play()
        {
            //Play
            if (btnPlayPause.Content.ToString() == PlaySymbol)
            {
                btnPlayPause.Content = PauseSymbol;

                //Reset decoder
                if (_decoder != null)
                {
                    _decoder.Dispose();
                    _decoder = null;
                    _decoder = new VideoDecoder();
                    _comparer.Decoder = _decoder;
                }

                //Reopen the file and load info
                Open();

                //Can't change quality if playing
                sliderQuality.IsEnabled = false;

                //Clear chart points after the current position
                _chart.ClearAfter(_comparer.MostRecentFrameIndex);
                _activity.RemoveAll(p => p.X > _comparer.MostRecentFrameIndex);

                //Adjust for any quality changes, before starting again
                _decoder.PlayerOutputWidth = (int)(_decoder.VideoInfo.Width * _quality);
                _decoder.PlayerOutputHeight = (int)(_decoder.VideoInfo.Height * _quality);

                //Play or resume
                _comparer.Start();
            }
            else //Pause
            {
                btnPlayPause.Content = PlaySymbol;
                _comparer.Pause();
                sliderQuality.IsEnabled = true;
            }
        }

        private void Stop()
        {
            _comparer.Stop();
            Reset();
        }

        private void SeekTo(double percentLocation)
        {
            _comparer.SeekTo(percentLocation);
        }

        private void OnFrameCompared(object sender, FrameComparisonArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _chart.AddPoint(e.Results.FrameIndex, e.Results.ChangedPixelsCount);
                _activity.Add(new Point(e.Results.FrameIndex, e.Results.ChangedPixelsCount));

                lblChangedPixels.Content = string.Format("Changed Pixels: {0:n0}", e.Results.ChangedPixelsCount);
                lblTime.Content = e.Results.FrameTime.ToString();
            });
        }

        private void OnStopped(object sender, EventArgs eventArgs)
        {
            _chart.Stop();

            Reset();
        }

        LinkedList<long> frameRenderTimes = new LinkedList<long>();
        private void ShowFrame(Frame frame)
        {
            if (Application.Current == null)
                return;
            
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (Application.Current == null)
                    return;

                using (var memory = new MemoryStream())
                {
                    frame.Bitmap.Save(memory, ImageFormat.Bmp);
                    memory.Position = 0;
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    videoCanvas.Source = bitmapImage;
                }

                _sliderValueChangedByCode = true;
                sliderTime.Value = frame.FramePercentage * 1000;

                //Compute FPS
                frameRenderTimes.AddLast(frame.Watch.ElapsedMilliseconds);
                lblFPS.Content = string.Format("FPS: {0:n1}", 1000.0/frameRenderTimes.Average());
                if(frameRenderTimes.Count > 10)
                    frameRenderTimes.RemoveFirst();
            }));
        }

        private void ToggleCompare()
        {
            if (btnShowCompare.Content.ToString().Contains(">>"))
            {
                Width = 1309;
                btnShowCompare.Content = btnShowCompare.Content.ToString().Replace(">>", "<<");
            }
            else
            {
                Width = borderComparison.Margin.Left + 15;
                btnShowCompare.Content = btnShowCompare.Content.ToString().Replace("<<", ">>");
            }

            CenterWindowOnScreen();
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void OnFrameReadyToRender(object sender, OnFrameReady e)
        {
            ShowFrame(e.Frame);
        }

        private void Reset()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                btnPlayPause.Content = PlaySymbol;

                _sliderValueChangedByCode = true;
                sliderTime.Value = 0;

                sliderQuality.IsEnabled = true;
            });
        }

        private void txtFileName_LostFocus(object sender, RoutedEventArgs e)
        {
            Open();
        }

        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();

            var result = ofd.ShowDialog();

            if (result == false)
                return;

            txtFileName.Text = ofd.FileName;

            Open();
            Play();
        }

        private void thresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(_comparer != null)
                _comparer.Threshold = (int)e.NewValue;

            if (lblThreshold != null)
                lblThreshold.Content = _comparer.Threshold;
        }

        private void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void OnStopClicked(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void chkShowMotion_Click(object sender, RoutedEventArgs e)
        {
            _renderer.ShowMotion = chkShowMotion.IsChecked.Value;
        }

        bool _sliderValueChangedByCode;
        private void timeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_sliderValueChangedByCode)
            {
                _sliderValueChangedByCode = false;
                return;
            }

            SeekTo(e.NewValue / 1000.0);
        }

        private void Slider_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Play();
        }

        private void Slider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Play();
        }

        private void btnShowCompare_Click(object sender, RoutedEventArgs e)
        {
            ToggleCompare();
        }

        private void sliderQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _quality = e.NewValue / 100.0;

            if(lblQuality != null)
                lblQuality.Content = string.Format("{0:n0}%", _quality*100.0);
        }

        private void contrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(_renderer != null)
                _renderer.ShadeRadius = (int)e.NewValue;

            if (lblContrast != null)
            {
                lblContrast.Content = _renderer.ShadeRadius + "X";
            }
        }

        private void btnSaveActivity_Click(object sender, RoutedEventArgs e)
        {
            new Thread(SaveCSV).Start(txtFileName.Text);
        }

        private void SaveCSV(object videoFileName)
        {
            Dispatcher.Invoke(() => btnSaveActivity.Content = "Saving...");

            var fileInfo = new FileInfo(videoFileName.ToString());

            using(var writer = new StreamWriter(fileInfo.FullName + ".csv", false))
            {
                writer.WriteLine("Frame, Changed Pixels");

                _activity.ForEach(a => writer.WriteLine("{0}, {1}", a.X+1, a.Y));

                writer.Flush();
            }

            Dispatcher.InvokeAsync(() => btnSaveActivity.Content = "Saved!");

            new Thread(() =>
            {
                Thread.Sleep(2000);

                Dispatcher.InvokeAsync(() =>
                {
                    btnSaveActivity.Content = "Save Activity Data";
                });

            }).Start();
        }

    }
}
