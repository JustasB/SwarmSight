using Classes;
using SwarmSight.Filters;
using SwarmSight.VideoPlayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DPoint = System.Drawing.Point;

namespace SwarmSight
{
    public class VideoPlayerController
    {
        public TextBox txtFileName;
        public Button btnBrowse;
        public Image Canvas;
        public Button btnPlayPause;
        public Button btnStop;
        public Button btnStepFrame;
        public Slider sliderTime;
        public Label lblTime;
        public Label lblFPS;
        public Button btnSave;
        public VideoProcessorBase Processor;

        public VideoPipeline Pipeline;
        public double Quality = 1.0;


        private const int maxFps = 10;
        
        public event Action OnFinishSetupPlayer;
        public event Action OnOpen;
        public event Action OnReset;
        public event Action<Filters.Frame> OnShowFrame;
        public event Action<Filters.Frame> OnRefreshMostRecentFrame;
        public event Action OnAfterStopped;
        public event Action OnStartPlaying;
        public event Action<Filters.Frame> OnProcessed;

        public void Init()
        {
            btnPlayPause.FontFamily = 
                btnStop.FontFamily = 
                btnStepFrame.FontFamily = 
                new FontFamily(Constants.SymbolFontFamily);

            btnPlayPause.Content = Constants.PlaySymbol;
            btnStop.Content = Constants.StopSymbol;
            btnStepFrame.Content = Constants.StepSymbol;

            txtFileName.LostFocus += txtFileName_LostFocus;
            btnBrowse.Click += OnBrowseClicked;
            btnPlayPause.Click += OnPlayClicked;
            btnStop.Click += OnStopClicked;
            btnStepFrame.Click += OnStepClicked;
            sliderTime.MouseDown += sliderTime_MouseDown;
            sliderTime.PreviewMouseDown += sliderTime_MouseDown;
            sliderTime.ValueChanged += timeSlider_ValueChanged;

            SetupPlayer();
        }

        public void SetupPlayer()
        {
            Pipeline = new VideoPipeline();
            Pipeline.FrameReady += OnFrameReadyToRender;
            Pipeline.Stopped += OnStopped;

            if(OnFinishSetupPlayer != null)
                OnFinishSetupPlayer();
        }

        public void LoadFile(bool oneFrame = true)
        {
            Stop();
            Reset();
            Open();
            Play(oneFrame);
        }

        public void Open()
        {
            var videoFile = txtFileName.Text;

            if (!System.IO.File.Exists(videoFile))
            {
                MessageBox.Show("Please select a valid video file");
                return;
            }

            Pipeline.Open(videoFile);

            if (OnOpen != null)
                OnOpen();

            Stop();
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

                if (OnStartPlaying != null)
                    OnStartPlaying();

                //Adjust for any quality changes, before starting again
                Pipeline.Supervisor.Processor.VD.PlayerOutputWidth = (int)(Pipeline.VideoInfo.Width * Quality);
                Pipeline.Supervisor.Processor.VD.PlayerOutputHeight = (int)(Pipeline.VideoInfo.Height * Quality);

                if (oneFrame)
                    Pipeline.RunOneFrame();
                else
                    Pipeline.Start();

                if (!oneFrame)
                    btnPlayPause.Content = Constants.PauseSymbol;
            }
        }

        public void Pause()
        {
            if (Pipeline.Supervisor.State == VideoPlayer.Pipeline.WorkState.Working)
            {
                btnPlayPause.Content = Constants.PlaySymbol;
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
        
        public void Reset()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    btnPlayPause.Content = Constants.PlaySymbol;

                    _sliderValueChangedByCode = true;
                    sliderTime.Value = 0;

                    if (OnReset != null)
                        OnReset();
                });
            }
            catch { }
        }

        public void SeekTo(double percentLocation)
        {
            if (Pipeline == null || Pipeline.VideoInfo == null)
                return;

            Pipeline.Seek(percentLocation);

            lblTime.Content = TimeSpan.FromMilliseconds(Pipeline.VideoInfo.Duration.TotalMilliseconds * percentLocation).ToString();
        }
        
        private void OnFrameReadyToRender(object sender, OnFrameReady e)
        {
            if (OnProcessed != null)
                OnProcessed(e.Frame);

            ShowFrame(e.Frame);
        }

        private void OnStopped(object sender, EventArgs eventArgs)
        {
            if (Application.Current == null)
                return;

            Reset();

            try
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if(OnAfterStopped != null)
                        OnAfterStopped();
                }));
            }
            catch { }
        }

        public static LinkedList<DateTime> fpsHist = new LinkedList<DateTime>();
        private static bool isDrawing = false;
        private static WriteableBitmap canvasBuffer;
        private static DateTime prevFrameTime = DateTime.Now;
        private static Filters.Frame mostRecentFrame;
        private void ShowFrame(Filters.Frame frame)
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
                if (canvasBuffer == null || canvasBuffer.Height != frame.Height || canvasBuffer.Width != frame.Width)
                {
                    canvasBuffer = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr24, null);
                    Canvas.Source = canvasBuffer;

                    mostRecentFrame = frame.Clone();
                }

                //Limit frame rate shown
                if ((DateTime.Now - prevFrameTime).TotalMilliseconds >= 1000.0 / maxFps)
                {
                    //Save the most recent frame
                    mostRecentFrame.DrawFrame(frame);
                    mostRecentFrame.ProcessorResult = frame.ProcessorResult;
                    mostRecentFrame.FrameIndex = frame.FrameIndex;
                    
                    if (OnShowFrame != null)
                        OnShowFrame(frame);

                    RefreshMostRecentFrame();

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

        private Filters.Frame tempFrame;
        public void RefreshMostRecentFrame()
        {
            if (mostRecentFrame == null)
                return;

            //Prepare the temp frame for annotations
            if (tempFrame == null || !tempFrame.SameSizeAs(mostRecentFrame))
                tempFrame = mostRecentFrame.Clone();
            else
                tempFrame.DrawFrame(mostRecentFrame);

            tempFrame.FrameIndex = mostRecentFrame.FrameIndex;
            tempFrame.ProcessorResult = mostRecentFrame.ProcessorResult;

            if (OnRefreshMostRecentFrame != null)
                OnRefreshMostRecentFrame(tempFrame);

            tempFrame.CopyToWriteableBitmap(canvasBuffer);
        }

        private void txtFileName_LostFocus(object sender, RoutedEventArgs e)
        {
            LoadFile();
        }
        
        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            Stop();

            var ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = Constants.VideoFileFilter;
            var result = ofd.ShowDialog();

            if (result == false)
                return;

            txtFileName.Text = ofd.FileName;

            Stop();
            Reset();
            Open();
            Play(oneFrame: true);
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

        private bool _sliderValueChangedByCode;
        private void timeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_sliderValueChangedByCode)
            {
                _sliderValueChangedByCode = false;
                return;
            }

            SeekTo(e.NewValue / 1000.0);
        }

        private void OnStepClicked(object sender, RoutedEventArgs e)
        {
            if (Pipeline.Supervisor.State != VideoPlayer.Pipeline.WorkState.Working)
                Play(oneFrame: true);
        }


        public System.Drawing.Point ToVideoCoordinates(Point source)
        {
            return new System.Drawing.Point(
                ((int)(Pipeline.VideoInfo.Width * Quality) * source.X / Canvas.ActualWidth).Rounded(),
                ((int)(Pipeline.VideoInfo.Height * Quality) * source.Y / Canvas.ActualHeight).Rounded()
            );
        }

        public Point ToCanvasCoordinates(System.Drawing.Point source)
        {
            var x = Canvas.ActualWidth * source.X / (int)(Pipeline.VideoInfo.Width * Quality);
            var y = Canvas.ActualHeight * source.Y / (int)(Pipeline.VideoInfo.Height * Quality);

            return new Point(x, y);
        }

        public string GetCSVfileEnding()
        {
            return Environment.UserName + "_" + DateTime.Now.ToString("yyyy-MM-dd hh-mm") + ".csv";
        }
    }
}
