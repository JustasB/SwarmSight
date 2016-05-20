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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking;
using SwarmSight.UserControls;
using Frame = SwarmSight.Filters.Frame;
using Point = System.Windows.Point;
using System.Windows.Input;
using SwarmSight.HeadPartsTracking.Algorithms;
using SwarmSight.HeadPartsTracking.Models;
using Settings;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string PlaySymbol = "4";
        private const string PauseSymbol = ";";

        private double _fullSizeWidth;
        private int _fpsStartFrame;
        private Stopwatch _fpsStopwatch = new Stopwatch();
        
        private VideoDecoder _decoder;
        private FrameComparer _comparer;
        private FrameRenderer _renderer;
        private VideoProcessorBase _processor;// = new AntennaAndPERDetector();
        
        public MainWindow()
        {
            InitializeComponent();

            _fullSizeWidth = Width;

            SetupPlayer();
            
            Closing += (sender, args) => Stop();

            Application.Current.Exit += (sender, args) => Stop();
        }

        private void SetupReceptiveField()
        {
            receptiveField.Canvas = videoCanvas;
            receptiveField.Dimensions = ToCanvasCoordinates(AppSettings.Default.Dimensions);
            receptiveField.Scale = new Point(AppSettings.Default.HeadScale, AppSettings.Default.HeadScale);
            receptiveField.Position = ToCanvasCoordinates(AppSettings.Default.Origin);
            receptiveField.Angle = AppSettings.Default.HeadAngle;

            SyncSettings();

            receptiveField.Moved += (s,e) =>
            {
                var loc = ToVideoCoordinates(receptiveField.Position);

                AppSettings.Default.HeadX = loc.X;
                AppSettings.Default.HeadY = loc.Y;
                AppSettings.Default.SaveAsync();

                SyncSettings();
            };

            receptiveField.Scaled += (s,e) =>
            {
                var loc = ToVideoCoordinates(receptiveField.Position);

                AppSettings.Default.HeadX = loc.X;
                AppSettings.Default.HeadY = loc.Y;
                AppSettings.Default.HeadW = receptiveField.Dimensions.X.Rounded();
                AppSettings.Default.HeadH = receptiveField.Dimensions.Y.Rounded();
                AppSettings.Default.HeadScale = Math.Max(receptiveField.Scale.X, receptiveField.Scale.Y);
                AppSettings.Default.SaveAsync();

                SyncSettings();
            };

            receptiveField.Rotated += (s,e) =>
            {
                AppSettings.Default.HeadAngle = receptiveField.Angle;
                AppSettings.Default.SaveAsync();

                SyncSettings();
            };
        }

        private void SyncSettings()
        {
            txtHeadX.Text = AppSettings.Default.HeadX.ToString();
            txtHeadY.Text = AppSettings.Default.HeadY.ToString();

            txtHeadW.Text = AppSettings.Default.HeadW.ToString();
            txtHeadH.Text = AppSettings.Default.HeadH.ToString();

            txtHeadScale.Text = AppSettings.Default.HeadScale.ToString();
            txtHeadAngle.Text = AppSettings.Default.HeadAngle.ToString();
        }

        private System.Drawing.Point ToVideoCoordinates(Point source)
        {
            return new System.Drawing.Point(
                ((int)(_decoder.VideoInfo.Width * AppSettings.Default.Quality) *  source.X / videoCanvas.ActualWidth).Rounded(),
                ((int)(_decoder.VideoInfo.Height * AppSettings.Default.Quality) * source.Y / videoCanvas.ActualHeight).Rounded()
            );
        }

        private Point ToCanvasCoordinates(System.Drawing.Point source)
        {
            var x = videoCanvas.ActualWidth * source.X / (int)(_decoder.VideoInfo.Width * AppSettings.Default.Quality);
            var y = videoCanvas.ActualHeight * source.Y / (int)(_decoder.VideoInfo.Height * AppSettings.Default.Quality);
            
            return new Point(x,y);
        }


        private void SetupPlayer()
        {
            _decoder = new VideoDecoder();
            _renderer = new FrameRenderer();
            _comparer = new FrameComparer(_decoder, _renderer);

            _decoder.Processor = _comparer.Processor = _processor;
            _comparer.FrameCompared += OnFrameCompared;
            _renderer.FrameReady += OnFrameReadyToRender;
            _comparer.Stopped += OnStopped;
            
        }

        private bool Open()
        {
            var videoFile = txtFileName.Text;

            if (!System.IO.File.Exists(videoFile))
            {
                MessageBox.Show("Please select a valid video file");
                return false;
            }

            _decoder.Open(videoFile);

            SetupReceptiveField();

            return true;
        }

        private void DeleteTempFiles()
        {
            var filesToDelete = Directory
                .GetFiles(@"c:\temp\frames")
                .Where(name => name.EndsWith(".jpg"))
                .ToList();


            filesToDelete.ForEach(f => File.Delete(f));
        }
        private static bool playOneFrame;
        private void Play(bool oneFrame = false)
        {
            //Play
            if (btnPlayPause.Content.ToString() == PlaySymbol)
            {
                if (!File.Exists(txtFileName.Text))
                {
                    MessageBox.Show("Please select a video file.");
                    return;
                }

                DeleteTempFiles();

                //Reset decoder
                if (_decoder != null)
                {
                    _decoder.Dispose();
                    _decoder = null;
                    _decoder = new VideoDecoder();
                    _decoder.PlayStartTimeInSec = 0;
                    _decoder.Processor = _processor;
                    _decoder.Open(txtFileName.Text);
                    _comparer.Decoder = _decoder;
                }

                //Clear canvas buffer
                canvasBuffer = null;

                //Can't change quality if playing
                sliderQuality.IsEnabled = false;

                //Adjust for any quality changes, before starting again
                _decoder.PlayerOutputWidth = (int) (_decoder.VideoInfo.Width* AppSettings.Default.Quality); //204;//
                _decoder.PlayerOutputHeight = (int) (_decoder.VideoInfo.Height* AppSettings.Default.Quality); //152;//

                //Setup fps counter
                _fpsStartFrame = _comparer.MostRecentFrameIndex;
                _fpsStopwatch.Restart();

                //Play or resume
                _comparer.Start();

                btnPlayPause.Content = PauseSymbol;

                playOneFrame = oneFrame;
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

            lblTime.Content = TimeSpan.FromMilliseconds(_decoder.VideoInfo.Duration.TotalMilliseconds*percentLocation).ToString();
        }

        private void OnFrameCompared(object sender, FrameComparisonArgs e)
        {
            if (e.Results == null || Application.Current == null)
                return;
        }

        private void OnStopped(object sender, EventArgs eventArgs)
        {
            //var rows = ((AntennaAndPERDetector)_processor).dataFrame;

            //File.WriteAllLines(filesToProcess[currentFileIndex] + "_antennaBins_"+ DateTime.Now.ToString("yyyyMMdd-HHmm") +".csv", rows);

            

            Reset();


            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Activate();
            }));
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

                if ((DateTime.Now - prevFrameTime).TotalMilliseconds >= 1000/10.0)
                {
                    frame.CopyToWriteableBitmap(canvasBuffer);
                    prevFrameTime = DateTime.Now;
                }

                _sliderValueChangedByCode = true;
                sliderTime.Value = frame.FramePercentage*1000;
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

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth/2) - (windowWidth/2);
            this.Top = (screenHeight/2) - (windowHeight/2);
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

                    Dictionary<int, AntenaPoints> prevData = null;

                    if (_processor != null)
                    {
                        prevData = ((AntennaAndPERDetector)_processor).frameData;
                    }
                    

                    _comparer.Processor = _decoder.Processor = _processor = new AntennaAndPERDetector();
                    
                    if(prevData != null)
                        ((AntennaAndPERDetector) _processor).frameData = prevData;
                });
        }

        private void txtFileName_LostFocus(object sender, RoutedEventArgs e)
        {
            Stop();
            Reset();
            Open();
            Play(oneFrame: true);
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
            Play(oneFrame: true);
        }

        private void thresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void OnStopClicked(object sender, RoutedEventArgs e)
        {
            Stop();
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

            SeekTo(e.NewValue/1000.0);
        }

        private void sliderQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Force redraw of the buffer
            canvasBuffer = null;

            AppSettings.Default.Quality = e.NewValue/100.0;
            AppSettings.Default.SaveAsync();

            if (lblQuality != null)
                lblQuality.Content = string.Format("{0:n0}%", AppSettings.Default.Quality * 100.0);
        }

        private void btnSaveActivity_Click(object sender, RoutedEventArgs e)
        {
            new Thread(SaveCSV) {IsBackground = true}.Start(txtFileName.Text);
        }

        private void SaveCSV(object videoFileName)
        {
            Dispatcher.Invoke(() => btnSaveActivity.Content = "Saving...");

            var fileInfo = new FileInfo(videoFileName.ToString());

            using (var writer = new StreamWriter(fileInfo.FullName + "_Tracker_" + DateTime.Now.ToString("yyyy-MM-dd hh-mm") + ".csv", false))
            {
                writer.WriteLine
                (
                    "Frame, BuzzerValue, " + 
                    "LeftSector, RightSector, " +
                    "LeftFlagellumTip-X, LeftFlagellumTip-Y, RightFlagellumTip-X, RightFlagellumTip-Y, " +
                    "LeftFlagellumBase-X, LeftFlagellumBase-Y, RightFlagellumBase-X, RightFlagellumBase-Y, " +
                    "RotationAngle, ReceptiveFieldWidth, ReceptiveFieldHeight, " +
                    "ReceptiveFieldOffset-X, ReceptiveFieldOffset-Y, ReceptiveFieldScale-X, ReceptiveFieldScale-Y, " +
                    "SectorData"
                );

                var data = ((AntennaAndPERDetector)_processor).frameData;
                var frames = data.Keys.OrderBy(k => k).ToList();

                frames.ForEach(frameIndex =>
                {
                    var value = data[frameIndex];

                    var line = string.Join(",", new[]
                    {
                        frameIndex,

                        value.Buzzer,

                        value.LeftSector,
                        value.RightSector,

                        value.LFT.X, value.LFT.Y,
                        value.RFT.X, value.RFT.Y,

                        value.LFB.X, value.LFB.Y,
                        value.RFB.X, value.RFB.Y,

                        value.RecordingConditions.RotationAngle,

                        value.RecordingConditions.Dimensions.X,
                        value.RecordingConditions.Dimensions.Y,

                        value.RecordingConditions.HeadOffset.X,
                        value.RecordingConditions.HeadOffset.Y,

                        value.RecordingConditions.Scale.X,
                        value.RecordingConditions.Scale.Y
                    });

                    string sectorData = "";

                    if (value.LeftSectorData != null && value.RightSectorData != null)
                    {
                        sectorData = string.Join(",", value.LeftSectorData) + "," + string.Join(",", value.RightSectorData);
                    }
                    

                    writer.WriteLine(line + ", " + sectorData);

                });

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

        private ParameterSetter paramSetter;
        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            paramSetter = new ParameterSetter();
            paramSetter.Offset = AppSettings.Default.Origin;
            paramSetter.Dims = AppSettings.Default.Dimensions;
            paramSetter.Path = txtFileName.Text;

            paramSetter.Init();
            paramSetter.Show();
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lblMedianFilterRadius == null)
                return;

            AppSettings.Default.MedianFilterRadius = e.NewValue.Rounded();
            lblMedianFilterRadius.Content = e.NewValue.Rounded();
            AppSettings.Default.SaveAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Play(oneFrame: true);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            pnlManualData.IsEnabled = (bool)((CheckBox)sender).IsChecked;
            AppSettings.Default.CompareToManualData = pnlManualData.IsEnabled;
            AppSettings.Default.SaveAsync();
        }

        private void OnManualBrowseClicked(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();

            var result = ofd.ShowDialog();

            if (result == false)
                return;

            txtManualFile.Text = ofd.FileName;
            AppSettings.Default.ManualDataFile = ofd.FileName;
            AppSettings.Default.SaveAsync();
        }

        private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (lblCrawlHop == null)
                return;

            AppSettings.Default.MaxTipCrawlerHop = e.NewValue.Rounded();
            lblCrawlHop.Content = e.NewValue.Rounded();
            AppSettings.Default.SaveAsync();
        }
    }
}