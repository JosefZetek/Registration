using System;
using DataView;
using System.Collections.Generic;
using System.Diagnostics;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.MockObjects;

public class PointDistanceMock : AMockObject
{

	private const int DIMENSIONS = 3;

    private int[] measures;
    private double[] spacings;

    public override int[] Measures { get => measures; }

    public override double XSpacing { get => spacings[0]; }
    public override double YSpacing { get => spacings[1]; }
    public override double ZSpacing { get => spacings[2]; }

    private List<Point3D> randomPoints = new List<Point3D>();

    private double MinValue = double.MaxValue;
    private double MaxValue = double.MinValue;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="Measures"></param>
    /// <param name="Spacings"></param>
    public PointDistanceMock(int[] Measures, double[] Spacings)
	{
        this.measures = Measures;
        this.spacings = Spacings;

        GeneratePoints(Math.Max(FindArrayMin(measures)/2, 1));
        FindBoundaryDistances();
    }

    private void GeneratePoints(double minDimension)
    {
        System.Random random = new System.Random();
        Point3D generatedPoint;

        for (int i  = 0; i<minDimension; i++)
        {
            generatedPoint = new Point3D(
                random.NextDouble() * MaxValueX,
                random.NextDouble() * MaxValueY,
                random.NextDouble() * MaxValueZ
            );

            randomPoints.Add(generatedPoint);
        }
    }

    private void FindBoundaryDistances()
    {
        Point3D currentPoint;
        double currentValue;
        for (double x = 0; x <= MaxValueX; x += spacings[0])
        {
            for (double y = 0; y <= MaxValueY; y += spacings[1])
            {
                for (double z = 0; z <= MaxValueZ; z += spacings[1])
                {
                    currentPoint = new Point3D(x, y, z);

                    currentValue = CalculateValue(currentPoint);

                    this.MinValue = Math.Min(this.MinValue, currentValue);
                    this.MaxValue = Math.Max(this.MaxValue, currentValue);
                }
            }
        }
    }

    private double CalculateValueNormalized(Point3D point)
    {
        return (CalculateValue(point) - this.MinValue) / (this.MaxValue - this.MinValue);
    }

    private double CalculateValue(Point3D point)
    {
        return CalculateDistancesSum(point);
    }

    private double CalculateDistancesSum(Point3D currentPoint)
    {
        double sum = 0;

        foreach (Point3D point in randomPoints)
            sum += point.Distance(currentPoint);
        return sum;
    }

    private double CalculateDistancesSquared(Point3D currentPoint)
    {
        double sum = 0;

        foreach (Point3D point in randomPoints)
            sum += Math.Pow(point.Distance(currentPoint), 2);

        return sum;
    }

    private double ClosestPointDistance(Point3D currentPoint)
    {
        double closestDistance = double.MaxValue;

        foreach (Point3D point in randomPoints)
            closestDistance = Math.Min(currentPoint.Distance(point), closestDistance);

        return closestDistance;
    }

    private int FindArrayMin(int[] array)
    {
        int minValue = int.MaxValue;

        foreach (int value in array)
        {
            minValue = Math.Min(value, minValue);
        }

        return minValue;
    }

    /// <summary>
    /// Checks whether all values in array are more than 0. Made for check of measures and sizing properties.
    /// </summary>
    /// <param name="CheckedArray">Array thats going to be checked.</param>
    /// <returns>Returns false if at least one value is 0 or bellow.</returns>
    private bool CheckArrayPositiveValues(int[] CheckedArray)
    {
        for (int i = 0; i<CheckedArray.Length; i++)
        {
            if (CheckedArray[i] <= 0)
                return false;
        }

        return true;
    }

    public override double GetValue(double x, double y, double z)
    {
        return GetValue(new Point3D(x, y, z));
    }

    public override double GetValue(Point3D p)
    {
        return CalculateValueNormalized(p);
    }
}

