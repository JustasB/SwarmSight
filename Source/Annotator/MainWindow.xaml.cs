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
using SwarmVision.Filters;
using SwarmVision.HeadPartsTracking.Algorithms;

namespace SwarmVision
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Current;

        private VideoDecoder _decoder;
        public static int ResumeFrame = 0; // Start at 10s 
        public static int RandomFramesCount = 100;
        public static List<int> RandomFrames = new List<int>();
        public FrameIterator iterator = null;
        public MainWindow()
        {
            Current = this;

            InitializeComponent();

            Closed += (sender, args) => Environment.Exit(0);

            this.KeyUp += Grid_PreviewKeyUp;

            //TEST VIDEO
            txtFileName.Text =
                @"C:\temp\frames\Bee1_Feb10-Test.mov";
                //@"C:\temp\frames\B2-Feb11-bouquet.mov";
                //@"C:\temp\frames\B1-Feb11-bouquet.mov";
                //@"Y:\Downloads\BeeVids\2015.8.15 Bee 5 Rose White Back.MP4";

            SetupPlayer();

        }

        private void SetupPlayer()
        {
            if (_decoder != null)
            {
                _decoder.Stop();
                _decoder.ClearBuffer();
                _decoder.Dispose();
                _decoder = null;
            }

            string file = "";

            Dispatcher.Invoke(() => file = txtFileName.Text);

            _decoder = new VideoDecoder();
            _decoder.Open(file);
            _decoder.PlayerOutputWidth = _decoder.VideoInfo.Width;
            _decoder.PlayerOutputHeight = _decoder.VideoInfo.Height;

            Dispatcher.Invoke(() =>
            {
                sliderTime.Minimum = 0;
                sliderTime.Maximum = _decoder.VideoInfo.TotalFrames - 1;
            });

            if (iterator == null)
            {
                iterator = new FrameIterator(_decoder.VideoInfo.TotalFrames, templateView.TemplatePaths.Keys.Count, 100);
                //iterator.InitLinearSequence(10, 0);
                iterator.InitRandomSequence(50, 0);
            }

            _decoder.SeekTo(iterator.BurstBeginFrameIndex);
            _decoder.Start();
            _decoder.FrameDecoder.FrameBufferCapacity = 1;
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
                    string.Format("The associated .CSV file has data for frame {0}. Do you want to resume from frame {1}?", maxFrame, maxFrame + 1),
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
        public static SwarmVision.Filters.Frame currentFrame = null;
        public static Filters.Frame nextFrame = null;
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
                    //for(var i = 0; i < 500; i++)
                    { 
                        lock(_decoder)
                        {
                            _decoder.Stop();
                            _decoder.ClearBuffer();
                            _decoder.SeekTo(iterator.FrameIndex);
                            _decoder.Start(true);
                    
                            while(!_decoder.FramesInBuffer)
                            {
                                Thread.Sleep(10);
                            }

                            if (nextFrame != null)
                                nextFrame.Dispose();

                            nextFrame = _decoder.PlayNextFrame();
                        }
                    }
                    if (currentFrame != null)
                        currentFrame.Dispose();

                    currentFrame = nextFrame;

                    Dispatcher.Invoke(() =>
                    {
                        ShowFrame(currentFrame);
                    });

                    ReadyForNextFrame = false;

                    while (!ReadyForNextFrame && IsMarking)
                    {
                        Thread.Sleep(1); //Wait for position data (use value < smallest inter-click interval)
                    }

                    //Store the position data
                    if (!FrameData.ContainsKey(currentFrame.FrameIndex))
                        FrameData[currentFrame.FrameIndex] = new Dictionary<string, System.Drawing.Point?>();

                    FrameData[currentFrame.FrameIndex][TemplateView.Current.CurrentPartName] = _capturedMousePosition;

                    UpdateReward();

                    if (iterator.IsAtEndOfBurstOrSequenceOrVideo && iterator.IsAtEndOfBatch)
                    {
                        iterator.AdvanceBurst();
                        templateView.CurrentPartIndex = iterator.BatchPartIndex;
                        IsMarking = false;
                        ShowInstructions();
                        return;
                    }

                    if (iterator.IsAtEndOfBurstOrSequenceOrVideo)
                    {
                        iterator.AdvanceBurst();
                        templateView.CurrentPartIndex = iterator.BatchPartIndex;
                        SetupPlayer(); //Rewind to first frame
                        IsMarking = false;
                        ShowInstructions();
                        return;
                    }

                    iterator.AdvanceBurst();

                    Thread.Sleep(10);
                }

            }).Start();
        }

        private void UpdateReward()
        {
            if (iterator.BurstPositionIndex == 0)
                ShowReward("Very Good! Keep Going...");

            else if (iterator.BurstPositionIndex == 1)
                ShowReward("Excellent!");

            else if (iterator.BurstPositionIndex == iterator.BurstPositionCount - 2)
                ShowReward("Last one!");

            else if (iterator.BurstPositionIndex == iterator.BurstPositionCount - 1)
                ShowReward("Great job!");

            else if (iterator.BurstPositionIndex <= iterator.BurstPositionCount - 10 - 1)
                ShowReward((iterator.BurstPositionCount - iterator.BurstPositionIndex - 1) + " more to go");
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

        private void ShowFrame(Filters.Frame frame)
        {
            using (var g = Graphics.FromImage(frame.Bitmap))
            {
                var white = new System.Drawing.Pen(System.Drawing.Color.White, 1);
                var black = new System.Drawing.Pen(System.Drawing.Color.Black, 1);

                if (FrameData.ContainsKey(frame.FrameIndex))
                {
                    FrameData[frame.FrameIndex].Select(p => p.Value).ToList().ForEach(p =>
                    {
                        if (p != null)
                        {
                            g.DrawEllipse(white, new RectangleF(p.Value.ToWindowsPoint().ToPointF(), new SizeF(1, 1)));
                            g.DrawEllipse(black, new RectangleF(p.Value.ToWindowsPoint().Moved(-1, -1).ToPointF(), new SizeF(3, 3)));

                        }
                    });
                }
            }


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

            videoCanvas_MouseMove(this, null);

            lblTime.Content = currentFrame.FrameTime.ToString(@"mm\:ss") + " (" + currentFrame.FrameIndex + ")";
            _sliderValueChangedByCode = true;
            sliderTime.Value = currentFrame.FrameIndex;
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
            var videoWidth = currentFrame.Width;
            var videoHeight = currentFrame.Height;

            var screenWidth = videoCanvas.ActualWidth == 0 ? videoCanvas.Width : videoCanvas.ActualWidth;
            var screenHeight = videoCanvas.ActualHeight == 0 ? videoCanvas.Height : videoCanvas.ActualHeight;

            return new Point
            (
                x: Math.Max(0, Math.Min(videoWidth, (target.X / screenWidth * videoWidth).Rounded())),
                y: Math.Max(0, Math.Min(videoHeight, (target.Y / screenHeight * videoHeight).Rounded()))
            );
        }

        private Point RelativeToHead(Point headTop, Point headBack, Point target)
        {
            //Find head center by taking the midpoint of top and back
            var headCenter = new Point
            (
                x: (int)Math.Round((headTop.X + headBack.X) / 2.0),
                y: (int)Math.Round((headTop.Y + headBack.Y) / 2.0)
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

            var targetDistanceInTermsOfHeadLength = targetDistance / headLength;

            return new Point
            (
                x: (int)Math.Round(100.0 * targetDistanceInTermsOfHeadLength * Math.Cos((targetAngleRelativeToHead + 90) * Math.PI / 180.0)),
                y: (int)Math.Round(100.0 * targetDistanceInTermsOfHeadLength * Math.Sin((targetAngleRelativeToHead + 90) * Math.PI / 180.0))
            );
        }

        private static Point? _capturedMousePosition;
        private void CaptureMousePosition(bool skipped)
        {
            if (!skipped)
            {
                var pos = Mouse.GetPosition(videoCanvas).ToDrawingPoint();
                _capturedMousePosition = RelativeToVideo(pos);
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
            var csvFile = fileInfo.FullName + "_HandAnnotated_"+ DateTime.Now.ToString("yyyyMMdd HHmm") + ".csv";
            var csvExists = File.Exists(csvFile);

            if (csvExists)
            {
                var choice = MessageBox.Show("The file '" + csvFile + "' already exists. Append data? \n Yes - Append to the end of the file. \n No - Overwrite the file contents. \n Cancel", "Append or overwrite?", MessageBoxButton.YesNoCancel);

                if (choice == MessageBoxResult.No)
                    csvExists = false;

                if (choice == MessageBoxResult.Cancel)
                    return;
            }
            using (var writer = new StreamWriter(csvFile, csvExists))
            {
                //Column names based on template keys
                var columns = templateView.TemplatePaths.Keys.ToList();

                //Split into X & Y and add Relative to Head Columns
                if (!csvExists)
                    writer.WriteLine("Frame, " + string.Join(",", columns.SelectMany(k => new[] { k + "X", k + "Y" }).ToList()));

                //Write out the CSV line
                foreach (var f in FrameData.Keys)
                {
                    if (!FrameData.ContainsKey(f))
                        continue;

                    var sb = new StringBuilder();

                    //Point? headTop = null, headBack = null;
                    //
                    //if (FrameData[f].ContainsKey("Center of Mouth") &&
                    //    FrameData[f].ContainsKey("Back of the Head") &&
                    //    FrameData[f]["Center of Mouth"].HasValue &&
                    //    FrameData[f]["Back of the Head"].HasValue)
                    //{
                    //    headTop = FrameData[f]["Center of Mouth"].Value;
                    //    headBack = FrameData[f]["Back of the Head"].Value;
                    //}

                    columns.ForEach(c =>
                    {
                        //Replace video coordinates with head relative coordinates
                        //if (headBack != null && headTop != null &&
                        //    FrameData[f].ContainsKey(c) && FrameData[f][c].HasValue)
                        //{
                        //    FrameData[f][c] = RelativeToHead
                        //    (
                        //        headTop.Value,
                        //        headBack.Value,
                        //        FrameData[f][c].Value
                        //    );
                        //}

                        if (FrameData[f].ContainsKey(c))
                        {
                            var point = FrameData[f][c];

                            if (point.HasValue)
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

            Dispatcher.InvokeAsync(() => MessageBox.Show(".CSV file saved to " + csvFile));
        }

        public static int zoomWidth = 30;
        private void videoCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            var pos = Mouse.GetPosition(videoCanvas);
            pos.Offset(-zoomWidth / 2, -zoomWidth / 2);
            var topLeft = RelativeToVideo(new Point((int)pos.X, (int)pos.Y));

            var zoomSize = RelativeToVideo(new Point(zoomWidth, zoomWidth));

            topLeft.X = Math.Max(0, Math.Min(topLeft.X, currentFrame.Width - zoomSize.X));
            topLeft.Y = Math.Max(0, Math.Min(topLeft.Y, currentFrame.Height - zoomSize.Y));

            using (var clip = currentFrame.SubClipped(topLeft.X, topLeft.Y, zoomSize.X, zoomSize.Y))
            {
                var zoomImage = new WriteableBitmap(zoomSize.X, zoomSize.Y, 96, 96, PixelFormats.Bgr24, null);
                clip.CopyToWriteableBitmap(zoomImage);
                zoomClip.Source = zoomImage;
            }
        }

        private void btnRotate_Click(object sender, RoutedEventArgs e)
        {
            templateView.Angle -= 90;
            rotateThumbnail.Angle = templateView.Angle;
        }

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnClockwise_Click(object sender, RoutedEventArgs e)
        {
            templateView.Angle += 90;
            rotateThumbnail.Angle = templateView.Angle;
        }
    }
}