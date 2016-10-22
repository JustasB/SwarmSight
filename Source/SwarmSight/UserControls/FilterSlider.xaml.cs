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
        public bool IsValueInt { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }

        public event Action OnChanged;
        public event Action OnBeginChange;

        public FilterSlider()
        {
            IsValueInt = true;

            InitializeComponent();
        }

        public void LoadFromSettings()
        {   
            var rawValue = Convert.ToDouble(typeof(AppSettings).GetProperty(SettingsKey).GetValue(AppSettings.Default));
            
            var value = GetProperValue(rawValue);

            if (Min < Max)
            {
                slider.Minimum = Min;
                slider.Maximum = Max;

                if (IsValueInt)
                    slider.SmallChange = 1;

                else
                    slider.SmallChange = (Max-Min) / 100;
            }

            if(IsValueInt)
                slider.Value = (int)value;
            else
                slider.Value = (double)value;

            lblValue.Content = value;
            lblTitle.Content = Label;
        }

        private void slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var value = GetProperValue(slider.Value);

            typeof(AppSettings)
                .GetProperty(SettingsKey)
                .SetValue(AppSettings.Default, value);

            LoadFromSettings();

            AppSettings.Default.SaveAsync();

            if (OnChanged != null)
                OnChanged();
        }


        private void slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            slider_MouseUp(sender, e);
        }

        private void slider_MouseMove(object sender, MouseEventArgs e)
        {
            var value = GetProperValue(slider.Value);

            lblValue.Content = value;

            typeof(AppSettings)
                .GetProperty(SettingsKey)
                .SetValue(AppSettings.Default, value);

            AppSettings.Default.SaveAsync();

            if (OnChanged != null)
                OnChanged();
        }

        private object GetProperValue(double rawValue)
        {
            object value;

            if (IsValueInt)
                value = (int)Math.Round(rawValue, 0);
            else
                value = Math.Round(rawValue, 1);

            return value;
        }

        private void slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (OnBeginChange != null)
                OnBeginChange();
        }

        private void slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            slider_MouseDown(sender, e);
        }
    }
}
