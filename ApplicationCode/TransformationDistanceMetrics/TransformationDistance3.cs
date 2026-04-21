using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

public class TransformationDistanceThree : ITransformationDistance
{
    private AData microData;
    private long numberOfVertices;

	public TransformationDistanceThree(AData microData)
	{
        this.microData = microData;
        this.numberOfVertices = (long)microData.Measures[0] * (long)microData.Measures[1] * (long)microData.Measures[2];
    }

    public double GetSqrtTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return Math.Sqrt(GetTransformationsDistance(transformation1, transformation2));
    }

    public Vector<double> GetVector(double x, double y, double z)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            x, y, z
        });
    }

    public double GetTransformationsDistance(Transform3D transformation1, Transform3D transformation2)
    {

        double redPart = 0;
        double greenPart = 0;
        double bluePart = 0;
        double purplePart = 0;
        double blackPart = 0;

        double blackPart1 = 0;
        double blackPart23 = 0;
        double blackPart4 = 0;

        for (int xIndex = 0; xIndex < microData.Measures[0]; xIndex++)
        {
            for (int yIndex = 0; yIndex < microData.Measures[1]; yIndex++)
            {
                for (double zIndex = 0; zIndex < microData.Measures[2]; zIndex++)
                {
                    Vector<double> currentVertex = GetVector(xIndex * microData.XSpacing, yIndex * microData.YSpacing, zIndex * microData.ZSpacing);

                    redPart += (currentVertex.ToRowMatrix() * transformation1.RotationMatrix.Transpose() * transformation1.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];
                    redPart += (currentVertex.ToRowMatrix() * transformation2.RotationMatrix.Transpose() * transformation2.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];

                    greenPart += (currentVertex.ToRowMatrix() * transformation1.RotationMatrix.Transpose() * transformation1.TranslationVector.ToColumnMatrix())[0, 0];
                    greenPart -= (currentVertex.ToRowMatrix() * transformation1.RotationMatrix.Transpose() * transformation2.TranslationVector.ToColumnMatrix())[0, 0];
                    greenPart += (transformation1.TranslationVector.ToRowMatrix() * transformation1.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];
                    greenPart -= (transformation2.TranslationVector.ToRowMatrix() * transformation1.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];

                    bluePart -= (transformation1.TranslationVector.ToRowMatrix() * transformation2.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];
                    bluePart -= (currentVertex.ToRowMatrix() * transformation2.RotationMatrix.Transpose() * transformation1.TranslationVector.ToColumnMatrix())[0, 0];
                    bluePart += (currentVertex.ToRowMatrix() * transformation2.RotationMatrix.Transpose() * transformation2.TranslationVector.ToColumnMatrix())[0, 0];
                    bluePart += (transformation2.TranslationVector.ToRowMatrix() * transformation2.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];

                    purplePart -= (currentVertex.ToRowMatrix() * transformation1.RotationMatrix.Transpose() * transformation2.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];
                    purplePart -= (currentVertex.ToRowMatrix() * transformation2.RotationMatrix.Transpose() * transformation1.RotationMatrix * currentVertex.ToColumnMatrix())[0, 0];

                    blackPart1 += transformation1.TranslationVector.DotProduct(transformation1.TranslationVector);
                    blackPart23 -= transformation1.TranslationVector.DotProduct(transformation2.TranslationVector);
                    blackPart23 -= transformation2.TranslationVector.DotProduct(transformation1.TranslationVector);
                    blackPart4 += transformation2.TranslationVector.DotProduct(transformation2.TranslationVector);
                }
            }
        }

        blackPart = blackPart1 + blackPart23 + blackPart4;

        return redPart + greenPart + bluePart + purplePart + blackPart;
    }

    public double GetRelativeTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return GetTransformationsDistance(transformation1, transformation2) / numberOfVertices;
    }
}

