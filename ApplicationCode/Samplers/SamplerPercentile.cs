using System;
using System.Collections.Generic;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Samplers;

/// <summary>
/// Sampler class generating random number of points
/// </summary>
public class SamplerPercentile : ISampler
{
    private Random r;
    private double percentile;
    private int sampledMultiplier = 10;

    public SamplerPercentile(int seed, double percentile)
    {
        this.r = new Random(seed);
        this.percentile = Constrain(percentile, 0, 1);
    }

    public SamplerPercentile(double percentile)
    {
        this.r = new Random();
        this.percentile = Constrain(percentile, 0, 1);
    }

    public SamplerPercentile()
    {
        this.r = new Random();
        this.percentile = Constrain(0.1, 0, 1);
    }

    public Point3D[] Sample(AData d, int count)
    {
        Point3D[] points = new Point3D[count];
        Point3D currentPoint;
        //int currentIndex = 0;

        SampledPoint[] sampledPoints = new SampledPoint[count * sampledMultiplier];

        for (int i = 0; i < count * sampledMultiplier; i++)
        {
            currentPoint = GetRandomPoint(d);
            sampledPoints[i] = new SampledPoint(currentPoint, CalculateVariance(d, currentPoint));
        }

        Array.Sort(sampledPoints);

        for (int i = 0; i < points.Length; i++)
            points[i] = sampledPoints[sampledPoints.Length - 1 - i].sampledPoint;

        /////* Sample 1000 points and obtain percentile */
        //double threshold = FindGivenPercentile(d, 1000);

        //while (currentIndex < count)
        //{
        //    currentPoint = GetRandomPoint(d);
        //    if (CalculateVariance(d, currentPoint) < threshold)
        //        continue;

        //    points[currentIndex++] = currentPoint;
        //}

        return points;
    }

    private double FindGivenPercentile(AData d, int sampleCount)
    {
        List<double> values = new List<double>();
        List<Point2D> hodnoty = new List<Point2D>();
        double hodnota = 0;
        Point3D point;
        //put variances of each randomly sampled points into array
        for (int i = 0; i < sampleCount; i++)
        {
            point = GetRandomPoint(d);
            hodnota = CalculateVariance(d, point);
            values.Add(hodnota);
            hodnoty.Add(new Point2D(d.GetValue(point), hodnota));
        }




        CSVWriter.WriteResult("/Users/pepazetek/soubor.csv", "Hodnota", "variance", hodnoty);

        QuickSelectClass qc = new QuickSelectClass();
        return qc.QuickSelect(values, (int)((1 - percentile) * values.Count));
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
                    //values.Add(0);
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
