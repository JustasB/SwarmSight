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
using System.Collections.Generic;

namespace SwarmSight.AntennaTracking.UI
{
    [TestClass]
    public class BatchTests
    {
        public static FileInfo TestVideoFileInfo;
        public static Application application;
        public static Window window;

        [TestInitialize]
        public void StartApp()
        {
            TestVideoFileInfo = new FileInfo("test.mov");

            application = Application.Launch("SwarmSight Antenna Tracking.exe");
            window = application.GetWindow("SwarmSight Antenna Tracking", InitializeOption.NoCache);
        }

        [TestCleanup]
        public void CloseApp()
        {
            application.Close();

            TestVideoFileInfo.Directory.GetFiles(TestVideoFileInfo.Name + "*.csv").ToList().ForEach(f => f.Delete());
        }

        [TestMethod]
        public void TestAntennaBatchProcess()
        {
            //Open batch window
            window.Get<Button>("btnBatchList").Click();
            Thread.Sleep(100);
            var batchWindow = application.GetWindow("Batch Processing", InitializeOption.NoCache);

            //Select file
            batchWindow.Get<Button>("btnAdd").Click();
            Thread.Sleep(100);
            window.Keyboard.Enter(TestVideoFileInfo.FullName);
            window.Keyboard.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.RETURN);

            //Row 2
            batchWindow.Get<Button>("btnAdd").Click();
            Thread.Sleep(100);
            window.Keyboard.Enter(TestVideoFileInfo.FullName);
            window.Keyboard.PressSpecialKey(TestStack.White.WindowsAPI.KeyboardInput.SpecialKeys.RETURN);


            batchWindow.Get<Button>("btnSet").Click();
            Thread.Sleep(1000);
            window.MessageBox("").Close(); //Close instructions
            
            //Set antenna sensor position
            var expAntSens = window.Get<GroupBox>("expAntSens");
            expAntSens.Click();

            Tests.SetSliderValue(expAntSens, "sliderScale", 2.4);
            Tests.SetSliderValue(expAntSens, "sliderX", 243);
            Tests.SetSliderValue(expAntSens, "sliderY", 84);
            Tests.SetSliderValue(expAntSens, "sliderAngle", 5);
            expAntSens.Items[1].Click();

            var expFilters = window.Get<GroupBox>("expFilters");
            expFilters.Click();
            Tests.SetSliderValue(expFilters, "sliderFast", 2);
            Tests.SetSliderValue(expFilters, "sliderSlow", 2);
            Tests.SetSliderValue(expFilters, "sliderStationary", 255);
            expFilters.Items[1].Click();

            //Save to batch
            window.Get<Button>("btnSaveBatchParams").Click();
            Thread.Sleep(100);

            //Start
            batchWindow.Get<Button>("btnStart").Click();
            batchWindow.ModalWindows()[0].Keyboard.Enter("y"); //yes to begin

            //Wait till batch done: 2 csv files present
            var counter = 0;
            do
            {
                Thread.Sleep(1000);
                counter++;
            }
            while (counter < 30 && TestVideoFileInfo.Directory.GetFiles(TestVideoFileInfo.Name + "*.csv").Length < 2);

            //The two files are the same and should result in identical csv files
            var csvs = TestVideoFileInfo.Directory.GetFiles(TestVideoFileInfo.Name + "*.csv");
            var trackerResult = Tests.CompareCSVfiles(csvs[0], csvs[1]);
            Assert.IsTrue(trackerResult == 1);
        }
    }
}
