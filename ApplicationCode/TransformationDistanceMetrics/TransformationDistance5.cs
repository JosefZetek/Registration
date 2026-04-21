using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

public class TransformationDistanceFive : ITransformationDistance
{
    private AData microData;

    private Vector<double> centeringTranslation;

    private double innerProductSum;
    private Matrix<double> outerProductSum;
    private Vector<double> vertexSum;
    private long numberOfVertices;


    public TransformationDistanceFive(AData microData)
    {
        this.microData = microData;

        this.centeringTranslation = GetCenteringTranslation(microData);

        this.innerProductSum = InnerProductSum();
        this.outerProductSum = OuterProductSum();
        this.vertexSum = SumVertices();
        this.numberOfVertices = (long)microData.Measures[0] * (long)microData.Measures[1] * (long)microData.Measures[2];
    }

    private Vector<double> GetCenteringTranslation(AData microData)
    {
        return GetVector(
            -microData.MaxValueX / 2.0,
            -microData.MaxValueY / 2.0,
            -microData.MaxValueZ / 2.0
        );
    }


    private Vector<double> GetVector(double x, double y, double z)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            x,y,z
        });
    }

    private double InnerProductSum()
    {
        double sum = 0;

        for (int xIndex = 0; xIndex < microData.Measures[0]; xIndex++)
        {
            for (int yIndex = 0; yIndex < microData.Measures[1]; yIndex++)
            {
                for (double zIndex = 0; zIndex < microData.Measures[2]; zIndex++)
                {
                    Vector<double> currentVertex = GetVector(xIndex * microData.XSpacing, yIndex * microData.YSpacing, zIndex * microData.ZSpacing);
                    currentVertex += centeringTranslation;
                    sum += currentVertex.DotProduct(currentVertex);
                }
            }
        }

        return sum;
    }

    private Matrix<double> OuterProductSum()
    {
        Matrix<double> sum = Matrix<double>.Build.Dense(3, 3);

        for (int xIndex = 0; xIndex < microData.Measures[0]; xIndex++)
        {
            for (int yIndex = 0; yIndex < microData.Measures[1]; yIndex++)
            {
                for (double zIndex = 0; zIndex < microData.Measures[2]; zIndex++)
                {
                    Vector<double> currentVertex = GetVector(xIndex * microData.XSpacing, yIndex * microData.YSpacing, zIndex * microData.ZSpacing);
                    currentVertex += centeringTranslation;
                    sum += currentVertex.OuterProduct(currentVertex);
                }
            }
        }

        return sum;
    }

    private Vector<double> SumVertices()
    {
        Vector<double> sum = Vector<double>.Build.Dense(3);

        for (int xIndex = 0; xIndex < microData.Measures[0]; xIndex++)
        {
            for (int yIndex = 0; yIndex < microData.Measures[1]; yIndex++)
            {
                for (double zIndex = 0; zIndex < microData.Measures[2]; zIndex++)
                {
                    Vector<double> currentVertex = GetVector(xIndex * microData.XSpacing, yIndex * microData.YSpacing, zIndex * microData.ZSpacing);
                    currentVertex += centeringTranslation;
                    sum += currentVertex;
                }
            }
        }

        Console.WriteLine("Vertex sum: " + sum);

        return sum;
    }

    public double GetTransformationsDistance(Transform3D transformation1, Transform3D transformation2)
    {
        TransformationChaining transformationChaining = new TransformationChaining();

        transformation1 = transformationChaining
            .ChainRotationMatrix(transformation1.RotationMatrix)
            .ChainTranslationVector(transformation1.TranslationVector)
            .ChainTranslationVector(-centeringTranslation)
            .Build();

        transformationChaining.Clear();

        transformation2 = transformationChaining
            .ChainRotationMatrix(transformation2.RotationMatrix)
            .ChainTranslationVector(transformation2.TranslationVector)
            .ChainTranslationVector(-centeringTranslation)
            .Build();


        double blackSum1;
        double blackSum23;
        double blackSum4;

        double redSum = 2 * innerProductSum;

        double greenSum = 2 * (transformation1.TranslationVector - transformation2.TranslationVector).DotProduct(transformation1.RotationMatrix.Multiply(vertexSum));
        double blueSum = 2 * (transformation2.TranslationVector - transformation1.TranslationVector).DotProduct(transformation2.RotationMatrix.Multiply(vertexSum));

        Console.WriteLine("Green sum: " + greenSum);
        Console.WriteLine("Green sum: " + blueSum);

        blackSum1 = numberOfVertices * transformation1.TranslationVector.DotProduct(transformation1.TranslationVector);
        blackSum23 = (-2 * numberOfVertices * transformation1.TranslationVector.DotProduct(transformation2.TranslationVector));
        blackSum4 = numberOfVertices * transformation2.TranslationVector.DotProduct(transformation2.TranslationVector);

        double purpleSum = FrobeniusMatrixProduct(-2 * transformation1.RotationMatrix.Transpose() * transformation2.RotationMatrix, outerProductSum);

        double blackSum = blackSum1 + blackSum23 + blackSum4;
        return redSum + blackSum + purpleSum;
    }

    private double FrobeniusMatrixProduct(Matrix<double> matrix1, Matrix<double> matrix2)
    {
        if (matrix1.ColumnCount != matrix2.ColumnCount)
            throw new ArgumentException("Matrices need to have the same number of columns");
        if (matrix1.RowCount != matrix2.RowCount)
            throw new ArgumentException("Matrices need to have the same number of rows");


        double sum = 0;
        for (int i = 0; i < matrix1.RowCount; i++)
        {
            for (int j = 0; j < matrix1.ColumnCount; j++)
            {
                sum += (matrix1[i, j] * matrix2[i, j]);
            }
        }

        return sum;
    }

    public double GetSqrtTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return Math.Sqrt(GetTransformationsDistance(transformation1, transformation2));
    }

    public double GetRelativeTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return GetTransformationsDistance(transformation1, transformation2) / numberOfVertices;
    }
}