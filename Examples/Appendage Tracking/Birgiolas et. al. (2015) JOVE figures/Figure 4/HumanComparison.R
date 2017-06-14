# Run this R file to produce Figure 4 of Birgiolas, et.al. (2017)
require(zoo)
sets = list()

# Specify the location of automated and manual tracking files

sets[[1]] = c(
"./B1-Feb22-0.2M-heptanal/B1-Feb22-0.2M-heptanal.mov_Tracker_2016-05-04 05-12.csv",
"./B1-Feb22-0.2M-heptanal/B1-Feb22-0.2M-heptanal.mov_HandAnnotated_20160331 0058-ONE.csv",
"./B1-Feb22-0.2M-heptanal/B1-Feb22-0.2M-heptanal.mov_HandAnnotated_autosave-TWO.csv"
)

sets[[2]] = c(
"./B1-Feb22-0.2M-heptanal-1/B1-Feb22-0.2M-heptanal-1.mov_Tracker_2016-05-04 05-19.csv",
"./B1-Feb22-0.2M-heptanal-1/B1-Feb22-0.2M-heptanal-1.mov_HandAnnotated_autosave-ONE.csv"
)

sets[[3]] = c(
  "./B1-Feb22-0.2M-heptanol/B1-Feb22-0.2M-heptanol.mov_Tracker_2016-05-06 02-48.csv",
"./B1-Feb22-0.2M-heptanol/B1-Feb22-0.2M-heptanol.mov_HandAnnotated_autosave-TWO.csv"
)

sets[[4]] = c(
"./B1-Feb22-heptanal/B1-Feb22-heptanal.mov_Tracker_2016-05-06 02-57.csv",
"./B1-Feb22-heptanal/B1-Feb22-heptanal.mov_HandAnnotated_autosave-incomplete-Frame0-179-ONE.csv",
"./B1-Feb22-heptanal/B1-Feb22-heptanal.mov_HandAnnotated_autosave-TWO.csv"
)

sets[[5]] = c(
"./B1-Feb23-0.2M-heptanol/B1-Feb23-0.2M-heptanol.mov_Tracker_2016-05-04 05-41.csv",
"./B1-Feb23-0.2M-heptanol/B1-Feb23-0.2M-heptanol.mov_HandAnnotated_autosave-TARYN.csv"
)

sets[[6]] = c(
"./B4-Feb22-blank/B4-Feb22-blank.mov_Tracker_2016-05-04 05-24.csv",
"./B4-Feb22-blank/B4-Feb22-blank.mov_HandAnnotated_20160410 2030-FramesAll-ONE.csv"
)

# Initialize
setCount = 6
dfCombined = NA
s = 1

# Process each set
for(s in seq(setCount))
{
  numFiles = length(sets[[s]])

  # Read software tracker results
  dfTracker = read.csv(sets[[s]][1], fill = TRUE,header=FALSE,skip=1)[,c(1,5:8,16:17)]
  names(dfTracker) = c("F",	"LFTX",	"LFTY",	"RFTX",	"RFTY", "OffX","OffY")
  
  # Filter each coordinate by a 3-frame median filter
  medianWindow = 3
  dfTracker$LFTX = rollmedian(dfTracker$LFTX + dfTracker$OffX,medianWindow, fill=NA)
  dfTracker$RFTX = rollmedian(dfTracker$RFTX + dfTracker$OffX,medianWindow, fill=NA)
  
  dfTracker$LFTY = rollmedian(dfTracker$LFTY + dfTracker$OffY,medianWindow, fill=NA)
  dfTracker$RFTY = rollmedian(dfTracker$RFTY + dfTracker$OffY,medianWindow, fill=NA)
  
  plot(dfTracker$LFTX,dfTracker$LFTY)
  plot(dfTracker$RFTX,dfTracker$RFTY)
  
  # Read the first human rater's results
  dfOne = read.csv(sets[[s]][2], fill = TRUE,header=FALSE,skip=1)[,c(1,14:15,20:21)]
  names(dfOne) = c("F","LFTX",	"LFTY",	"RFTX",	"RFTY")
  
  # Create empty columns for human data
  dfTracker$F1 = NA
  dfTracker$LFTX1 = NA
  dfTracker$LFTY1 = NA
  dfTracker$RFTX1 = NA
  dfTracker$RFTY1 = NA
  
  # Place the human 1 data next to the software tracker data
  row = 1
  for(row in seq(nrow(dfTracker)))
  {
     rowIndex = which(dfTracker$F[row] == dfOne$F)
     
     if(length(rowIndex) != 0)
     {
       dfTracker$F1[row] = dfOne$F[rowIndex]
       dfTracker$LFTX1[row] = dfOne$LFTX[rowIndex]
       dfTracker$LFTY1[row] = dfOne$LFTY[rowIndex]
       dfTracker$RFTX1[row] = dfOne$RFTX[rowIndex]
       dfTracker$RFTY1[row] = dfOne$RFTY[rowIndex]
     }
  }
  
  # Sanity check
  plot(dfTracker$LFTX1,dfTracker$LFTY1)
  plot(dfTracker$RFTX1,dfTracker$RFTY1)
  
  dfTracker$F2 = NA
  dfTracker$LFTX2 = NA
  dfTracker$LFTY2 = NA
  dfTracker$RFTX2 = NA
  dfTracker$RFTY2 = NA
  
  # If there is a second human, read his/er data
  if(numFiles == 3)
  {
    dfTwo = read.csv(sets[[s]][3], fill = TRUE,header=FALSE,skip=1)[,c(1,14:15,20:21)]
    names(dfTwo) = c("F","LFTX",	"LFTY",	"RFTX",	"RFTY")
    
    row = 1
    for(row in seq(nrow(dfTracker)))
    {
      rowIndex = which(dfTracker$F[row] == dfTwo$F)
      
      if(length(rowIndex) != 0)
      {
        dfTracker$F2[row] = dfTwo$F[rowIndex]
        dfTracker$LFTX2[row] = dfTwo$LFTX[rowIndex]
        dfTracker$LFTY2[row] = dfTwo$LFTY[rowIndex]
        dfTracker$RFTX2[row] = dfTwo$RFTX[rowIndex]
        dfTracker$RFTY2[row] = dfTwo$RFTY[rowIndex]
      }
    }
    
    plot(dfTracker$LFTX2,dfTracker$LFTY2)
    plot(dfTracker$RFTX2,dfTracker$RFTY2)
    
  }
  
  plot(c(dfTracker$LFTX,dfTracker$LFTX1,dfTracker$LFTX2),c(dfTracker$LFTY,dfTracker$LFTY1,dfTracker$LFTY2))
  plot(c(dfTracker$RFTX,dfTracker$RFTX1,dfTracker$RFTX2),c(dfTracker$RFTY,dfTracker$RFTY1,dfTracker$RFTY2))
  
  # Prepend the software + 1/2 human(s) data to the previous video data
  if(!is.data.frame(dfCombined))
  {
    dfCombined = dfTracker
  } else {
    dfCombined = rbind(dfCombined,dfTracker)
  }
}

# Compute euclidean distances between tip locations found by software and by human 1
dfCombined$DS1L = sqrt((dfCombined$LFTX-dfCombined$LFTX1)^2+(dfCombined$LFTY-dfCombined$LFTY1)^2)
dfCombined$DS1R = sqrt((dfCombined$RFTX-dfCombined$RFTX1)^2+(dfCombined$RFTY-dfCombined$RFTY1)^2)

# Between software and human 2
dfCombined$DS2L = sqrt((dfCombined$LFTX-dfCombined$LFTX2)^2+(dfCombined$LFTY-dfCombined$LFTY2)^2)
dfCombined$DS2R = sqrt((dfCombined$RFTX-dfCombined$RFTX2)^2+(dfCombined$RFTY-dfCombined$RFTY2)^2)

# Between human 1 and 2
dfCombined$D12L = sqrt((dfCombined$LFTX1-dfCombined$LFTX2)^2+(dfCombined$LFTY1-dfCombined$LFTY2)^2)
dfCombined$D12R = sqrt((dfCombined$RFTX1-dfCombined$RFTX2)^2+(dfCombined$RFTY1-dfCombined$RFTY2)^2)

# Keep only rows fully rated by two humans 
dfCombined = subset(dfCombined,(!is.na(D12L)) & (!is.na(D12R)))

# Find the closest human provided point to software provided point
# Why: we don't know which human is the "best". We assume the human closest to software is the best.
dfCombined$BestL = ifelse(
  dfCombined$DS1L > dfCombined$DS2L,
  dfCombined$DS2L,
  dfCombined$DS1L
)

# Repeat for right antenna
dfCombined$BestR = ifelse(
  dfCombined$DS1R > dfCombined$DS2R,
  dfCombined$DS2R,
  dfCombined$DS1R
)

# Exclude software NA frames (1st one due to median filter)
dfCombined = subset(dfCombined,(!is.na(BestL)) & (!is.na(BestR)))


# Combine left and right distances
SHDs = c(dfCombined$BestR,dfCombined$BestL)
HHDs = c(dfCombined$D12R,dfCombined$D12L)


# Find the mean software-human and human-human disagreement
# + summary stats for S-H and H-H distances
summary(SHDs)
summary(HHDs)

# Find 95th percentiles for software-human distances
quantile(SHDs,c(0.95))
# Human-Human distances
quantile(HHDs,c(0.95))

# Create histograms of each type of distance
hist(SHDs,breaks=c(0,10,20,30,40,50,100))
hist(HHDs,breaks=c(0,10,20,30,40,50,100))

# Plot left tip Y coord for humans and software
plot(dfCombined$LFTY,type="l",ylim=c(0,350))
lines(dfCombined$LFTY1,type="l",col="red")
lines(dfCombined$LFTY2,type="l",col="blue")
lines(dfCombined$BestL,type="l")
lines(dfCombined$D12L,type="l",col="red")

# Save the results to humanComp.csv file, then open it in Excel for plotting, etc...
write.csv(dfCombined,"humanComp.csv")
