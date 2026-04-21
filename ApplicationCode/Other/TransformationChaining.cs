using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System;

namespace Registration.ApplicationCode.Other;

public class TransformationChaining
{
	private Stack<Matrix<double>> transformationStack;

	public TransformationChaining()
	{
		transformationStack = new Stack<Matrix<double>>();
	}

	public TransformationChaining ChainTranslationVector(Vector<double> translationVector)
	{
		if (translationVector.Count != 3)
			throw new ArgumentException("Translation vector must be of size 3");

		transformationStack.Push(UnifyTranslationVector(translationVector));
		return this;
	}

	public TransformationChaining ChainRotationMatrix(Matrix<double> rotationMatrix)
	{
        if (rotationMatrix.RowCount != 3 || rotationMatrix.ColumnCount != 3)
            throw new ArgumentException("Rotation matrix must be of size 3x3");

        transformationStack.Push(UnifyRotationMatrix(rotationMatrix));
		return this;
    }

    private Matrix<double> UnifyRotationMatrix(Matrix<double> rotationMatrix)
    {
        Matrix<double> unifiedMatrix = Matrix<double>.Build.DenseIdentity(4);
		unifiedMatrix.SetSubMatrix(0, 0, rotationMatrix);
		return unifiedMatrix;
    }

    private Matrix<double> UnifyTranslationVector(Vector<double> translationVector)
	{
		Matrix<double> unifiedMatrix = Matrix<double>.Build.DenseIdentity(4);
		unifiedMatrix[0, 3] = translationVector[0];
        unifiedMatrix[1, 3] = translationVector[1];
        unifiedMatrix[2, 3] = translationVector[2];
		return unifiedMatrix;
	}

    /// <summary>
    /// Chains all transformations
    /// </summary>
    /// <returns>
	/// Outputs Transform3D instance that should be applied in order
    /// 1) Rotation
    /// 2) Translation
	/// </returns>
    /// <exception cref="ArgumentException">Throws error when there are no transformations to chain</exception>
    public Transform3D Build()
	{
		if (transformationStack.Count == 0)
			throw new ArgumentException("No transformations to chain");

		Matrix<double> unifiedMatrix = Matrix<double>.Build.DenseIdentity(4);

		while (transformationStack.Count != 0)
			unifiedMatrix *= transformationStack.Pop();

		Matrix<double> rotationMatrix = unifiedMatrix.SubMatrix(0, 3, 0, 3);
		Vector<double> translationVector = Vector<double>.Build.DenseOfArray(new double[]
		{
			unifiedMatrix[0, 3],
            unifiedMatrix[1, 3],
            unifiedMatrix[2, 3]
        });

        return new Transform3D(rotationMatrix, translationVector);
	}

	/// <summary>
	/// Clears chained transformations
	/// </summary>
	public void Clear()
	{
		this.transformationStack.Clear();
	}

}

