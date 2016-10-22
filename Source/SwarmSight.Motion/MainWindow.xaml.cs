using System.Linq;
using System.Windows.Controls;
using Classes;
using SwarmSight.VideoPlayer;
using SwarmSight.Stats;
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
using SwarmSight.Filters;
using SwarmSight.UserControls;
using Frame = SwarmSight.Filters.Frame;
using Point = System.Windows.Point;
using System.Windows.Input;
using System.Windows.Media;
using SwarmSight.Motion.Processor;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private List<Point> _activity = new List<Point>();

        private ChartModel _chart;

        public MotionProcessor Processor;
        public VideoPlayerController Controller;

        public MainWindow()
        {
            InitializeComponent();

            Controller = new VideoPlayerController()
            {
                btnBrowse = btnBrowse,
                btnPlayPause = btnPlayPause,
                btnSave = btnSaveActivity,
                btnStop = btnStop,
                Canvas = videoCanvas,
                lblTime = lblTime,
                lblFPS = lblFPS,
                sliderTime = sliderTime,
                txtFileName = txtFileName,
                Quality = AppSettings.Default.Quality,
            };

            Controller.OnFinishSetupPlayer += OnFinishSetupPlayer;
            Controller.OnOpen += OnOpen;
            Controller.OnReset += InitDetector;
            Controller.OnShowFrame += OnShowFrame;
            Controller.OnRefreshMostRecentFrame += OnRefreshMostRecentFrame;
            Controller.OnAfterStopped += () => Activate();
            Controller.Init();

            ToggleCompare();
            SetupChart();
            SetupPlayer();

            Loaded += (sender, args) => UpdateProcessorBounds();
            Closing += (sender, args) => Stop();

            Application.Current.Exit += (sender, args) => Stop();

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => { MessageBox.Show((args.ExceptionObject as Exception).Message); };
#endif
        }

        private void SetupChart()
        {
            _chart = new ChartModel();
            _activity = new List<Point>(100);

            chartPlaceholder.Children.Add(new PlotView()
            {
                Model = _chart.MyModel,
                Width = chartPlaceholder.Width,
                Height = chartPlaceholder.Height,
            });

            chartA.UseCurrentClicked += OnUseCurrentClicked;
            chartB.UseCurrentClicked += OnUseCurrentClicked;
        }

        private void OnUseCurrentClicked(object sender, EventArgs e)
        {
            ((VideoActivityChart)sender).AddPointsToChart(_activity);

            ComputeComparisonStats();
        }

        private void ComputeComparisonStats()
        {
            if (TTest.Busy ||
                chartA.Activity == null || chartB.Activity == null ||
                chartA.Activity.Count <= 1 || chartB.Activity.Count <= 1)
                return;

            //Compute T-test
            var tTest = TTest.Perform
                (
                    chartA.Activity.Select(p => p.Y).ToList(),
                    chartB.Activity.Select(p => p.Y).ToList()
                );

            //Update table
            comparisonTable.lblAvgA.Content = tTest.TTest.FirstSeriesMean.ToString("N2");
            comparisonTable.lblAvgB.Content = tTest.TTest.SecondSeriesMean.ToString("N2");

            comparisonTable.lblNA.Content = tTest.FirstSeriesCount.ToString("N0");
            comparisonTable.lblNB.Content = tTest.SecondSeriesCount.ToString("N0");

            comparisonTable.lblStDevA.Content = tTest.FirstSeriesStandardDeviation.ToString("N2");
            comparisonTable.lblStDevB.Content = tTest.SecondSeriesStandardDeviation.ToString("N2");

            comparisonTable.lblAvgDiff.Content = (tTest.MeanDifference > 0 ? "+" : "") +
                                                 tTest.MeanDifference.ToString("N2");

            if (tTest.TTest.FirstSeriesMean != 0)
                comparisonTable.lblAvgPercent.Content = tTest.PercentMeanDifference.ToString("P2");
            else
                comparisonTable.lblAvgPercent.Content = "-";

            //Update Chart
            comparisonChart.UpdateChart
                (
                    tTest.TTest.FirstSeriesMean, tTest.FirstSeries95ConfidenceBound,
                    tTest.TTest.SecondSeriesMean, tTest.SecondSeries95ConfidenceBound
                );
        }

        private void SetupPlayer()
        {
            Pipeline = new VideoPipeline();
            Pipeline.FrameReady += OnFrameReadyToRender;
            Pipeline.Stopped += OnStopped;
            
            roi.RegionChanged += (sender, args) => UpdateProcessorBounds();
        }

        private bool Open()
        {
            var videoFile = txtFileName.Text;

            if (!System.IO.File.Exists(videoFile))
            {
                MessageBox.Show("Please select a valid video file");
                return false;
            }

            Pipeline.Open(videoFile);

            //Set chart range to number of frames in vid            
            _chart.SetRange(0, Pipeline.VideoInfo.TotalFrames);
            _activity = new List<Point>(Pipeline.VideoInfo.TotalFrames);

            return true;
        }


        private void Play()
        {
            //Play
            if (btnPlayPause.Content.ToString() == PlaySymbol)
            {
                if (!File.Exists(txtFileName.Text))
                {
                    MessageBox.Show("Please select a video file.");
                    return;
                }

                //Reset decoder
                if (_decoder != null)
                {
                    _decoder.Dispose();
                    _decoder = null;
                    _decoder = new VideoDecoder();
                    _decoder.PlayStartTimeInSec = 0;
                    _decoder.Processor = Processor;
                    _decoder.Open(txtFileName.Text);
                    _comparer.Decoder = _decoder;
                }

                //Clear canvas buffer
                canvasBuffer = null;

                //Can't change quality if playing
                sliderQuality.IsEnabled = false;

                //Clear chart points after the current position
                _chart.ClearAfter(_comparer.MostRecentFrameIndex);
                _activity.RemoveAll(p => p.X > _comparer.MostRecentFrameIndex);

                //Adjust for any quality changes, before starting again
                _decoder.PlayerOutputWidth = (int)(_decoder.VideoInfo.Width * _quality); //204;//
                _decoder.PlayerOutputHeight = (int)(_decoder.VideoInfo.Height * _quality); //152;//

                //Setup fps counter
                _fpsStartFrame = _comparer.MostRecentFrameIndex;
                _fpsStopwatch.Restart();

                //Play or resume
                _comparer.Start();

                btnPlayPause.Content = PauseSymbol;
            }
            else //Pause
            {
                Pause();
            }
        }

        private void Pause()
        {
            btnPlayPause.Content = PlaySymbol;
            _comparer.Pause();
            sliderQuality.IsEnabled = true;
        }

        private void Stop()
        {
            _comparer.Stop();
            _renderer.Stop();
            Reset();
        }

        private void SeekTo(double percentLocation)
        {
            if (_decoder == null || _decoder.VideoInfo == null)
                return;

            _comparer.SeekTo(percentLocation);

            lblTime.Content = TimeSpan.FromMilliseconds(_decoder.VideoInfo.Duration.TotalMilliseconds * percentLocation).ToString();
        }

        private void OnFrameCompared(object sender, FrameComparisonArgs e)
        {
            if (e.Results == null || Application.Current == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _chart.AddPoint(e.Results.FrameIndex, e.Results.ChangedPixelsCount);
                }
                catch
                {
                }

                _activity.Add(new Point(e.Results.FrameIndex, e.Results.ChangedPixelsCount));
                lblChangedPixels.Content = string.Format("Changed Pixels: {0:n0}", e.Results.ChangedPixelsCount);
            });
        }

        private void OnStopped(object sender, EventArgs eventArgs)
        {
            _chart.Stop();

            Reset();
        }

        public static LinkedList<DateTime> fpsHist = new LinkedList<DateTime>();
        private static bool isDrawing = false;
        private static WriteableBitmap canvasBuffer;
        private static DateTime prevFrameTime = DateTime.Now;
        private void ShowFrame(Frame frame)
        {
            if (isDrawing)
                return;

            if (Application.Current == null)
                return;

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (Application.Current == null)
                    return;

                isDrawing = true;

                //Create WriteableBitmap the first time
                if (canvasBuffer == null)
                {
                    canvasBuffer = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr24, null);
                    videoCanvas.Source = canvasBuffer;
                }

                if ((DateTime.Now - prevFrameTime).TotalMilliseconds >= 1000 / 30.0)
                {
                    frame.CopyToWriteableBitmap(canvasBuffer);
                    prevFrameTime = DateTime.Now;
                }

                _sliderValueChangedByCode = true;
                sliderTime.Value = frame.FramePercentage * 1000;
                lblTime.Content = frame.FrameTime.ToString();

                //Compute FPS
                var now = DateTime.Now;
                fpsHist.AddLast(now);
                lblFPS.Content = string.Format("FPS: {0:n1}", fpsHist.Count / 1.0);

                while ((now - fpsHist.First()).TotalMilliseconds > 1000)
                {
                    fpsHist.RemoveFirst();
                }

                isDrawing = false;
            }));
        }

        private void ToggleCompare()
        {
            if (btnShowCompare.Content.ToString().Contains(">>"))
            {
                Width = _fullSizeWidth + 15;
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
            Stop();
            Reset();
            Open();
        }

        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();

            var result = ofd.ShowDialog();

            if (result == false)
                return;

            txtFileName.Text = ofd.FileName;

            Stop();
            Reset();
            Open();
        }

        private void thresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Processor != null)
                ((MotionProcessor)Processor).Threshold = (int)e.NewValue;

            if (lblThreshold != null)
                lblThreshold.Content = ((MotionProcessor)Processor).Threshold;
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
            _renderer.ShowMotion = sliderContrast.IsEnabled = chkShowMotion.IsChecked.Value;
        }


        private void sliderTime_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //Pause if slider is clicked
            if (!_comparer.IsPaused)
                Pause();
        }

        private void sliderTime_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            sliderTime_MouseDown(sender, e);
        }

        private bool _sliderValueChangedByCode;

        private void timeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_sliderValueChangedByCode)
            {
                _sliderValueChangedByCode = false;
                return;
            }

            if (btnPlayPause.Content.ToString() == PlaySymbol)
                Pause();

            SeekTo(e.NewValue / 1000.0);
        }

        private void btnShowCompare_Click(object sender, RoutedEventArgs e)
        {
            ToggleCompare();
        }

        private void sliderQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Force redraw of the buffer
            canvasBuffer = null;

            _quality = e.NewValue / 100.0;

            if (lblQuality != null)
                lblQuality.Content = string.Format("{0:n0}%", _quality * 100.0);
        }

        private void contrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_renderer != null)
                _renderer.ShadeRadius = (int)e.NewValue;

            if (lblContrast != null)
            {
                lblContrast.Content = _renderer.ShadeRadius + "X";
            }
        }

        private void btnSaveActivity_Click(object sender, RoutedEventArgs e)
        {
            new Thread(SaveCSV) { IsBackground = true }.Start(txtFileName.Text);
        }

        private void SaveCSV(object videoFileName)
        {
            Dispatcher.Invoke(() => btnSaveActivity.Content = "Saving...");

            var fileInfo = new FileInfo(videoFileName.ToString());

            using (var writer = new StreamWriter(fileInfo.FullName + ".csv", false))
            {
                writer.WriteLine("Frame, Changed Pixels");

                _activity.ForEach(a => writer.WriteLine("{0}, {1}", a.X + 1, a.Y));

                writer.Flush();
            }

            Dispatcher.InvokeAsync(() => btnSaveActivity.Content = "Saved!");

            new Thread(() =>
                {
                    Thread.Sleep(2000);

                    Dispatcher.InvokeAsync(() => { btnSaveActivity.Content = "Save Activity Data"; });
                })
            {
                IsBackground = true
            }
                .Start();
        }

        private void btnComputeStats_Click(object sender, RoutedEventArgs e)
        {
            ComputeComparisonStats();
        }

        private void btnROI_Click(object sender, RoutedEventArgs e)
        {
            if (roi.Visibility == Visibility.Visible)
            {
                roi.Visibility = Visibility.Hidden;
                btnROI.Content = "Add Region of Interest";
            }
            else //Hidden
            {
                roi.Visibility = Visibility.Visible;
                btnROI.Content = "Remove Region of Interest";
            }

            UpdateProcessorBounds();
        }

        private void UpdateProcessorBounds()
        {
            if (roi.Visibility == Visibility.Visible)
            {
                ((MotionProcessor)Processor).SetBounds(roi.LeftPercent, roi.TopPercent, roi.RightPercent, roi.BottomPercent);

                txtLeft.Text = roi.LeftPercent.ToString("P2");
                txtRight.Text = roi.RightPercent.ToString("P2");
                txtTop.Text = roi.TopPercent.ToString("P2");
                txtBottom.Text = roi.BottomPercent.ToString("P2");
            }

            else
            {
                ((MotionProcessor)Processor).SetBounds(0, 0, 1, 1);

                txtLeft.Text = 0.ToString("P2");
                txtRight.Text = 0.ToString("P2");
                txtTop.Text = 1.ToString("P2");
                txtBottom.Text = 1.ToString("P2");
            }
        }
    }
}