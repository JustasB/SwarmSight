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
        private List<Point> Activity = new List<Point>();

        private ChartModel _chart;

        public MotionProcessor Processor;
        public VideoPlayerController Controller;

        public MainWindow()
        {
            InitializeComponent();

            Processor = new MotionProcessor();

            Controller = new VideoPlayerController()
            {
                btnBrowse = btnBrowse,
                btnPlayPause = btnPlayPause,
                btnStepFrame = btnStepFrame,
                btnSave = btnSaveActivity,
                btnStop = btnStop,
                Canvas = videoCanvas,
                lblTime = lblTime,
                lblFPS = lblFPS,
                sliderTime = sliderTime,
                txtFileName = txtFileName,
                Quality = sliderQuality.Value / 100,
            };

            Controller.OnFinishSetupPlayer += OnFinishSetupPlayer;
            Controller.OnOpen += OnOpen;
            Controller.OnReset += InitDetector;
            Controller.OnStartPlaying += OnStartPlaying;
            Controller.OnProcessed += OnProcessed;
            Controller.OnRefreshMostRecentFrame += OnRefreshMostRecentFrame;
            Controller.OnAfterStopped += OnAfterStopped;
            Controller.Init();

            ToggleCompare();

            Loaded += (sender, args) => UpdateProcessorBounds();
            Closing += (sender, args) => Controller.Stop();

            Application.Current.Exit += (sender, args) => Controller.Stop();

#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException +=
                (sender, args) => { MessageBox.Show((args.ExceptionObject as Exception).Message); };
#endif
        }

        private void OnAfterStopped()
        {
            Activate();

            sliderQuality.IsEnabled = true;

            _chart.Stop();

            InitDetector();
        }

        private void OnStartPlaying()
        {
            //Can't change quality if playing
            sliderQuality.IsEnabled = false;

            //Clear chart points after the current position
            _chart.ClearAfter(Controller.Pipeline.Supervisor.Processor.MostRecentFrameIndex);
            Activity.RemoveAll(p => p.X > Controller.Pipeline.Supervisor.Processor.MostRecentFrameIndex);
        }

        private void OnProcessed(Frame frame)
        {
            var result = (MotionProcessorResult)frame.ProcessorResult;

            if (result == null || Application.Current == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    _chart.AddPoint(result.FrameIndex, result.ChangedPixelsCount);
                }
                catch
                {
                }

                Activity.Add(new Point(result.FrameIndex, result.ChangedPixelsCount));
                lblChangedPixels.Content = string.Format("Changed Pixels: {0:n0}", result.ChangedPixelsCount);
            });
        }
        private void OnRefreshMostRecentFrame(Frame frame)
        {
            if(chkShowMotion.IsChecked.Value)
                Processor.Annotate(frame);
        }

        private void InitDetector()
        {
            Controller.Processor = Controller.Pipeline.VideoProcessor = Processor = new MotionProcessor();

            Processor.ShadeRadius = (int)sliderContrast.Value;
            Processor.Threshold = (int)sliderThreshold.Value;
            Controller.Quality = sliderQuality.Value / 100.0;

            UpdateProcessorBounds();

            Application.Current.Dispatcher.Invoke(() =>
            {
                sliderQuality.IsEnabled = true;
            });
        }

        private void OnFinishSetupPlayer()
        {
            SetupChart();
            SetupPlayer();
        }

        private void SetupChart()
        {
            _chart = new ChartModel();
            Activity = new List<Point>(100);

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
            ((VideoActivityChart)sender).AddPointsToChart(Activity);

            ComputeComparisonStats();
        }

        private void ComputeComparisonStats()
        {
            comparisonTable.Clear();
            comparisonChart.Clear();

            var activityA = chartA.Activity;
            var activityB = chartB.Activity;

            if (TTest.Busy ||
                activityA == null || activityB == null ||
                activityA.Count <= 1 || activityB.Count <= 1)
                return;

            var firstValue = activityA[0].Y;
            var allSame = activityA.Zip(activityB, (a, b) => a.Y == b.Y && a.Y == firstValue).All(i => i);

            if (allSame)
            {
                MessageBox.Show("The selected activity sets are identical and of singular value.");
                return;
            }

            //Compute T-test
            var tTest = TTest.Perform
            (
                activityA.Select(p => p.Y).ToList(),
                activityB.Select(p => p.Y).ToList()
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
            roi.RegionChanged += (sender, args) => UpdateProcessorBounds();
        }

        private void OnOpen()
        {
            //Set chart range to number of frames in vid            
            _chart.SetRange(0, Controller.Pipeline.VideoInfo.TotalFrames);
            Activity = new List<Point>(Controller.Pipeline.VideoInfo.TotalFrames);
        }

        private void ToggleCompare()
        {
            if (btnShowCompare.Content.ToString().Contains(">>"))
            {
                Width = 1260;
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
        
        private void thresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Processor != null)
                Processor.Threshold = (int)e.NewValue;

            if (lblThreshold != null)
                lblThreshold.Content = Processor.Threshold;
        }
        
        private void chkShowMotion_Click(object sender, RoutedEventArgs e)
        {
            Processor.ShowMotion = sliderContrast.IsEnabled = chkShowMotion.IsChecked.Value;
        }

        private void btnShowCompare_Click(object sender, RoutedEventArgs e)
        {
            ToggleCompare();
        }

        private void sliderQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(Controller != null)
                Controller.Quality = e.NewValue / 100.0;

            if (lblQuality != null)
                lblQuality.Content = string.Format("{0:n0}%", Controller.Quality * 100.0);
        }

        private void contrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Processor != null)
                Processor.ShadeRadius = (int)e.NewValue;

            if (lblContrast != null)
            {
                lblContrast.Content = Processor.ShadeRadius + "X";
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

            using (var writer = new StreamWriter(fileInfo.FullName + "_Motion_" + Controller.GetCSVfileEnding(), false))
            {
                writer.WriteLine("Frame, Changed Pixels");

                Activity.ForEach(a => writer.WriteLine("{0}, {1}", a.X, a.Y));

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
                Processor.SetBounds(roi.LeftPercent, roi.TopPercent, roi.RightPercent, roi.BottomPercent);

                txtLeft.Text = roi.LeftPercent.ToString("P2");
                txtRight.Text = roi.RightPercent.ToString("P2");
                txtTop.Text = roi.TopPercent.ToString("P2");
                txtBottom.Text = roi.BottomPercent.ToString("P2");
            }

            else
            {
                Processor.SetBounds(0, 0, 1, 1);

                txtLeft.Text = 0.ToString("P2");
                txtRight.Text = 0.ToString("P2");
                txtTop.Text = 1.ToString("P2");
                txtBottom.Text = 1.ToString("P2");
            }
        }
    }
}