using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;
using Registration.ApplicationCode.Samplers;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerPCALength : AFeatureComputer
{
    public override int NumberOfFeatures => 3;

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        List<Point3D> points = UniformSphereSampler.GetDistributedPoints(d, p);
        List<double>? values = CalculateValues(points, d);

        if (values == null)
        {
            /* NaN marks the vector as invalid; such vectors are dropped before
               normalization so a single flat neighborhood cannot poison the statistics. */
            for (int i = 0; i < NumberOfFeatures; i++)
                array[startIndex + i] = double.NaN;

            return;
        }

        /* Threshold to filter insignificant  values */
        QuickSelectClass quickSelectClass = new QuickSelectClass();
        double threshold = quickSelectClass.QuickSelect(values, values.Count / 2);
        FilterPoints(ref points, ref values, threshold);

        Vector<double> meanVector = CalculateWeightedMeanVector(points);
        Matrix<double> covarianceMatrix = CalculateCovarianceMatrix(points, meanVector);

        Vector<double> eigenValues = covarianceMatrix.Evd().EigenValues.Real();
        eigenValues = eigenValues.Map(x => double.IsNaN(x) || double.IsInfinity(x) ? 0 : x);
        eigenValues /= eigenValues.L2Norm();
        eigenValues = eigenValues.Map(x => double.IsNaN(x) || double.IsInfinity(x) ? 0 : x);

        array[startIndex] = Math.Abs(eigenValues[0]);
        array[startIndex + 1] = Math.Abs(eigenValues[1]);
        array[startIndex + 2] = Math.Abs(eigenValues[2]);

        Array.Sort(array, startIndex, NumberOfFeatures);
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0;
    }

    public override FCConfiguration GetConfiguration()
    {
        var config = new FCConfiguration();
        config.FeatureComputerType = FCType.PCA_FEATURE_COMPUTER;
        config.AddParameter("Comment", "PCA");
        return config;
    }

    private List<double>? CalculateValues(List<Point3D> points, AData d)
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
        {
            Console.WriteLine("Basis cannot be calculated because all sampled values in the point surrounding are the same.");
            return null;
        }

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

    private Vector<double> CalculateWeightedMeanVector(List<Point3D> pointsInSphere)
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
}