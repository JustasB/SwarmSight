using Classes;
using Settings;
using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking;

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
using DPoint = System.Drawing.Point;
using System.Deployment.Application;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ProcessorWindow
    {
        private WindowManager WindowManager
        {
            get
            {
                return ((WindowManager)Application.Current);
            }
        }

        public EfficientTipAndPERdetector Processor;
        public VideoPlayerController Controller;

        public ProcessorWindow()
        {
            AppSettings.Default.Upgrade();
            
            InitializeComponent();

            try { Title += ApplicationDeployment.CurrentDeployment.CurrentVersion; } catch { }

            Controller = new VideoPlayerController
            {
                btnBrowse = btnBrowse,
                btnPlayPause = btnPlayPause,
                btnSave = btnSaveActivity,
                btnStop = btnStop,
                btnStepFrame = btnStepFrame,
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

            sliderX.Label = "X:";
            sliderX.SettingsKey = "HeadX";
            sliderX.IsEnabled = false;
            sliderX.LoadFromSettings();
            sliderX.OnChanged += SyncAntennaSensor;
            sliderX.OnBeginChange += Controller.Stop;

            sliderY.Label = "Y:";
            sliderY.SettingsKey = "HeadY";
            sliderY.IsEnabled = false;
            sliderY.LoadFromSettings();
            sliderY.OnChanged += SyncAntennaSensor;
            sliderY.OnBeginChange += Controller.Stop;

            sliderScale.Label = "Scale:";
            sliderScale.SettingsKey = "HeadScale";
            sliderScale.IsValueInt = false;
            sliderScale.IsEnabled = false;
            sliderScale.LoadFromSettings();
            sliderScale.OnChanged += SyncAntennaSensor;
            sliderScale.OnBeginChange += Controller.Stop;

            sliderAngle.Label = "Angle:";
            sliderAngle.SettingsKey = "HeadAngle";
            sliderAngle.IsValueInt = false;
            sliderAngle.IsEnabled = false;
            sliderAngle.LoadFromSettings();
            sliderAngle.OnChanged += SyncAntennaSensor;
            sliderAngle.OnBeginChange += Controller.Stop;

            sliderTreatX.Label = "X:";
            sliderTreatX.SettingsKey = "TreatmentSensorX";
            sliderTreatX.IsEnabled = false;
            sliderTreatX.LoadFromSettings();
            sliderTreatX.OnChanged += SyncTreatmentSensor;

            sliderTreatY.Label = "Y:";
            sliderTreatY.SettingsKey = "TreatmentSensorY";
            sliderTreatY.IsEnabled = false;
            sliderTreatY.LoadFromSettings();
            sliderTreatY.OnChanged += SyncTreatmentSensor;

            sliderFast.Label = "Fast Motion Threshold:";
            sliderFast.SettingsKey = "FastThreshold";
            sliderFast.LoadFromSettings();

            sliderSlow.Label = "Slow Motion Threshold:";
            sliderSlow.SettingsKey = "SlowThreshold";
            sliderSlow.LoadFromSettings();

            sliderStationary.Label = "Stationary Threshold:";
            sliderStationary.SettingsKey = "StationaryThreshold";
            sliderStationary.LoadFromSettings();

            SizeChanged += (sender, e) =>
            {
                SetupCanvas();

                SyncAntennaSensor();
                SyncTreatmentSensor();
                SyncExclusionZones();
            };

            Closing += (sender, args) =>
            {
                Controller.Stop();
                Hide();
                WindowManager.ExitIfLastWindow();
            };

            WindowManager.Exit += (sender, args) => Controller.Stop();
        }

        private void OnFinishSetupPlayer()
        {
            InitDetector();

            ShowHideModelViews(hide: true);
        }

        public double CanvasScale = 1.0;
        private void SetupCanvas()
        {
            if (Controller.Pipeline.VideoInfo == null)
                return;

            CanvasScale = videoCanvas.ActualWidth / Controller.Pipeline.VideoInfo.Width;
            canvasRow.Height = new GridLength(Controller.Pipeline.VideoInfo.Height * CanvasScale + videoCanvas.Margin.Top);

            scrollViewer.Height = canvasRow.Height.Value + 30;

            Height = double.NaN;
            SizeToContent = SizeToContent.Height;
        }

        private void SetupAntennaSensor()
        {
            antennaSensor.Canvas = videoCanvas;
            antennaSensor.CanvasScale = CanvasScale;
            antennaSensor.Dimensions = new Point(AppSettings.Default.HeadScale * 100, AppSettings.Default.HeadScale * 100);
            antennaSensor.Position = Controller.ToCanvasCoordinates(AppSettings.Default.Origin);
            antennaSensor.Angle = AppSettings.Default.HeadAngle;

            antennaSensor.MouseDown += (s, e) =>
            {
                Controller.Pause();
            };

            antennaSensor.Moved += (s, e) =>
            {
                var loc = Controller.ToVideoCoordinates(antennaSensor.Position);

                AppSettings.Default.HeadX = loc.X;
                AppSettings.Default.HeadY = loc.Y;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();

                Controller.RefreshMostRecentFrame();

                Controller.Stop();
            };

            antennaSensor.Scaled += (s, e) =>
            {
                var loc = Controller.ToVideoCoordinates(antennaSensor.Position);
                var dims = Controller.ToVideoCoordinates(antennaSensor.Dimensions);

                AppSettings.Default.HeadX = loc.X;
                AppSettings.Default.HeadY = loc.Y;
                AppSettings.Default.HeadScale = antennaSensor.Scale;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();

                Controller.RefreshMostRecentFrame();

                Controller.Stop();
            };

            antennaSensor.Rotated += (s, e) =>
            {
                AppSettings.Default.HeadAngle = antennaSensor.Angle;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();

                Controller.RefreshMostRecentFrame();

                Controller.Stop();
            };
        }

        private void SetupExclusionZones()
        {
            exclusionManager.window = this;
            exclusionManager.LoadFromSettings();

            exclusionManager.Changed += (s, e) =>
            {
                Controller.Stop();
            };
        }

        private void SetupTreatmentSensor()
        {
            treatmentSensor.Canvas = videoCanvas;
            treatmentSensor.Position = Controller.ToCanvasCoordinates(new DPoint(AppSettings.Default.TreatmentSensorX, AppSettings.Default.TreatmentSensorY));
            
            treatmentSensor.Moved += (s, e) =>
            {
                var loc = Controller.ToVideoCoordinates(treatmentSensor.Position);

                AppSettings.Default.TreatmentSensorX = loc.X;
                AppSettings.Default.TreatmentSensorY = loc.Y;
                AppSettings.Default.SaveAsync();

                LoadFromSettings();
            };
        }

        private void LoadFromSettings()
        {
            ValidateSettings();

            var headPos = new System.Drawing.Point(AppSettings.Default.HeadX, AppSettings.Default.HeadY);
            var dim = AppSettings.Default.HeadScale * 100 * CanvasScale;

            sliderX.Max = AppSettings.Default.MaxHeadX;
            sliderX.IsEnabled = true;
            sliderX.LoadFromSettings();

            sliderY.Max = AppSettings.Default.MaxHeadY;
            sliderY.LoadFromSettings();
            sliderY.IsEnabled = true;

            sliderScale.Max = AppSettings.Default.MaxHeadScale;
            sliderScale.Min = 0.1;
            sliderScale.LoadFromSettings();
            sliderScale.IsEnabled = true;

            sliderAngle.Max = 360;
            sliderAngle.Min = -360;
            sliderAngle.LoadFromSettings();
            sliderAngle.IsEnabled = true;

            sliderTreatX.Max = AppSettings.Default.MaxTreatX;
            sliderTreatX.LoadFromSettings();
            sliderTreatX.IsEnabled = true;

            sliderTreatY.Max = AppSettings.Default.MaxTreatY;
            sliderTreatY.LoadFromSettings();
            sliderTreatY.IsEnabled = true;

            chkShowFilterPoints.IsChecked = AppSettings.Default.ShowModel;
            txtVideoLabel.Text = AppSettings.Default.VideoLabel;
            txtVideoLabelColumn.Text = AppSettings.Default.VideoLabelColumn;

        }

        private void ValidateSettings()
        {
            AppSettings.Default.Validate(Controller.Pipeline.VideoInfo.Dimensions);
        }

        private void SyncAntennaSensor()
        {
            var headPos = new System.Drawing.Point(AppSettings.Default.HeadX, AppSettings.Default.HeadY);
            var dim = AppSettings.Default.HeadScale * 100 * CanvasScale;
            
            if (Controller?.Pipeline?.VideoInfo != null)
            {
                antennaSensor.Position = Controller.ToCanvasCoordinates(headPos);
                antennaSensor.Dimensions = new Point(dim, dim);
            }

            Controller.RefreshMostRecentFrame();
        }

        private void SyncTreatmentSensor()
        {
            var pos = new System.Drawing.Point(AppSettings.Default.TreatmentSensorX, AppSettings.Default.TreatmentSensorY);
            
            if (Controller?.Pipeline?.VideoInfo != null)
            {
                treatmentSensor.Position = Controller.ToCanvasCoordinates(pos);
            }
        }

        private void SyncExclusionZones()
        {
            exclusionManager.LoadFromSettings();
        }

        private void ShowHideModelViews(bool hide)
        {
            modelView.Visibility = 
            sectorView.Visibility = 
            treatmentSensor.Visibility = 
            antennaSensor.Visibility = 
            lblFPS.Visibility = 
            btnAddExclusion.Visibility = 
                hide ? Visibility.Collapsed : Visibility.Visible;
        }

        public void LoadParamsFromJSON(string newParams)
        {
            AppSettings.Default.LoadFromJSON(newParams);
        }

        private void OnOpen()
        {
            ValidateSettings();

            SetupCanvas();
            SetupAntennaSensor();
            SetupTreatmentSensor();
            SetupExclusionZones();
            LoadFromSettings();
            
            SyncAntennaSensor();
            SyncTreatmentSensor();
            SyncExclusionZones();

            ShowHideModelViews(hide: false);

            Dispatcher.InvokeAsync(() => {

                Thread.Sleep(500);
                WindowManager.CenterWindowOnScreen(this);

            });
            
        }        
        
        private void OnShowFrame(Frame frame)
        {
            if (frame.ProcessorResult != null)
            {
                var frameResult = (EfficientTipAndPERdetector.TipAndPERResult)frame.ProcessorResult;

                treatmentSensor.SensorValue = frameResult.TreatmentSensorValue.ToString();

                modelView.Show(frameResult);
                sectorView.Show(frameResult);
            }
        }

        private void OnRefreshMostRecentFrame(Frame frame)
        {
            Processor.Annotate(frame);
        }
        
        private void InitDetector()
        {
            //Preserve existing frame results if resetting
            Dictionary<int, EfficientTipAndPERdetector.TipAndPERResult> prevData = null;

            if (Processor != null)
            {
                prevData = Processor.Results;
            }

            Controller.Pipeline.VideoProcessor = Processor = new EfficientTipAndPERdetector();

            if (prevData != null)
                Processor.Results = prevData;
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

            using (var writer = new StreamWriter(fileInfo.FullName + "_Tracker_" + Controller.GetCSVfileEnding(), false))
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

                var data = Processor.Results;

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