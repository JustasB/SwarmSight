using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Classes;
using SwarmVision.VideoPlayer;
using OxyPlot.Wpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;

namespace SwarmVision
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Current;

        private VideoDecoder _decoder;
        public static int ResumeFrame = 300; // Start at 10s 

        public MainWindow()
        {
            Current = this;

            InitializeComponent();

            Closed += (sender, args) => Environment.Exit(0);

            this.KeyUp += Grid_PreviewKeyUp;

            //TEST VIDEO
            txtFileName.Text =
                @"Y:\Documents\Dropbox\Research\Christina Burden\out.mp4";
            
            SetupPlayer();
        }

        private void SetupPlayer()
        {
            if (_decoder != null)
            {
                _decoder.Stop();
                _decoder.Dispose();
                _decoder = null;
            }

            string file = "";

            Dispatcher.Invoke(() => file = txtFileName.Text);

            _decoder = new VideoDecoder();
            _decoder.Open(file);

            Dispatcher.Invoke(() =>
            {
                sliderTime.Minimum = 0;
                sliderTime.Maximum = _decoder.VideoInfo.TotalFrames - 1;
            });

            _decoder.SeekTo(ResumeFrame);
            _decoder.Start();
            _decoder.FrameDecoder.FrameBufferCapacity = 2;
            _decoder.FrameDecoder.MinimumWorkingFrames = 1;
        }

        //private bool Open()
        //{
        //    var videoFile = txtFileName.Text;

        //    if (!System.IO.File.Exists(videoFile))
        //    {
        //        MessageBox.Show("Please select a valid video file");
        //        return false;
        //    }

        //    _decoder.Open(videoFile);

        //    return true;
        //}


        //private void Play()
        //{
        //    //Play
        //    if (btnPlayPause.Content.ToString() == PlaySymbol)
        //    {
        //        //Reset decoder
        //        if (_decoder != null)
        //        {
        //            _decoder.Dispose();
        //            _decoder = null;
        //            _decoder = new VideoDecoder();
        //            _decoder.Processor = _processor;
        //            _decoder.Open(txtFileName.Text);
        //            _comparer.Decoder = _decoder;
        //        }

        //        //Can't change quality if playing
        //        sliderQuality.IsEnabled = false;

        //        //Clear chart points after the current position
        //        _chart.ClearAfter(_comparer.MostRecentFrameIndex);
        //        _activity.RemoveAll(p => p.X > _comparer.MostRecentFrameIndex);

        //        //Adjust for any quality changes, before starting again
        //        _decoder.PlayerOutputWidth = (int) (_decoder.VideoInfo.Width*_quality);
        //        _decoder.PlayerOutputHeight = (int) (_decoder.VideoInfo.Height*_quality);

        //        //Setup fps counter
        //        _fpsStartFrame = _comparer.MostRecentFrameIndex;
        //        _fpsStopwatch.Restart();

        //        //Play or resume
        //        _comparer.Start();

        //        btnPlayPause.Content = PauseSymbol;
        //    }
        //    else //Pause
        //    {
        //        btnPlayPause.Content = PlaySymbol;
        //        _comparer.Pause();
        //    }
        //}

        //private void Stop()
        //{
        //    _comparer.Stop();
        //    _renderer.Stop();
        //    Reset();
        //}

        private void SeekTo(int frame)
        {
            //_comparer.SeekTo(percentLocation);
        }

        //private void OnStopped(object sender, EventArgs eventArgs)
        //{
        //    Reset();
        //}

        //private void ShowFrame(Frame frame)
        //{
        //    if (Application.Current == null)
        //        return;

        //    Application.Current.Dispatcher.Invoke(new Action(() =>
        //        {
        //            if (Application.Current == null)
        //                return;

        //            using (var memory = new MemoryStream())
        //            {
        //                frame.Bitmap.Save(memory, ImageFormat.Bmp);
        //                memory.Position = 0;
        //                var bitmapImage = new BitmapImage();
        //                bitmapImage.BeginInit();
        //                bitmapImage.StreamSource = memory;
        //                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //                bitmapImage.EndInit();

        //                videoCanvas.Source = bitmapImage;
        //            }

        //            _sliderValueChangedByCode = true;
        //            sliderTime.Value = frame.FramePercentage*1000;
        //        }));
        //}

        //private void OnFrameReadyToRender(object sender, OnFrameReady e)
        //{
        //    ShowFrame(e.Frame);
        //}

        private void Reset()
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    _sliderValueChangedByCode = true;
                    sliderTime.Value = 0;
                });
        }

        private void txtFileName_LostFocus(object sender, RoutedEventArgs e)
        {
            //Stop();
            //Reset();
            //Open();
        }

        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();

            var result = ofd.ShowDialog();

            if (result == false)
                return;

            txtFileName.Text = ofd.FileName;

            try
            {
                var maxFrame = File
                    .ReadAllLines(txtFileName.Text + "_HandAnnotated.csv")
                    .Skip(1)
                    .Select(l => int.Parse(l.Split(',')[0]))
                    .Max();

                var decision = MessageBox.Show(
                    string.Format("The associated .CSV file has data for frame {0}. Do you want to resume from frame {1}?", maxFrame, maxFrame+1), 
                    "Resume where left off?", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question
                );

                if (decision == MessageBoxResult.Yes)
                    ResumeFrame = maxFrame + 1;
            }
            catch { } //Ignore if the file is unreadable 


            //Stop();
            //Reset();
            //Open();
        }

        //private void OnPlayClicked(object sender, RoutedEventArgs e)
        //{
        //    Play();
        //}

        //private void OnStopClicked(object sender, RoutedEventArgs e)
        //{
        //    Stop();
        //}

        //private void sliderTime_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    //Pause if slider is clicked
        //    if (!_comparer.IsPaused)
        //        Play(); //Pause
        //}

        //private void sliderTime_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    sliderTime_MouseDown(sender, e);
        //}

        private bool _sliderValueChangedByCode;

        private void timeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_sliderValueChangedByCode)
            {
                _sliderValueChangedByCode = false;
                return;
            }

            ResumeFrame = (int)sliderTime.Value;

            var resumeTime = TimeSpan.FromMilliseconds(ResumeFrame / (1.0 * _decoder.VideoInfo.TotalFrames)
                                      * _decoder.VideoInfo.Duration.TotalMilliseconds);

            lblTime.Content = resumeTime.ToString(@"mm\:ss") + " (" + ResumeFrame + ")";
        }

        private void sliderTime_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SetupPlayer();
        }

        private void btnSaveActivity_Click(object sender, RoutedEventArgs e)
        {
            new Thread(SaveCSV) { IsBackground = true }.Start(txtFileName.Text);
        }

        private Dictionary<int, Dictionary<string, System.Drawing.Point?>> FrameData = new Dictionary<int, Dictionary<string, System.Drawing.Point?>>();
        public static bool IsMarking;
        public static bool ReadyForNextFrame = false;
        private void StartStopMarking(object sender, RoutedEventArgs e)
        {
            IsMarking = true;

            SetupPlayer();

            videoCanvas.Focus();

            Thread.Sleep(50);
            HideInstructions();
            

            new Thread(() =>
            {
                while (IsMarking)
                {
                    using (var nextFrame = _decoder.PlayNextFrame())
                    {
                        if (nextFrame != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ShowFrame(nextFrame.Bitmap);
                                lblTime.Content = nextFrame.FrameTime.ToString(@"mm\:ss") + " (" + nextFrame.FrameIndex + ")";
                            });

                            ReadyForNextFrame = false;

                            while (!ReadyForNextFrame && IsMarking)
                            {
                                Thread.Sleep(10); //Wait for position data
                            }

                            //Store the position data
                            if (!FrameData.ContainsKey(nextFrame.FrameIndex))
                                FrameData[nextFrame.FrameIndex] = new Dictionary<string, System.Drawing.Point?>();

                            FrameData[nextFrame.FrameIndex][TemplateView.Current.CurrentPartName] = _capturedMousePosition;

                            UpdateReward();

                            templateView.AdvanceBurst();

                            if (templateView.AtBatchStart && _decoder.VideoInfo.TotalFrames > nextFrame.FrameIndex + 1)
                            {
                                ResumeFrame = nextFrame.FrameIndex + 1; //Save current position for later resuming
                                
                                IsMarking = false;
                                ShowInstructions();
                                return;
                            }
                            
                            if (templateView.AtBurstStart)
                            {
                                SetupPlayer(); //Rewind to first frame
                                IsMarking = false;
                                ShowInstructions();
                                return;
                            }
                        }
                    }

                    Thread.Sleep(10);
                }

            }).Start();
        }

        private void UpdateReward()
        {
            if (templateView.CurrentBurstPosition == 0)
                ShowReward("Very Good! Keep Going...");

            else if (templateView.CurrentBurstPosition == 1)
                ShowReward("Excellent!");

            else if (templateView.CurrentBurstPosition == templateView.BurstSize - 2)
                ShowReward("Last one!");

            else if (templateView.CurrentBurstPosition == templateView.BurstSize - 1)
                ShowReward("Great job!");

            else if (templateView.CurrentBurstPosition == templateView.BurstSize - 10 - 1)
                ShowReward((templateView.BurstSize - templateView.CurrentBurstPosition - 1) + " more to go");
        }

        private void ShowReward(string text)
        {
            Dispatcher.Invoke(() =>
                {
                    lblReward.Visibility = Visibility.Visible;
                    lblReward.Content = text;
                });
        }

        private void HideInstructions()
        {
            Dispatcher.Invoke(() =>
            {
                smallTemplate.Visibility = Visibility.Visible;
                smallTemplate.Source = templateView.partImage.Source;
                pnlInstructions.Visibility = Visibility.Hidden;
            });
        }
        private void ShowInstructions()
        {
            Dispatcher.Invoke(() =>
            {
                templateView.UpdateView();

                smallTemplate.Visibility = Visibility.Hidden;
                pnlInstructions.Visibility = Visibility.Visible;

                btnStartStopMarking.Focus();
            });
        }

        private void ShowFrame(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                videoCanvas.Source = bitmapImage;
            }
        }

        private void videoCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CaptureMousePosition(e.ChangedButton == MouseButton.Right);
        }

        private void Grid_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!IsMarking)
                return;

            if (e.Key == Key.Q)
            {
                CaptureMousePosition(false);
            }
            if (e.Key == Key.E)
            {
                CaptureMousePosition(true);
            }
        }

        /// <summary>
        /// Translates the mouse coordinates to video coordinates
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Point RelativeToVideo(Point target)
        {
            var videoWidth = _decoder.VideoInfo.Width;
            var videoHeight = _decoder.VideoInfo.Height;

            var screenWidth = videoCanvas.ActualWidth;
            var screenHeight = videoCanvas.ActualHeight;

            return new Point
            (
                x: (int)Math.Round(target.X/screenWidth*videoWidth), 
                y: (int)Math.Round(target.Y/screenHeight*videoHeight)
            );
        }

        private Point RelativeToHead(Point headTop, Point headBack, Point target)
        {
            //Find head center by taking the midpoint of top and back
            var headCenter = new Point
            (
                x: (int) Math.Round((headTop.X + headBack.X)/2.0),
                y: (int) Math.Round((headTop.Y + headBack.Y)/2.0)
            );

            //Find the head angle, where 0 degrees is pointing right and 90 is up
            var headAngle = Math.Atan2(x: headTop.X - headBack.X, y: -(headTop.Y - headBack.Y)) * 180.0 / Math.PI;

            //Find the head length
            var headLength = Math.Sqrt(Math.Pow(headTop.X - headBack.X, 2) + Math.Pow(headTop.Y - headBack.Y, 2));

            //Find distance to target
            var targetDistance = Math.Sqrt(Math.Pow(headCenter.X - target.X, 2) + Math.Pow(headCenter.Y - target.Y, 2));

            //Find angle to target (0 deg = right)
            var targetAngle = Math.Atan2(x: target.X - headCenter.X, y: -(target.Y - headCenter.Y)) * 180.0 / Math.PI;

            var targetAngleRelativeToHead = targetAngle - headAngle;

            var targetDistanceInTermsOfHeadLength = targetDistance/headLength;

            return new Point
            (
                x: (int)Math.Round(100.0 * targetDistanceInTermsOfHeadLength * Math.Cos((targetAngleRelativeToHead+90) * Math.PI / 180.0)),
                y: (int)Math.Round(100.0 * targetDistanceInTermsOfHeadLength * Math.Sin((targetAngleRelativeToHead+90) * Math.PI / 180.0))
            );
        }

        private static Point? _capturedMousePosition;
        private void CaptureMousePosition(bool skipped)
        {
            if (!skipped)
            {
                var pos = Mouse.GetPosition(videoCanvas);
                _capturedMousePosition = RelativeToVideo(new Point((int) pos.X, (int) pos.Y));
            }
            else
            {
                _capturedMousePosition = null;
            }

            ReadyForNextFrame = true;
        }

        private void SaveCSV(object videoFileName)
        {
            if (FrameData.Count == 0)
                return;

            Dispatcher.Invoke(() => btnSaveActivity.Content = "Saving...");

            var fileInfo = new FileInfo(videoFileName.ToString());
            var csvFile = fileInfo.FullName + "_HandAnnotated.csv";
            var csvExists = File.Exists(csvFile);

            using (var writer = new StreamWriter(csvFile, csvExists))
            {
                //Column names based on template keys
                var columns = templateView.TemplatePaths.Keys.ToList();

                //Split into X & Y and add Relative to Head Columns
                if (!csvExists)
                    writer.WriteLine("Frame, " + string.Join(",", columns.SelectMany(k => new[] {k + "X", k + "Y"}).ToList()));
                
                //Write out the CSV line
                foreach (var f in FrameData.Keys)
                {
                    if (!FrameData.ContainsKey(f))
                        continue;

                    var sb = new StringBuilder();

                    Point? headTop = null, headBack = null;

                    if (FrameData[f].ContainsKey("Center of Mouth") &&
                        FrameData[f].ContainsKey("Back of the Head") &&
                        FrameData[f]["Center of Mouth"].HasValue &&
                        FrameData[f]["Back of the Head"].HasValue)
                    {
                        headTop = FrameData[f]["Center of Mouth"].Value;
                        headBack = FrameData[f]["Back of the Head"].Value;
                    }

                    columns.ForEach(c =>
                    {
                        //Replace video coordinates with head relative coordinates
                        if (headBack != null && headTop != null &&
                            FrameData[f].ContainsKey(c) && FrameData[f][c].HasValue)
                        {
                            FrameData[f][c] = RelativeToHead
                            (
                                headTop.Value,
                                headBack.Value,
                                FrameData[f][c].Value
                            );
                        }

                        if (FrameData[f].ContainsKey(c))
                        {
                            var point = FrameData[f][c];

                            if(point.HasValue)
                                sb.Append("," + point.Value.X + "," + point.Value.Y);
                            else
                                sb.Append(",NA,NA");
                        }
                        else
                            sb.Append(",NA,NA");
                    });
                    
                    writer.WriteLine(f + sb.ToString());
                }
    
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
    }
}