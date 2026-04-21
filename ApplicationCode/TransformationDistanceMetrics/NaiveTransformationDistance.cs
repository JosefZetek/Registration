using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

/// <summary>
/// Class calculating distance between two given transformations as squared differece between each vertex after applying transformation on it
/// Highly ineficient, use for debug purposes only
/// </summary>
public class NaiveTransformationDistance : ITransformationDistance
{

    private AData data;
    private double diag;
    private long numberOfVertices;

    /// <summary>
    /// Constructor takes in data object and saves it
    /// </summary>
    /// <param name="data">Instance of IData for object on which the transformations are going to be applied</param>
    public NaiveTransformationDistance(AData data)
    {
        this.data = data;
        this.diag = Math.Sqrt(data.MaxValueX * data.MaxValueX + data.MaxValueY * data.MaxValueY + data.MaxValueZ * data.MaxValueZ);
        this.numberOfVertices = (long)data.Measures[0] * (long)data.Measures[1] * (long)data.Measures[2];
    }

    public double GetRelativeTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return Math.Sqrt(GetTransformationsDistance(transformation1, transformation2) / numberOfVertices) / diag;
    }

    public double GetTransformationsDistance(Transform3D transformation1, Transform3D transformation2)
    {
        double distanceSquared = 0;

        Vector<double> currentVector;
        Vector<double> firstTransformationResult;
        Vector<double> secondTransformationResult;

        for (int xIndex = 0; xIndex < data.Measures[0]; xIndex++)
        {
            for (int yIndex = 0; yIndex < data.Measures[1]; yIndex++)
            {
                for (int zIndex = 0; zIndex < data.Measures[2]; zIndex++)
                {
                    currentVector = GetVector(xIndex * data.XSpacing, yIndex * data.YSpacing, zIndex * data.ZSpacing);

                    firstTransformationResult = transformation1.RotationMatrix.Multiply(currentVector);
                    firstTransformationResult += transformation1.TranslationVector;

                    secondTransformationResult = transformation2.RotationMatrix.Multiply(currentVector);
                    secondTransformationResult += transformation2.TranslationVector;
                    
                    distanceSquared += Math.Pow((firstTransformationResult - secondTransformationResult).L2Norm(), 2);
                }
            }
        }
        return distanceSquared;
    }

    private Vector<double> GetVector(double x, double y, double z)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            x, y, z
        });
    }
}

