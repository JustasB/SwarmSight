using Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwarmSight.UserControls
{
    /// <summary>
    /// Interaction logic for FilterSliders.xaml
    /// </summary>
    public partial class FilterSlider : UserControl
    {
        public string SettingsKey { get; set; }
        public string Label { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }

        public FilterSlider()
        {
            InitializeComponent();

            //Loaded += FilterSlider_Loaded;
        }

        private void FilterSlider_Loaded(object sender, RoutedEventArgs e)
        {
            //LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            var settingValue = (int)(typeof(AppSettings).GetProperty(SettingsKey).GetValue(AppSettings.Default));

            if (Min < Max)
            {
                slider.Minimum = Min;
                slider.Maximum = Max;
            }
            slider.Value = settingValue;
            lblValue.Content = settingValue;
            lblTitle.Content = Label;
        }

        private void slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            typeof(AppSettings)
                .GetProperty(SettingsKey)
                .SetValue(AppSettings.Default, (int)((Slider)sender).Value);

            LoadFromSettings();

            AppSettings.Default.SaveAsync();
        }


        private void slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            slider_MouseUp(sender, e);
        }

        private void slider_MouseMove(object sender, MouseEventArgs e)
        {
            lblValue.Content = (int)Math.Round(slider.Value,0);

            typeof(AppSettings)
                .GetProperty(SettingsKey)
                .SetValue(AppSettings.Default, (int)((Slider)sender).Value);
        }
    }
}
