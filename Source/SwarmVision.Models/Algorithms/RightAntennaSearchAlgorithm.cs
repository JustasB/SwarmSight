using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using AForge.Neuro;
using SwarmVision.HeadPartsTracking.Models;
using Point = System.Windows.Point;

namespace SwarmVision.HeadPartsTracking.Algorithms
{
    public class RightAntennaSearchAlgorithm : AntennaSearchAlgorithm
    {
        protected override HeadModel CreateChild(HeadModel parent1, HeadModel parent2)
        {
            var result = new HeadModel();

            result.RightAntena.Root.StartX.Value = Cross
            (
                parent1.RightAntena.Root.StartX,
                parent2.RightAntena.Root.StartX,
                parent1.RightAntena.Root.StartX.Min,
                parent1.RightAntena.Root.StartX.Max
            );

            result.RightAntena.Root.StartY.Value = Cross
            (
                parent1.RightAntena.Root.StartY,
                parent2.RightAntena.Root.StartY,
                parent1.RightAntena.Root.StartY.Min,
                parent1.RightAntena.Root.StartY.Max
            );

            //ANTENA - RIGHT - ROOT
            result.RightAntena.Root.Angle.Index = Cross
                (
                    parent1.RightAntena.Root.Angle.Index,
                    parent2.RightAntena.Root.Angle.Index,
                    0,
                    1
                );

            result.RightAntena.Root.Length.Value = (int)Cross
                                                      (
                                                          parent1.RightAntena.Root.Length,
                                                          parent2.RightAntena.Root.Length,
                                                          parent1.RightAntena.Root.Length.Min,
                                                          parent1.RightAntena.Root.Length.Max
                                                      );

            result.RightAntena.Root.Thickness.Value = (int)Cross
                                                      (
                                                          parent1.RightAntena.Root.Thickness,
                                                          parent2.RightAntena.Root.Thickness,
                                                          parent1.RightAntena.Root.Thickness.Min,
                                                          parent1.RightAntena.Root.Thickness.Max
                                                      );

            //ANTENA - RIGHT - TIP
            result.RightAntena.Tip.Angle.Index = Cross
                (
                    parent1.RightAntena.Tip.Angle.Index,
                    parent2.RightAntena.Tip.Angle.Index,
                    0,
                    1
                );

            result.RightAntena.Tip.Length.Value = (int)Cross
                                                     (
                                                         parent1.RightAntena.Tip.Length,
                                                         parent2.RightAntena.Tip.Length,
                                                         parent1.RightAntena.Tip.Length.Min,
                                                         parent1.RightAntena.Tip.Length.Max
                                                     );

            result.RightAntena.Tip.Thickness.Value = (int)Cross
                                                      (
                                                          parent1.RightAntena.Tip.Thickness,
                                                          parent2.RightAntena.Tip.Thickness,
                                                          parent1.RightAntena.Tip.Thickness.Min,
                                                          result.RightAntena.Root.Thickness
                                                      );

            return result;
        }

        protected override HeadModel CreateNewRandomMember()
        {
            //HEAD
            var result = new HeadModel();

            result.RightAntena.Root.StartX.Value = Random.Next
            (
                (int)result.RightAntena.Root.StartX.Min,
                (int)result.RightAntena.Root.StartX.Max
            );

            result.RightAntena.Root.StartY.Value = Random.Next
            (
                (int)result.RightAntena.Root.StartY.Min,
                (int)result.RightAntena.Root.StartY.Max
            );

            //ANTENA - RIGHT - ROOT
            result.RightAntena.Root.Angle.Index = Random.NextDouble();
            result.RightAntena.Root.Length.Value = Random.Next
            (
                (int)result.RightAntena.Root.Length.Min,
                (int)result.RightAntena.Root.Length.Max
            );

            result.RightAntena.Root.Thickness.Value = Random.Next
            (
                (int)result.RightAntena.Root.Thickness.Min,
                (int)result.RightAntena.Root.Thickness.Max
            );

            //ANTENA - RIGHT - TIP
            result.RightAntena.Tip.Angle.Index = Random.NextDouble();
            result.RightAntena.Tip.Length.Value = Random.Next
            (
                (int)result.RightAntena.Tip.Length.Min,
                (int)result.RightAntena.Tip.Length.Max
            );

            result.RightAntena.Tip.Thickness.Value = Random.Next
            (
                (int)result.RightAntena.Tip.Thickness.Min,
                (int)result.RightAntena.Root.Thickness
            );

            return result;
        }
    }
}