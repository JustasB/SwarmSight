using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Classes;
using SwarmSight.VideoPlayer;
using OxyPlot.Wpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;
using SwarmSight.Filters;
using SwarmSight.HeadPartsTracking.Algorithms;
using SwarmSight;
using SwarmSight.Annotator;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Current;

        private VideoDecoder _decoder;
        public static int ResumeFrame = 0;
        public FrameIterator iterator = null;
        public MainWindow()
        {
            Current = this;

            InitializeComponent();

            Closing += (s, e) =>
            {
                if (FrameData.Count > 0)
                {
                    var answer = MessageBox.Show("Save frame annotations before closing?", "Save?", MessageBoxButton.YesNoCancel);

                    if (answer == MessageBoxResult.Cancel)
                        e.Cancel = true; //Cancel the close
                    else if (answer == MessageBoxResult.Yes)
                        SaveCSV(txtFileName.Text);
                }
            };

            Closed += (s, e) =>
            {
                Environment.Exit(0);
            };


            videoSettingsControl.Saved += (sender, args) =>
            {
                ShowInstructions();
            };

            this.KeyUp += Grid_PreviewKeyUp;

            OnBrowseClicked(this, null);
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
                iterator = new FrameIterator(_decoder.VideoInfo.TotalFrames, 
                    templateView.TemplatePaths.Keys.Count, 
                    AppSettings.Default.BurstSize
                );

                var startFrame = (int)Math.Round(AppSettings.Default.StartTime * _decoder.VideoInfo.FPS);
                var endFrame = (int)Math.Round(AppSettings.Default.EndTime * _decoder.VideoInfo.FPS);

                if (AppSettings.Default.UseRandom)
                {
                    iterator.InitRandomSequence(
                        AppSettings.Default.MaxFrames, 
                        Math.Max(0,startFrame), 
                        Math.Min(_decoder.VideoInfo.TotalFrames, endFrame)-1,
                        AppSettings.Default.UseSeed ? AppSettings.Default.Seed : (int?)null
                    );
                }
                else
                {
                    iterator.InitLinearSequence(
                        AppSettings.Default.EveryNth,
                        Math.Max(0, startFrame),
                        Math.Min(_decoder.VideoInfo.TotalFrames - 1, endFrame)
                    );
                }
            }

            _decoder.SeekTo(iterator.BurstBeginFrameIndex);
            _decoder.Start();
            _decoder.FrameDecoder.FrameBufferCapacity = 1;
            _decoder.FrameDecoder.MinimumWorkingFrames = 1;
        }

        private void SeekTo(int frame)
        {
            //_comparer.SeekTo(percentLocation);
        }
        

        private void Reset()
        {
            iterator = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                templateView.CurrentPartIndex = 0;
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
            HideSettings();

            var ofd = new Microsoft.Win32.OpenFileDialog();

            ofd.Title = "Select a video file to annotate";

            do
            {
                var result = ofd.ShowDialog();

                if (result == false)
                    Environment.Exit(0);
            }
            while (!File.Exists(ofd.FileName));

            txtFileName.Text = ofd.FileName;

            btnCounterClockwise.Visibility = Visibility.Visible;
            gridZoom.Visibility = Visibility.Visible;
            lblActivePartName.Visibility = Visibility.Visible;
            gridControls.Visibility = Visibility.Visible;

            ShowSettings();
        }

        private void ShowSettings()
        {
            pnlSettings.Visibility = Visibility.Visible;

            using (var decoder = new VideoDecoder())
            {
                decoder.Open(txtFileName.Text);
                videoSettingsControl.LoadSettings(decoder.VideoInfo);
            }
        }

        private void HideSettings()
        {
            pnlSettings.Visibility = Visibility.Hidden;
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
            Dispatcher.InvokeAsync(new Action(() => {
                SaveCSV(txtFileName.Text);
            }));
        }

        private void Undo()
        {
            if (IsMarking)
                iterator = FrameIterator.GetPreviousState();
            
        }

        public event EventHandler FrameMarked;
        public event EventHandler FrameSkipped;

        private Dictionary<int, Dictionary<string, System.Drawing.Point?>> FrameData = new Dictionary<int, Dictionary<string, System.Drawing.Point?>>();
        public static bool IsMarking;
        public static bool ReadyForNextFrame = false;
        public static SwarmSight.Filters.Frame currentFrame = null;
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
                        if (_decoder == null)
                            SetupPlayer();

                        lock(_decoder)
                        {
                            _decoder.Stop();
                            _decoder.ClearBuffer();
                            _decoder.SeekTo(iterator.FrameIndex);
                            _decoder.Start(true);
                    
                            while(!_decoder.FramesInBuffer)
                            {
                                Thread.Sleep(5);
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
                        lblActivePartName.Text = TemplateView.Current.CurrentPartName;
                        ShowFrame(currentFrame);

                        GC.Collect();
                    });

                    

                    ReadyForNextFrame = false;

                    while (!ReadyForNextFrame && IsMarking)
                    {
                        Thread.Sleep(5); //Wait for position data (use value < smallest inter-click interval)
                    }

                    //Store the position data
                    if (!FrameData.ContainsKey(currentFrame.FrameIndex))
                        FrameData[currentFrame.FrameIndex] = new Dictionary<string, System.Drawing.Point?>();

                    if(_capturedMousePosition != null)
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

            else if (iterator.FramesTillEndOfBurst == 1)
                ShowReward("Last one!");

            else if (iterator.FramesTillEndOfBurst == 0)
                ShowReward("Great job!");

            else
                ShowReward(iterator.FramesTillEndOfBurst + " more to go");
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
                SaveCSV(txtFileName.Text, autoSave: true);

                HideSettings();
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
        private int counter;
        private void CaptureMousePosition(bool skipped)
        {
            if (ReadyForNextFrame) //Don't overcapture
                return;

            if (!skipped)
            {
                var pos = Mouse.GetPosition(videoCanvas).ToDrawingPoint();
                _capturedMousePosition = RelativeToVideo(pos);
                //_capturedMousePosition.Value.Offset(-1, -1);
            }
            else
            {
                _capturedMousePosition = null;
            }

            ReadyForNextFrame = true;

            counter++;
            Debug.WriteLine((iterator.BurstPositionCount - counter) + " - " + iterator.FramesTillEndOfBurst);
        }

        private void SaveCSV(object videoFileName, bool autoSave = false)
        {
            if (FrameData.Count == 0)
                return;

            Dispatcher.Invoke(() => btnSaveActivity.Content = "Saving...");

            var fileInfo = new FileInfo(videoFileName.ToString());
            var csvFile = fileInfo.FullName + "_HandAnnotated_"+ DateTime.Now.ToString("yyyyMMdd HHmm") + ".csv";

            if (autoSave)
                csvFile = fileInfo.FullName + "_HandAnnotated_autosave.csv";

            var csvExists = File.Exists(csvFile);

            if (csvExists && !autoSave)
            {
                var choice = MessageBox.Show("The file '" + csvFile + "' already exists. Append data? \n Yes - Append to the end of the file. \n No - Overwrite the file contents. \n Cancel", "Append or overwrite?", MessageBoxButton.YesNoCancel);

                if (choice == MessageBoxResult.No)
                    csvExists = false;

                if (choice == MessageBoxResult.Cancel)
                    return;
            }
            using (var writer = new StreamWriter(csvFile, csvExists && !autoSave))
            {
                //Column names based on template keys
                var columns = templateView.TemplatePaths.Keys.ToList();

                //Split into X & Y and add Relative to Head Columns
                if (!csvExists || autoSave)
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

            Dispatcher.Invoke(() => btnSaveActivity.Content = "Save Activity");

            if (!autoSave)
                Dispatcher.InvokeAsync(() => MessageBox.Show(".CSV file saved to " + csvFile));
        }

        public static int zoomWidth = 30;
        public WriteableBitmap zoomImage;
        private void videoCanvas_MouseMove(object sender, MouseEventArgs e)
        {

            var pos = Mouse.GetPosition(videoCanvas);
            pos.Offset(-zoomWidth / 2+1, -zoomWidth / 2+1);
            var topLeft = RelativeToVideo(new Point((int)pos.X, (int)pos.Y));

            var zoomSize = RelativeToVideo(new Point(zoomWidth, zoomWidth));

            topLeft.X = Math.Max(0, Math.Min(topLeft.X, currentFrame.Width - zoomSize.X));
            topLeft.Y = Math.Max(0, Math.Min(topLeft.Y, currentFrame.Height - zoomSize.Y));

            using (var clip = currentFrame.SubClipped(topLeft.X, topLeft.Y, zoomSize.X, zoomSize.Y))
            {
                if (zoomImage == null)
                {
                    zoomImage = new WriteableBitmap(zoomSize.X, zoomSize.Y, 96, 96, PixelFormats.Bgr24, null);
                    zoomClip.Source = zoomImage;
                }

                clip.CopyToWriteableBitmap(zoomImage);
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