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
    public class LeftAntennaSearchAlgorithm : AntennaSearchAlgorithm
    {
        protected override HeadModel CreateChild(HeadModel parent1, HeadModel parent2)
        {
            var result = new HeadModel();

            result.LeftAntena.Root.Start.X = Cross
            (
                parent1.LeftAntena.Root.Start.X,
                parent2.LeftAntena.Root.Start.X,
                parent1.LeftAntena.Root.StartXMin,
                parent1.LeftAntena.Root.StartXMax
            );

            result.LeftAntena.Root.Start.Y = Cross
            (
                parent1.LeftAntena.Root.Start.Y,
                parent2.LeftAntena.Root.Start.Y,
                parent1.LeftAntena.Root.StartYMin,
                parent1.LeftAntena.Root.StartYMax
            );

            //ANTENA - RIGHT - ROOT
            result.LeftAntena.Root.AngleIndex = Cross
                (
                    parent1.LeftAntena.Root.AngleIndex,
                    parent2.LeftAntena.Root.AngleIndex,
                    0,
                    1
                );

            result.LeftAntena.Root.Length = (int)Cross
                                                      (
                                                          parent1.LeftAntena.Root.Length,
                                                          parent2.LeftAntena.Root.Length,
                                                          parent1.LeftAntena.Root.LengthMin,
                                                          parent1.LeftAntena.Root.LengthMax
                                                      );

            result.LeftAntena.Root.Thickness = (int)Cross
                                                      (
                                                          parent1.LeftAntena.Root.Thickness,
                                                          parent2.LeftAntena.Root.Thickness,
                                                          parent1.LeftAntena.Root.ThicknessMin,
                                                          parent1.LeftAntena.Root.ThicknessMax
                                                      );

            //ANTENA - RIGHT - TIP
            result.LeftAntena.Tip.AngleIndex = Cross
                (
                    parent1.LeftAntena.Tip.AngleIndex,
                    parent2.LeftAntena.Tip.AngleIndex,
                    0,
                    1
                );

            result.LeftAntena.Tip.Length = (int)Cross
                                                     (
                                                         parent1.LeftAntena.Tip.Length,
                                                         parent2.LeftAntena.Tip.Length,
                                                         parent1.LeftAntena.Tip.LengthMin,
                                                         parent1.LeftAntena.Tip.LengthMax
                                                     );

            result.LeftAntena.Tip.Thickness = (int)Cross
                                                      (
                                                          parent1.LeftAntena.Tip.Thickness,
                                                          parent2.LeftAntena.Tip.Thickness,
                                                          parent1.LeftAntena.Tip.ThicknessMin,
                                                          result.LeftAntena.Root.Thickness
                                                      );

            return result;
        }

        protected override HeadModel CreateNewRandomMember()
        {
            //HEAD
            var result = new HeadModel();

            result.LeftAntena.Root.Start.X = Random.Next
            (
                (int)result.LeftAntena.Root.StartXMin,
                (int)result.LeftAntena.Root.StartXMax
            );

            result.LeftAntena.Root.Start.Y = Random.Next
            (
                (int)result.LeftAntena.Root.StartYMin,
                (int)result.LeftAntena.Root.StartYMax
            );

            //ANTENA - RIGHT - ROOT
            result.LeftAntena.Root.AngleIndex = Random.NextDouble();
            result.LeftAntena.Root.Length = Random.Next
            (
                (int)result.LeftAntena.Root.LengthMin,
                (int)result.LeftAntena.Root.LengthMax
            );

            result.LeftAntena.Root.Thickness = Random.Next
            (
                (int)result.LeftAntena.Root.ThicknessMin,
                (int)result.LeftAntena.Root.ThicknessMax
            );

            //ANTENA - RIGHT - TIP
            result.LeftAntena.Tip.AngleIndex = Random.NextDouble();
            result.LeftAntena.Tip.Length = Random.Next
            (
                (int)result.LeftAntena.Tip.LengthMin,
                (int)result.LeftAntena.Tip.LengthMax
            );

            result.LeftAntena.Tip.Thickness = Random.Next
            (
                (int)result.LeftAntena.Tip.ThicknessMin,
                (int)result.LeftAntena.Root.Thickness
            );

            return result;
        }
    }
}