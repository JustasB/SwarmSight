# Run this R file to generate Figure 5 Right of Birgiolas, et.al. (2017)
if (!require("pacman")) install.packages("pacman")
pacman::p_load(TTR, spdep, TSA, seewave, signal, reshape2, stats, fields, scales, MASS)


filter = stats::filter

#Prepare columns
colNames = c("Frame","BuzzerValue","LeftSector","RightSector","LeftFlagellumTip-X","LeftFlagellumTip-Y","RightFlagellumTip-X","RightFlagellumTip-Y","LeftFlagellumBase-X","LeftFlagellumBase-Y","RightFlagellumBase-X","RightFlagellumBase-Y","RotationAngle","ReceptiveFieldWidth","ReceptiveFieldHeight","ReceptiveFieldOffset-X","ReceptiveFieldOffset-Y","ReceptiveFieldScale-X","ReceptiveFieldScale-Y",paste("L",seq(1,5),sep=""),paste("R",seq(1,5),sep=""))

# Get the list of .csv files produced by SwarmSight with frame numbers identifying odor onset (see below)
files = read.csv("FilesToRun.csv",as.is = TRUE)

distinctConditions = c("heptanol", "0.2M-heptanol", "air", "0.2M-heptanal", "heptanal")

#This will store all the odor-onset aligned data
alignedDF = read.csv(files$File[1], fill = TRUE,header=FALSE,skip=1,nrows = 1)[1,]
names(alignedDF) = c(colNames)
alignedDF$Subject = ""
alignedDF$Condition = ""  
alignedDF = alignedDF[0,]


getOdorOnsetFrame = function(indexInFilesToRunFile, largeChange = 30)
{
  par(mfrow=c(2,1),mai=c(0.5,1.2,0.5,0.5))
  f = indexInFilesToRunFile
  file = files$File[f]
  df = read.csv(file, fill = TRUE,header=FALSE,skip=1)
  names(df) = colNames
  
  # Plot LED brightness
  plot(df$BuzzerValue,type="l",ylab=paste(files$Subject[f], files$Condition[f]))
  
  # Plot the brightness change (derivative)
  deriv = diff(filter(df$BuzzerValue,rep(1,1)))
  plot(deriv,type="l",xaxt="n")
  axis(1, at = seq(0,length(deriv),50), las=1)
  
  # Find the frames where brightness changes
  print(paste("File index: ", f))
  print(paste("File name: ", file))
  print("Frames with large brightness changes (confirm visualy to find the LED onset): ")
  print(which(abs(deriv) > largeChange))
  print(which.max(deriv))
  print(which.min(deriv))
}

# Call this function to find the odor onset frame for each .csv file listed in FilesToRun.csv
# The first frame with a large change in brightness is usually due to LED onset
# Confirm visually to rule out other brightness fluctuations (shadows, people, etc...)
# Record the onset frame in BuzzerStartRow column of FilesToRun.csv
getOdorOnsetFrame(indexInFilesToRunFile = 1)

# Begin processing. Initialize lists to pool aligned measurements across individuals in each condition
maxLag = function(subjectFrames, frames, plot=TRUE,lag.max=100)
{
  r = ccf(subjectFrames$LeftTipAngle[frames],subjectFrames$RightTipAngle[frames],plot=FALSE,lag.max = lag.max)
  
  signal = (r$acf)
  
  if(plot)
  {
    plot(signal,type="l",ylim=c(-1,1))
  }      
  
  maxLoc = which.max(abs(signal))
  
  return(c(r$lag[maxLoc],r$acf[maxLoc]))
}

leftWaveAmps = matrix(0, nrow=300,ncol=113)
rightWaveAmps = matrix(0, nrow=300,ncol=113)



pooledDFs = list()
pooledCounts = list()
pooledSpectrogramsR = list()
pooledSpectrogramsL = list()
pooledFrameAngles = list()

#Extract 100 frames before and 200 after the odor onset (@ 30fps = 10s of video)
framesPre = 100
framesPost = 200

#For figure 3, random files selected for each condition:
#Condition	RandNum	File Index
#heptanol	11	54
#0.2M-heptanol	13	66
#air	3	12
#0.2M-heptanal	11	45
#heptanal	10	53



# For debuging
f=13

baseLagA = c()
baseLagAsig = c()

baseLagB = c()
onAlags = c()
onAlagssig = c()

onBlags = c()
postLagC = c()
postLagA = c()
postLagAsig = c()
postLagB = c()

dfPeaks = as.data.frame(matrix(rep(NA,10),nrow = 1, ncol = 10))
names(dfPeaks) = c("file","condition","baselineValue","peakVal","peakTime","recoverTime","baselineValueA","peakValA","peakTimeA","recoverTimeA")

dfCols = data.frame(matrix(nrow = framesPre+framesPost,ncol=0))

for(f in seq(1:length(files$File)))
{
  # Read the .csv file
  file = files$File[f]
  df = read.csv(file, fill = TRUE,header=FALSE,skip=1)
  names(df) = colNames
  condition = files$Condition[f]
  
  #Sanity check
  #plot(df$BuzzerValue,type="l")
  
  #Get first LED onset frame for the .csv file
  buzzerStartRow = files$BuzzerStartRow[f]
  subjectFrames = df[(buzzerStartRow-framesPre+1):(buzzerStartRow + framesPost),]
  
  subjectFrames$Subject = files$Subject[f]
  subjectFrames$Condition = files$Condition[f]
  
  # Get antenna positions relative to the center of the head
  lpts = matrix(
      c(subjectFrames$`LeftFlagellumTip-X`-subjectFrames$ReceptiveFieldWidth/2,
        subjectFrames$`LeftFlagellumTip-Y`-subjectFrames$ReceptiveFieldHeight/2), ncol =2)
  
  rpts = matrix(
    c(subjectFrames$`RightFlagellumTip-X`-subjectFrames$ReceptiveFieldWidth/2,
      subjectFrames$`RightFlagellumTip-Y`-subjectFrames$ReceptiveFieldHeight/2), ncol =2)
  
  lbpts = matrix(
    c(subjectFrames$`LeftFlagellumBase-X`-subjectFrames$ReceptiveFieldWidth/2,
      subjectFrames$`LeftFlagellumBase-Y`-subjectFrames$ReceptiveFieldHeight/2), ncol =2)
  
  rbpts = matrix(
    c(subjectFrames$`RightFlagellumBase-X`-subjectFrames$ReceptiveFieldWidth/2,
      subjectFrames$`RightFlagellumBase-Y`-subjectFrames$ReceptiveFieldHeight/2), ncol =2)
  
  # Head is rotated, so de-rotate the points (assuming rotation does not change mid course)
  rotang = -median(subjectFrames$RotationAngle,na.rm = TRUE) * pi / 180
  
  lpts = Rotation(lpts,rotang)
  rpts = Rotation(rpts,rotang)
  
  lbpts = Rotation(lbpts,rotang)
  rbpts = Rotation(rbpts,rotang)
  
  subjectFrames$LeftTipAdjustedX = lpts[,1]
  subjectFrames$LeftTipAdjustedY = lpts[,2]
  subjectFrames$RightTipAdjustedX = rpts[,1]
  subjectFrames$RightTipAdjustedY = rpts[,2]
  
  subjectFrames$LeftBaseAdjustedX = lbpts[,1]
  subjectFrames$LeftBaseAdjustedY = lbpts[,2]
  subjectFrames$RightBaseAdjustedX = rbpts[,1]
  subjectFrames$RightBaseAdjustedY = rbpts[,2]
  
  # Visualy confirm (red = tips, blue = base of flagelli)
  # plot(subjectFrames$RightTipAdjustedX,
  #      subjectFrames$RightTipAdjustedY,type="p",
  #      xlim=c(-120,120), ylim=c(-120,120),col=rgb(0.5, 0, 0, 0.5), pch=16) 
  # 
  # points(subjectFrames$LeftTipAdjustedX, 
  #        subjectFrames$LeftTipAdjustedY,
  #      xlim=c(-120,120), ylim=c(-120,120),col=rgb(0.5, 0, 0, 0.5), pch=16) 
  # 
  # points(subjectFrames$LeftBaseAdjustedX, 
  #        subjectFrames$LeftBaseAdjustedY,
  #        xlim=c(-120,120), ylim=c(-120,120),col=rgb(0, 0, 0.5, 0.5), pch=16) 
  # 
  # points(subjectFrames$RightBaseAdjustedX, 
  #        subjectFrames$RightBaseAdjustedY,
  #        xlim=c(-120,120), ylim=c(-120,120),col=rgb(0, 0, 0.5, 0.5), pch=16) 
  
  # Compute the angle formed by tips & bases of each antennae
  subjectFrames$LeftTipAngle = (atan2(
    subjectFrames$LeftTipAdjustedY-subjectFrames$LeftBaseAdjustedY,
    subjectFrames$LeftTipAdjustedX-subjectFrames$LeftBaseAdjustedX)*180/pi+90)%%360
  
  subjectFrames$RightTipAngle = (atan2(
    subjectFrames$RightTipAdjustedY-subjectFrames$RightBaseAdjustedY,
    subjectFrames$RightTipAdjustedX-subjectFrames$RightBaseAdjustedX)*180/pi+90)%%360
  
  # Ensure the angle is +/- 180
  subjectFrames$LeftTipAngle = ifelse(subjectFrames$LeftTipAngle > 180, subjectFrames$LeftTipAngle - 360, subjectFrames$LeftTipAngle)
  subjectFrames$LeftTipAngle = ifelse(subjectFrames$LeftTipAngle < -180, subjectFrames$LeftTipAngle + 360, subjectFrames$LeftTipAngle)
  subjectFrames$RightTipAngle = ifelse(subjectFrames$RightTipAngle > 180, subjectFrames$RightTipAngle - 360, subjectFrames$RightTipAngle)
  subjectFrames$RightTipAngle = ifelse(subjectFrames$RightTipAngle < -180, subjectFrames$RightTipAngle + 360, subjectFrames$RightTipAngle)
  subjectFrames$LeftTipAngle = -subjectFrames$LeftTipAngle
  
  #Remove tracking artifacts with a 3-frame rolling median filter
  #subjectFrames$LeftTipAngle = c(runmed(subjectFrames$LeftTipAngle, 3))
  #subjectFrames$RightTipAngle = c(runmed(subjectFrames$RightTipAngle, 3))
  
   #Sanity checks
   #plot(subjectFrames$LeftTipAngle,type="l")
   #plot(subjectFrames$RightTipAngle,type="l")
    
    #Plot left & right angles separately and superimposed (Figure 3)
    x = (seq(1,framesPre+framesPost)-framesPre)/30*1000

    #First & 2nd &4th files
    par(mfrow=c(5,1),mai=c(0.05,0.8,0.1,0.1),oma=c(1,0,0,0))
    
    plot(x,subjectFrames$RightTipAngle,type="l",ylab="",xlab="",main="",col="blue",lwd=2,xaxt="n",ylim=c(170,-10),xaxp=c(-3000,6500,19))
    polygon(c(0,0,4000,4000),c(1500,-150,-150,1500),border=NA,col=rgb(0,0,0,0.08))
    lines(x,subjectFrames$LeftTipAngle,type="l",col="red",lwd=2,xaxt="n",ylim=c(170,-10))
    text(2000,10,condition,cex=1.5)
    
    #3Rd file
    par(mai=c(0.05,0.8,0,0.1))
    plot(x,subjectFrames$RightTipAngle,type="l",ylab="",xlab="",main="",col="blue",lwd=2,xaxt="n",ylim=c(170,-10),xaxp=c(-3000,6500,19))
    polygon(c(0,0,4000,4000),c(1500,-150,-150,1500),border=NA,col=rgb(0,0,0,0.08))
    lines(x,subjectFrames$LeftTipAngle,type="l",col="red",lwd=2,xaxt="n",ylim=c(170,-10))
    text(2000,10,condition,cex=1.5)
    mtext("Antenna Angle (degrees)",side=2,line=2.5,cex=0.8)
    
    #Last File
    par(mai=c(0.5,0.8,0,0.1))
    plot(x,subjectFrames$RightTipAngle,type="l",ylab="",xlab="",main="",col="blue",lwd=2,xaxt="s",ylim=c(170,-10),xaxp=c(-3000,6500,19))
    polygon(c(0,0,4000,4000),c(1500,-150,-150,1500),border=NA,col=rgb(0,0,0,0.08))
    lines(x,subjectFrames$LeftTipAngle,type="l",col="red",lwd=2,xaxt="n",ylim=c(170,-10))
    text(2000,10,condition,cex=1.5)
    mtext("Time after Odor Onset (ms)",side=1,line=2.2,cex=0.8)
    
      
      
  # Add measurements to the pool  
  if(!(condition %in% names(pooledDFs)))
  {
    pooledDFs[[condition]] = subjectFrames
    pooledCounts[[condition]] = 1
    
    pooledFrameAngles[[paste(condition,"Left")]] = matrix(nrow=framesPre+framesPost,ncol = length(subset(files$Condition,files$Condition==condition)))
    pooledFrameAngles[[paste(condition,"Left")]][,pooledCounts[[condition]]] = subjectFrames$LeftTipAngle
    
    pooledFrameAngles[[paste(condition,"Right")]] = matrix(nrow=framesPre+framesPost,ncol = length(subset(files$Condition,files$Condition==condition)))
    pooledFrameAngles[[paste(condition,"Right")]][,pooledCounts[[condition]]] = subjectFrames$RightTipAngle
    
  } else
  { 
    #pool averagable values names(subjectFrames)
    avgCols = c(2:4,20:29,32:41)
    names(subjectFrames)[avgCols]
    pooledDFs[[condition]][,avgCols] = pooledDFs[[condition]][,avgCols] + subjectFrames[,avgCols]
    
    pooledFrameAngles[[paste(condition,"Left")]][,pooledCounts[[condition]]] = subjectFrames$LeftTipAngle
    pooledFrameAngles[[paste(condition,"Right")]][,pooledCounts[[condition]]] = subjectFrames$RightTipAngle
    
    pooledCounts[[condition]] = pooledCounts[[condition]] + 1
  }
  
#    Sanity checks
#    plot(subjectFrames$BuzzerValue,type="l")
#    plot(pooledDFs[[condition]]$BuzzerValue,type="l")
#    plot(subjectFrames$LeftSector,type="l",ylim=c(1,5))
#    lines(subjectFrames$RightSector,type="l",col="gray")
#    plot(pooledDFs[[condition]]$LeftSector,type="l")
#    lines(pooledDFs[[condition]]$RightSector,type="l",col="gray")
       

}


#Divide the pooled sums to get the averages
par(mfrow=c(1,1),mai=c(0.5,1.2,0.5,0.5))
k = 1 # Debugging

for(k in seq(length(names(pooledDFs))))
{
  key = names(pooledDFs)[k]
  
  #Sanity checks
  #plot(pooledDFs[[key]]$LeftSector,type="l", xlab = key)
  #lines(pooledDFs[[key]]$RightSector,type="l",col="gray")
  
  pooledDFs[[key]][,avgCols] = pooledDFs[[key]][,avgCols] / pooledCounts[[key]]
  
  pooledSpectrogramsR[[key]] = pooledSpectrogramsR[[key]] / pooledCounts[[key]]
  pooledSpectrogramsL[[key]] = pooledSpectrogramsL[[key]] / pooledCounts[[key]]

}
#END DATA PREP


#TIP ANGLE DENSITIES & MEANS
# Initialize - run the code from here to the end of the file to ensure the color range for the
# density plots has the same scale for all conditions
ranges = c()
k = 1
plotQuality = 8 #5-8 are low-high values (high values take longer to compute)



# Setup color pallete: blue for low density, red for high
library(RColorBrewer)
rf <- colorRampPalette((rev(brewer.pal(11,'RdYlBu'))))
r <- rf(32)


# Rerun starting here if color scales different
par(mfrow=c(5,1),mai=c(0.1,0.5,0,0),oma=c(1,1,0,0))
for(k in seq(length(distinctConditions)))
{
    key = distinctConditions[k]
  
    # Prepare pooled data for density analysis
    molten = rbind(
       na.omit(melt(pooledFrameAngles[[paste(key,"Left")]]))
      ,na.omit(melt(pooledFrameAngles[[paste(key,"Right")]]))
    )
    
    # Average the angles across both antennae
    bothAvg = (pooledDFs[[key]]$RightTipAngle+pooledDFs[[key]]$LeftTipAngle)/2
    
    # Set the plot margin for the last condition
    if(k==5) 
    {
      par(mai=c(0.5,0.5,0,0))
    }
      
    # Compute the angle densities for each frame
    densityKernel = kde2d((molten$Var1-100)/30*1000,molten$value,n=2^plotQuality,lims=c(range(-100/30*1000,200/30*1000),range(10,135)))
    ranges = c(ranges, range(densityKernel$z))
    image.plot(densityKernel,col=r,main="",ylim=c(135,10),zlim=range((ranges)),xaxt=ifelse(k==5,"s","n"),xaxp=c(-3000,6500,19))
    
    # odor on/off lines
    polygon(c(0,0,3600,3600),c(150,0,0,150),col=rgb(0,0,0,0.2),border = NA)
    text(2000,30,key,cex = 1.5,col="white")
  
    # Show the frame-by-frame average angle 
    lines((seq(1,300)-100)/30*1000,bothAvg,type="l",col=rgb(0,0,0,0.6),lwd=2)
    
    # Show pre-odor baseline
    baseline = mean(bothAvg[1:framesPre],na.rm = TRUE)
    abline(h = baseline,col=rgb(0,0,0,0.5))
    

    # Return to baseline times    
    # x = seq(300)
    # y = bothAvg - baseline
    # plot(x,y,xlim=c(100,250),xaxp=c(100,250,10))
    # lines(smooth.spline(x,y, spar = 0.8))
    # abline(h = 0,col=rgb(0,0,0,0.5))
    # print(key)
    
    
    # Plot labels
    if(k == 3)
    {
      mtext("Antenna Angle (degrees)", side=2,line=2.5,cex=0.8)
    }
    
    if(k == 5)
    {
      mtext("Time after Odor Onset (ms)", side=1,line=2.5,cex=0.8)
      
      # Density cluster labels
      arrows(1500,100,1200,120,length = 0.1,angle=15)
      arrows(5000,40,5250,30,length = 0.1,angle=15)
      arrows(1500,40,1100,30,length = 0.1,angle=15)
    }
}
# IMPORTANT: CHECK THE COLOR SCALES ON THE RIGHT SIDE OF THE PLOT
# If maximum values are different across conditions, rerun the for-loop above (line 264)

