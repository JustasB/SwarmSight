using System;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class WindowManager : Application
    {
        public ProcessorWindow ProcessorWindow;
        public BatchList BatchWindow;

        public WindowManager()
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

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

            try
            {
                ProcessorWindow.Show();
            }
            catch { }

            CenterWindowOnScreen(ProcessorWindow);

            if (file != null)
            {
                ProcessorWindow.txtFileName.Text = file;
                ProcessorWindow.Controller.LoadFile(oneFrame);
            }
        }

        public void HideProcessorWindow()
        {
            Application.Current.Dispatcher.Invoke(ProcessorWindow.Hide);
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
            double windowWidth = target.ActualWidth;
            double windowHeight = target.ActualHeight;
            target.Left = (screenWidth / 2) - (windowWidth / 2);
            target.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        public void Quit()
        {
            if (ProcessorWindow != null)
                ProcessorWindow.Controller.Stop();

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