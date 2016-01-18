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
    public class LeftAntennaSearchAlgorithm : AntennaSearchAlgorithm
    {
        protected override HeadModel CreateChild(HeadModel parent1, HeadModel parent2)
        {
            var result = new HeadModel();

            result.LeftAntena.Root.StartX.Value = Cross
            (
                parent1.LeftAntena.Root.StartX,
                parent2.LeftAntena.Root.StartX,
                parent1.LeftAntena.Root.StartX.Min,
                parent1.LeftAntena.Root.StartX.Max
            );

            result.LeftAntena.Root.StartY.Value = Cross
            (
                parent1.LeftAntena.Root.StartY,
                parent2.LeftAntena.Root.StartY,
                parent1.LeftAntena.Root.StartY.Min,
                parent1.LeftAntena.Root.StartY.Max
            );

            //ANTENA - RIGHT - ROOT
            result.LeftAntena.Root.Angle.Index = Cross
                (
                    parent1.LeftAntena.Root.Angle.Index,
                    parent2.LeftAntena.Root.Angle.Index,
                    0,
                    1
                );

            result.LeftAntena.Root.Length.Value = (int)Cross
                                                      (
                                                          parent1.LeftAntena.Root.Length,
                                                          parent2.LeftAntena.Root.Length,
                                                          parent1.LeftAntena.Root.Length.Min,
                                                          parent1.LeftAntena.Root.Length.Max
                                                      );

            result.LeftAntena.Root.Thickness.Value = (int)Cross
                                                      (
                                                          parent1.LeftAntena.Root.Thickness,
                                                          parent2.LeftAntena.Root.Thickness,
                                                          parent1.LeftAntena.Root.Thickness.Min,
                                                          parent1.LeftAntena.Root.Thickness.Max
                                                      );

            //ANTENA - RIGHT - TIP
            result.LeftAntena.Tip.Angle.Index = Cross
                (
                    parent1.LeftAntena.Tip.Angle.Index,
                    parent2.LeftAntena.Tip.Angle.Index,
                    0,
                    1
                );

            result.LeftAntena.Tip.Length.Value = (int)Cross
                                                     (
                                                         parent1.LeftAntena.Tip.Length,
                                                         parent2.LeftAntena.Tip.Length,
                                                         parent1.LeftAntena.Tip.Length.Min,
                                                         parent1.LeftAntena.Tip.Length.Max
                                                     );

            result.LeftAntena.Tip.Thickness.Value = (int)Cross
                                                      (
                                                          parent1.LeftAntena.Tip.Thickness,
                                                          parent2.LeftAntena.Tip.Thickness,
                                                          parent1.LeftAntena.Tip.Thickness.Min,
                                                          result.LeftAntena.Root.Thickness
                                                      );

            return result;
        }

        protected override HeadModel CreateNewRandomMember()
        {
            //HEAD
            var result = new HeadModel();

            result.LeftAntena.Root.StartX.Value = Random.Next
            (
                (int)result.LeftAntena.Root.StartX.Min,
                (int)result.LeftAntena.Root.StartX.Max
            );

            result.LeftAntena.Root.StartY.Value = Random.Next
            (
                (int)result.LeftAntena.Root.StartY.Min,
                (int)result.LeftAntena.Root.StartY.Max
            );

            //ANTENA - RIGHT - ROOT
            result.LeftAntena.Root.Angle.Index = Random.NextDouble();
            result.LeftAntena.Root.Length.Value = Random.Next
            (
                (int)result.LeftAntena.Root.Length.Min,
                (int)result.LeftAntena.Root.Length.Max
            );

            result.LeftAntena.Root.Thickness.Value = Random.Next
            (
                (int)result.LeftAntena.Root.Thickness.Min,
                (int)result.LeftAntena.Root.Thickness.Max
            );

            //ANTENA - RIGHT - TIP
            result.LeftAntena.Tip.Angle.Index = Random.NextDouble();
            result.LeftAntena.Tip.Length.Value = Random.Next
            (
                (int)result.LeftAntena.Tip.Length.Min,
                (int)result.LeftAntena.Tip.Length.Max
            );

            result.LeftAntena.Tip.Thickness.Value = Random.Next
            (
                (int)result.LeftAntena.Tip.Thickness.Min,
                (int)result.LeftAntena.Root.Thickness
            );

            return result;
        }
    }
}