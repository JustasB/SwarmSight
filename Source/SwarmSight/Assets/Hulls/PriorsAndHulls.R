library(phonTools)
files = c("B1.csv","B2.csv","Bee1.csv")
dfull = read.csv(files[1])[0,]

i = 1
for(i in 1:length(files))
{
  df = read.csv(files[i])
  
  rsY = mean(df$Right.ScapeY,na.rm = TRUE)
  lsY = mean(df$Left.ScapeY,na.rm = TRUE)
  rsX = mean(df$Right.ScapeX,na.rm = TRUE)
  lsX = mean(df$Left.ScapeX,na.rm = TRUE)
  
  angle = 180/pi*atan2(rsY-lsY,rsX-lsX)
  centerX = (rsX+lsX)/2
  centerY = (rsY+lsY)/2
  length = sqrt((rsY-lsY)^2+(rsX-lsX)^2)
  print(length)
  
  df$lmt = rotate(cbind((df$Left.Mandible.TipX - centerX) / length, (df$Left.Mandible.TipY - centerY) / length),-angle,degrees=TRUE)
  df$rmt = rotate(cbind((df$Right.Mandible.TipX - centerX) / length, (df$Right.Mandible.TipY - centerY) / length),-angle,degrees=TRUE)
  
  df$lmb = rotate(cbind((df$Left.Mandible.BaseX - centerX) / length, (df$Left.Mandible.BaseY - centerY) / length),-angle,degrees=TRUE)
  df$rmb = rotate(cbind((df$Right.Mandible.BaseX - centerX) / length, (df$Right.Mandible.BaseY - centerY) / length),-angle,degrees=TRUE)
  
  df$lfb = rotate(cbind((df$Left.Flagellum.BaseX - centerX) / length, (df$Left.Flagellum.BaseY - centerY) / length),-angle,degrees=TRUE)
  df$rfb = rotate(cbind((df$Right.Flagellum.BaseX - centerX) / length, (df$Right.Flagellum.BaseY - centerY) / length),-angle,degrees=TRUE)
  
  df$lft = rotate(cbind((df$Left.Flagellum.TipX - centerX) / length, (df$Left.Flagellum.TipY - centerY) / length),-angle,degrees=TRUE)
  df$rft = rotate(cbind((df$Right.Flagellum.TipX - centerX) / length, (df$Right.Flagellum.TipY - centerY) / length),-angle,degrees=TRUE)
  
  
  df$ls = rotate(cbind((df$Left.ScapeX - centerX) / length,  (df$Left.ScapeY - centerY) / length),-angle,degrees=TRUE) 
  df$rs = rotate(cbind((df$Right.ScapeX - centerX) / length, (df$Right.ScapeY - centerY) / length),-angle,degrees=TRUE)
  
  df = subset(df, rfb[,1] < 4)
  df = subset(df, rft[,1] > 1)
  df = subset(df, ls[,1] < -0.1)
  
  dfull = rbind(dfull,df)
}

getHull = function(xy)
{
  library(grDevices)
  targetCoords = na.omit(xy)
  return (targetCoords[chull(targetCoords),])
}

dfcomb = as.data.frame(seq(1,dim(dfull)[1]*2))
dfcomb$rft = rbind(dfull$rft,cbind(-dfull$lft[,1],dfull$lft[,2]))
dfcomb$lft = rbind(dfull$lft,cbind(-dfull$rft[,1],dfull$rft[,2]))

dfcomb$rfb = rbind(dfull$rfb,cbind(-dfull$lfb[,1],dfull$lfb[,2]))
dfcomb$lfb = rbind(dfull$lfb,cbind(-dfull$rfb[,1],dfull$rfb[,2]))
dfcomb$rs = rbind(dfull$rs,cbind(-dfull$ls[,1],dfull$ls[,2]))
dfcomb$ls = rbind(dfull$ls,cbind(-dfull$rs[,1],dfull$rs[,2]))

dfcomb$rmb = rbind(dfull$rmb,cbind(-dfull$lmb[,1],dfull$lmb[,2]))
dfcomb$lmb = rbind(dfull$lmb,cbind(-dfull$rmb[,1],dfull$rmb[,2]))
dfcomb$rmt = rbind(dfull$rmt,cbind(-dfull$lmt[,1],dfull$lmt[,2]))
dfcomb$lmt = rbind(dfull$lmt,cbind(-dfull$rmt[,1],dfull$rmt[,2]))

dfcomb.allHull = getHull(rbind(dfcomb$rft,dfcomb$lft))

dfcomb.rftHull = getHull(dfcomb$rft)
dfcomb.lftHull = getHull(dfcomb$lft)
dfcomb.rfbHull = getHull(dfcomb$rfb)
dfcomb.lfbHull = getHull(dfcomb$lfb)
dfcomb.rsHull = getHull(dfcomb$rs)
dfcomb.lsHull = getHull(dfcomb$ls)

dfcomb.rmbHull = getHull(dfcomb$rmb)
dfcomb.lmbHull = getHull(dfcomb$lmb)
dfcomb.rmtHull = getHull(dfcomb$rmt)
dfcomb.lmtHull = getHull(dfcomb$lmt)

dfcomb.mandiblesHull = getHull(rbind(dfcomb$rmt,dfcomb$rmb,dfcomb$lmt,dfcomb$lmb))

plot(dfcomb.allHull)
points(dfcomb$lft[,1],dfcomb$lft[,2],pch=21,bg="yellow");lines(dfcomb.lftHull);
points(dfcomb$rft[,1],dfcomb$rft[,2],pch=21,bg="pink");lines(dfcomb.rftHull);

points(dfcomb$lfb[,1],dfcomb$lfb[,2],pch=21,bg='Red'); lines(dfcomb.lfbHull);
points(dfcomb$rfb[,1],dfcomb$rfb[,2],pch=21,bg='Red'); lines(dfcomb.rfbHull);

points(dfcomb$rmb[,1],dfcomb$rmb[,2],pch=21,bg="blue"); lines(dfcomb.rmbHull);
points(dfcomb$lmb[,1],dfcomb$lmb[,2],pch=21,bg="purple"); lines(dfcomb.lmbHull);
points(dfcomb$rmt[,1],dfcomb$rmt[,2],pch=21,bg="blue"); lines(dfcomb.rmbHull);
points(dfcomb$lmt[,1],dfcomb$lmt[,2],pch=21,bg="purple"); lines(dfcomb.lmbHull);

lines(dfcomb.mandiblesHull);

points(dfcomb$rs[,1],dfcomb$rs[,2],pch=21,bg="green"); lines(dfcomb.rsHull);
points(dfcomb$ls[,1],dfcomb$ls[,2],pch=21,bg="orange"); lines(dfcomb.lsHull);

abline(lm(c(dfcomb$ls[,2],dfcomb$rs[,2])~c(dfcomb$ls[,1],dfcomb$rs[,1])))

library(rowr)
conHullDf = cbind.fill(
  dfcomb.allHull, 
  dfcomb.mandiblesHull,
  
  dfcomb.lftHull, 
  dfcomb.rftHull, 
  dfcomb.lfbHull, 
  dfcomb.rfbHull, 
  dfcomb.lsHull, 
  dfcomb.rsHull,
  
  dfcomb.lmtHull, 
  dfcomb.rmtHull, 
  dfcomb.lmbHull, 
  dfcomb.rmbHull, 
fill = NA)

names(conHullDf) = c(
  "allX", "allY", 
  "mandiblesX", "mandiblesY",
  
  "lftX","lftY",
  "rftX","rftY", 
  "lfbX","lfbY",
  "rfbX","rfbY", 
  "lsX","lsY",
  "rsX","rsY", 
  
  "lmtX","lmtY",
  "rmtX","rmtY", 
  "lmbX","lmbY",
  "rmbX","rmbY"
)

write.csv(conHullDf, "convexHulls.csv")

priorPointsDf = data.frame(cbind(  
  dfcomb$lft, 
  dfcomb$rft, 
  dfcomb$lfb, 
  dfcomb$rfb, 
  dfcomb$ls, 
  dfcomb$rs,
  
  dfcomb$lmt, 
  dfcomb$rmt, 
  dfcomb$lmb, 
  dfcomb$rmb
))

names(priorPointsDf) = c(
  "lftX","lftY",
  "rftX","rftY", 
  "lfbX","lfbY",
  "rfbX","rfbY", 
  "lsX","lsY",
  "rsX","rsY", 
  
  "lmtX","lmtY",
  "rmtX","rmtY", 
  "lmbX","lmbY",
  "rmbX","rmbY"
)

library(zoo)
priorPointsDf = na.aggregate(priorPointsDf) # replace NAs with column means

write.csv(priorPointsDf, "priorPoints.csv")

