using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.Other;


namespace Registration.ApplicationCode.Test;

public class PCATester
{
    public Vector<double> v1;
    public Vector<double> v2;
    public Vector<double> v3;

    Point3D p;
    public Point3D[] Qs;
    Random rnd;


    public PCATester(int count)
    {
        rnd = new Random();
        GenerateRandomVectors(3);
        OrthogonalizeVectors();
        NormalizeVectors();
        GenerateRandomPoint(500);
        GenerateQPoints(count);
    }

    private void GenerateQPoints(int count)
    {
        Qs = new Point3D[count];

        double a, b, c;
        double x, y, z;

        for(int i = 0; i < count; i++)
        {
            a = rnd.NextDouble() * 1000 - 500;
            b = rnd.NextDouble() * 100 - 50;
            c = rnd.NextDouble() * 10 - 5;

            x = p.X + a * v1[0] + b * v2[0] + c * v3[0];
            y = p.Y + a * v1[1] + b * v2[1] + c * v3[1];
            z = p.Z + a * v1[2] + b * v2[2] + c * v3[2];

            Qs[i] = new Point3D(x, y, z);
        }
    }

    /// <summary>
    /// Generates a random point with coordinates between 0 and cap
    /// </summary>
    /// <param name="cap"></param>
    private void GenerateRandomPoint(double cap)
    {
        p = new Point3D(rnd.NextDouble() * cap, rnd.NextDouble() * cap, rnd.NextDouble() * cap);
    }

    private void NormalizeVectors()
    {
        v1 = v1 / Math.Sqrt(ScalarProduct(v1, v1));
        v2 = v2 / Math.Sqrt(ScalarProduct(v2, v2));
        v3 = v3 / Math.Sqrt(ScalarProduct(v3, v3));
    }

    public void GenerateRandomVectors(int size)
    {
        v1 = Vector<double>.Build.Random(size);
        v2 = Vector<double>.Build.Random(size);
        v3 = Vector<double>.Build.Random(size);
    }

    private void OrthogonalizeVectors()
    {
        //v1 = v1;

        v2 = v2.Subtract(Proj(v1, v2));

        v3 = v3.Subtract(Proj(v1, v3).Add(Proj(v2, v3)));
    }

    public static double ScalarProduct(Vector<double> v1, Vector<double> v2)
    {
        return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
    }

    /// <summary>
    /// Proj u (v) = (v,u)/(u,u) * u
    /// </summary>
    /// <param name="u"></param>
    /// <param name="v"></param>
    /// <returns></returns>
    private Vector<double> Proj(Vector<double> u, Vector<double> v)
    {
        return u.Multiply(ScalarProduct(v, u) / ScalarProduct(u, u));
    }

}