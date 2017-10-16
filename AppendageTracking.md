---
layout: subpage
title: SwarmSight Antenna and Proboscis Extension Reflex Tracking Software
---

# SwarmSight Appendage Tracking Module : Free Software for Tracking Antenna and Proboscis Movements of Honey Bees, Bumble Bees, Crickets, Locusts, Ants, Flies, and Other Insects.

SwarmSight Appendage Tracking module will analyze videos of restrained insects, filmed from the top-down view, and provide the user with the antenna and proboscis tip locations for each frame of the video.

# SwarmSight Appendage Tracking Features

 1. **Fast:** Tracks antenna and proboscis movements in real-time (30+ fps)
 1. **Accurate:** Has same or better accuracy than human trackers (consistent, fatigue-free performance)
 2. **No hassle:** Is free, open-source, and easy to install (no command line or special setup needed)
 3. **No painting:** Does not require painting of antenna tips (head must be restrained)
 3. **No special equipment:** Does not require special equipment (average web camera and a Windows PC will do)

# Video Setup

Insect heads filmed from the top-down view can be analyzed by the software. Below is an example setup for filming a honey bee that is delivered an odor stimulus. The bee and its head is restrained from movement, and the camera is positioned above the head on a tripod.
![SwarmSight Antenna and Proboscis Tracking Experimental Setup](Screenshots/experiment%20diagram.jpg)

The user positions the Antenna Sensor widget, adjusts the filter, and exclusion zone settings.
![SwarmSight Antenna and Proboscis Tracking Screenshot](Screenshots/AntennaTracking.JPG)

The program computes the antenna and proboscis tip locations, and saves the data into a .CSV file that can be opened with most statistical software packages.
![SwarmSight Antenna and Proboscis Tracking CSV File Screenshot](Screenshots/output.jpg)

# Download, Install, and Analyze Example Video

1. Download the latest [SwarmSight Antenna Tracking installer](https://github.com/JustasB/SwarmSight/raw/master/Setup/AntennaTracking/setup.exe) 
2. Download [an example video](https://github.com/JustasB/SwarmSight/raw/master/Examples/Appendage%20Tracking/B1-Feb22-heptanal.mov)
3. Open the installer in Windows OS, and follow the steps on screen
4. The installer will download, install, create shortcuts, and launch the app. The app can be launched from Start Menu > SwarmSight > SwarmSight Antenna Tracking
5. Using the app, open the example video, position/scale/rotate the AntennaSensor widget over the head, and play the video. You may need to adjust filter settings in the "Filters" panel on the right.
6. Once the video finishes playing, save the tracking data using the "Save" panel on the right. The .csv file will be saved in the same folder as the video.

# Video Tutorials for Installation, Output File, and Analysis
[![SwarmSight Antenna Tracking Tutorial](Screenshots/SwarmSight%20Appendage%20Tracking%20Tutorials.jpg)](https://www.youtube.com/playlist?list=PLGOMalOIacj3D5QkkzYop7O_JR-ojcJpl)

Files used in the tutorial can be found in [the Examples folder](https://github.com/JustasB/SwarmSight/tree/master/Examples/Appendage%20Tracking/Birgiolas%20et.%20al.%20(2015)%20JOVE%20figures/Figures%204%265). You can also [download and unzip the full repository](https://github.com/JustasB/SwarmSight/archive/master.zip).

# Output File Reference
The columns of the output .CSV file are described in [the Output Column Reference](Examples/Appendage%20Tracking/ColumnReference). See video tutorials for how to analyze the .CSV files.

# [Society for Neuroscience](http://www.sfn.org/) Conference Poster
Description of software comparison to human trackers and effects of odor presentation on antenna movements can be found in the [SFN 2016 conference poster](https://github.com/JustasB/SwarmSight/raw/master/Examples/Appendage%20Tracking/SwarmSight%20Antenna%20Tracking%20Poster.pdf) and the [abstract](https://github.com/JustasB/SwarmSight/raw/master/Examples/Appendage%20Tracking/SwarmSight%20Antenna%20Tracking%20Abstract.pdf).

# Funding

Development of this software was supported in part by NIH grant R01MH1006674 to SM Crook, NIH R01EB021711 to RC Gerkin, and NSF Ideas lab project on ‘Cracking the olfactory Code’ to BH Smith. 

