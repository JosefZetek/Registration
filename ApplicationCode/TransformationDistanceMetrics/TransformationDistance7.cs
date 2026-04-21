using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

/// <summary>
/// This class calculates the transformation distance based on the equation marked with number five from article "On evaluating consensus in RANSAC surface registration"
/// Equation: d(T1, T2) = 2*Î£(from i = 1 to vq) (qi^T * qi) + 2*(t1 - t2)^T * R1 * Î£(from i = 1 to vq) qi + 2*(t2 - t1)^T * R2 * Î£(from i = 1 to vq) qi + vq*t1^T*t1 - 2*vq*t1^T*t2 + vq*t2^T*t2 - 2*R1^T*R2 : Î£(from i = 1 to vq)qi*qi^T
/// where ":" denotes frobenius matrix product

/// This class is supposed to output the same result as the first two methods, but it doesn't
/// 2*Î£(from i = 1 to vq) (qi^T * qi) - red part
/// vq*t1^T*t1 - 2*vq*t1^T*t2 + vq*t2^T*t2 - black part
/// diag(- 2*R1^T*R2) . diag(Î£(from i = 1 to vq)qi*qi^T), where . denotes Dot Product
/// </summary>
public class TransformationDistanceSeven : ITransformationDistance
{
    private long numberOfVertices;
    private double diag;

    private double innerProductSum;

    private Vector<double> outerProductSum;

    private Vector<double> centerPoint;

    /// <summary>
    /// Constructor initializes precomputed values for the given data
    /// </summary>
    /// <param name="microData">Instance of IData for Micro Data</param>
    public TransformationDistanceSeven(AData microData)
    {
        double xSquared = RowSumOfCoordinatesSquares(microData.Measures[0], -microData.MaxValueX / 2, microData.XSpacing) * microData.Measures[1] * microData.Measures[2];
        double ySquared = RowSumOfCoordinatesSquares(microData.Measures[1], -microData.MaxValueY / 2, microData.YSpacing) * microData.Measures[0] * microData.Measures[2];
        double zSquared = RowSumOfCoordinatesSquares(microData.Measures[2], -microData.MaxValueZ / 2, microData.ZSpacing) * microData.Measures[0] * microData.Measures[1];

        this.diag = Math.Sqrt(microData.MaxValueX * microData.MaxValueX + microData.MaxValueY * microData.MaxValueY + microData.MaxValueZ * microData.MaxValueZ);
        this.numberOfVertices = microData.Measures[0] * microData.Measures[1] * microData.Measures[2];
        this.innerProductSum = xSquared + ySquared + zSquared;
        this.outerProductSum = Vector<double>.Build.DenseOfArray(new double[] { xSquared, ySquared, zSquared });

        this.centerPoint = Vector<double>.Build.DenseOfArray(new double[]
        {
            microData.MaxValueX / 2.0,
            microData.MaxValueY / 2.0,
            microData.MaxValueZ / 2.0
        });
    }

    private double RowSumOfCoordinatesSquares(int numberOfValues, double minValue, double spacing)
    {
        return minValue * spacing * numberOfValues * (numberOfValues - 1) + numberOfValues * Math.Pow(minValue, 2) + Math.Pow(spacing, 2) / 6 * numberOfValues * (2 * numberOfValues * numberOfValues - 3 * numberOfValues + 1);
    }

    /// <summary>
    /// Calculates the distance between given transformations in O(1)
    /// </summary>
    /// <param name="transformation1">Transformation 1</param>
    /// <param name="transformation2">Transformation 2</param>
    /// <returns>Returns number evaluating the proximity of two given transformations.</returns>
    /// <exception cref="ArgumentException">Throws exception if either of the matrices is not rotation matrix.</exception>
    public double GetTransformationsDistance(Transform3D transformation1, Transform3D transformation2)
    {
        Matrix<double> rotationMatrix1 = transformation1.RotationMatrix;
        Matrix<double> rotationMatrix2 = transformation2.RotationMatrix;

        Vector<double> translationVector1 = transformation1.TranslationVector;
        Vector<double> translationVector2 = transformation2.TranslationVector;

        translationVector1 += rotationMatrix1.Multiply(centerPoint);
        translationVector2 += rotationMatrix2.Multiply(centerPoint);

        double redPart = calculateRedPart();
        double blackPart = calculateBlackPart(translationVector1, translationVector2);
        double pinkPart = calculatePinkPart(rotationMatrix1, rotationMatrix2);

        double result = redPart;
        result += blackPart;
        result += pinkPart;

        return result;
    }

    /// <summary>
    /// Calculates the result of this expression:
    /// 2*Î£(from i = 1 to vq) (qi^T * qi), 
    /// where the qi are the vertices
    /// </summary>
    /// <returns>Returns the result of the expression written above.</returns>
    private double calculateRedPart()
    {
        return 2 * innerProductSum;
    }

    /// <summary>
    /// Calculate the result of this expression:
    /// vq*t1^T*t1 - 2*vq*t1^T*t2 + vq*t2^T*t2, 
    /// where the vq is the number of vertices,
    /// t1 and t2 are the translation vectors
    /// </summary>
    /// <param name="translationVector1">Translation vector from the first transformation</param>
    /// <param name="translationVector2">Translation vector from the second transformation</param>
    /// <returns>Returns the result of the expression written above.</returns>
    private double calculateBlackPart(Vector<double> translationVector1, Vector<double> translationVector2)
    {
        double result = numberOfVertices * translationVector1.DotProduct(translationVector1);
        result -= 2 * numberOfVertices * translationVector1.DotProduct(translationVector2);
        result += numberOfVertices * translationVector2.DotProduct(translationVector2);
        return result;
    }

    /// <summary>
    /// Calculates the result of this expression:
    /// diag(- 2*R1^T*R2) . diag(Î£(from i = 1 to vq)qi*qi^T), 
    /// where . denotes Dot Product
    /// </summary>
    /// <param name="rotationMatrix1">Rotation matrix from the first transformation</param>
    /// <param name="rotationMatrix2">Rotation matrix from the second transformation</param>
    /// <returns>eturns the result of the expression written above</returns>
    private double calculatePinkPart(Matrix<double> rotationMatrix1, Matrix<double> rotationMatrix2)
    {
        Vector<double> leftVector = (-2 * rotationMatrix1.Transpose() * rotationMatrix2).Diagonal();
        return leftVector.DotProduct(outerProductSum);
    }

    public double GetRelativeTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return Math.Sqrt(GetTransformationsDistance(transformation1, transformation2) / numberOfVertices) / diag;
    }
}