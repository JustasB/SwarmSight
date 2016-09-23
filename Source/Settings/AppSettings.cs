using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;

namespace Settings {


    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    public sealed partial class AppSettings {

        private System.Timers.Timer timer;

        public AppSettings()
        { 
            //Create a cancellable, delay-timer for saving
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += (s, e) =>
            {
                Save();

                Debug.WriteLine("AppSettings Saved");

                timer.Stop();
            };

            timer.Stop();
        }
        
        public string AsJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        public System.Drawing.Point Origin
        {
            get { return new System.Drawing.Point(HeadX,HeadY); }
        }
        
        /// <summary>
        /// Save after a 1s delay, if this method is not called within the next 1s
        /// </summary>
        public void SaveAsync()
        {
            timer.Stop();
            timer.Start();
        }

        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Add code to handle the SettingChangingEvent event here.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }

        public void LoadFromJSON(string paramsText)
        {
            JsonConvert.PopulateObject(paramsText, this,
            new JsonSerializerSettings
            {
                Error = delegate (object sender, ErrorEventArgs args)
                {
                    args.ErrorContext.Handled = true;
                },
            });
        }
    }
}
