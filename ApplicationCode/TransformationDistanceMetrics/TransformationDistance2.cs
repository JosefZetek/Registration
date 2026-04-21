using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

/// <summary>
/// This class calculates the transformation distance based on the second equation from article "On evaluating consensus in RANSAC surface registration"
/// Equation: d(T1, T2) = Î£(from i = 1 to vq) (R1 * qi + t1 - R2 * qi - t2)^T * (R1 * qi + t1 - R2 * qi - t2)
/// These differences are squared, summed and finally returned as a result
/// This class is just for testing purposes and should work as a refference method for checking results
/// Outputs the same results as the first method - as expected
/// </summary>
class TransformationDistanceSecond : ITransformationDistance
{
    private AData microData;
    private long numberOfVertices;


    public TransformationDistanceSecond(AData microData)
    {
        this.microData = microData;
        this.numberOfVertices = (long)microData.Measures[0] * (long)microData.Measures[1] * (long)microData.Measures[2];
    }

    public double GetSqrtTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return Math.Sqrt(GetSqrtTransformationDistance(transformation1, transformation2));
    }

    private Vector<double> GetVector(double x, double y, double z)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            x,y,z
        });
    }

    public double GetTransformationsDistance(Transform3D transformation1, Transform3D transformation2)
    {
        Matrix<double> rotationMatrix1 = transformation1.RotationMatrix;
        Vector<double> translationVector1 = transformation1.TranslationVector;

        Matrix<double> rotationMatrix2 = transformation2.RotationMatrix;
        Vector<double> translationVector2 = transformation2.TranslationVector;

        double sum = 0;

        for (int xIndex = 0; xIndex < microData.Measures[0]; xIndex++)
        {
            for (int yIndex = 0; yIndex < microData.Measures[1]; yIndex++)
            {
                for (double zIndex = 0; zIndex < microData.Measures[2]; zIndex++)
                {
                    Vector<double> originalVector = GetVector(xIndex * microData.XSpacing, yIndex * microData.YSpacing, zIndex * microData.ZSpacing);

                    Matrix<double> calculation = (rotationMatrix1.Multiply(originalVector) + translationVector1 - rotationMatrix2.Multiply(originalVector) - translationVector2).ToColumnMatrix();
                    Matrix<double> result = calculation.Transpose() * calculation;

                    sum += result[0, 0];
                }
            }
        }
        return sum;
    }

    public double GetRelativeTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return GetTransformationsDistance(transformation1, transformation2) / numberOfVertices;
    }
}

