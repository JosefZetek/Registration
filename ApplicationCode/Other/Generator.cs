
using System;
using MathNet.Numerics.LinearAlgebra;

namespace Registration.ApplicationCode.Other;

public class Generator
{
    private static Random random = new Random();

    public static Vector<double> GetTranslationVector(double x, double y, double z)
    {
        return Vector<double>.Build.DenseOfArray(new double[] { x, y, z });
    }

    public static Matrix<double> GetRotationMatrix(double angleX, double angleY, double angleZ)
    {
        Matrix<double> rotationX = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            {1, 0,  0 },
            {0,  Math.Cos(angleX), -Math.Sin(angleX) },
            { 0, Math.Sin(angleX), Math.Cos(angleX) }
        });

        Matrix<double> rotationY = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { Math.Cos(angleY), 0, Math.Sin(angleY) },
            { 0, 1, 0 },
            { -Math.Sin(angleY), 0, Math.Cos(angleY) }
        });

        Matrix<double> rotationZ = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            {Math.Cos(angleZ), -Math.Sin(angleZ), 0 },
            {Math.Sin(angleZ), Math.Cos(angleZ), 0 },
            { 0, 0, 1 }
        });

        return rotationX * rotationY * rotationZ;
    }

    public static Transform3D GetRandomTransformation()
    {
        return new Transform3D(
            GetRotationMatrix(
                random.NextDouble() * Math.PI * 2,
                random.NextDouble() * Math.PI * 2,
                random.NextDouble() * Math.PI * 2
            ),
            GetTranslationVector(
                random.NextDouble() * 40,
                random.NextDouble() * 40,
                random.NextDouble() * 40
            )
        );
    }
}

