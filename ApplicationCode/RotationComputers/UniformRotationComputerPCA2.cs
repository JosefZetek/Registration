using System;
using System.Collections.Generic;
using System.Linq;
using DataView;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Samplers;

namespace Registration.ApplicationCode.RotationComputers;

public class UniformRotationComputerPCA : ATransformer
{
    protected override Matrix<double>[] GetRotationMatrices(AData dataMicro, AData dataMacro, Point3D pointMicro, Point3D pointMacro)
    {
        Matrix<double>[] basisMicro = GetPointBasis(dataMicro, pointMicro);
        Matrix<double>[] basisMacro = GetPointBasis(dataMacro, pointMacro);

        if (basisMicro == null || basisMacro == null)
            return null;

        /* Macro basis is alwats the same, one of the two micro basis is the right one */
        return new Matrix<double>[] {
            CalculateTransitionMatrix(basisMicro[0], basisMacro[0]),
            CalculateTransitionMatrix(basisMicro[1], basisMacro[0]),
        };
    }

    // od pana Váši
    private Matrix<double>[] GetPointBasis(AData d, Point3D point)
    {
        Matrix<double> alternativeBasisMatrix = Matrix<double>.Build.Dense(3, 3);

        List<Point3D> points = UniformSphereSampler.GetDistributedPoints(d, point);
        List<double> values = CalculateValues(points, d);

        if (values == null)
            return null;

        /* Threshold to filter insignificant  values */
        QuickSelectClass quickSelectClass = new QuickSelectClass();
        double threshold = quickSelectClass.QuickSelect(values, values.Count / 2);
        FilterPoints(ref points, ref values, threshold);

        Vector<double> meanVector = CalculateMeanVector(points);

        Matrix<double> covarianceMatrix = CalculateCovarianceMatrix(points, meanVector);

        Matrix<double> basisMatrix = GetEigenVectors(covarianceMatrix.Evd());

        Vector<double> gradient = GradientCalculator.GetFunctionGradient(point, d);

        int fixedColumn = AdjustColumnBasedOnGradient(basisMatrix, gradient);
        int unstableColumnIndex = fixedColumn == 1 ? 0 : 1;
        int crossProductColumn = fixedColumn == 2 ? 0 : 2;

        basisMatrix.CopyTo(alternativeBasisMatrix);


        //Set unstable columns
        basisMatrix.SetColumn(
            unstableColumnIndex,
            -basisMatrix.Column(unstableColumnIndex)
        );

        alternativeBasisMatrix.SetColumn(
            unstableColumnIndex,
            alternativeBasisMatrix.Column(unstableColumnIndex)
        );

        //Set cross product
        basisMatrix.SetColumn(
            crossProductColumn,
            CrossProduct(basisMatrix.Column(fixedColumn), basisMatrix.Column(unstableColumnIndex))
        );

        alternativeBasisMatrix.SetColumn(
            crossProductColumn,
            CrossProduct(alternativeBasisMatrix.Column(fixedColumn), alternativeBasisMatrix.Column(unstableColumnIndex))
        );

        return new Matrix<double>[] { basisMatrix, alternativeBasisMatrix };
    }

    private int AdjustColumnBasedOnGradient(Matrix<double> basisMatrix, Vector<double> gradient)
    {
        double greatestDotProduct = double.NegativeInfinity, currentDotProduct = 0;
        Vector<double> column;

        int columnIndex = 0;

        for (int i = 0; i < basisMatrix.ColumnCount; i++)
        {
            column = basisMatrix.Column(i);
            currentDotProduct = column.DotProduct(gradient);

            if(greatestDotProduct < Math.Abs(currentDotProduct))
            {
                greatestDotProduct = currentDotProduct;
                columnIndex = i;
            }
        }

        basisMatrix.SetColumn(
            columnIndex,
            basisMatrix.Column(columnIndex) * Math.Sign(currentDotProduct)
        );

        return columnIndex;
    }

    private Matrix<double> GetEigenVectors(Evd<double> evd)
    {
        Vector<double> eigenValues = evd.EigenValues.Real();
        Matrix<double> eigenVectors = evd.EigenVectors;

        // Get indices sorted by eigenvalues in descending order
        int[] sortedIndices = eigenValues.Enumerate()
                                         .Select((value, index) => (value, index))
                                         .OrderByDescending(pair => Math.Abs(pair.value))
                                         .Select(pair => pair.index)
                                         .ToArray();

        Matrix<double> sortedEigenVectors = Matrix<double>.Build.Dense(eigenVectors.RowCount, eigenVectors.ColumnCount);

        for (int i = 0; i < sortedIndices.Length; i++)
            sortedEigenVectors.SetColumn(i, eigenVectors.Column(sortedIndices[i]));

        return sortedEigenVectors;
    }

    private List<double> CalculateValues(List<Point3D> points, AData d)
    {
        List<double> values = new List<double>();
        double min = double.MaxValue, max = double.MinValue;

        for (int i = 0; i < points.Count; i++)
        {
            values.Add(d.GetValue(points[i]));
            min = Math.Min(min, values[values.Count - 1]);
            max = Math.Max(max, values[values.Count - 1]);
        }


        if (Math.Abs(min - max) < Double.Epsilon)
            return null;// basis cannot be calculated because all sampled values in the point surrounding are the same

        return values;
    }

    private void FilterPoints(ref List<Point3D> points, ref List<double> values, double threshold)
    {

        List<Point3D> filteredPoints = new List<Point3D>();
        List<double> filteredValues = new List<double>();

        for (int i = 0; i < points.Count; i++)
        {
            if (values[i] >= threshold)
            {
                filteredValues.Add(values[i]);
                filteredPoints.Add(points[i]);
            }
        }

        points = filteredPoints;
        values = filteredValues;
    }

    private Matrix<double> CalculateCovarianceMatrix(List<Point3D> pointsInSphere, Vector<double> meanVector)
    {
        int N = pointsInSphere.Count;
        Matrix<double> A = Matrix<double>.Build.Dense(N, 3); // Each row is a point (N x 3)

        for (int i = 0; i < N; i++)
        {
            A.SetRow(i, new double[] {
                pointsInSphere[i].X - meanVector[0],
                pointsInSphere[i].Y - meanVector[1],
                pointsInSphere[i].Z - meanVector[2]
            });
        }

        return (A.Transpose() * A) / (N - 1); // (3xN) * (Nx3) = 3x3 covariance matrix
    }

    private Vector<double> CalculateMeanVector(List<Point3D> pointsInSphere)
    {
        Vector<double> meanVector = Vector<double>.Build.Dense(3);

        for (int i = 0; i < pointsInSphere.Count; i++)
        {
            meanVector[0] += pointsInSphere[i].X;
            meanVector[1] += pointsInSphere[i].Y;
            meanVector[2] += pointsInSphere[i].Z;
        }

        meanVector /= pointsInSphere.Count;
        return meanVector;
    }

    private Vector<double> CrossProduct(Vector<double> firstVector, Vector<double> secondVector)
    {
        return Vector<double>.Build.DenseOfArray(new double[] {
            firstVector[1] * secondVector[2] - firstVector[2] * secondVector[1],
            firstVector[2] * secondVector[0] - firstVector[0] * secondVector[2],
            firstVector[0] * secondVector[1] - firstVector[1] * secondVector[0]
        });
    }

    private Matrix<double> CalculateTransitionMatrix(Matrix<double> basisMicro, Matrix<double> basisMacro)
    {
        return basisMacro * basisMicro.Transpose();
    }


}