using SwarmSight.Annotator;
using SwarmSight.VideoPlayer;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for VideoSettings.xaml
    /// </summary>
    public partial class VideoSettings : UserControl
    {
        public event EventHandler Saved;
        public VideoSettings()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppSettings.Default.StartTime = double.Parse(txtStartSeconds.Text);
                AppSettings.Default.EndTime = double.Parse(txtEndSeconds.Text);
                AppSettings.Default.UseRandom = chkRandomSeq.IsChecked.Value;

                if (AppSettings.Default.StartTime > AppSettings.Default.EndTime)
                    throw new Exception("Start time should be smaller than end time");

                if (AppSettings.Default.UseRandom)
                {
                    AppSettings.Default.MaxFrames = int.Parse(txtMaxRandomFrames.Text);
                    AppSettings.Default.UseSeed = chkUseRandomSeed.IsChecked.Value;

                    if (AppSettings.Default.UseSeed)
                    {
                        AppSettings.Default.Seed = int.Parse(txtRandomSeed.Text);

                        if (AppSettings.Default.Seed <= 0)
                            throw new Exception("Random seed should be a positive integer");
                    }

                }
                else
                {
                    AppSettings.Default.EveryNth = int.Parse(txtNthFrame.Text);
                }
                
                AppSettings.Default.BurstSize = int.Parse(txtBurstSize.Text);

                AppSettings.Default.Save();

                if (Saved != null)
                    Saved(this, null);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Please make sure all the above values are valid" + 
                    Environment.NewLine + 
                    Environment.NewLine + 
                    ex.Message);
            }
        }

        public void LoadSettings(VideoInfo info)
        {
            AppSettings.Default.Reload();

            AppSettings.Default.EndTime = info.Duration.TotalSeconds;
            
            txtStartSeconds.Text = AppSettings.Default.StartTime.ToString();
            txtEndSeconds.Text = AppSettings.Default.EndTime.ToString();
            chkRandomSeq.IsChecked = AppSettings.Default.UseRandom;

            txtMaxRandomFrames.Text = AppSettings.Default.MaxFrames.ToString();
            chkUseRandomSeed.IsChecked = AppSettings.Default.UseSeed;
            txtRandomSeed.Text = AppSettings.Default.Seed.ToString();
            txtNthFrame.Text = AppSettings.Default.EveryNth.ToString();
            txtBurstSize.Text = AppSettings.Default.BurstSize.ToString();
        }

        private void chkRandomSeq_Checked(object sender, RoutedEventArgs e)
        {
            groupRandom.IsEnabled = true;
            groupSequential.IsEnabled = !groupRandom.IsEnabled;
        }

        private void chkSequential_Checked(object sender, RoutedEventArgs e)
        {
            groupRandom.IsEnabled = false;
            groupSequential.IsEnabled = !groupRandom.IsEnabled;
        }

        private void chkUseRandomSeed_Checked(object sender, RoutedEventArgs e)
        {
            if(txtRandomSeed != null)
                txtRandomSeed.IsEnabled = true;
        }

        private void chkUseRandomSeed_Unchecked(object sender, RoutedEventArgs e)
        {
            txtRandomSeed.IsEnabled = false;
        }
    }
}
