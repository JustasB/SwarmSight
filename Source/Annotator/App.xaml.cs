using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public void Restart()
        {
            //System.Windows.Forms.Application.Restart();
            //System.Windows.Application.Current.Shutdown();

            var newWindow = new MainWindow();
            var oldWindow = MainWindow;

            oldWindow.Close();

            newWindow.Show();
            MainWindow = newWindow;
        }
    }
}