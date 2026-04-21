using System;
using System.Collections.Generic;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
namespace Registration.ApplicationCode.Samplers;

/// <summary>
/// Sampler class generating random number of points
/// </summary>
public class SamplerVariance : ISampler
{
    private Random r;
    private double minVariance;

    public SamplerVariance(int seed, double minVariance)
    {
        this.r = new Random(seed);
        this.minVariance = minVariance;
    }

    public SamplerVariance(double minVariance)
    {
        this.minVariance = minVariance;
    }

    public SamplerVariance()
    {
        this.r = new Random();
        this.minVariance = 0.2;
    }

    public Point3D[] Sample(AData d, int count)
    {
        Point3D[] points = new Point3D[count];
        Point3D currentPoint;
        int currentIndex = 0;

        while(currentIndex < count)
        {
            currentPoint = GetRandomPoint(d);
            if (CalculateVariance(d, currentPoint) < minVariance)
                continue;

            points[currentIndex++] = currentPoint;
        }

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

    private Point3D GetRandomPoint(AData d)
    {
        return new Point3D(
            r.NextDouble() * d.MaxValueX,
            r.NextDouble() * d.MaxValueY,
            r.NextDouble() * d.MaxValueZ
        );
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

        for(int i = 0; i<list.Count; i++)
            average += list[i] / list.Count;

        return average;
    }


}
