using System;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Samplers;

/// <summary>
/// Sampler class generating random number of points
/// </summary>
public class Sampler : ISampler
{
    private Random r;

    public Sampler(int seed)
    {
        this.r = new Random(seed);
    }

    public Sampler()
    {
        this.r = new Random();
    }

    public Point3D[] Sample(AData d, int count)
    {
        Point3D[] points = new Point3D[count];
            

        for (int i = 0; i < count; i++)
        {

            double x = r.NextDouble() * d.MaxValueX;
            double y = r.NextDouble() * d.MaxValueY;
            double z = r.NextDouble() * d.MaxValueZ;

            points[i] = new Point3D(x, y, z);
        }
        return points;
    }
}
