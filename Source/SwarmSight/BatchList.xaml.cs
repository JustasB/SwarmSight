using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using Settings;
using System.Xml.Serialization;
using System.IO;
using System.Threading;
using SwarmSight.VideoPlayer;

namespace SwarmSight
{
    /// <summary>
    /// Interaction logic for BatchList.xaml
    /// </summary>
    public partial class BatchList : Window
    {
        private WindowManager WindowManager
        {
            get
            {
                return ((WindowManager)Application.Current);
            }
        }

        public enum BatchItemStatus
        {
            NotStarted,
            Working,
            Finished,
            Error
        }
        public class BatchItem
        {
            public int ID { get; set; }
            public bool IsSelected { get; set; }
            public BatchItemStatus Status { get; set; }
            public string File { get; set; }
            public string ParamsSet { get; set; }
            public string ParamsText { get; set; }
            public string Remove { get; set; }
            public int TotalFrames { get; set; }
            public DateTime TimeStarted { get; set; }
        }
        public ObservableCollection<BatchItem> Items = new ObservableCollection<BatchItem>();

        public Thread BatchMonitor;

        public BatchList()
        {
            InitializeComponent();

            topRow.Height = new GridLength(0);
            dataGrid.DataContext = Items;
        }

        #region UI Events
        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var fileBrowser = new OpenFileDialog();
            fileBrowser.Multiselect = true;
            fileBrowser.Filter = Constants.VideoFileFilter;



            if (fileBrowser.ShowDialog() == true)
            {
                var selectedFiles = fileBrowser.FileNames;

                AddNewFiles(selectedFiles);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            List<BatchItem> selected = GetSelectedItems();

            if (selected.Count == 0)
                return;

            if (MessageBox.Show("Are you sure you want to remove the highlighted files from the batch processing list?", "Remove From Batch?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            RemoveItems(selected);
        }
        
        private void RemoveOne_Click(object sender, RoutedEventArgs e)
        {
            //cast sender to TextBlock, and get it's data context
            var selectedItem = ((TextBlock)sender).DataContext as BatchItem;

            RemoveItems(new List<BatchItem> { selectedItem });
        }

        #endregion

        private List<BatchItem> GetSelectedItems()
        {
            return dataGrid
                            .SelectedCells
                            .Select(c => (BatchItem)c.Item)
                            .Distinct()
                            .ToList();
        }

        private void AddNewFiles(string[] selectedFiles)
        {
            foreach (var fileName in selectedFiles)
            {
                Items.Add(new BatchItem()
                {
                    ID = Items.Count > 0 ? Items.Max(i => i.ID) + 1 : 1,
                    IsSelected = true,
                    Status = BatchItemStatus.NotStarted,
                    File = fileName,
                    ParamsSet = "-",
                    Remove = "Remove"
                });
            }

            dataGrid.DataContext = Items;
            dataGrid.SelectAll();

            btnRemove.Visibility = btnSet.Visibility = btnStart.Visibility = Visibility.Visible;
        }

        private void RemoveItems(List<BatchItem> selected)
        {
            selected.ForEach(i => Items.Remove(i));

            dataGrid.DataContext = Items;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (Items.Count <= 0)
            {
                return;
            }

            if (Items.Any(i => string.IsNullOrWhiteSpace(i.ParamsText)))
            {
                MessageBox.Show("Make sure that all items in the batch have their sensor positions set.");
                return;
            }

            if (MessageBox.Show("This will start processing the files from the top of the list, and in the order in which they appear. Are you ready to begin?", "Start Batch?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            CollapseWindow();

            BatchMonitor = new Thread(ProcessBatch)
            {
                IsBackground = true,
                Name = "BatchMonitorThread"
            };
            BatchMonitor.Start();

        }

        private void CollapseWindow()
        {
            topRow.Height = new GridLength(36);
            Height = 75;
            ResizeMode = ResizeMode.NoResize;
            middleRow.Height = new GridLength(0);
            bottomRow.Height = new GridLength(0);
            Top = 10;
        }

        private void ExpandWindow()
        {
            topRow.Height = new GridLength(0);
            

            ResizeMode = ResizeMode.CanResize;
            middleRow.Height = GridLength.Auto;
            bottomRow.Height = new GridLength(45);


            Height = double.NaN;
            SizeToContent = SizeToContent.Height;
        }

        private bool dontStop = true;
        private void ProcessBatch()
        {
            //Initialize - Get Video Frame Counts
            Parallel.ForEach(Items, item =>
            {
                item.TotalFrames = new VideoInfo(item.File).TotalFrames;
            });

            Dispatcher.Invoke(() =>
            {
                lblCountProgress.Content = "0/" + Items.Count;
                lblPercentProgress.Content = "0%";
                lblTime.Content = "N/A";
            });

            dontStop = true;
            var working = false;
            var reachedEndOfVideo = false;
            BatchItem currentItem = null;
            WindowManager.ProcessorWindow.Controller.Pipeline.OnReachedEndOfVideo += () => reachedEndOfVideo = true;

            //While there is work to do
            while (dontStop && (working || Items.Any(i => i.Status == BatchItemStatus.NotStarted)))
            {
                if(!working)
                {
                    currentItem = Items.First(i => i.Status == BatchItemStatus.NotStarted);                    

                    WindowManager.ProcessorWindow.LoadParamsFromJSON(currentItem.ParamsText);

                    reachedEndOfVideo = false;                    

                    currentItem.TimeStarted = DateTime.Now;
                    currentItem.Status = BatchItemStatus.Working;

                    Dispatcher.Invoke(() =>
                    {
                        WindowManager.ShowProcessorWindow(currentItem.File, oneFrame: false);
                        
                        dataGrid.DataContext = null;
                        dataGrid.DataContext = Items;
                    });
                    
                    working = true;
                }
                else //working
                {
                    //Check if finished
                    if(reachedEndOfVideo)
                    {
                        currentItem.Status = BatchItemStatus.Finished;
                        WindowManager.ProcessorWindow.SaveCSV(currentItem.File);

                        var timeTaken = DateTime.Now - currentItem.TimeStarted;
                        var fps = currentItem.TotalFrames / timeTaken.TotalSeconds;
                        
                        var framesRemaining = Items
                            .Where(i => i.Status == BatchItemStatus.NotStarted)
                            .Sum(i => i.TotalFrames);

                        var timeRemaining = TimeSpan.FromSeconds(framesRemaining / fps);

                        var itemsRemaining = Items
                            .Count(i => i.Status == BatchItemStatus.NotStarted);

                        var itemsFinished = (Items.Count - itemsRemaining);
                        var percentFinished = (int)Math.Round(1.0 * itemsFinished / Items.Count * 100,0);

                        Dispatcher.Invoke(() =>
                        {
                            lblTime.Content = timeRemaining.ToString(@"hh\ \h\ mm\ \m\ ss\ \s");
                            lblCountProgress.Content = itemsFinished + "/" + Items.Count;
                            lblPercentProgress.Content = percentFinished + "%";
                            Show();
                            
                            dataGrid.DataContext = null;
                            dataGrid.DataContext = Items;
                        });

                        
                        working = false;
                    }

                    Thread.Sleep(100);
                }
            }

            WindowManager.HideProcessorWindow();
            Dispatcher.Invoke(ExpandWindow);

            if (dontStop)
                MessageBox.Show("The batch is complete");
            else
                currentItem.Status = BatchItemStatus.NotStarted;

            Dispatcher.Invoke(new Action(() => 
            {
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = Items;
            }));
        }

        private void Stop_Clicked(object sender, RoutedEventArgs e)
        {
            WindowManager.ProcessorWindow.Controller.Pipeline.Stop();
            dontStop = false;
        }

        private void ShowDetails_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.SelectedItem = Items.First(i => i.ID == ((int)((TextBlock)sender).Tag));
            AddEditParams();
        }

        private void SetParameters_Click(object sender, RoutedEventArgs e)
        {
            AddEditParams();
        }

        private void AddEditParams()
        {
            var selected = GetSelectedItems();

            if (selected.Count == 0)
                return;
                
            selected.ForEach(i => Items.First(i2 => i2.ID == i.ID).Status = BatchItemStatus.NotStarted);

            var first = selected[0];

            if (!string.IsNullOrWhiteSpace(first.ParamsText))
                WindowManager.ProcessorWindow.LoadParamsFromJSON(first.ParamsText);
                
            Hide();
            WindowManager.ProcessorWindow.btnBatchList.Visibility = Visibility.Hidden;
            WindowManager.ProcessorWindow.btnSaveBatchParams.Visibility = Visibility.Visible;
            WindowManager.ShowProcessorWindow(first.File);

            if (Items.All(i => string.IsNullOrWhiteSpace(i.ParamsText)))
                MessageBox.Show("Position the sensors and click 'Save Parameters to Batch' when you are done.");
        }

        public void SaveParams(AppSettings parameters)
        {
            var paramsText = parameters.AsJSON();

            var selected = GetSelectedItems();

            foreach (var selItem in selected)
            {
                var item = Items.First(i => i.ID == selItem.ID);

                item.ParamsText = paramsText;
                item.ParamsSet = "Set";
            }

            dataGrid.DataContext = null;
            dataGrid.DataContext = Items;

            WindowManager.ProcessorWindow.btnBatchList.Visibility = Visibility.Visible;
            WindowManager.ProcessorWindow.btnSaveBatchParams.Visibility = Visibility.Hidden;
        }
    }
}
