# SwarmVision
SwarmVision is a video motion analysis tool to assess the aggregate movement or activity levels of groups or swarms of animals. It is used by behavioral scientists to study the behavior of insects, birds, fish, and other animals. It's free, open-source, and runs on Windows.

##Analyze Video Motion

SwarmVision provides frame-by-frame motion data from any video. Vary the sensitivity to find just the moving objects you want. Amplify the size of the detected motion to quickly spot fast moving objects. Pick between processing speed or accuracy. You can even restrict to a specific region in the video.

![Main UI](https://raw.githubusercontent.com/justasb/SwarmVision/master/Screenshots/Main.JPG)

Motion of fast-moving [stingless bees](https://en.wikipedia.org/wiki/Tetragonisca_angustula) shown in yellow & blue.

##Compare Motion

Have videos with a control and treatment group? Load their motion activity files and compare the two. SwarmVision will also compare parts of the same video. Just load it twice and pick different times in the video to compare.

![SwarmVision Compare Videos UI](https://raw.githubusercontent.com/justasb/SwarmVision/master/Screenshots/Compare.JPG)

##Motion Comparison Statistics

Need to know if the motion between two videos is different in a statistically significant way? Just load the video motion data and click "Compute Statistics". You'll get a the results of a tow-tailed T-Test, including the p-value, and its *** significance. Will also show a chart of the two videos with error bars showing the 0.05 significance thresholds.

![SwarmVision Statistics](https://raw.githubusercontent.com/justasb/SwarmVision/master/Screenshots/Stats.JPG)

#Download, Unzip, & Install

1. [Download the ZIP file](https://github.com/justasb/SwarmVision/raw/master/Download/SwarmVision.zip)
2. Unzip the downloaded file
3. Install by clicking setup.exe

#Problems
If you run into problems, please [report them as issues](https://github.com/justasb/SwarmVision/issues).

#Source

The app is written in C#. To make changes, you will need [.Net Framework](https://www.microsoft.com/net) and [Visual Studio (free)](https://www.visualstudio.com/products/visual-studio-community-vs).

Once you get the [source code](https://github.com/justasb/SwarmVision/tree/master/Source), click on SwarmVision.sln to load the project into Visual Studio. Then click "Start" to launch the app.

If you run into problems, please [report them as issues](https://github.com/justasb/SwarmVision/issues).

#Legal

You may use the app for any purpose (commercial or otherwise) and can modify, copy, and redistribute it as long as you cite me, Justas Birgiolas, as the developer. The app comes with NO WARRANTY. 
