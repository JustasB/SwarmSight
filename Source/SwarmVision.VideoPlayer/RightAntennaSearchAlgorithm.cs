using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using AForge.Neuro;
using SwarmVision.Models;
using Point = System.Windows.Point;

namespace SwarmVision.VideoPlayer
{
    public class RightAntennaSearchAlgorithm : AntennaSearchAlgorithm
    {
        protected override HeadModel CreateChild(HeadModel parent1, HeadModel parent2)
        {
            var result = new HeadModel();

            result.RightAntena.Root.Start.X = Cross
            (
                parent1.RightAntena.Root.Start.X,
                parent2.RightAntena.Root.Start.X,
                parent1.RightAntena.Root.StartXMin,
                parent1.RightAntena.Root.StartXMax
            );

            result.RightAntena.Root.Start.Y = Cross
            (
                parent1.RightAntena.Root.Start.Y,
                parent2.RightAntena.Root.Start.Y,
                parent1.RightAntena.Root.StartYMin,
                parent1.RightAntena.Root.StartYMax
            );

            //ANTENA - RIGHT - ROOT
            result.RightAntena.Root.AngleIndex = Cross
                (
                    parent1.RightAntena.Root.AngleIndex,
                    parent2.RightAntena.Root.AngleIndex,
                    0,
                    1
                );

            result.RightAntena.Root.Length = (int)Cross
                                                      (
                                                          parent1.RightAntena.Root.Length,
                                                          parent2.RightAntena.Root.Length,
                                                          parent1.RightAntena.Root.LengthMin,
                                                          parent1.RightAntena.Root.LengthMax
                                                      );

            result.RightAntena.Root.Thickness = (int)Cross
                                                      (
                                                          parent1.RightAntena.Root.Thickness,
                                                          parent2.RightAntena.Root.Thickness,
                                                          parent1.RightAntena.Root.ThicknessMin,
                                                          parent1.RightAntena.Root.ThicknessMax
                                                      );

            //ANTENA - RIGHT - TIP
            result.RightAntena.Tip.AngleIndex = Cross
                (
                    parent1.RightAntena.Tip.AngleIndex,
                    parent2.RightAntena.Tip.AngleIndex,
                    0,
                    1
                );

            result.RightAntena.Tip.Length = (int)Cross
                                                     (
                                                         parent1.RightAntena.Tip.Length,
                                                         parent2.RightAntena.Tip.Length,
                                                         parent1.RightAntena.Tip.LengthMin,
                                                         parent1.RightAntena.Tip.LengthMax
                                                     );

            result.RightAntena.Tip.Thickness = (int)Cross
                                                      (
                                                          parent1.RightAntena.Tip.Thickness,
                                                          parent2.RightAntena.Tip.Thickness,
                                                          parent1.RightAntena.Tip.ThicknessMin,
                                                          result.RightAntena.Root.Thickness
                                                      );

            return result;
        }

        protected override HeadModel CreateNewRandomMember()
        {
            //HEAD
            var result = new HeadModel();

            result.RightAntena.Root.Start.X = Random.Next
            (
                (int)result.RightAntena.Root.StartXMin,
                (int)result.RightAntena.Root.StartXMax
            );

            result.RightAntena.Root.Start.Y = Random.Next
            (
                (int)result.RightAntena.Root.StartYMin,
                (int)result.RightAntena.Root.StartYMax
            );

            //ANTENA - RIGHT - ROOT
            result.RightAntena.Root.AngleIndex = Random.NextDouble();
            result.RightAntena.Root.Length = Random.Next
            (
                (int)result.RightAntena.Root.LengthMin,
                (int)result.RightAntena.Root.LengthMax
            );

            result.RightAntena.Root.Thickness = Random.Next
            (
                (int)result.RightAntena.Root.ThicknessMin,
                (int)result.RightAntena.Root.ThicknessMax
            );

            //ANTENA - RIGHT - TIP
            result.RightAntena.Tip.AngleIndex = Random.NextDouble();
            result.RightAntena.Tip.Length = Random.Next
            (
                (int)result.RightAntena.Tip.LengthMin,
                (int)result.RightAntena.Tip.LengthMax
            );

            result.RightAntena.Tip.Thickness = Random.Next
            (
                (int)result.RightAntena.Tip.ThicknessMin,
                (int)result.RightAntena.Root.Thickness
            );

            return result;
        }
    }
}