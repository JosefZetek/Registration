using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

/// <summary>
/// This class calculates the transformation distance based on the equation marked with number five from article "On evaluating consensus in RANSAC surface registration"
/// Equation: d(T1, T2) = 2*Σ(from i = 1 to vq) (qi^T * qi) + 2*(t1 - t2)^T * R1 * Σ(from i = 1 to vq) qi + 2*(t2 - t1)^T * R2 * Σ(from i = 1 to vq) qi + vq*t1^T*t1 - 2*vq*t1^T*t2 + vq*t2^T*t2 - 2*R1^T*R2 : Σ(from i = 1 to vq)qi*qi^T
/// where ":" denotes frobenius matrix product

/// This class is supposed to output the same result as the first two methods, but it doesn't
/// 2*Σ(from i = 1 to vq) (qi^T * qi) - red part
/// vq*t1^T*t1 - 2*vq*t1^T*t2 + vq*t2^T*t2 - black part
/// diag(- 2*R1^T*R2) . diag(Σ(from i = 1 to vq)qi*qi^T), where . denotes Dot Product
/// </summary>
public class TransformationDistanceSix : ITransformationDistance
{
    private long numberOfVertices;

    double dotProductSum = 0;

    /// <summary>
    /// The result of Σ [x^2, y^2, z^2] for all vertices
    /// </summary>
    Vector<double> sumOfMultipliedVertices = Vector<double>.Build.Dense(3);

    Vector<double> centerPoint;

    /// <summary>
    /// Constructor initializes precomputed values for the given data
    /// </summary>
    /// <param name="microData">Instance of AData on which the result transformation is going to be applied</param>
    public TransformationDistanceSix(AData microData)
    {
        this.numberOfVertices = (long)microData.Measures[0] * (long)microData.Measures[1] * (long)microData.Measures[2];

        centerPoint = Vector<double>.Build.DenseOfArray(new double[]
        {
            microData.MaxValueX / 2.0,
            microData.MaxValueY / 2.0,
            microData.MaxValueZ / 2.0
        });


        for (double x = 0; x <= microData.MaxValueX; x += microData.XSpacing)
        {
            for (double y = 0; y < microData.MaxValueY; y += microData.YSpacing)
            {
                for (double z = 0; z < microData.MaxValueZ; z += microData.ZSpacing)
                {
                    Vector<double> currentVertex = Vector<double>.Build.DenseOfArray(new double[] { x, y, z });
                    currentVertex -= centerPoint;

                    dotProductSum += DotProduct(currentVertex, currentVertex);
                    sumOfMultipliedVertices += MultiplyVectors(currentVertex, currentVertex);
                }
            }
        }
    }

    /// <summary>
    /// This method multiplies the two vectors passed as parameter at corresponding indices.
    /// The results are summed together and returned.
    /// The two vectors need to have the same dimension.
    /// </summary>
    /// <param name="vector1">Vector 1</param>
    /// <param name="vector2">Vector 2</param>
    /// <returns>Returns sum of elements of a vector calculated by multiplying the two passed vectors at corresponding indices.</returns>
    /// <exception cref="ArgumentException"></exception>
    private double DotProduct(Vector<double> vector1, Vector<double> vector2)
    {
        if (vector1.Count != vector2.Count)
            throw new ArgumentException("Vectors need to be the same size");

        double sum = 0;

        for (int i = 0; i < vector1.Count; i++)
            sum += vector1[i] * vector2[i];

        return sum;
    }

    /// <summary>
    /// Multiplies the vectors at corresponding indexes
    /// Vectors need to be the same size.
    /// </summary>
    /// <param name="vector1">First vector</param>
    /// <param name="vector2">Second vector</param>
    /// <returns>This method returns a vector [vector1[0] * vector2[0], vector1[1] * vector2[1], ..., vector1[n] * vector2[n]]</returns>
    /// <exception cref="ArgumentException">Throws an exception when the vectors dont have the same size</exception>
    private Vector<double> MultiplyVectors(Vector<double> vector1, Vector<double> vector2)
    {
        if (vector1.Count != vector2.Count)
            throw new ArgumentException("Vectors need to be the same size");

        Vector<double> resultVector = Vector<double>.Build.Dense(vector1.Count);
        for (int i = 0; i < resultVector.Count; i++)
            resultVector[i] = vector1[i] * vector2[i];

        return resultVector;
    }
    

    /// <summary>
    /// Checks if the matrix by checking if R^T = R^(-1)
    /// </summary>
    /// <param name="matrix"></param>
    /// <returns></returns>
    private bool IsRotationMatrix(Matrix<double> matrix)
    {
        if (matrix.RowCount != matrix.ColumnCount)
            return false;

        Matrix<double> evaluationMatrix = matrix.Transpose() - matrix.Inverse();
        double result = 0;

        for(int i = 0; i<evaluationMatrix.RowCount; i++)
        {
            for(int j = 0; j<evaluationMatrix.ColumnCount; j++)
            {
                result += evaluationMatrix[i, j];
            }
        }

        if (result < 0.00001)
            return true;

        Console.WriteLine("Passed matrix isnt rotation matrix.");
        return false;
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
        TransformationChaining transformationChaining = new TransformationChaining();


        Transform3D transformation1Chained = transformationChaining
            .ChainTranslationVector(centerPoint)
            .ChainRotationMatrix(transformation1.RotationMatrix)
            .ChainTranslationVector(transformation1.TranslationVector)
            .Build();

        transformationChaining.Clear();

        Transform3D transformation2Chained = transformationChaining
            .ChainTranslationVector(centerPoint)
            .ChainRotationMatrix(transformation2.RotationMatrix)
            .ChainTranslationVector(transformation2.TranslationVector)
            .Build();




        //Matrix<double> rotationMatrix1 = transformation1.RotationMatrix;
        //Matrix<double> rotationMatrix2 = transformation2.RotationMatrix;

        //Vector<double> translationVector1 = transformation1.TranslationVector;
        //Vector<double> translationVector2 = transformation2.TranslationVector;

        //translationVector1 += rotationMatrix1.Multiply(centerPoint);
        //translationVector2 += rotationMatrix2.Multiply(centerPoint);

        double redPart = calculateRedPart();
        double blackPart = calculateBlackPart(transformation1Chained.TranslationVector, transformation2Chained.TranslationVector);
        double pinkPart = calculatePinkPart(transformation1Chained.RotationMatrix, transformation2Chained.RotationMatrix);

        double result = redPart;
        result += blackPart;
        result += pinkPart;

        return result;
    }

    public double GetSqrtTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return Math.Sqrt(GetTransformationsDistance(transformation1, transformation2));
    }

    /// <summary>
    /// Calculates the result of this expression:
    /// diag(- 2*R1^T*R2) . diag(Σ(from i = 1 to vq)qi*qi^T), 
    /// where . denotes Dot Product
    /// </summary>
    /// <param name="rotationMatrix1">Rotation matrix from the first transformation</param>
    /// <param name="rotationMatrix2">Rotation matrix from the second transformation</param>
    /// <returns>eturns the result of the expression written above</returns>
    private double calculatePinkPart(Matrix<double> rotationMatrix1, Matrix<double> rotationMatrix2)
    {
        Vector<double> leftVector = (-2 * rotationMatrix1.Transpose() * rotationMatrix2).Diagonal();
        Vector<double> rightVector = sumOfMultipliedVertices;

        return DotProduct(leftVector, rightVector);
    }


    /// <summary>
    /// Calculates the result of this expression:
    /// 2*Σ(from i = 1 to vq) (qi^T * qi), 
    /// where the qi are the vertices
    /// </summary>
    /// <returns>Returns the result of the expression written above.</returns>
    private double  calculateRedPart()
    {
        return 2*dotProductSum;
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
        double result = (numberOfVertices * DotProduct(translationVector1, translationVector1));
        result += (-2 * numberOfVertices * DotProduct(translationVector1, translationVector2));
        result += (numberOfVertices * DotProduct(translationVector2, translationVector2));
        return result;
    }

    public double GetRelativeTransformationDistance(Transform3D transformation1, Transform3D transformation2)
    {
        return GetTransformationsDistance(transformation1, transformation2) / numberOfVertices;
    }
}