using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;
using TestStack.White;
using TestStack.White.UIItems;
using TestStack.White.Factory;
using System.Threading;
using TestStack.White.UIItems.WPFUIItems;
using System.Linq;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems.WindowItems;

namespace SwarmSight.Motion.UI
{
	[TestClass]
	public class Tests
	{
        public static FileInfo TestVideoFileInfo;
        public static Application application;
        public static Window window;
        public static FileInfo csvFile;

        [TestInitialize]
        public void StartApp()
        {
            TestVideoFileInfo = new FileInfo("test.mov");
            application = Application.Launch("SwarmSight Motion Analysis.exe");
            window = application.GetWindow("SwarmSight - Video Motion Analysis Tool for Behavioral Scientists", InitializeOption.NoCache);
        }
        
        [TestCleanup]
        public void CloseApp()
        {
            application.Close();

            if (csvFile != null && csvFile.Exists && csvFile.Name.StartsWith(TestVideoFileInfo.Name))
                csvFile.Delete();
        }

        [TestMethod]
		public void AnalyzeMotionCSV()
		{
            
            
            var txtFileName = window.Get<TextBox>("txtFileName");

            txtFileName.Click();
            txtFileName.Text = TestVideoFileInfo.FullName;
            window.Keyboard.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.TAB);

            //wait one sec
            Thread.Sleep(1000);

            //press play
            window.Get<Button>("btnPlayPause").Click();

            //Wait for at least 1/2 sec
            Thread.Sleep(500);

            //wait till slider is at 0 - stopped
            var timeSlider = window.Get<Slider>("sliderTime");
            var counter = 0;

            while (timeSlider.Value != 0 && counter < 10)
            { 
                Thread.Sleep(1000);
                counter++;
            }

            if (counter >= 10)
                throw new System.Exception("Playing did not finish within 10 secs");

            //save csv
            window.Get<Button>("btnSaveActivity").Click();

            Thread.Sleep(200);

            //get the saved csv file name
            csvFile = new DirectoryInfo(".").GetFiles("test.mov*.csv")[0];

            //expand stats
            window.Get<Button>("btnShowCompare").Click();

            //load it to first chart
            var csvFileTexts = window.GetMultiple(TestStack.White.UIItems.Finders.SearchCriteria.ByAutomationId("txtFileNameCSV")).Cast<TextBox>().ToArray();
            
            var firstText = csvFileTexts[0];
            firstText.Text = csvFile.FullName;
            window.Keyboard.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.TAB);

            //load current to second chart
            var useCurrent2 = window.GetMultiple(SearchCriteria.ByAutomationId("btnUseCurrentActivity"))[1] as Button;
            useCurrent2.Click();

            //wait to compute averages
            Thread.Sleep(200);

            //check if difference is 0
            var percentDiffLabel = window.Get<WPFLabel>("lblAvgPercent");

            if (percentDiffLabel.Text != "0.00 %")
                throw new System.Exception("CSV file contents and current activity are not the same");
        }
    }
}
