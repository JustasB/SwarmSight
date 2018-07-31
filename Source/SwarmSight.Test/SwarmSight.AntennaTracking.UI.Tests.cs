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
using System;
using TestStack.White.InputDevices;
using SwarmSight.Common.UI;
using System.Data;

namespace SwarmSight.AntennaTracking.UI
{
	[TestClass]
	public class Tests
	{
        public static FileInfo TestVideoFileInfo;
        public static FileInfo ExpectedDataFile;
        public static FileInfo ManualDataFile;
        public static Application application;
        public static Window window;
        public static FileInfo csvFile;

        [TestInitialize]
        public void StartApp()
        {
            TestVideoFileInfo = new FileInfo("test.mov");
            ExpectedDataFile = new FileInfo("expected.csv");
            ManualDataFile = new FileInfo("manualAppendageCoords.csv");

            application = Application.Launch("SwarmSight Antenna Tracking.exe");
            window = application.GetWindow("SwarmSight Antenna Tracking", InitializeOption.NoCache);
            
        }
        
        [TestCleanup]
        public void CloseApp()
        {
            application.Close();

            if (csvFile != null && csvFile.Exists && csvFile.Name.StartsWith(TestVideoFileInfo.Name))
                csvFile.Delete();
        }

        [TestMethod]
		public void AnalyzeAntennaCSV()
        {

            var txtFileName = window.Get<TextBox>("txtFileName");

            txtFileName.Click();
            txtFileName.Text = TestVideoFileInfo.FullName;
            window.Keyboard.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.TAB);

            //wait one sec
            Thread.Sleep(1000);

            //Set antenna sensor position
            var expAntSens = window.Get<GroupBox>("expAntSens");            
            expAntSens.Click();

            SetSliderValue(expAntSens, "sliderScale", 2.4);
            SetSliderValue(expAntSens, "sliderX", 243);
            SetSliderValue(expAntSens, "sliderY", 84);
            SetSliderValue(expAntSens, "sliderAngle", 5);
            expAntSens.Items[1].Click();

            var expFilters = window.Get<GroupBox>("expFilters");
            expFilters.Click();
            SetSliderValue(expFilters, "sliderFast", 2);
            SetSliderValue(expFilters, "sliderSlow", 2);
            SetSliderValue(expFilters, "sliderStationary", 255);
            expFilters.Items[1].Click();


            //press play
            window.Get<Button>("btnPlayPause").Focus();
            window.Keyboard.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.RETURN);

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
            window.Get<GroupBox>("expSave").Click();
            window.Get<Button>("btnSaveActivity").Click();

            Thread.Sleep(200);

            //Save as default csv file
            Keyboard.Instance.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.RETURN);

            Thread.Sleep(200);

            //get the saved csv file name
            csvFile = new DirectoryInfo(".").GetFiles("test.mov*.csv")[0];

            //Compare manual-to-tracker
            var manualResult = CompareToManual(csvFile.FullName, ManualDataFile.FullName);
            Assert.IsTrue(manualResult < 11); 

            //Compare tracker-to-tracker - should be identical between runs
            var trackerResult = CompareCSVfiles(csvFile, ExpectedDataFile);
            Assert.IsTrue(trackerResult < 1); 
        }

        private double CompareToManual(string trackerFile, string manualFile)
        {
            var manualDT = CSV.ToDataTable(manualFile);
            var trackerDT = CSV.ToDataTable(trackerFile);
            var totDist = 0.0;

            for(var r = 0; r < manualDT.Rows.Count; r++)
            {
                var rowManual = manualDT.Rows[r];
                var rowTracker = trackerDT.Rows[r];

                Assert.IsTrue(rowManual["Frame"].ToString() == rowTracker["Frame"].ToString(), "Frame numbers in both files must be the same");

                var lxm = int.Parse(rowManual["Left Flagellum TipX"].ToString());
                var lym = int.Parse(rowManual["Left Flagellum TipY"].ToString());

                var rxm = int.Parse(rowManual["Right Flagellum TipX"].ToString());
                var rym = int.Parse(rowManual["Right Flagellum TipY"].ToString());

                int lxt = 0;
                int.TryParse(rowTracker["LeftFlagellumTip-X"].ToString(), out lxt);

                int lyt = 0;
                int.TryParse(rowTracker["LeftFlagellumTip-Y"].ToString(), out lyt);

                int rxt = 0;
                int.TryParse(rowTracker["RightFlagellumTip-X"].ToString(), out rxt);

                int ryt = 0;
                int.TryParse(rowTracker["RightFlagellumTip-Y"].ToString(), out ryt);

                var lDist = Math.Sqrt(Math.Pow((lxm - lxt), 2) + Math.Pow((lym - lyt), 2));
                var rDist = Math.Sqrt(Math.Pow((rxm - rxt), 2) + Math.Pow((rym - ryt), 2));

                totDist += (lDist + rDist);
            }

            //Average dist of L and R tips
            return totDist / (2 * manualDT.Rows.Count);
        }

        public static double CompareCSVfiles(FileInfo csvFile, FileInfo expectedDataFile)
        {
            var act = ParseCSV(csvFile);
            var exp = ParseCSV(expectedDataFile);

            if (act.Length != exp.Length || act[0].Length != exp[0].Length)
                throw new Exception("The number of rows/columns is different in CVS than expected");

            var result = 0.0;

            for (int row = 0; row < exp.Length; row++)
            {
                for (int col = 0; col < exp[0].Length; col++)
                {
                    var expVal = exp[row][col];
                    var actVal = act[row][col];
                    var percent = 1 - Math.Abs((expVal - actVal) / expVal);

                    if (double.IsNaN(percent))
                        percent = 1;

                    result += percent;
                }
            }

            result /= exp.Length * exp[0].Length;

            return result;
        }

        private static double[][] ParseCSV(FileInfo csvFile)
        {
            return 
                File
                .ReadAllLines(csvFile.FullName)
                .Skip(1)
                .Select(line => 
                    line
                        .Split(',')
                        .Select(col =>
                        {
                            double parsed = 0;
                            double.TryParse(col, out parsed);
                            return parsed;
                        })
                        .ToArray()
                )
                .ToArray();
        }

        public static void SetSliderValue(GroupBox expander, string ucName, double value)
        {
            var uc = (expander.GetMultiple(SearchCriteria.ByAutomationId(ucName)))[0];
            var slider = uc.Get<WPFSlider>("slider");
            slider.Value = value;

            //click on thumb
            (slider.GetMultiple(SearchCriteria.All))[2].Click();
        }
    }
}
