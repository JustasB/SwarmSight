using Classes;
using Settings;
using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking;
using SwarmSight.HeadPartsTracking.Algorithms;
using SwarmSight.Helpers;
using SwarmSight.VideoPlayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Frame = SwarmSight.Filters.Frame;
using Point = System.Windows.Point;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ProcessorWindow
    {
        private const string PlaySymbol = "4";
        private const string PauseSymbol = ";";

        private double _fullSizeWidth;
        private Stopwatch _fpsStopwatch = new Stopwatch();

        public VideoPipeline Pipeline;
        private VideoProcessorBase _processor;// = new AntennaAndPERDetector();

        private WindowManager WindowManager
        {
            get
            {
                return ((WindowManager)Application.Current);
            }
        }

        public ProcessorWindow()
        {
            AppSettings.Default.Upgrade();
            
            InitializeComponent();

            sliderFast.Label = "Fast Motion Threshold:";
            sliderFast.SettingsKey = "FastThreshold";
            sliderFast.LoadFromSettings();

            sliderSlow.Label = "Slow Motion Threshold:";
            sliderSlow.SettingsKey = "SlowThreshold";
            sliderSlow.LoadFromSettings();

            sliderStationary.Label = "Stationary Threshold:";
            sliderStationary.SettingsKey = "StationaryThreshold";
            sliderStationary.LoadFromSettings();

            _fullSizeWidth = Width;

            SetupPlayer();

            SizeChanged += (sender, e) =>
            {
                SetupCanvas();

                SyncAntennaSensor();
                SyncTreatmentSensor();
                SyncExclusionZones();
            };

            Closing += (sender, args) =>
            {
                Stop();
                Hide();
                WindowManager.ExitIfLastWindow();
            };

            WindowManager.Exit += (sender, args) => Stop();
        }


        public double CanvasScale = 1.0;
        private void SetupCanvas()
        {
            if (Pipeline.VideoInfo == null)
                return;

            CanvasScale = videoCanvas.ActualWidth / Pipeline.VideoInfo.Width;
            canvasRow.Height = new GridLength(Pipeline.VideoInfo.Height * CanvasScale + videoCanvas.Margin.Top);

            Height = double.NaN;
            SizeToContent = SizeToContent.Height;
        }

        private void SetupAntennaSensor()
        {
            antennaSensor.Canvas = videoCanvas;
            antennaSensor.CanvasScale = CanvasScale;
            antennaSensor.Dimensions = new Point(AppSettings.Default.HeadScale * 100, AppSettings.Default.HeadScale * 100);
            antennaSensor.Position = ToCanvasCoordinates(AppSettings.Default.Origin);
            antennaSensor.Angle = AppSettings.Default.HeadAngle;

            antennaSensor.MouseDown += (s, e) =>
            {
                Pause();
            };

            antennaSensor.Moved += (s, e) =>
            {
                var loc = ToVideoCoordinates(antennaSensor.Position);

                AppSettings.Default.HeadX = loc.X;
                AppSettings.Default.HeadY = loc.Y;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();

                RefreshMostRecentFrame();

                Stop();
            };

            antennaSensor.Scaled += (s, e) =>
            {
                var loc = ToVideoCoordinates(antennaSensor.Position);
                var dims = ToVideoCoordinates(antennaSensor.Dimensions);

                AppSettings.Default.HeadX = loc.X;
                AppSettings.Default.HeadY = loc.Y;
                AppSettings.Default.HeadScale = antennaSensor.Scale;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();

                RefreshMostRecentFrame();

                Stop();
            };

            antennaSensor.Rotated += (s, e) =>
            {
                AppSettings.Default.HeadAngle = antennaSensor.Angle;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();

                RefreshMostRecentFrame();

                Stop();
            };
        }

        private void SetupExclusionZones()
        {
            exclusionManager.window = this;
            exclusionManager.LoadFromSettings();

            exclusionManager.Changed += (s, e) =>
            {
                Stop();
            };
        }

        private void SetupTreatmentSensor()
        {
            treatmentSensor.Canvas = videoCanvas;
            treatmentSensor.Position = ToCanvasCoordinates(AppSettings.Default.TreatmentSensor);
            
            treatmentSensor.Moved += (s, e) =>
            {
                var loc = ToVideoCoordinates(treatmentSensor.Position);

                AppSettings.Default.TreatmentSensor = loc;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();
            };
        }

        private void LoadFromSettings()
        {
            var headPos = new System.Drawing.Point(AppSettings.Default.HeadX, AppSettings.Default.HeadY);
            var dim = AppSettings.Default.HeadScale * 100 * CanvasScale;

            txtHeadX.Text = headPos.X.ToString();
            txtHeadY.Text = headPos.Y.ToString();

            txtHeadScale.Text = AppSettings.Default.HeadScale.ToString();
            txtHeadAngle.Text = AppSettings.Default.HeadAngle.ToString();

            txtTreatmentX.Text = AppSettings.Default.TreatmentSensor.X.ToString();
            txtTreatmentY.Text = AppSettings.Default.TreatmentSensor.Y.ToString();

            chkShowFilterPoints.IsChecked = AppSettings.Default.ShowModel;
            txtVideoLabel.Text = AppSettings.Default.VideoLabel;
            txtVideoLabelColumn.Text = AppSettings.Default.VideoLabelColumn;

        }

        private void SyncAntennaSensor()
        {
            var headPos = new System.Drawing.Point(AppSettings.Default.HeadX, AppSettings.Default.HeadY);
            var dim = AppSettings.Default.HeadScale * 100 * CanvasScale;
            
            if (Pipeline?.VideoInfo != null)
            {
                antennaSensor.Position = ToCanvasCoordinates(headPos);
                antennaSensor.Dimensions = new Point(dim, dim);
            }
        }

        private void SyncTreatmentSensor()
        {
            var pos = AppSettings.Default.TreatmentSensor;
            
            if (Pipeline?.VideoInfo != null)
            {
                treatmentSensor.Position = ToCanvasCoordinates(pos);
            }
        }

        private void SyncExclusionZones()
        {
            exclusionManager.LoadFromSettings();
        }


        public System.Drawing.Point ToVideoCoordinates(Point source)
        {
            return new System.Drawing.Point(
                ((int)(Pipeline.VideoInfo.Width * AppSettings.Default.Quality) *  source.X / videoCanvas.ActualWidth).Rounded(),
                ((int)(Pipeline.VideoInfo.Height * AppSettings.Default.Quality) * source.Y / videoCanvas.ActualHeight).Rounded()
            );
        }

        public Point ToCanvasCoordinates(System.Drawing.Point source)
        {
            var x = videoCanvas.ActualWidth * source.X / (int)(Pipeline.VideoInfo.Width * AppSettings.Default.Quality);
            var y = videoCanvas.ActualHeight * source.Y / (int)(Pipeline.VideoInfo.Height * AppSettings.Default.Quality);
            
            return new Point(x,y);
        }


        private void SetupPlayer()
        {
            Pipeline = new VideoPipeline();
            Pipeline.FrameReady += OnFrameReadyToRender;
            Pipeline.Stopped += OnStopped;

            InitDetector();

            ShowHideModelViews(hide: true);
        }

        private void ShowHideModelViews(bool hide)
        {
            modelView.Visibility = 
            sectorView.Visibility = 
            treatmentSensor.Visibility = 
            antennaSensor.Visibility = 
            lblFPS.Visibility = 
            btnAddExclusion.Visibility = 
                hide ? Visibility.Hidden : Visibility.Visible;
        }

        public void LoadParamsFromJSON(string newParams)
        {
            AppSettings.Default.LoadFromJSON(newParams);
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

            SetupCanvas();
            SetupAntennaSensor();
            SetupTreatmentSensor();
            SetupExclusionZones();
            LoadFromSettings();
            
            SyncAntennaSensor();
            SyncTreatmentSensor();
            SyncExclusionZones();

            ShowHideModelViews(hide: false);

            Stop();

            return true;
        }

        public void Play(bool oneFrame = false)
        {
            if (Pipeline.Supervisor.State == VideoPlayer.Pipeline.WorkState.Working)
            {
                Pause();
            }
            else
            {
                if (!File.Exists(txtFileName.Text))
                {
                    MessageBox.Show("Please select a video file.");
                    return;
                }

                //Clear canvas buffer
                canvasBuffer = null;
                
                //Adjust for any quality changes, before starting again
                Pipeline.Supervisor.Processor.VD.PlayerOutputWidth = (int)(Pipeline.VideoInfo.Width * AppSettings.Default.Quality); //204;//
                Pipeline.Supervisor.Processor.VD.PlayerOutputHeight = (int)(Pipeline.VideoInfo.Height * AppSettings.Default.Quality); //152;//

                if (oneFrame)
                    Pipeline.RunOneFrame();
                else
                    Pipeline.Start();

                if(!oneFrame)
                    btnPlayPause.Content = PauseSymbol;
            }
        }

        private void Pause()
        {
            if (Pipeline.Supervisor.State == VideoPlayer.Pipeline.WorkState.Working)
            {
                btnPlayPause.Content = PlaySymbol;
                Pipeline.Pause();
            }
        }

        public void Stop()
        {
            if (Pipeline.Supervisor.State == VideoPlayer.Pipeline.WorkState.Stopped)
                return;

            Pipeline.Stop();
            Reset();
        }

        private void SeekTo(double percentLocation)
        {
            if (Pipeline == null || Pipeline.VideoInfo == null)
                return;

            Pipeline.Seek(percentLocation);

            lblTime.Content = TimeSpan.FromMilliseconds(Pipeline.VideoInfo.Duration.TotalMilliseconds*percentLocation).ToString();
        }

        private void OnStopped(object sender, EventArgs eventArgs)
        {
            //var rows = ((AntennaAndPERDetector)_processor).dataFrame;

            //File.WriteAllLines(filesToProcess[currentFileIndex] + "_antennaBins_"+ DateTime.Now.ToString("yyyyMMdd-HHmm") +".csv", rows);


            if (Application.Current == null)
                return;

            Reset();

            try
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Activate();
                }));
            }
            catch { }
        }

        public static LinkedList<DateTime> fpsHist = new LinkedList<DateTime>();
        private static bool isDrawing = false;
        private static WriteableBitmap canvasBuffer;
        private static DateTime prevFrameTime = DateTime.Now;
        private static Frame mostRecentFrame;
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

                    mostRecentFrame = frame.Clone();
                }

                //Limit frame rate shown
                var maxFps = 10;
                if ((DateTime.Now - prevFrameTime).TotalMilliseconds >= 1000.0 / maxFps)
                {
                    //Save the most recent frame
                    mostRecentFrame.DrawFrame(frame);
                    mostRecentFrame.ProcessorResult = frame.ProcessorResult;
                    mostRecentFrame.FrameIndex = frame.FrameIndex;

                    RefreshMostRecentFrame();

                    if (frame.ProcessorResult != null)
                    {
                        var frameResult = (EfficientTipAndPERdetector.TipAndPERResult)frame.ProcessorResult;

                        treatmentSensor.SensorValue = frameResult.TreatmentSensorValue.ToString();

                        modelView.Show(frameResult);
                        sectorView.Show(frameResult);
                    }
                    
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

        private Frame tempFrame;
        private void RefreshMostRecentFrame()
        {
            if (mostRecentFrame == null)
                return;

            //Prepare the temp frame for annotations
            if (tempFrame == null)
                tempFrame = mostRecentFrame.Clone();
            else
                tempFrame.DrawFrame(mostRecentFrame);

            tempFrame.FrameIndex = mostRecentFrame.FrameIndex;
            tempFrame.ProcessorResult = mostRecentFrame.ProcessorResult;
            (_processor as EfficientTipAndPERdetector).Annotate(tempFrame);
            tempFrame.CopyToWriteableBitmap(canvasBuffer);
        }

        private void OnFrameReadyToRender(object sender, OnFrameReady e)
        {
            ShowFrame(e.Frame);
        }

        private void Reset()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    btnPlayPause.Content = PlaySymbol;

                    _sliderValueChangedByCode = true;
                    sliderTime.Value = 0;

                    InitDetector();
                });
            }
            catch { }
        }

        private void InitDetector()
        {
            //Preserve existing frame results if resetting
            Dictionary<int, EfficientTipAndPERdetector.TipAndPERResult> prevData = null;

            if (_processor != null)
            {
                prevData = ((EfficientTipAndPERdetector)_processor).Results;
            }

            Pipeline.VideoProcessor = _processor = new EfficientTipAndPERdetector();

            if (prevData != null)
                ((EfficientTipAndPERdetector)_processor).Results = prevData;
        }

        private void txtFileName_LostFocus(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }

        public void LoadFile(bool oneFrame = true)
        {
            Stop();
            Reset();
            Open();
            Play(oneFrame);
        }

        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = WindowManager.VideoFileFilter;
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
            if (Pipeline.Supervisor.State == VideoPlayer.Pipeline.WorkState.Working)
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

            SeekTo(e.NewValue/1000.0);
        }

        private void btnSaveActivity_Click(object sender, RoutedEventArgs e)
        {
            new Thread(SaveCSV) {IsBackground = true}.Start(txtFileName.Text);
        }

        public void SaveCSV(object videoFileName)
        {
            if (string.IsNullOrWhiteSpace(videoFileName.ToString()))
                return;

            if(AppSettings.Default.VideoLabelColumn.Split(',').Length != AppSettings.Default.VideoLabel.Split(',').Length)
            {
                MessageBox.Show("Make sure the number of commas (',') is the same in both the column label and column value field.");
                return;
            }
            

            var fileInfo = new FileInfo(videoFileName.ToString());

            using (var writer = new StreamWriter(fileInfo.FullName + "_Tracker_" + Environment.UserName + "_" + DateTime.Now.ToString("yyyy-MM-dd hh-mm") + ".csv", false))
            {
                writer.WriteLine
                (
                    AppSettings.Default.VideoLabelColumn + ", Frame, TreatmentSensor, " + 
                    "PER-X, PER-Y, " +
                    "LeftSector, RightSector, " +
                    "LeftFlagellumTip-X, LeftFlagellumTip-Y, RightFlagellumTip-X, RightFlagellumTip-Y, " +
                    "LeftFlagellumBase-X, LeftFlagellumBase-Y, RightFlagellumBase-X, RightFlagellumBase-Y, " +
                    "RotationAngle, AntennaSensorWidth, AntennaSensorHeight, " +
                    "AntennaSensorOffset-X, AntennaSensorOffset-Y, AntennaSensorScale-X, AntennaSensorScale-Y" //, " +
                    //"SectorData"
                );

                var data = ((EfficientTipAndPERdetector)_processor).Results;

                if (data == null)
                    return;

                var frames = data.Keys.OrderBy(frameIndex => frameIndex).ToList();

                Dispatcher.Invoke(() => btnSaveActivity.Content = "Saving...");

                frames.ForEach(frameIndex =>
                {
                    var value = data[frameIndex];
                    var recordingConditions = value?.Left?.Tip?.Space;

                    var line = string.Join(",", new object[]
                    {
                        AppSettings.Default.VideoLabel, frameIndex, value?.TreatmentSensorValue,

                        value?.Proboscis?.Tip?.FramePoint.X,
                        value?.Proboscis?.Tip?.FramePoint.Y,

                        value?.Left?.DominantSector,
                        value?.Right?.DominantSector,

                        value?.Left?.Tip?.FramePoint.X, value?.Left?.Tip?.FramePoint.Y,
                        value?.Right?.Tip?.FramePoint.X, value?.Right?.Tip?.FramePoint.Y,

                        value?.Left?.Base.FramePoint.X, value?.Left?.Base.FramePoint.Y,
                        value?.Right?.Base.FramePoint.X, value?.Right?.Base.FramePoint.Y,

                        recordingConditions?.HeadAngle,

                        recordingConditions?.HeadDims.X,
                        recordingConditions?.HeadDims.Y,

                        recordingConditions?.HeadOffset.X,
                        recordingConditions?.HeadOffset.Y,

                        recordingConditions?.ScaleX,
                        recordingConditions?.ScaleX
                    });

                    //string sectorData = "";

                    //if (value.Left.SectorCounts != null && value.Right.SectorCounts != null)
                    //{
                    //    sectorData = string.Join(",", value.Left.SectorCounts) + "," + string.Join(",", value.Right.SectorCounts);
                    //}

                    writer.WriteLine(line);// + ", " + sectorData);

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(Pipeline.Supervisor.State != VideoPlayer.Pipeline.WorkState.Working)
                Play(oneFrame: true);
        }
        
        private void btnShowBatchList_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            WindowManager.ShowBatchWindow();
        }

        private void btnSaveBatchParams_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.BatchWindow.SaveParams(AppSettings.Default);
            WindowManager.ShowBatchWindow();
            Hide();
        }

        private void txtHeadX_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Pipeline?.VideoInfo == null)
                return;

            int newVal = 0;

            if (int.TryParse(((TextBox)sender).Text, out newVal) && newVal >= 0)
            {
                AppSettings.Default.HeadX = newVal;
                RefreshMostRecentFrame();
                SyncAntennaSensor();
            }

            Stop();
        }

        private void txtHeadY_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Pipeline?.VideoInfo == null)
                return;

            int newVal = 0;

            if (int.TryParse(((TextBox)sender).Text, out newVal) && newVal >= 0)
            {
                AppSettings.Default.HeadY = newVal;
                RefreshMostRecentFrame();
                SyncAntennaSensor();
            }

            Stop();
        }

        private void txtHeadScale_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Pipeline?.VideoInfo == null)
                return;

            double newVal = 0;

            if (double.TryParse(((TextBox)sender).Text, out newVal) && newVal >= 0.1 && newVal <= 5)
            {
                AppSettings.Default.HeadScale = newVal;
                RefreshMostRecentFrame();
                SyncAntennaSensor();
            }

            Stop();
        }

        private void txtHeadAngle_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Pipeline?.VideoInfo == null)
                return;

            int newVal = 0;

            if (int.TryParse(((TextBox)sender).Text, out newVal))
            {
                AppSettings.Default.HeadAngle = newVal;
                RefreshMostRecentFrame();
                SyncAntennaSensor();
            }

            Stop();
        }

        private void txtTreatmentY_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Pipeline?.VideoInfo == null)
                return;
                
            int y = 0;

            if (int.TryParse(txtTreatmentY?.Text, out y) && y >= 0)
            {
                AppSettings.Default.TreatmentSensor = new System.Drawing.Point(AppSettings.Default.TreatmentSensor.X, y);
                LoadFromSettings();
                SyncTreatmentSensor();
            }
        }

        private void txtTreatmentX_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Pipeline?.VideoInfo == null)
                return;

            int x = 0;

            if (int.TryParse(txtTreatmentX?.Text, out x) && x >= 0)
            {
                AppSettings.Default.TreatmentSensor = new System.Drawing.Point(x, AppSettings.Default.TreatmentSensor.Y);
                LoadFromSettings();
                SyncTreatmentSensor();
            }
        }

        ExclusionZoneManager exclusionManager = new ExclusionZoneManager();
        private void btnAddExclusion_Click(object sender, RoutedEventArgs e)
        {
            exclusionManager.window = this;
            exclusionManager.AddClicked();
        }
        
        private void exclusionShim_MouseDown(object sender, MouseButtonEventArgs e)
        {
            exclusionManager.MouseDown();
        }

        private void exclusionShim_MouseMove(object sender, MouseEventArgs e)
        {
            exclusionManager.MouseMove();
        }

        private void btnRemoveExclusion_Click(object sender, RoutedEventArgs e)
        {
            exclusionManager.RemoveClicked();
        }

        private void chkShowFilterPoints_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Default.ShowModel = chkShowFilterPoints.IsChecked.Value;
            AppSettings.Default.SaveAsync();
        }

        private void txtVideoLabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.Default.VideoLabel = txtVideoLabel.Text;
            AppSettings.Default.SaveAsync();
        }

        private void txtVideoLabelColumn_TextChanged(object sender, TextChangedEventArgs e)
        {

            AppSettings.Default.VideoLabelColumn = txtVideoLabelColumn.Text;
            AppSettings.Default.SaveAsync();
        }
    }
}