using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.TransformationDistanceMetrics;

namespace Registration.ApplicationCode.Other;

public class Transform3D : IComparable<Transform3D>
{
    private static ITransformationDistance transformationDistance;

    private Matrix<double> rotationMatrix;
    private Vector<double> translationVector;

    /// <summary>
    /// Set transformation distance for this object
    /// </summary>
    /// <param name="transformationDistance"></param>
    public static void SetTransformationDistance(ITransformationDistance transformationDistance)
    {
        Transform3D.transformationDistance = transformationDistance;
    }

    public Transform3D(Matrix<double> rotationMatrix, Vector<double> translationVector)
    {
        RotationMatrix = rotationMatrix;
        TranslationVector = translationVector;
    }

    /// <summary>
    /// Creates identity transformation
    /// </summary>
    public Transform3D()
    {
        RotationMatrix = Matrix<double>.Build.DenseIdentity(3);
        TranslationVector = Vector<double>.Build.Dense(3);
    }

    public Matrix<double> RotationMatrix { get => rotationMatrix; set => rotationMatrix = value; }
    public Vector<double> TranslationVector { get => translationVector; set => translationVector = value; }

    public override string ToString()
    {
        return RotationMatrix.ToString() + TranslationVector.ToString();
    }

    public double DistanceTo(Transform3D anotherTransformation)
    {
        if (transformationDistance == null)
            throw new Exception("Transformation distance needs to bet set before calling this method.");

        return transformationDistance.GetTransformationsDistance(this, anotherTransformation);
    }

    public double SqrtDistanceTo(Transform3D anotherTransformation)
    {
        return Math.Sqrt(Math.Abs(DistanceTo(anotherTransformation)));
    }

    public double RelativeDistanceTo(Transform3D anotherTransformation)
    {
        return transformationDistance.GetRelativeTransformationDistance(this, anotherTransformation);
    }

    public int CompareTo(Transform3D other)
    {
        return Math.Sign(DistanceTo(other));
    }

    /// <summary>
    /// This method chains this transformation T1 with the given transformation T2
    /// </summary>
    /// <param name="chainedTransformation">Secondly applied transformation T2</param>
    /// <returns>Transformation with same effect as T2(T1(image)).</returns>
    public Transform3D ChainWithTransformation(Transform3D chainedTransformation)
    {
        //Derived from T2 x T1, where T1 and T2 are matrices composed of rotation matrix and translation vector
        return new Transform3D(
            chainedTransformation.RotationMatrix * this.RotationMatrix,
            chainedTransformation.RotationMatrix.Multiply(this.TranslationVector) + chainedTransformation.TranslationVector
        );
    }

    /// <summary>
    /// Method calculates the inverse transformation
    /// </summary>
    /// <returns>Returns the inverse of this object's transformation</returns>
    public Transform3D GetInverseTransformation()
    {
        return new Transform3D(this.rotationMatrix.Transpose(), -this.rotationMatrix.Transpose().Multiply(this.translationVector));
    }
}
