using System;
using MathNet.Numerics.LinearAlgebra;

namespace Registration.ApplicationCode.Other;

public class TransformationConstructor
{
    public static Matrix<double> GenerateRotationMatrix(double angleX, double angleY, double angleZ)
    {
        Matrix<double> rotationMatrixX = Matrix<double>.Build.DenseOfArray(new double[,]
        {
                { 1, 0, 0 },
                { 0, Math.Cos(angleX), -Math.Sin(angleX) },
                { 0, Math.Sin(angleX), Math.Cos(angleX) }
        });

        Matrix<double> rotationMatrixY = Matrix<double>.Build.DenseOfArray(new double[,]
        {
                {Math.Cos(angleY), 0, Math.Sin(angleY) },
                { 0, 1, 0 },
                { -Math.Sin(angleY), 0, Math.Cos(angleY) }
        });

        Matrix<double> rotationMatrixZ = Matrix<double>.Build.DenseOfArray(new double[,]
        {
                { Math.Cos(angleZ), -Math.Sin(angleZ), 0 },
                { Math.Sin(angleZ), Math.Cos(angleZ), 0 },
                { 0, 0, 1 }
        });

        return rotationMatrixX * rotationMatrixY * rotationMatrixZ;
    }

    public static Vector<double> GenerateTranslationVector(double x, double y, double z)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            x, y, z
        });
    }
}

