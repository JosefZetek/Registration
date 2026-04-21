using System;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.MockObjects;

public class SpheresMockData : AMockObject
{
    private const int SPHERES = 8;

    double[] radiusArray;
    double[] positions;

    public SpheresMockData()
    {
        radiusArray = new double[SPHERES];
        positions = new double[SPHERES];

        double radius = 20;
        double spacing = 10;
        double difference = 20;

        for (int i = 0; i < SPHERES; i++)
        {
            double currentSpacing = spacing * (i + 1);
            double currentRadius = radius + i * difference;
            double diametersSum = ((i + 1) / 2.0) * (i * difference * 2);

            positions[i] = currentSpacing + currentRadius + diametersSum;
            radiusArray[i] = currentRadius;

            Console.WriteLine($"Posiiton {i}: {positions[i]}");
            Console.WriteLine($"Radius {i}: {radiusArray[i]}");
        }
    }

    private double Circle(Point3D point, Point3D centerPoint, double closeRadius, double farRadius)
    {
        if (Math.Abs(point.X - centerPoint.X) > farRadius ||
            Math.Abs(point.Y - centerPoint.Y) > farRadius ||
            Math.Abs(point.Z - centerPoint.Z) > farRadius)
            return 0;

        double distance = point.Distance(centerPoint);

        if (distance <= closeRadius)
            return 1;

        else if (distance <= farRadius)
            return 1 - ((distance - closeRadius) / (farRadius - closeRadius));

        return 0;
    }

    public int GetSphereIndex(double x, double y, double z)
    {
        for (int i = 0; i < SPHERES; i++)
        {
            if ((positions[i] + radiusArray[i]) < x)
                continue;

            if ((positions[i] - radiusArray[i]) > x)
                continue;

            if (y < 10 || y > (170 + radiusArray[i]))
                continue;

            if (z < 10 || z > (170 + radiusArray[i]))
                continue;

            return i;
        }

        return SPHERES;
    }

    public override double GetValue(double x, double y, double z)
    {
        return GetValue(new Point3D(x, y, z));
    }

    public override double GetValue(Point3D p)
    {
        int spheres = 8;
        double result;

        for (int i = 0; i < spheres; i++)
        {
            result = Circle(p, new Point3D(positions[i], 170, 170), radiusArray[i] - 10, radiusArray[i]);
            if (result != 0)
                return result * 1000;
        }

        return 0;
    }

    public override double XSpacing => 1;

    public override double YSpacing => 1;

    public override double ZSpacing => 1;

    public override int[] Measures => new int[] { (int)(1530 * 1.0 / XSpacing), (int)(340 * 1.0 / YSpacing), (int)(340 * 1.0 / ZSpacing) };
}

