using System;
using System.Collections.Generic;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Samplers;

/// <summary>
/// Sampler dividing the volume into a uniform grid of cells. Within each cell only the
/// candidate point with the highest local variance survives, and of these cell winners
/// only the best keepFraction is returned. Every returned point therefore covers a
/// distinct part of the volume - salient regions are not sampled repeatedly, which is
/// the property Lowe's ratio test in the matcher relies on.
/// </summary>
public class SamplerSegmented : ISampler
{
    /* Cells whose best local standard deviation is below this fraction of the data's
       value range are background (only noise) and produce no sample at all. */
    private const double MIN_RELATIVE_STD = 0.01;

    private Random r;
    private double keepFraction;
    private int candidatesPerCell;

    public SamplerSegmented(int seed, double keepFraction, int candidatesPerCell = 5)
    {
        this.r = new Random(seed);
        this.keepFraction = Constrain(keepFraction, double.Epsilon, 1);
        this.candidatesPerCell = Math.Max(1, candidatesPerCell);
    }

    public SamplerSegmented(double keepFraction, int candidatesPerCell = 5)
    {
        this.r = new Random();
        this.keepFraction = Constrain(keepFraction, double.Epsilon, 1);
        this.candidatesPerCell = Math.Max(1, candidatesPerCell);
    }

    public SamplerSegmented()
    {
        this.r = new Random();
        this.keepFraction = 0.1;
        this.candidatesPerCell = 5;
    }

    public Point3D[] Sample(AData d, int count)
    {
        /* Enough cells so that keepFraction of the cell winners still yields `count` points. */
        int totalCells = (int)Math.Ceiling(count / keepFraction);

        double volume = d.MaxValueX * d.MaxValueY * d.MaxValueZ;
        double cellSize = Math.Cbrt(volume / totalCells);

        int cellsX = Math.Max(1, (int)Math.Ceiling(d.MaxValueX / cellSize));
        int cellsY = Math.Max(1, (int)Math.Ceiling(d.MaxValueY / cellSize));
        int cellsZ = Math.Max(1, (int)Math.Ceiling(d.MaxValueZ / cellSize));

        double stepX = d.MaxValueX / cellsX;
        double stepY = d.MaxValueY / cellsY;
        double stepZ = d.MaxValueZ / cellsZ;

        double valueRange = d.MaxValue - d.MinValue;
        double minVariance = Math.Pow(MIN_RELATIVE_STD * valueRange, 2);

        List<SampledPoint> cellWinners = new List<SampledPoint>(cellsX * cellsY * cellsZ);

        for (int ix = 0; ix < cellsX; ix++)
        {
            for (int iy = 0; iy < cellsY; iy++)
            {
                for (int iz = 0; iz < cellsZ; iz++)
                {
                    Point3D bestPoint = default;
                    double bestVariance = double.NegativeInfinity;

                    for (int c = 0; c < candidatesPerCell; c++)
                    {
                        Point3D candidate = new Point3D(
                            (ix + r.NextDouble()) * stepX,
                            (iy + r.NextDouble()) * stepY,
                            (iz + r.NextDouble()) * stepZ
                        );

                        double variance = CalculateVariance(d, candidate);

                        if (variance > bestVariance)
                        {
                            bestVariance = variance;
                            bestPoint = candidate;
                        }
                    }

                    if (bestVariance >= minVariance)
                        cellWinners.Add(new SampledPoint(bestPoint, bestVariance));
                }
            }
        }

        cellWinners.Sort();

        int resultCount = Math.Min(count, cellWinners.Count);
        Point3D[] points = new Point3D[resultCount];

        for (int i = 0; i < resultCount; i++)
            points[i] = cellWinners[cellWinners.Count - 1 - i].sampledPoint;

        return points;
    }

    private double CalculateVariance(AData d, Point3D point)
    {
        List<double> values = new List<double>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    double neighborX = Constrain(point.X + x * d.XSpacing, 0, d.MaxValueX);
                    double neighborY = Constrain(point.Y + y * d.YSpacing, 0, d.MaxValueY);
                    double neighborZ = Constrain(point.Z + z * d.ZSpacing, 0, d.MaxValueZ);

                    values.Add(d.GetValue(new Point3D(neighborX, neighborY, neighborZ)));
                }
            }
        }

        return GetListVariance(values);
    }

    private double Constrain(double value, double minValue, double maxValue)
    {
        return Math.Min(Math.Max(value, minValue), maxValue);
    }

    private double GetListVariance(List<double> values)
    {
        double average = GetListAverage(values);
        double variance = 0;

        for (int i = 0; i < values.Count; i++)
            variance += Math.Pow(values[i] - average, 2) / values.Count;

        return variance;
    }

    private double GetListAverage(List<double> list)
    {
        double average = 0;

        for (int i = 0; i < list.Count; i++)
            average += list[i] / list.Count;

        return average;
    }

    private class SampledPoint : IComparable<SampledPoint>
    {
        public Point3D sampledPoint { get; }
        public double variance { get; }

        public SampledPoint(Point3D sampledPoint, double variance)
        {
            this.sampledPoint = sampledPoint;
            this.variance = variance;
        }

        public int CompareTo(SampledPoint other)
        {
            return this.variance.CompareTo(other.variance);
        }
    }
}
