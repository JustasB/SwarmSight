using System;
using System.Windows;
using System.Windows.Threading;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class WindowManager : Application
    {
        public string VideoFileFilter = @"Video Files|*.3g2;*.3gp;*.3gp2;*.3gpp;*.amv;*.asf;*.avi;*.bik;*.bin;*.divx;*.drc;*.dv;*f4v;*.flv;*.gvi;*.gxf;*.iso;*.m1v;*.m2v;*.m2t;*.m2ts;*.m4v;*.mkv;*.mov;*.mp2;*.mp2v;*.mp4;*.mp4v;*.mpe;*.mpeg;*.mpeg1;*.mpeg2;*.mpeg4;*.mpg;*.mpv2;*.mts;*.mtv;*.mxf;*.mxg;*.nsv;*.nuv;*.ogg;*.ogm;*.ogv;*.ogx;*.ps;*.rec;*.rm;*.rmvb;*.rpl;*.thp;*.tod;*.ts;*.tts;*.txd;*.vob;*.vro;*.webm;*.wm;*.wmv;*.wtv;*.xesc|All Files|*.*";

        public ProcessorWindow ProcessorWindow;
        public BatchList BatchWindow;

        public WindowManager()
        {
            Startup += (object sender, StartupEventArgs e) =>
            {
                ShowProcessorWindow();
            };
        }

        public void ShowProcessorWindow(string file = null, bool oneFrame = true)
        {
            if (ProcessorWindow == null)
            {
                ProcessorWindow = new ProcessorWindow();
                ProcessorWindow.Closing += Window_Closing;
            }

            ProcessorWindow.Show();
            CenterWindowOnScreen(ProcessorWindow);

            if (file != null)
            {
                ProcessorWindow.txtFileName.Text = file;
                ProcessorWindow.LoadFile(oneFrame);
            }
        }

        public void HideProcessorWindow()
        {

        }

        public void ShowBatchWindow()
        {
            if(BatchWindow == null) 
            {
                BatchWindow = new BatchList();
                BatchWindow.Closing += (sender,e) =>
                {
                    Window_Closing(sender, e);

                    ShowProcessorWindow();
                };
            }

            BatchWindow.Show();
            CenterWindowOnScreen(BatchWindow);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Hide Window
            Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate (object o)
            {
                ((Window)sender).Hide();
                return null;
            }, null);

            //Do not close application
            e.Cancel = true;

        }

        public void HideBatchWindow()
        {
            if (BatchWindow != null)
                BatchWindow.Hide();
        }
        
        public void CenterWindowOnScreen(Window target)
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = target.Width;
            double windowHeight = target.Height;
            target.Left = (screenWidth / 2) - (windowWidth / 2);
            target.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        public void Quit()
        {
            if (ProcessorWindow != null)
                ProcessorWindow.Stop();

            Shutdown();
        }

        private bool IsWindowVisible(Window target)
        {
            if (target == null || target.Visibility == Visibility.Hidden)
                return false;

            return true;
        }

        public void ExitIfLastWindow()
        {
            if(!IsWindowVisible(BatchWindow) && 
               !IsWindowVisible(ProcessorWindow))
            {
                DispatcherUnhandledException += WindowManager_DispatcherUnhandledException;
                
                Quit();
            }
        }

        private void WindowManager_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }
    }
}