using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

/// <summary>
/// This class calculates the transformation distance based on the first equation from article "On evaluating consensus in RANSAC surface registration" - applies transformations to all the vertices and calculates the difference
/// These differences are squared, summed and finally returned as a result
/// This class is just for testing purposes and should work as a refference method for checking results
/// </summary>
class TransformationDistanceFirst : ITransformationDistance
{

    private AData microData;
    private long numberOfVertices;

    public TransformationDistanceFirst(AData microData)
    {
        this.microData = microData;
        this.numberOfVertices = (long)microData.Measures[0] * (long)microData.Measures[1] * (long)microData.Measures[2];
    }

    private Vector<double> GetVector(double x, double y, double z)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            x, y, z
        });
    }

    public double GetSqrtTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return Math.Sqrt(GetTransformationsDistance(transformation1, transformation2));
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

                    Vector<double> resultVector1 = rotationMatrix1.Multiply(originalVector);
                    resultVector1 += translationVector1;

                    Vector<double> resultVector2 = rotationMatrix2.Multiply(originalVector);
                    resultVector2 += translationVector2;

                    sum += Math.Pow((resultVector1 - resultVector2).L2Norm(), 2);
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

