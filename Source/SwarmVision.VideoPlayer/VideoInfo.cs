using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SwarmVision.VideoPlayer
{
    public class VideoInfo
    {
        public double FPS;
        public TimeSpan Duration;
        public int TotalFrames;
        public int Height;
        public int Width;

        public VideoInfo(string videoPath)
        {
            var probeResult = Runffprobe(videoPath);

            ParseResult(probeResult);
        }

        private void ParseResult(string probeResult)
        {
            try
            {
                FPS = double.Parse(new Regex(".*, (.*?) fps").Match(probeResult).Groups[1].Value);
            }
            catch
            {
            }

            try
            {
                Duration = TimeSpan.Parse(new Regex("Duration: (.*?),").Match(probeResult).Groups[1].Value);
            }
            catch
            {
            }

            try
            {
                Height = int.Parse(new Regex(".*?, [0-9]{1,5}x([0-9]{1,5})").Match(probeResult).Groups[1].Value);
            }
            catch
            {
            }

            try
            {
                Width = int.Parse(new Regex(".*?, ([0-9]{1,5})x").Match(probeResult).Groups[1].Value);
            }
            catch
            {
            }

            TotalFrames = (int) (Duration.TotalSeconds*FPS);
        }

        private string Runffprobe(string videoPath)
        {
            //return RunExternalExe(, "-hide_banner " + videoPath);

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo("ffprobe.exe");
            //p.StartInfo.Arguments = "/C ffprobe -hide_banner '" + videoPath + "'";
            //p.StartInfo.Arguments = @"-h";
            p.StartInfo.Arguments = string.Format(@"-hide_banner ""{0}""", videoPath);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();
            p.Start();

            string output = (p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd()).Replace("\r\n", "\n");

            p.WaitForExit();

            return output;
        }
    }
}